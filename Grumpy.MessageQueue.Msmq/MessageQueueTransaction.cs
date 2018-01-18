using System.Messaging;
using Grumpy.MessageQueue.Msmq.Interfaces;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public class MessageQueueTransaction : IMessageQueueTransaction
    {
        private bool _disposed;

        /// <inheritdoc />
        public System.Messaging.MessageQueueTransaction Transaction { get; }

        /// <inheritdoc />
        public MessageQueueTransactionStatus Status => Transaction.Status;

        /// <inheritdoc />
        public MessageQueueTransaction()
        {
            Transaction = new System.Messaging.MessageQueueTransaction();
        }

        /// <inheritdoc />
        public void Begin()
        {
            Transaction.Begin();
        }

        /// <inheritdoc />
        public void Commit()
        {
            Transaction.Commit();
        }

        /// <inheritdoc />
        public void Abort()
        {
            Transaction.Abort();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                    Transaction.Dispose();
            }
        }
    }
}