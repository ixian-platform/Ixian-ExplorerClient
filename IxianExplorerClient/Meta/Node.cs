using IXICore;
using IXICore.Inventory;
using IXICore.Meta;
using IXICore.Network;
using IXICore.RegNames;
using IXICore.Storage;
using IXICore.Streaming;
using IXICore.Utils;
using IXICore.Activity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IxianExplorerClient.API;
using IxianExplorerClient.Network;

namespace IxianExplorerClient.Meta
{
    class Node : IxianNode
    {
        public static TransactionInclusion tiv = null;

        public static StreamProcessor streamProcessor = null;

        public static NetworkClientManagerStatic networkClientManagerStatic = null;

        public static IActivityStorage activityStorage = null;

        public static IStorage storage = null;

        public static ulong transactionsAdded = 0;

        // Private data
        private StatsConsoleScreen statsConsoleScreen;

        private GenericAPIServer? apiServer = null;

        private Thread? mainLoopThread = null;

        private bool running = false;

        public Node()
        {
            Logging.info("Initing node constructor");

            init();
        }

        private bool init()
        {
            CoreConfig.defaultPaymentAddressMode = AddressPaymentFlag.Primary;
            IxianHandler.init(Config.version, this, NetworkType.main, true);

            // Load or Generate the wallet
            if (!initWallet())
            {
                IxianHandler.requestShutdown();
                return false;
            }

            // Initialize storage
            storage = new RocksDBStorage(Config.headersFolderPath, Config.blocksDbCacheSize, CoreConfig.maxBlockHeadersPerDatabase, 3, RocksDBOptimizations.Mobiles, Config.minRequiredDiskSpace);

            activityStorage = new ActivityStorage(Config.activityFolderPath, Config.activityDbCacheSize, 0, RocksDBOptimizations.Mobiles, Config.minRequiredDiskSpace);

            PeerStorage.init(Config.dataFolder);

            // Network configuration
            networkClientManagerStatic = new NetworkClientManagerStatic(Config.maxRelaySectorNodesToConnectTo);
            NetworkClientManager.init(networkClientManagerStatic);
            StreamClientManager.init(Config.maxConnectedStreamingNodes, true);

            // Prepare the stream processor
            streamProcessor = new StreamProcessor(new ICPendingMessageProcessor(Config.dataFolder, false), Config.streamCapabilities);

            // Init TIV
            tiv = new TransactionInclusion(storage, new ECTransactionInclusionCallbacks(), Config.blockVerificationMode);

            Logging.info("Initing local storage");

            // Prepare the local storage
            IxianHandler.localStorage = new LocalStorage(Config.dataFolder, new ICLocalStorageCallbacks());

            FriendList.init(Config.dataFolder, true);

            UpdateVerify.init(Config.checkVersionUrl, Config.checkVersionSeconds);

            // TODO Maybe enable push notifications at some point

            InventoryCache.init(new InventoryCacheClient(tiv));

            RelaySectors.init(CoreConfig.relaySectorLevels, null);

            apiServer = new APIServer();

            // Setup the stats console
            statsConsoleScreen = new StatsConsoleScreen();

            Logging.info("Node init done");

            return true;
        }

