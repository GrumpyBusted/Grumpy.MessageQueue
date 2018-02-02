using Grumpy.Common;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.IntegrationTests
{
    public class RemoteQueueTests
    {
        private readonly IMessageQueueManager _messageQueueManager = new MessageQueueManager();
        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory = new MessageQueueTransactionFactory();

        [Fact]
        public void SendToNoneExistingRemoteQueueShouldThrowException()
        {
            using (var queue = new RemoteQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, "Test", $"IntegrationTest_{UniqueKeyUtility.Generate()}", false, RemoteQueueMode.Durable, true, AccessMode.Send))
            {
                Assert.Throws<QueueMissingException>(() => queue.Send("Hallo"));
            }
        }
    }
}