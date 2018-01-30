using System;
using System.IO;
using System.Messaging;
using System.Text;
using System.Threading;
using FluentAssertions;
using Grumpy.Json;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Dto;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Grumpy.MessageQueue.Msmq.UnitTests.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.UnitTests
{
    public class LocaleQueueTests
    {
        private readonly IMessageQueueManager _messageQueueManager = Substitute.For<IMessageQueueManager>();
        private readonly CancellationToken _cancellationToken = new CancellationToken();
        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory = Substitute.For<IMessageQueueTransactionFactory>();
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public void GetLocaleQueueWithCreateModeAutoShouldCreateQueue()
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(e => false, e => true);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(new System.Messaging.MessageQueue());

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                cut.Connect(AccessMode.Receive);
            }

            _messageQueueManager.Received(1).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        }
        
        [Fact]
        public void GetLocaleQueueWithCreateModeEnableShouldCreateQueue()
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(e => false, e => false, e => false, e => true);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(new System.Messaging.MessageQueue());

            using (var cut = CreateLocalQueue("MyQueue", true, LocaleQueueMode.DurableCreate))
            {
                cut.Connect(AccessMode.Receive);
            }

            _messageQueueManager.Received(1).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        }
        
        [Fact]
        public void GetLocaleQueueWithCreateModeDisableShouldCreateQueue()
        {
            using (CreateLocalQueue("MyQueue", true, LocaleQueueMode.Durable))
            {
            }

            _messageQueueManager.Received(0).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        }
        
        [Fact]
        public void SendToExistingQueueShouldNotCallCreate()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue", true, LocaleQueueMode.Durable))
            {
                cut.Durable.Should().Be(true);
                cut.Private.Should().Be(true);
                cut.Name.Should().Be("MyQueue");
                cut.Send("Message");
            }

            _messageQueueManager.Received(0).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
            _messageQueueManager.Received(2).Get(".", Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>());
        }
        
        [Fact]
        public void SendToNoneExistingQueueShouldNotCallCreate()
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(e => false, e => true);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(new System.Messaging.MessageQueue());

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                cut.Send("Message");
            }

            _messageQueueManager.Received(1).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        }

        [Fact]
        public void ShouldDeleteNoneDurableQueueAfterUse()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), false);

            using (var cut = CreateLocalQueue("MyQueue", false))
            {
                cut.Send("Message");
            }

            _messageQueueManager.Received(1).Delete(Arg.Any<string>(), Arg.Any<bool>());
        }

        [Fact]
        public void ShouldNotDeleteDurableQueueAfterUse()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue", true, LocaleQueueMode.DurableCreate))
            {
                cut.Send("Message");
            }

            _messageQueueManager.Received(0).Delete(Arg.Any<string>(), Arg.Any<bool>());
        }

        [Fact]
        public void SendNullMessageShouldSendOneMessage()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                cut.Send((MyDto)null);
            }

            _messageQueueManager.Received(1).Send(Arg.Any<System.Messaging.MessageQueue>(), Arg.Any<Message>(), Arg.Any<System.Messaging.MessageQueueTransaction>());
        }

        [Fact]
        public void SendShortMessageShouldSendOneMessageWithAppSpecificOneAndNoCorrelationId()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                cut.Send(new MyDto());
            }

            _messageQueueManager.Received(1).Send(Arg.Any<System.Messaging.MessageQueue>(), Arg.Is<Message>(e => e.AppSpecific == 1 && e.CorrelationId == ""), Arg.Any<System.Messaging.MessageQueueTransaction>());
        }

        [Fact]
        public void SendLargeMessageShouldSendTwoMessage()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                cut.Send(new string('A', 5000000));
            }

            _messageQueueManager.Received(2).Send(Arg.Any<System.Messaging.MessageQueue>(), Arg.Is<Message>(e => e.AppSpecific == 2), Arg.Any<System.Messaging.MessageQueueTransaction>());
        }

        [Fact]
        public void ReceiveSpecificTypeDtoShouldReturnData()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            AddMessageToQueue(messageQueue, new MyDto { I = 1, S = "S" });

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                var receiveDte = (MyDto)cut.Receive(1, _cancellationToken).Message;

                receiveDte.I.Should().Be(1);
                receiveDte.S.Should().Be("S");
            }
        }

        [Fact]
        public void ReceiveObjectDtoShouldReturnData()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            AddMessageToQueue(messageQueue, new MyDto { I = 1, S = "S" });

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                var dto = cut.Receive(1, _cancellationToken).Message;

                dto.GetType().Should().Be(typeof(MyDto));

                var receiveDto = (MyDto)dto;

                receiveDto.I.Should().Be(1);
                receiveDto.S.Should().Be("S");
            }
        }

        [Fact]
        public void ReceiveStringDtoShouldReturnData()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            AddMessageToQueue(messageQueue, "Hallo");

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                var dto = cut.Receive(100000, _cancellationToken).Message;

                dto.GetType().Should().Be(typeof(string));
                ((string)dto).Should().Be("Hallo");
            }
        }

        [Fact]
        public void ReceiveFromEmptyQueueShouldReturnNullAfterTimeout()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue"))
            {
                cut.Receive(10, _cancellationToken).Message.Should().BeNull();
            }
        }

        private ILocaleQueue CreateLocalQueue(string queue, bool privateQueue = true, LocaleQueueMode localeQueueMode = LocaleQueueMode.TemporaryMaster)
        {
            return new LocaleQueue(_logger, _messageQueueManager, _messageQueueTransactionFactory, queue, privateQueue, localeQueueMode, true);
        }

        private void SetQueue(System.Messaging.MessageQueue queue, bool exists)
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(exists);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(null, queue);
        }

        private void AddMessageToQueue(System.Messaging.MessageQueue messageQueue, object dto)
        {
            var body = new QueueMessage { MessageBody = dto.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }), MessageType = dto.GetType() };

            var message = new Message
            {
                Body = body,
                AppSpecific = 1,
                BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })))
            };

            _messageQueueManager.Receive(messageQueue, Arg.Any<TimeSpan>(), Arg.Any<System.Messaging.MessageQueueTransaction>()).Returns(message);
            _messageQueueManager.EndPeek(messageQueue, Arg.Any<IAsyncResult>()).Returns(message);
            _messageQueueManager.BeginPeek(messageQueue, Arg.Any<TimeSpan>()).Returns((IAsyncResult)null);
        }
    }
}