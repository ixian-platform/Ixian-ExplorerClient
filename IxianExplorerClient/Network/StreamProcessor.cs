using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Streaming;
using System;

namespace IxianExplorerClient.Network
{
    class StreamProcessor : CoreStreamProcessor
    {
        public StreamProcessor(PendingMessageProcessor pendingMessageProcessor, StreamCapabilities streamCapabilites) : base(pendingMessageProcessor, streamCapabilites)
        {
        }

        // Called when receiving S2 data from clients
        public override ReceiveDataResponse? receiveData(byte[] bytes, RemoteEndpoint endpoint, bool fireLocalNotification = true, bool alert = true)
        {
            ReceiveDataResponse? rdr = base.receiveData(bytes, endpoint, fireLocalNotification);
            if (rdr == null)
            {
                return rdr;
            }

            StreamMessage message = rdr.streamMessage;
            SpixiMessage spixi_message = rdr.spixiMessage;
            Friend? friend = rdr.friend;
            Address sender_address = rdr.senderAddress;
            Address? group_sender_address = rdr.groupSenderAddress;

            if (friend != null)
            {
                if (endpoint != null)
                {
                    // Update friend's last seen and relay if outgoing stream capabilities are disabled
                    if ((streamCapabilities & StreamCapabilities.Outgoing) == 0)
                    {
                        friend.updatedStreamingNodes = Clock.getNetworkTimestamp();
                        friend.relayNode = new Peer(endpoint.getFullAddress(true), endpoint.serverWalletAddress, Clock.getTimestamp(), Clock.getTimestamp(), Clock.getTimestamp(), 0);
                        friend.online = true;
                    }
                }
            }
            
            try
            {
                switch (spixi_message.type)
                {
                    case SpixiMessageCode.chat:
                        if (friend != null && !friend.bot)
                        {
                            sendReceivedConfirmation(friend, message.id, spixi_message.channel);
                        }
                        break;

                    case SpixiMessageCode.chatStream:
                        if (friend != null && !friend.bot)
                        {
                            sendReceivedConfirmation(friend, message.id, spixi_message.channel);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in StreamProcessor.receiveData: " + e);
            }
            return rdr;
        }
    }
}
