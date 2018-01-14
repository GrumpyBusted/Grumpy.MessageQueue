using System;
using System.Messaging;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Dto;
using Newtonsoft.Json;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TransactionalMessage : ITransactionalMessage
    {
        private readonly QueueMessage _queueMessage;
        private readonly MessageQueueTransaction _messageQueueTransaction;
        private bool _disposed;

        /// <inheritdoc />
        public TransactionalMessage()
        {
            _queueMessage = null;
            _messageQueueTransaction = null;
        }

        /// <inheritdoc />
        public TransactionalMessage(QueueMessage queueMessage, MessageQueueTransaction messageQueueTransaction)
        {
            _queueMessage = queueMessage;
            _messageQueueTransaction = messageQueueTransaction;
        }

        /// <inheritdoc />
        public string Body => _queueMessage?.MessageBody;

        /// <inheritdoc />
        public Type Type => _queueMessage?.MessageType;

        /// <inheritdoc />
        public object Message => Type == null ? null : JsonConvert.DeserializeObject(Body, Type);

        /// <inheritdoc />
        public void Ack()
        {
            if (_messageQueueTransaction?.Status == MessageQueueTransactionStatus.Pending)
                _messageQueueTransaction.Commit();
        }

        /// <inheritdoc />
        public void NAck()
        {
            if (_messageQueueTransaction?.Status == MessageQueueTransactionStatus.Pending)
                _messageQueueTransaction.Abort();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose locale objects
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _messageQueueTransaction?.Dispose();

                _disposed = true;
            }
        }
    }
}