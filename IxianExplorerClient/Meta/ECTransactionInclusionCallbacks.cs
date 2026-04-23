using IXICore;
using IXICore.Meta;
using System;
using System.Linq;

namespace IxianExplorerClient.Meta
{
    internal class ECTransactionInclusionCallbacks : TransactionInclusionCallbacks
    {
        public void transactionVerified(Transaction tx)
        {
        }

        public void transactionRejected(Transaction tx)
        {
        }

        public void transactionExpired(Transaction tx)
        {
        }

        public void transactionCannotVerify(Transaction tx)
        {
        }

        public void receivedBlockHeader(Block blockHeader, bool verified)
        {
            foreach (Balance balance in IxianHandler.balances.Values)
            {
                if (balance.blockChecksum != null && balance.blockChecksum.SequenceEqual(blockHeader.blockChecksum))
                {
                    balance.verified = true;
                }
            }

            /*if (blockHeader.blockNum + 10 >= IxianHandler.getHighestKnownNetworkBlockHeight()
                && (IxianHandler.status == NodeStatus.warmUp || IxianHandler.status == NodeStatus.stalled))
            {*/
            IxianHandler.status = NodeStatus.ready;
            //}
        }

        public void blockReorg(Block blockHeader)
        {
        }
    }
}
