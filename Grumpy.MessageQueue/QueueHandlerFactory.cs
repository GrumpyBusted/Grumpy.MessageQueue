using Grumpy.Common.Interfaces;
using Grumpy.Common.Threading;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.MessageQueue
{
    /// <inheritdoc />
    public class QueueHandlerFactory : IQueueHandlerFactory
    {
        private readonly ITaskFactory _taskFactory;
        private readonly IQueueFactory _queueFactory;

        /// <inheritdoc />
        public QueueHandlerFactory(IQueueFactory queueFactory)
        {
            _taskFactory = new TaskFactory();
            _queueFactory = queueFactory;
        }

        /// <inheritdoc />
        public IQueueHandler Create()
        {
            return new QueueHandler(_queueFactory, _taskFactory);
        }
    }
}