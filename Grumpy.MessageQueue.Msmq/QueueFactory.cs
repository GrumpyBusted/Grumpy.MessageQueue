using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public class QueueFactory : IQueueFactory
    {
        private readonly ILogger _logger;
        private readonly IMessageQueueManager _messageQueueManager;
        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory;

        /// <inheritdoc />
        public QueueFactory(ILogger logger)
        {
            _logger = logger;
            _messageQueueManager = new MessageQueueManager(logger);
            _messageQueueTransactionFactory = new MessageQueueTransactionFactory();
        }

        /// <inheritdoc />
        public ILocaleQueue CreateLocale(string name, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, AccessMode accessMode)
        {
            return new LocaleQueue(_logger, _messageQueueManager, _messageQueueTransactionFactory, name, privateQueue, localeQueueMode, transactional, accessMode);
        }

        /// <inheritdoc />
        public IRemoteQueue CreateRemote(string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional, AccessMode accessMode)
        {
            return new RemoteQueue(_logger, _messageQueueManager, _messageQueueTransactionFactory, serverName, name, privateQueue, remoteQueueMode, transactional, accessMode);
        }
    }
}