        public bool start(bool verboseConsoleOutput)
        {
            if (running)
            {
                Logging.warn("Cannot start Node, it is already running.");
                return false;
            }
            Logging.info("Starting node");

            running = true;
            IxianHandler.status = NodeStatus.warmUp;

            // Start local storage
            IxianHandler.localStorage.start();
            if (IxianHandler.localStorage.nickname == "")
            {
                IxianHandler.localStorage.nickname = Config.friendlyName;
            }

            FriendList.loadContacts();

            UpdateVerify.start();

            if (!storage.prepareStorage(false))
            {
                Logging.error("Error while preparing block storage! Aborting.");
                return false;
            }

            activityStorage.prepareStorage(false);

            var pending_txs = activityStorage.getActivitiesByStatus(ActivityStatus.Pending, true);
            pending_txs.AddRange(activityStorage.getActivitiesByStatus(ActivityStatus.Reverted, true));
            // Load pending transactions
            foreach (var pending_tx in pending_txs)
            {
                if (pending_tx.type == ActivityType.TransactionReceived)
                {
                    PendingTransactions.addIncomingTransaction(pending_tx.transaction);
                }
                else if (pending_tx.type == ActivityType.TransactionSent
                        || pending_tx.type == ActivityType.IxiName)
                {
                    PendingTransactions.addOutgoingTransaction(pending_tx.transaction, pending_tx.transaction.toList.TakeLast(2).Select(x => x.Key).ToList());
                }
            }

            ulong block_height = 0;
            byte[]? block_checksum = null;
            if (IxianHandler.networkType == NetworkType.main)
            {
                block_height = CoreConfig.bakedBlockHeight;
                block_checksum = CoreConfig.bakedBlockChecksum;
            }

            // Start TIV
            tiv.start(block_height, block_checksum, !Config.disableBlockPruning);

            // Generate presence list
            PresenceList.init(IxianHandler.publicIP, 0, 'C', CoreConfig.clientKeepAliveInterval);

            // Start the network queue
            NetworkQueue.start();

            streamProcessor.start();

            // Start the keepalive thread
            PresenceList.startKeepAlive();

            // Start activity scanner
            ActivityScanner.start();

            mainLoopThread = new Thread(mainLoop);
            mainLoopThread.Name = "Main_Loop_Thread";
            mainLoopThread.Start();

            if (Config.apiBinds.Count == 0)
            {
                Config.apiBinds.Add("http://localhost:" + Config.apiPort + "/");
            }

            apiServer.start(Config.apiBinds, Config.apiUsers, Config.apiAllowedIps, activityStorage);

            Logging.info("Node started");

            // Prepare stats screen
            ConsoleHelpers.verboseConsoleOutput = verboseConsoleOutput;
            Logging.consoleOutput = verboseConsoleOutput;
            Logging.flush();
            if (ConsoleHelpers.verboseConsoleOutput == false)
            {
                statsConsoleScreen.clearScreen();
            }

            connectToNetwork();

            return true;
        }

        static public void connectToNetwork()
        {
            // Start the s2 client manager
            StreamClientManager.start();

            // Start the network client manager
            NetworkClientManager.start(2);
        }

        // Handle timer routines
        public void mainLoop()
        {
            try
            {
                while (running)
                {
                    try
                    {
                        PeerStorage.savePeersFile();
                        // Update the friendlist
                        updateFriendStatuses();

                        // Cleanup the presence list
                        // TODO: optimize this by using a different thread perhaps
                        PresenceList.performCleanup();

                        CoreProtocolMessage.fetchSectorNodes(IxianHandler.primaryWalletAddress, CoreConfig.maxRelaySectorNodesToRequest);
                        
                        updateAllBalances();
                    }
                    catch (Exception e)
                    {
                        Logging.error("Exception occured in mainLoop: " + e);
                    }
                    Thread.Sleep(120000);
                }
            }
            catch (ThreadInterruptedException)
            {

            }
        }

        static public void updateFriendStatuses()
        {
            lock (FriendList.friends)
            {
                // Go through each friend and check for the pubkey in the PL
                foreach (Friend friend in FriendList.friends)
                {
                    Presence? presence = null;

                    try
                    {
                        presence = PresenceList.getPresenceByAddress(friend.walletAddress);
                    }
                    catch (Exception e)
                    {
                        Logging.error("Presence Error {0}", e.Message);
                        presence = null;
                    }

                    if (presence != null)
                    {
                        if (friend.online == false
                            && friend.relayNode != null)
                        {
                            friend.online = true;
                        }
                    }
                    else
                    {
                        if (friend.online == true
                            && Clock.getNetworkTimestamp() - friend.updatedStreamingNodes > CoreConfig.requestPresenceTimeout)
                        {
                            friend.online = false;
                        }
                    }
                }
            }
        }

        private void stop()
        {
            if (!running)
            {
                return;
            }

            Logging.info("Stopping node...");
            running = false;

            // First stop localStorage, to flush any pending chat messages to storage
            // The Node is currently in shutting down state, so no incoming messages will be processed by the message processors
            IxianHandler.localStorage.stop();

            // Stop the stream processor, it includes pending messages
            streamProcessor.stop();

            // Stop activity scanner
            ActivityScanner.stop();

            // Stop everything else storage related
            activityStorage.stopStorage();

            PeerStorage.savePeersFile(true);

            // Stop the block storage
            storage.stopStorage();

            // Stop everything else

            // Stop TIV
            tiv.stop();

            // Stop the keepalive thread
            PresenceList.stopKeepAlive();

            // Stop the API server
            if (apiServer != null)
            {
                apiServer.stop();
                apiServer = null;
            }

            // Stop everything network related
            NetworkQueue.stop();
            NetworkClientManager.stop();
            StreamClientManager.stop();

            UpdateVerify.stop();

            if (mainLoopThread != null)
            {
                mainLoopThread.Interrupt();
                mainLoopThread.Join();
                mainLoopThread = null;
            }

            Logging.info("Node stopped");

            statsConsoleScreen.stop();

            // Stop logging
            Logging.stop();
        }

