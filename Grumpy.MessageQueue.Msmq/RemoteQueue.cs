using System.Messaging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Interfaces;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc cref="IRemoteQueue" />
    public class RemoteQueue : Queue, IRemoteQueue
    {
        private bool _disposed;

        /// <inheritdoc />
        public string ServerName { get; }

        /// <inheritdoc />
        public RemoteQueue(IMessageQueueManager messageQueueManager, string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional) : base(messageQueueManager, name, privateQueue, remoteQueueMode == RemoteQueueMode.Durable, transactional)
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