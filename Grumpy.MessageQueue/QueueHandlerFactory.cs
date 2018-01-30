using Grumpy.Common.Interfaces;
using Grumpy.Common.Threading;
using Grumpy.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grumpy.MessageQueue
{
    /// <inheritdoc />
    public class QueueHandlerFactory : IQueueHandlerFactory
    {
        private readonly ITaskFactory _taskFactory;
        private readonly ILogger _logger;
        private readonly IQueueFactory _queueFactory;

        /// <inheritdoc />
        public QueueHandlerFactory(ILogger logger, IQueueFactory queueFactory)
        {
            _taskFactory = new TaskFactory();
            _logger = logger;
            _queueFactory = queueFactory;
        }

        /// <inheritdoc />
        public IQueueHandler Create()
        {
            return new QueueHandler(_logger, _queueFactory, _taskFactory);
        }
    }
}