        public override bool isAcceptingConnections()
        {
            return false;
        }


        public override void shutdown()
        {
            stop();
        }

        public override ulong getLastBlockHeight()
        {
            Block? block = tiv.getLastBlockHeader();
            if (block == null)
            {
                return 0;
            }
            return block.blockNum;
        }

        public override int getLastBlockVersion()
        {
            Block? block = tiv.getLastBlockHeader();
            if (block == null
                || block.version < Block.maxVersion)
            {
                // TODO Omega force to v10 after upgrade
                return Block.maxVersion - 1;
            }
            return block.version;
        }

        public override bool addIncomingTransaction(Transaction tx)
        {
            return false;
        }

        public override bool addTransaction(Transaction tx, List<Address> relayNodeAddresses, List<ExtendedAddress>? extendedAddresses, byte[]? requestId, bool force_broadcast)
        {
            if (tx.timeStamp == 0)
            {
                tx.timeStamp = Clock.getTimestamp();
            }
            if (activityStorage.getActivityById(tx.id) == null)
            {
                if (PendingTransactions.addOutgoingTransaction(tx, relayNodeAddresses))
                {
                    foreach (var address in relayNodeAddresses)
                    {
                        NetworkClientManager.sendToClient(address, ProtocolMessageCode.transactionData2, tx.getBytes(true, true));
                    }
                    if (extendedAddresses != null)
                    {
                        CoreStreamProcessor.transactionSend(tx, extendedAddresses, requestId);
                    }
                    transactionsAdded++;
                    return true;
                }
            }
            return false;
        }

        public override Block? getLastBlock()
        {
            return tiv.getLastBlockHeader();
        }

        public override void parseProtocolMessage(ProtocolMessageCode code, byte[] data, RemoteEndpoint endpoint)
        {
            ProtocolMessage.parseProtocolMessage(code, data, endpoint);
        }

        public override Block? getBlockHeader(ulong blockNum)
        {
            return storage.getBlock(blockNum);
        }

        public override IxiNumber getMinSignerPowDifficulty(ulong blockNum, int curBlockVersion, long curBlockTimestamp)
        {
            return tiv.getMinSignerPowDifficulty(blockNum, curBlockVersion, curBlockTimestamp);
        }

        public override RegisteredNameRecord getRegName(byte[] name, bool useAbsoluteId = true)
        {
            throw new NotImplementedException();
        }

        public override byte[]? getBlockHash(ulong blockNum)
        {
            var tsd = storage.getBlockTotalSignerDifficulty(blockNum);
            return tsd.blockHash;
        }

        public static FriendMessage? addMessageWithType(byte[] id, FriendMessageType type, Address wallet_address, int channel, string message, bool local_sender = false, Address? sender_address = null, long timestamp = 0, bool fire_local_notification = true, int payable_data_len = 0)
        {
            FriendMessage? friend_message = FriendList.addMessageWithType(id, type, wallet_address, channel, message, local_sender, sender_address, timestamp, fire_local_notification, payable_data_len);
            if (friend_message != null)
            {
                bool oldMessage = false;

                Friend friend = FriendList.getFriend(wallet_address);

                if (!friend.online)
                {
                    StreamProcessor.fetchFriendsPresence(friend, true);
                }

                // Check if the message was sent before the friend was added to the contact list
                if (friend.addedTimestamp > friend_message.timestamp)
                {
                    oldMessage = true;
                }

                if (!friend_message.read)
                {
                    // Increase the unread counter if this is a new message
                    if (!oldMessage)
                        friend.metaData.unreadMessageCount++;

                    friend.saveMetaData();
                }
            }
            return friend_message;
        }

