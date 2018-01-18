using System.Messaging;
using FluentAssertions;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Interfaces;
using NSubstitute;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.UnitTests
{
    public class RemoteQueueTests
    {
        private readonly IMessageQueueManager _messageQueueManager = Substitute.For<IMessageQueueManager>();
        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory = Substitute.For<IMessageQueueTransactionFactory>();

        [Fact]
        public void SendToExistingQueueShouldGetQueueFromServer()
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(Substitute.For<System.Messaging.MessageQueue>());

            var cut = CreateRemoteQueue(RemoteQueueMode.Durable);
            cut.Send("Message");

            cut.ServerName.Should().Be("MyServerName");
            _messageQueueManager.Received(0).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
            _messageQueueManager.Received(1).Get("MyServerName", Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>());
        }

        [Fact]
        public void SendToExistingTempQueueShouldGetQueueFromServer()
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(Substitute.For<System.Messaging.MessageQueue>());

            var cut = CreateRemoteQueue(RemoteQueueMode.Temporary);
            cut.Send("Message");

            cut.ServerName.Should().Be("MyServerName");
            _messageQueueManager.Received(0).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
            _messageQueueManager.Received(1).Get("MyServerName", Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>());
        }

        private IRemoteQueue CreateRemoteQueue(RemoteQueueMode remoteQueueMode)
        {
            return new RemoteQueue(_messageQueueManager, _messageQueueTransactionFactory, "MyServerName", "MyQueue", false, remoteQueueMode, true);
        }
    }
}