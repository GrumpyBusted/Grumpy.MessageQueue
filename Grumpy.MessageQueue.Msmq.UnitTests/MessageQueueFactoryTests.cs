using FluentAssertions;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.UnitTests
{
    public class MessageQueueFactoryTests
    {
        private static readonly ILogger Logger = NullLogger.Instance;

        [Fact]
        public void MessageQueueFactoryCanCreateLocaleInstance()
        {
            using (var queue = CreateQueueFactory().CreateLocale("MyQueue", true, LocaleQueueMode.DurableCreate, true, AccessMode.Receive))
            {
                queue.Should().NotBeNull();
                queue.GetType().Should().Be(typeof(LocaleQueue));
            }
        }

        [Fact]
        public void MessageQueueFactoryCanCreateRemoteInstance()
        {
            var queue = CreateQueueFactory().CreateRemote("MyServer", "MyQueue", false, RemoteQueueMode.Durable, true, AccessMode.Receive);

            queue.Should().NotBeNull();
            queue.GetType().Should().Be(typeof(RemoteQueue));
        }

        private static IQueueFactory CreateQueueFactory()
        {
            return new QueueFactory(Logger);
        }
    }
}