        // Cleans the storage cache and logs
        public static bool cleanCacheAndLogs()
        {
            if (activityStorage is null)
            {
                activityStorage = new ActivityStorage(Config.activityFolderPath, Config.activityDbCacheSize, 0, RocksDBOptimizations.Mobiles, Config.minRequiredDiskSpace);
            }
            activityStorage.stopStorage();
            activityStorage.deleteData();
            activityStorage.prepareStorage(false);

            if (storage is null)
            {
                storage = new RocksDBStorage(Config.headersFolderPath, Config.blocksDbCacheSize, CoreConfig.maxBlockHeadersPerDatabase, 3, RocksDBOptimizations.Mobiles, Config.minRequiredDiskSpace);
            }
            storage.stopStorage();
            storage.deleteData();
            storage.prepareStorage(false);

            PeerStorage.deletePeersFile();

            Logging.clear();

            Logging.info("Cleaned cache and logs.");
            return true;
        }

        private bool initWallet()
        {
            WalletStorage walletStorage = new WalletStorage(Config.walletFile);

            Logging.flush();

            if (!walletStorage.walletExists())
            {
                ConsoleHelpers.displayBackupText();

                // Request a password
                // NOTE: This can only be done in testnet to enable automatic testing!
                string password = "";
                if (Config.dangerCommandlinePasswordCleartextUnsafe != "")
                {
                    Logging.warn("TestNet detected and wallet password has been specified on the command line!");
                    password = Config.dangerCommandlinePasswordCleartextUnsafe;
                    // Also note that the commandline password still has to be >= 10 characters
                }
                while (password.Length < 10)
                {
                    Logging.flush();
                    password = ConsoleHelpers.requestNewPassword("Enter a password for your new wallet: ");
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                }
                walletStorage.generateWallet(password);
            }
            else
            {
                ConsoleHelpers.displayBackupText();

                bool success = false;
                while (!success)
                {

                    // NOTE: This is only permitted on the testnet for dev/testing purposes!
                    string password = "";
                    if (Config.dangerCommandlinePasswordCleartextUnsafe != "")
                    {
                        Logging.warn("Attempting to unlock the wallet with a password from commandline!");
                        password = Config.dangerCommandlinePasswordCleartextUnsafe;
                    }
                    if (password.Length < 10)
                    {
                        Logging.flush();
                        Console.Write("Enter wallet password: ");
                        password = ConsoleHelpers.getPasswordInput();
                    }
                    if (password == "" || IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                    if (walletStorage.readWallet(password))
                    {
                        success = true;
                    }
                }
            }


            if (walletStorage.getPrimaryPublicKey() == null)
            {
                return false;
            }

            // Wait for any pending log messages to be written
            Logging.flush();

            Console.WriteLine();
            Console.WriteLine("Your IXIAN addresses are: ");
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var entry in walletStorage.getMyAddressesBase58())
            {
                Console.WriteLine(entry);
            }
            Console.ResetColor();
            Console.WriteLine();

            if (Config.onlyShowAddresses)
            {
                return false;
            }

            // Check if we should change the password of the wallet
            if (Config.changePass == true)
            {
                // Request a new password
                string new_password = "";
                while (new_password.Length < 10)
                {
                    new_password = ConsoleHelpers.requestNewPassword("Enter a new password for your wallet: ");
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                }
                walletStorage.writeWallet(new_password);
                return false;
            }

            Logging.info("Public Node Address: {0}", walletStorage.getPrimaryAddress().ToString());


            if (walletStorage.viewingWallet)
            {
                Logging.error("Viewing-only wallet {0} cannot be used as the primary DLT Node wallet.", walletStorage.getPrimaryAddress().ToString());
                return false;
            }

            IxianHandler.addWallet(walletStorage);

            // Prepare the balances list
            List<Address> address_list = IxianHandler.getWalletStorage().getMyAddresses();
            foreach (Address addr in address_list)
            {
                IxianHandler.balances.Add(addr, new Balance(addr, 0));
            }

            return true;
        }


        public static void updateAllBalances()
        {
            Logging.info("Updating all balances...");
            foreach (Balance balance in IxianHandler.balances.Values)
            {
                balance.balance = APIClient.getAmountByAddressAsync(balance.address.ToString());
            }
            Logging.info("All balances updated");
        }

        public static void updateBalance(string address)
        {
            foreach (Balance balance in IxianHandler.balances.Values)
            {
                if (address.Equals(balance.address.ToString()))
                {
                    balance.balance = APIClient.getAmountByAddressAsync(address);
                    return;
                }
            }
        }
    }
}