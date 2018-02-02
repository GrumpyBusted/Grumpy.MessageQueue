using System.Messaging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc cref="IRemoteQueue" />
    public class RemoteQueue : Queue, IRemoteQueue
    {
        private bool _disposed;

        /// <inheritdoc />
        public string ServerName { get; }

        /// <inheritdoc />
        public RemoteQueue(ILogger logger, IMessageQueueManager messageQueueManager, IMessageQueueTransactionFactory messageQueueTransactionFactory, string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional, AccessMode accessMode) : base(logger, messageQueueManager, messageQueueTransactionFactory, name, privateQueue, remoteQueueMode == RemoteQueueMode.Durable, transactional, accessMode)
        {
            ServerName = serverName;
        }

        /// <inheritdoc />
        protected override System.Messaging.MessageQueue GetQueue(AccessMode accessMode)
        {
            return MessageQueueManager.Get(ServerName, Name, Private, accessMode == AccessMode.Receive ? QueueAccessMode.Receive : accessMode == AccessMode.Send ? QueueAccessMode.Send : QueueAccessMode.SendAndReceive);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                base.Dispose(disposing);

                _disposed = true;
            }
        }
    }
}