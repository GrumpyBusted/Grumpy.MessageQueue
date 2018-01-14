using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Interfaces;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public class QueueFactory : IQueueFactory
    {
        private readonly IMessageQueueManager _messageQueueManager;

        /// <inheritdoc />
        public QueueFactory()
        {
            _messageQueueManager = new MessageQueueManager();
        }

        /// <inheritdoc />
        public ILocaleQueue CreateLocale(string name, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional)
        {
            return new LocaleQueue(_messageQueueManager, name, privateQueue, localeQueueMode, transactional);
        }

        /// <inheritdoc />
        public IRemoteQueue CreateRemote(string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional)
        {
            return new RemoteQueue(_messageQueueManager, serverName, name, privateQueue, remoteQueueMode, transactional);
        }
    }
}