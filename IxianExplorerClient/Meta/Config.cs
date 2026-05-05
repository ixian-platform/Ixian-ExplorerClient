using Fclp;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Streaming;
using IXICore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IxianExplorerClient.Meta
{
    public class Config
    {
        // Read-only values
        public static readonly string version = "xexc-0.9.8d";

        public static readonly string checkVersionUrl = "https://resources.ixian.io/xexc-update.txt";
        public static readonly int checkVersionSeconds = 6 * 60 * 60; // 6 hours

        public static readonly int maximumStreamClients = 1000; // Maximum number of stream clients this server can accept

        public static int maxRelaySectorNodesToConnectTo = 3;
        public static int maxConnectedStreamingNodes = 6;

        // Options
        public static NetworkType networkType = NetworkType.main;
        public static byte[] checksumLock = null;

        public static int apiPort = 8001;
        public static int serverPort = 0;

        public static string dataFolder = Path.Combine(Environment.CurrentDirectory, "data");

        public static string activityFolderPath = "";
        public static string headersFolderPath = "";
        public static string logFolderPath = "";

        public static Dictionary<string, string> apiUsers = new Dictionary<string, string>();

        public static List<string> apiAllowedIps = new List<string>();
        public static List<string> apiBinds = new List<string>();

        public static string configFilename = "ixian.cfg";
        public static string walletFile = "";

        public static int maxLogSize = 50;
        public static int maxLogCount = 10;

        public static int logVerbosity = (int)LogSeverity.info + (int)LogSeverity.warn + (int)LogSeverity.error;
        public static bool verboseOutput = false;

        public static bool onlyShowAddresses = false;

        public static string friendlyName = "Explorer Client";

        // Store the device id in a cache for reuse in later instances
        public static string externalIp = "";

        // Development/Testing options
        public static string dangerCommandlinePasswordCleartextUnsafe = "";

        // internal
        public static bool changePass = false;

        // expose own external IP:Port to PL
        public static bool exposePublicIP = false;

        public static StreamCapabilities streamCapabilities = StreamCapabilities.Incoming | StreamCapabilities.AppProtocols;

        public static ulong activityDbCacheSize = 8 << 20;
        public static ulong blocksDbCacheSize = 8 << 20;

        public static TIVBlockVerificationMode blockVerificationMode = TIVBlockVerificationMode.Minimal;
        public static bool disableBlockPruning = false;

        public static long minRequiredDiskSpace = 50 << 20; // 50MB

        public static int explorerAPITransactionPaginationLimit = 100;

        public static string explorerAPIBaseUrl = "https://explorer.ixian.io/api/v1";
        public static string explorerAPIKey = ""; // Set the API KEY here or supply it via commandline or config file
        public static bool enableActivityScanner = true;

        private Config()
        {

        }

        private static void outputHelp()
        {
            Console.WriteLine("Starts a new instance of IxianExplorerClient");
            Console.WriteLine("");
            Console.WriteLine(" IxianExplorerClient.exe [-h] [-v] [-a 8081] [-i ip] [-w ixian.wal]");
            Console.WriteLine("   [--config ixian.cfg] [--maxLogSize 50] [--maxLogCount 10] [--logVerbosity 14]");
            Console.WriteLine("   [--apiUrl https://explorer.ixian.io/api/v1] [--apiKey YOUR_API_KEY] [--disableActivity]");
            Console.WriteLine("");
            Console.WriteLine("    -h\t\t\t\t Displays this help");
            Console.WriteLine("    -v\t\t\t\t Displays version");
            Console.WriteLine("    -t\t\t\t\t Starts node in testnet mode");
            Console.WriteLine("    -x\t\t\t\t Change password of an existing wallet");
            Console.WriteLine("    -c\t\t\t\t Removes cache, peers.dat and ixian.log files before starting");
            Console.WriteLine("    -a\t\t\t\t HTTP/API port to listen on");
            Console.WriteLine("    -i\t\t\t\t External IP Address to use");
            Console.WriteLine("    -w\t\t\t\t Specify location of the ixian.wal file");
            Console.WriteLine("    -n\t\t\t\t Specify which seed node to use");
            Console.WriteLine("    --config\t\t\t Specify config filename (default ixian.cfg)");
            Console.WriteLine("    --maxLogSize\t\t Specify maximum log file size in MB");
            Console.WriteLine("    --maxLogCount\t\t Specify maximum number of log files");
            Console.WriteLine("    --logVerbosity\t\t Sets log verbosity (0 = none, trace = 1, info = 2, warn = 4, error = 8)");
            Console.WriteLine("    --checksumLock\t\t Sets the checksum lock for seeding checksums - useful for custom networks.");
            Console.WriteLine("    --verboseOutput\t\t Starts node with verbose output.");
            Console.WriteLine("    --networkType\t\t Network type - mainnet, testnet or regtest.");
            Console.WriteLine("    --logFolderPath\t\t Location where to store log files.");
            Console.WriteLine("    --headersFolderPath\t\t Location where to store block header data.");
            Console.WriteLine("    --activityFolderPath\t Location where to store activity files.");
            Console.WriteLine("    --dataFolderPath\t\t Root location where to store data.");
            Console.WriteLine("    --blocksDbCache\t\t Max RAM in bytes to use for RocksDB Blocks Cache.");
            Console.WriteLine("    --activityDbCache\t\t Max RAM in bytes to use for Activity Cache.");
            Console.WriteLine("    --disableBlockPruning\t\t Disable block pruning");
            Console.WriteLine("    --apiUrl\t\t Specify the explorer API URL (default https://explorer.ixian.io/api/v1)");
            Console.WriteLine("    --apiKey\t\t Specify the explorer API key");
            Console.WriteLine("    --disableActivity\t\t Disable the activity scanner");
            Console.WriteLine("");
            Console.WriteLine("----------- Developer CLI flags -----------");
            Console.WriteLine("    --walletPassword\t\t Specify the password for the wallet.");
            Console.WriteLine("");
            Console.WriteLine("----------- Config File Options -----------");
            Console.WriteLine(" Config file options should use parameterName = parameterValue semantics.");
            Console.WriteLine(" Each option should be specified in its own line. Example:");
            Console.WriteLine("    apiPort = 8001");
            Console.WriteLine("");
            Console.WriteLine(" Available options:");
            Console.WriteLine("    apiPort\t\t\t HTTP/API port to listen on (same as -a CLI)");
            Console.WriteLine("    apiAllowIp\t\t\t Allow API connections from specified source or sources (can be used multiple times)");
            Console.WriteLine("    apiBind\t\t\t Bind to given address to listen for API connections (can be used multiple times)");
            Console.WriteLine("    addApiUser\t\t\t Adds user:password that can access the API (can be used multiple times)");

            Console.WriteLine("    externalIp\t\t\t External IP Address to use (same as -i CLI)");
            Console.WriteLine("    addPeer\t\t\t Specify which seed node to use (same as -n CLI) (can be used multiple times)");
            Console.WriteLine("    addTestnetPeer\t\t Specify which seed node to use in testnet mode (same as -n CLI) (can be used multiple times)");
            Console.WriteLine("    maxLogSize\t\t\t Specify maximum log file size in MB (same as --maxLogSize CLI)");
            Console.WriteLine("    maxLogCount\t\t\t Specify maximum number of log files (same as --maxLogCount CLI)");
            Console.WriteLine("    logVerbosity\t\t Sets log verbosity (same as --logVerbosity CLI)");
            Console.WriteLine("    logFolderPath\t\t Location where to store log files.");
            Console.WriteLine("    headersFolderPath\t\t Location where to store block header data.");
            Console.WriteLine("    activityFolderPath\t\t Location where to store activity files.");
            Console.WriteLine("    dataFolderPath\t\t Root location where to store data.");
            Console.WriteLine("    checksumLock\t\t Sets the checksum lock for seeding checksums - useful for custom networks.");
            Console.WriteLine("    networkType\t\t\t Network type - mainnet, testnet or regtest.");
            Console.WriteLine("    blocksDbCache\t\t Max RAM in bytes to use for RocksDB Blocks Cache.");
            Console.WriteLine("    activityDbCache\t\t Max RAM in bytes to use for Activity Cache.");
            Console.WriteLine("    wallet\t\t\t Specify location of the ixian.wal file");
            Console.WriteLine("    walletPassword\t\t Specify the password for the walllet.");
            Console.WriteLine("    p2pTxMode\t\t\t P2P Transaction Mode (primaryAddress or custom)");
            Console.WriteLine("    blockVerificationMode\t Block verification mode - minimal, pocw, signatures, transactions");
            Console.WriteLine("    disableBlockPruning\t\t Disable block pruning");
            Console.WriteLine("    disableActivity\t\t\t Disable the activity scanner (same as --maxLogCount CLI)");
            Console.WriteLine("    apiUrl\t\t\t Specify the explorer API URL (default https://explorer.ixian.io/api/v1)");
            Console.WriteLine("    apiKey\t\t\t Specify the explorer API key");
            Console.WriteLine("");

            Environment.Exit(0);
        }

        private static void outputVersion()
        {
            // Do nothing but exit since version is the first thing displayed
            Environment.Exit(0);
        }

        private static NetworkType parseNetworkTypeValue(string value)
        {
            NetworkType netType;
            value = value.ToLower();
            switch (value)
            {
                case "mainnet":
                    netType = NetworkType.main;
                    break;
                case "testnet":
                    netType = NetworkType.test;
                    break;
                case "regtest":
                    netType = NetworkType.reg;
                    break;
                default:
                    throw new Exception(string.Format("Unknown network type '{0}'. Possible values are 'mainnet', 'testnet', 'regtest'", value));
            }
            return netType;
        }

        private static P2PTransactionMode parseP2PTxModeValue(string value)
        {
            P2PTransactionMode txMode;
            value = value.ToLower();
            switch (value)
            {
                case "primaryAddress":
                    txMode = P2PTransactionMode.PrimaryAddress;
                    break;
                case "custom":
                    txMode = P2PTransactionMode.Custom;
                    break;
                default:
                    throw new Exception(string.Format("Unknown P2P Transaction Mode '{0}'. Possible values are 'primaryAddress', 'custom'", value));
            }
            return txMode;
        }

        private static TIVBlockVerificationMode parseBlockVerificationMode(string value)
        {
            TIVBlockVerificationMode verificationMode;
            value = value.ToLower();
            switch (value)
            {
                case "minimal":
                    verificationMode = TIVBlockVerificationMode.Minimal;
                    break;
                case "pocw":
                    verificationMode = TIVBlockVerificationMode.PoCW;
                    break;
                case "signatures":
                    verificationMode = TIVBlockVerificationMode.Signatures;
                    break;
                case "transactions":
                    verificationMode = TIVBlockVerificationMode.Transactions;
                    break;
                default:
                    throw new Exception(string.Format("Unknown Block Verification Mode '{0}'. Possible values are 'minimal', 'pocw', 'signatures', 'transactions'", value));
            }
            return verificationMode;
        }

        private static void readConfigFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            Logging.info("Reading config file: " + filename);
            bool foundAddPeer = false;
            bool foundAddTestPeer = false;
            List<string> lines = File.ReadAllLines(filename).ToList();
            foreach (string line in lines)
            {
                string[] option = line.Split('=');
                if (option.Length < 2)
                {
                    continue;
                }
                string key = option[0].Trim([' ', '\t', '\r', '\n']);
                string value = option[1].Trim([' ', '\t', '\r', '\n']);

                if (key.StartsWith(";"))
                {
                    continue;
                }
                if (key == "walletPassword")
                {
                    Logging.info("Processing config parameter '" + key + "' = '****************'");
                }
                else
                {
                    Logging.info("Processing config parameter '" + key + "' = '" + value + "'");
                }
                switch (key)
                {
                    case "serverPort":
                        serverPort = int.Parse(value);
                        break;
                    case "apiPort":
                        apiPort = int.Parse(value);
                        break;
                    case "apiAllowIp":
                        apiAllowedIps.Add(value);
                        break;
                    case "apiBind":
                        apiBinds.Add(value);
                        break;
                    case "addApiUser":
                        string[] credential = value.Split(':');
                        if (credential.Length == 2)
                        {
                            apiUsers.Add(credential[0], credential[1]);
                        }
                        break;
                    case "externalIp":
                        externalIp = value;
                        break;
                    case "addPeer":
                        if (!foundAddPeer)
                        {
                            NetworkUtils.seedNodes.Clear();
                        }
                        foundAddPeer = true;
                        NetworkUtils.seedNodes.Add([value, null]);
                        break;
                    case "addTestnetPeer":
                        if (!foundAddTestPeer)
                        {
                            NetworkUtils.seedTestNetNodes.Clear();
                        }
                        foundAddTestPeer = true;
                        NetworkUtils.seedTestNetNodes.Add([value, null]);
                        break;
                    case "maxLogSize":
                        maxLogSize = int.Parse(value);
                        break;
                    case "maxLogCount":
                        maxLogCount = int.Parse(value);
                        break;
                    case "logVerbosity":
                        logVerbosity = int.Parse(value);
                        break;
                    case "checksumLock":
                        checksumLock = Encoding.UTF8.GetBytes(value);
                        break;
                    case "networkType":
                        networkType = parseNetworkTypeValue(value);
                        break;
                    case "streamCapabilities":
                        streamCapabilities = (StreamCapabilities)int.Parse(value);
                        break;
                    case "logFolderPath":
                        logFolderPath = value;
                        break;
                    case "activityFolderPath":
                        activityFolderPath = value;
                        break;
                    case "headersFolderPath":
                        headersFolderPath = value;
                        break;
                    case "dataFolderPath":
                        dataFolder = value;
                        break;
                    case "wallet":
                        walletFile = value;
                        break;
                    case "walletPassword":
                        dangerCommandlinePasswordCleartextUnsafe = value;
                        break;
                    case "blocksDbCache":
                        blocksDbCacheSize = ulong.Parse(value);
                        break;
                    case "activityDbCache":
                        activityDbCacheSize = ulong.Parse(value);
                        break;
                    case "p2pTxMode":
                        CoreConfig.p2pTransactionMode = parseP2PTxModeValue(value);
                        break;
                    case "blockVerificationMode":
                        blockVerificationMode = parseBlockVerificationMode(value);
                        break;
                    case "disableBlockPruning":
                        disableBlockPruning = value.ToLower() == "true" || value == "1";
                        break;
                    case "disableActivity":
                        if (int.Parse(value) != 0)
                        {
                            enableActivityScanner = false;
                        }
                        break;
                    case "apiUrl":
                        explorerAPIBaseUrl = value;
                        break;
                    case "apiKey":
                        explorerAPIKey = value;
                        break;
                    default:
                        // unknown key
                        Logging.warn("Unknown config parameter was specified '" + key + "'");
                        break;
                }
            }
        }

        public static void init(string[] args)
        {
            // first pass
            var cmd_parser = new FluentCommandLineParser();

            // help
            cmd_parser.SetupHelp("h", "help").Callback(text => outputHelp());

            // version
            cmd_parser.Setup<bool>('v', "version").Callback(text => outputVersion());

            // config file
            cmd_parser.Setup<string>("config").Callback(value => configFilename = value).Required();

            cmd_parser.Parse(args);

            readConfigFile(configFilename);



            // second pass
            cmd_parser = new FluentCommandLineParser();

            // testnet
            cmd_parser.Setup<bool>('t', "testnet").Callback(value => networkType = NetworkType.test).Required();
            cmd_parser.Setup<string>("networkType").Callback(value => networkType = parseNetworkTypeValue(value)).Required();

            cmd_parser.Parse(args);


            string seedNode = "";

            // third pass
            cmd_parser = new FluentCommandLineParser();

            bool start_clean = false; // Flag to determine if node should delete cache+logs

            // Check for password change
            cmd_parser.Setup<bool>('x', "changepass").Callback(value => changePass = value).Required();

            // Check for clean parameter
            cmd_parser.Setup<bool>('c', "clean").Callback(value => start_clean = value).Required();

            cmd_parser.Setup<int>('a', "apiport").Callback(value => apiPort = value).Required();

            cmd_parser.Setup<string>('i', "ip").Callback(value => externalIp = value).Required();

            cmd_parser.Setup<string>('w', "wallet").Callback(value => walletFile = value).Required();

            cmd_parser.Setup<string>('n', "node").Callback(value => seedNode = value).Required();

            cmd_parser.Setup<int>("maxLogSize").Callback(value => maxLogSize = value).Required();

            cmd_parser.Setup<int>("maxLogCount").Callback(value => maxLogCount = value).Required();

            cmd_parser.Setup<bool>("onlyShowAddresses").Callback(value => onlyShowAddresses = true).Required();

            cmd_parser.Setup<string>("checksumLock").Callback(value => checksumLock = Encoding.UTF8.GetBytes(value)).Required();

            cmd_parser.Setup<string>("activityFolderPath").Callback(value => activityFolderPath = value).Required();

            cmd_parser.Setup<string>("headersFolderPath").Callback(value => headersFolderPath = value).Required();

            cmd_parser.Setup<string>("logFolderPath").Callback(value => logFolderPath = value).Required();

            cmd_parser.Setup<string>("dataFolderPath").Callback(value => dataFolder = value).Required();

            cmd_parser.Setup<long>("blocksDbCache").Callback(value => blocksDbCacheSize = (ulong)value);

            cmd_parser.Setup<long>("activityDbCache").Callback(value => activityDbCacheSize = (ulong)value);

            cmd_parser.Setup<string>("p2pTxMode").Callback(value => CoreConfig.p2pTransactionMode = parseP2PTxModeValue(value)).Required();

            cmd_parser.Setup<string>("blockVerificationMode").Callback(value => blockVerificationMode = parseBlockVerificationMode(value)).Required();

            cmd_parser.Setup<bool>("disableBlockPruning").Callback(value => disableBlockPruning = value).Required();

            // Debug

            cmd_parser.Setup<string>("walletPassword").Callback(value => dangerCommandlinePasswordCleartextUnsafe = value).SetDefault("");

            cmd_parser.Setup<int>("logVerbosity").Callback(value => logVerbosity = value).Required();

            cmd_parser.Setup<bool>("verboseOutput").Callback(value => verboseOutput = value).SetDefault(false);

            cmd_parser.Setup<bool>("disableActivity").Callback(value => Config.enableActivityScanner = false).Required();

            cmd_parser.Setup<string>("apiUrl").Callback(value => explorerAPIBaseUrl = value).Required();
            cmd_parser.Setup<string>("apiKey").Callback(value => explorerAPIKey = value).Required();

            cmd_parser.Parse(args);


            if (explorerAPIKey == "")
            {
                Console.WriteLine("ERROR: Ixian Explorer API KEY is missing!\nAdd it using the --apiKey commandline parameter");
                Environment.Exit(0);
            }

            // Validate parameters

            if (start_clean)
            {
                Node.cleanCacheAndLogs();
            }

            if (seedNode != "")
            {
                switch (networkType)
                {
                    case NetworkType.main:
                        NetworkUtils.seedNodes = new List<string[]>
                            {
                                new string[2] { seedNode, null }
                            };
                        break;

                    case NetworkType.test:
                    case NetworkType.reg:
                        NetworkUtils.seedTestNetNodes = new List<string[]>
                            {
                                new string[2] { seedNode, null }
                            };
                        break;
                }
            }

            if (headersFolderPath == "")
            {
                if (networkType == NetworkType.main)
                {
                    headersFolderPath = Path.Combine(dataFolder, "headers");
                }
                else
                {
                    headersFolderPath = Path.Combine(dataFolder, "testnet-headers");
                }
            }

            if (activityFolderPath == "")
            {
                if (networkType == NetworkType.main)
                {
                    activityFolderPath = Path.Combine(dataFolder, "activity");
                }
                else
                {
                    activityFolderPath = Path.Combine(dataFolder, "testnet-activity");
                }
            }

            if (logFolderPath == "")
            {
                logFolderPath = dataFolder;
            }

            if (walletFile == "")
            {
                walletFile = Path.Combine(dataFolder, "ixian.wal");
            }
        }
    }
}