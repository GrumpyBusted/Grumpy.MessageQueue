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
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Grumpy.MessageQueue.Msmq.UnitTests.Helper;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Grumpy.MessageQueue.Msmq.UnitTests
{
    public class QueueTests
    {
        private readonly IMessageQueueManager _messageQueueManager;
        private readonly CancellationToken _cancellationToken;

        public QueueTests()
        {
            _cancellationToken = new CancellationToken();
            _messageQueueManager = Substitute.For<IMessageQueueManager>();
            _messageQueueManager.BeginPeek(Arg.Any<System.Messaging.MessageQueue>(), Arg.Any<TimeSpan>()).Returns(e => null as IAsyncResult);
        }

        [Fact]
        public void SendToExistingQueueShouldNotCallCreate()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue("MyQueue", true, LocaleQueueMode.Durable))
            {
                cut.Send("Message");
            }

            _messageQueueManager.Received(0).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
            _messageQueueManager.Received(2).Get(".", Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>());
        }

        [Fact]
        public void SendToNoneExistingQueueShouldNotCallCreate()
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(e => false, e => false, e => false, e => true);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(new System.Messaging.MessageQueue());

            using (var cut = CreateLocalQueue())
            {
                cut.Send("Message");
            }

            _messageQueueManager.Received(1).Create(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        }

        [Fact]
        public void ShouldDeleteNoneDurableQueueAfterUse()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), false);

            using (var cut = CreateLocalQueue())
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

            using (var cut = CreateLocalQueue())
            {
                cut.Send((MyDto)null);
            }

            _messageQueueManager.Received(1).Send(Arg.Any<System.Messaging.MessageQueue>(), Arg.Any<Message>(), Arg.Any<MessageQueueTransaction>());
        }

        [Fact]
        public void SendShortMessageShouldSendOneMessageWithAppSpecificOneAndNoCorrelationId()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue())
            {
                cut.Send(new MyDto());
            }

            _messageQueueManager.Received(1).Send(Arg.Any<System.Messaging.MessageQueue>(), Arg.Is<Message>(e => e.AppSpecific == 1 && e.CorrelationId == ""), Arg.Any<MessageQueueTransaction>());
        }

        [Fact]
        public void SendLargeMessageShouldSendTwoMessage()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue())
            {
                cut.Send(new string('A', 5000000));
            }

            _messageQueueManager.Received(2).Send(Arg.Any<System.Messaging.MessageQueue>(), Arg.Is<Message>(e => e.AppSpecific == 2), Arg.Any<MessageQueueTransaction>());
        }

        [Fact]
        public void ReceiveSpecificTypeDtoShouldReturnData()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            AddMessageToQueue(new MyDto { I = 1, S = "S" });

            using (var cut = CreateLocalQueue())
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

            AddMessageToQueue(new MyDto { I = 1, S = "S" });

            using (var cut = CreateLocalQueue())
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

            AddMessageToQueue("Hallo");

            using (var cut = CreateLocalQueue())
            {
                var dto = cut.Receive(100000, _cancellationToken).Message;

                dto.GetType().Should().Be(typeof(string));
                ((string)dto).Should().Be("Hallo");
            }
        }

        [Fact]
        public void ReceiveMessageShouldReturnData()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            AddMessageToQueue(new MyDto { S = "Message" });

            using (var cut = CreateLocalQueue())
            {
                var dto = cut.Receive<MyDto>(100000, _cancellationToken);

                dto.GetType().Should().Be(typeof(MyDto));
                dto.S.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveMessageWithTypeAndNoDataInQueueShouldReturnNull()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            using (var cut = CreateLocalQueue())
            {
                var dto = cut.Receive<MyDto>(100000, _cancellationToken);

                dto.Should().BeNull();
            }
        }

        [Fact]
        public void ReceiveMessageWithUnexpectedTypeShouldThrow()
        {
            var messageQueue = Substitute.For<System.Messaging.MessageQueue>();

            SetQueue(messageQueue, true);

            AddMessageToQueue(new MyDto { S = "Message" });

            using (var cut = CreateLocalQueue())
            {
                Assert.Throws<InvalidMessageTypeReceivedException>(() => cut.Receive<string>(100000, _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveFromEmptyQueueShouldReturnNullAfterTimeout()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue())
            {
                cut.Receive(10, _cancellationToken).Message.Should().BeNull();
            }
        }

        [Fact]
        public void ReceiveAsyncFromEmptyQueueShouldReturnNullAfterTimeout()
        {
            SetQueue(Substitute.For<System.Messaging.MessageQueue>(), true);

            using (var cut = CreateLocalQueue())
            {
                cut.ReceiveAsync(10, _cancellationToken).Result.Message.Should().BeNull();
            }
        }

        private IQueue CreateLocalQueue(string queue = "MyQueue", bool privateQueue = true, LocaleQueueMode localeQueueMode = LocaleQueueMode.TemporaryMaster)
        {
            return new LocaleQueue(_messageQueueManager, queue, privateQueue, localeQueueMode, true);
        }

        private void SetQueue(System.Messaging.MessageQueue queue, bool exists)
        {
            _messageQueueManager.Exists(Arg.Any<string>(), Arg.Any<bool>()).Returns(exists);
            _messageQueueManager.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<QueueAccessMode>()).Returns(null, queue);
        }

        private void AddMessageToQueue(object dto)
        {
            _messageQueueManager.Receive(Arg.Any<System.Messaging.MessageQueue>(), Arg.Any<TimeSpan>(), Arg.Any<MessageQueueTransaction>()).Returns(GetMessage(dto));
            _messageQueueManager.BeginPeek(Arg.Any<System.Messaging.MessageQueue>(), Arg.Any<TimeSpan>()).Returns((IAsyncResult)null);
            _messageQueueManager.EndPeek(Arg.Any<System.Messaging.MessageQueue>(), Arg.Any<IAsyncResult>()).Returns(GetMessage(dto));
        }

        private static Message GetMessage(object dto)
        {
            var body = new QueueMessage { MessageBody = dto.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }), MessageType = dto.GetType() };

            var message = new Message
            {
                Body = body,
                AppSpecific = 1,
                BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })))
            };

            return message;
        }
    }
}