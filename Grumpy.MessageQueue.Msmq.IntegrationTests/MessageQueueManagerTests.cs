using System;
using System.Linq;
using System.Messaging;
using FluentAssertions;
using Grumpy.Common;
using Grumpy.Common.Threading;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.IntegrationTests
{
    public class MessageQueueManagerTests
    {
        private readonly IMessageQueueManager _messageQueueManager = new MessageQueueManager(NullLogger.Instance);

        [Fact]
        public void CreatePrivateQueueShouldCreateFirstTime()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, true, true).Should().NotBeNull();
                Assert.Throws<QueueCreateException>(() => _messageQueueManager.Create(name, true, true).Should().BeNull());
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void DeleteLocaleQueueShouldDeleteQueue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, true, true);
                _messageQueueManager.Delete(name, true);
                TimerUtility.WaitForIt(() => !_messageQueueManager.Exists(name, true), 10000);
                _messageQueueManager.Exists(name, true).Should().BeFalse();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void ExistShouldBeFalseIfNotExists()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Exists(name, true).Should().BeFalse();
                _messageQueueManager.Create(name, true, true).Should().NotBeNull();
                _messageQueueManager.Exists(name, true).Should().BeTrue();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void GetShouldBeNullIfNotExists()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                GetQueue(name).Should().BeNull();
                _messageQueueManager.Create(name, true, true).Should().NotBeNull();
                GetQueue(name).Should().NotBeNull();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void ListQueueNamesShouldReturnList()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, true, true);
                _messageQueueManager.List(".", true).Should().Contain(n => n == name.ToLower());
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void SendAndReceiveMessageToFromQueueShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                var queue = _messageQueueManager.Create(name, true, true);
                var message = ReceiveMessage(queue);
                message.Should().BeNull();
                SendMessage(queue, "");
                message = ReceiveMessage(queue);
                message.Should().NotBeNull();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void ListQueue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, true, true);
                _messageQueueManager.List(".", true).Count().Should().BeGreaterOrEqualTo(1);
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        private System.Messaging.MessageQueue GetQueue(string name)
        {
            return _messageQueueManager.Get(".", name, true, QueueAccessMode.SendAndReceive);
        }

        [Fact]
        public void ReceiveMessageFromQueueShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                var message = ReceiveMessage(null);
                message.Should().BeNull();

                var queue = _messageQueueManager.Create(name, true, true);
                message = ReceiveMessage(queue);
                message.Should().BeNull();

                SendMessage(queue, "");
                message = ReceiveMessage(queue);
                message.Should().NotBeNull();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void ReceiveByCorrelationIdFromQueueShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                var queue = _messageQueueManager.Create(name, true, true);

                var id = SendMessage(queue, "Message1");
                SendMessage(queue, "Message2", id);


                var message = ReceiveMessageByCorrelationId(queue, "x");
                message.Should().BeNull();

                message = ReceiveMessageByCorrelationId(queue, id);
                message.Should().NotBeNull();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void PeekAsyncMessageFromQueueShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                var queue = _messageQueueManager.Create(name, true, true);

                var asyncResult = _messageQueueManager.BeginPeek(queue, TimeSpan.FromMilliseconds(100));

                var message = _messageQueueManager.EndPeek(queue, asyncResult);

                message.Should().BeNull("Because queue is empty");

                var id = SendMessage(queue, "Message");

                asyncResult = _messageQueueManager.BeginPeek(queue, TimeSpan.FromMilliseconds(100));
                message = _messageQueueManager.EndPeek(queue, asyncResult);

                message.Should().NotBeNull();
                message.Id.Should().Be(id);
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        private string SendMessage(System.Messaging.MessageQueue queue, object body, string correlationId = null)
        {
            var messageQueueTransaction = new System.Messaging.MessageQueueTransaction();

            messageQueueTransaction.Begin();

            var message = new Message { Body = body };

            if (correlationId != null)
                message.CorrelationId = correlationId;

            _messageQueueManager.Send(queue, message, messageQueueTransaction);

            messageQueueTransaction.Commit();

            return message.Id;
        }

        private Message ReceiveMessage(System.Messaging.MessageQueue queue)
        {
            var messageQueueTransaction = new System.Messaging.MessageQueueTransaction();

            messageQueueTransaction.Begin();

            var message = _messageQueueManager.Receive(queue, TimeSpan.FromMilliseconds(10), messageQueueTransaction);

            messageQueueTransaction.Commit();

            return message;
        }

        private Message ReceiveMessageByCorrelationId(System.Messaging.MessageQueue queue, string correlationId)
        {
            var messageQueueTransaction = new System.Messaging.MessageQueueTransaction();

            messageQueueTransaction.Begin();

            var message = _messageQueueManager.ReceiveByCorrelationId(queue, correlationId, TimeSpan.FromMilliseconds(0), messageQueueTransaction);

            messageQueueTransaction.Commit();

            return message;
        }
    }
}