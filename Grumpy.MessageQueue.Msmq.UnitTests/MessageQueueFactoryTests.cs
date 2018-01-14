using FluentAssertions;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.UnitTests
{
    public class MessageQueueFactoryTests
    {
        [Fact]
        public void MessageQueueFactoryCanCreateLocaleInstance()
        {
            using (var queue = CreateQueueFactory().CreateLocale("MyQueue", false, LocaleQueueMode.DurableCreate, true))
            {
                queue.Should().NotBeNull();
                queue.GetType().Should().Be(typeof(LocaleQueue));
            }
        }

        [Fact]
        public void MessageQueueFactoryCanCreateRemoteInstance()
        {
            var queue = CreateQueueFactory().CreateRemote("MyServer", "MyQueue", false, RemoteQueueMode.Durable, true);

            queue.Should().NotBeNull();
            queue.GetType().Should().Be(typeof(RemoteQueue));
        }

        private static IQueueFactory CreateQueueFactory()
        {
            return new QueueFactory();
        }
    }
}