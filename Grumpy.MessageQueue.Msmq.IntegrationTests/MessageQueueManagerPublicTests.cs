using FluentAssertions;
using Grumpy.Common;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.IntegrationTests
{
    public class MessageQueueManagerPublicTests
    {
        private readonly IMessageQueueManager _messageQueueManager = new MessageQueueManager();

        [Fact(Skip = "Only when on Active Directory Network")]
        public void CreatePublicQueueShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, false, true).Should().NotBeNull();
                _messageQueueManager.Exists(name, false).Should().BeFalse();
            }
            finally
            {
                _messageQueueManager.Delete(name, false);
            }
        }
    }
}