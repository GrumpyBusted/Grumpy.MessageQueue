using Grumpy.MessageQueue.Msmq.Interfaces;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public class MessageQueueTransactionFactory : IMessageQueueTransactionFactory
    {
        /// <inheritdoc />
        public IMessageQueueTransaction Create()
        {
            return new MessageQueueTransaction();
        }
    }
}