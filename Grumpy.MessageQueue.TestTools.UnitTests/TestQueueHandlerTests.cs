using System;
using System.Threading;
using FluentAssertions;
using Grumpy.MessageQueue.Enum;
using Xunit;

namespace Grumpy.MessageQueue.TestTools.UnitTests
{
    public class TestQueueHandlerTests
    {
        private string _messages = "";

        [Fact]
        public void TestQueueHandlerShouldWork()
        {
            var q = new TestQueueHandlerFactory();
            var w = q.Create();

            q.Messages.Add("Message1");
            q.Messages.Add("Message2");
            q.Messages.Add("Exception");

            var numberOfHeartbeats = 0;
            var numberOfError = 0;

            w.Start("MyQueue", true, LocaleQueueMode.DurableCreate, true, Handler, (m, e) => ++numberOfError, () => ++numberOfHeartbeats, 1, true, true, new CancellationToken());

            _messages.Should().Be("Message1;Message2;");
            numberOfError.Should().Be(1);
            numberOfHeartbeats.Should().BeGreaterOrEqualTo(1);
        }

        private void Handler(object message, CancellationToken cancellationToken)
        {
            if ((string)message == "Exception")
                throw new Exception((string)message);

            _messages += (string) message + ";";
        }
    }
}
