using System;
using System.Threading;
using FluentAssertions;
using Grumpy.Common.Interfaces;
using Grumpy.Json;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Exceptions;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.UnitTests.Helper;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Grumpy.MessageQueue.UnitTests
{
    public class QueueHandlerSyncTests
    {
        private readonly IQueueFactory _queueFactory;
        private readonly ITaskFactory _taskFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly ILocaleQueue _queue;

        public QueueHandlerSyncTests()
        {
            _queue = Substitute.For<ILocaleQueue>();

            _queueFactory = Substitute.For<IQueueFactory>();
            _queueFactory.CreateLocale(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(_queue);
            _taskFactory = Substitute.For<ITaskFactory>();
            _taskFactory.Create().Returns(new SyncTestTask());
            _cancellationToken = new CancellationToken();
        }

        [Fact]
        public void ReceiveAndHandleOneMessage()
        {
            var numberOfMessages = 0;

            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("MyMessage"), e => null);

            ExecuteHandler((m, c) => ++numberOfMessages);

            numberOfMessages.Should().Be(1);
        }

        [Fact]
        public void HandlerThrowExceptionShouldResultInErrors()
        {
            var numberOfErrors = 0;

            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("MyMessage"), e => null);

            ExecuteHandler((m, c) => throw new Exception((string)m), (m, e) => ++numberOfErrors);

            numberOfErrors.Should().Be(1);
        }

        [Fact]
        public void HandlerAndErrorHandlerThrowExceptionShouldNAckMessage()
        {
            var message = CreateMessage("MyMessage");

            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => message, e => null);

            ExecuteHandler((m, c) => throw new Exception((string)m), (m, e) => throw new Exception());

            message.Received(1).NAck();
        }

        [Fact]
        public void HeartbeatHandlerShouldBeCalled()
        {
            var numberOfHeartbeats = 0;

            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("MyMessage1"), e => CreateMessage("MyMessage2"), e => CreateMessage("MyMessage3"), e => null);
            
            ExecuteHandler((m, c) => Thread.Sleep(100), null, () => ++numberOfHeartbeats);

            numberOfHeartbeats.Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public void ExceptionInHeartbeatHandlerShouldNotThrowException()
        {
            var numberOfHeartbeats = 0;

            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("MyMessage1"), e => CreateMessage("MyMessage2"), e => CreateMessage("MyMessage3"), e => null);

            ExecuteHandler((m, c) => Thread.Sleep(100), null, () => { ++numberOfHeartbeats; throw new Exception(); });

            numberOfHeartbeats.Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public void ExceptionInReceiveShouldSkipAndContinue()
        {
            var numberOfMessages = 0;
            var numberOfErrors = 0;

            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("Message1"), e => throw new Exception("Exception"), e => CreateMessage("Message2"), e => CreateMessage("Message3"), e => (ITransactionalMessage)null);

            ExecuteHandler((m, c) => ++numberOfMessages, (m, c) => ++numberOfErrors);

            numberOfMessages.Should().Be(3);
            numberOfErrors.Should().Be(0);
        }

        [Fact]
        public void ExceptionInReceiveMultipleTimesShouldThrowException()
        {
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => throw new Exception("Exception"), e => throw new Exception("Exception"), e => throw new Exception("Exception"), e => (ITransactionalMessage)null);

            Assert.Throws<QueueHandlerProcessException>(() => ExecuteHandler((m, c) => { }));
        }

        [Fact]
        public void FunctionalHandlerShouldAckTransactionalMessage()
        {
            var transactionalMessage = CreateMessage("Message1");
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => transactionalMessage, e => null);

            ExecuteHandler((m, c) => { });

            transactionalMessage.Received(1).Ack();
            transactionalMessage.Received(0).NAck();
        }

        [Fact]
        public void NonFunctionalHandlerShouldNAckTransactionalMessage()
        {
            var transactionalMessage = CreateMessage("Message1");
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => transactionalMessage, e => null);

            ExecuteHandler((m, c) => throw new Exception());

            transactionalMessage.Received(0).Ack();
            transactionalMessage.Received(1).NAck();
        }

        [Fact]
        public void NonFunctionalHandlerAndTrueFromErrorHandlerShouldAckTransactionalMessage()
        {
            var transactionalMessage = CreateMessage("Message1");
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => transactionalMessage, e => null);

            ExecuteHandler((m, c) => throw new Exception(), (o, exception) => true);

            transactionalMessage.Received(1).Ack();
            transactionalMessage.Received(0).NAck();
        }

        [Fact]
        public void NonFunctionalHandlerAndFalseFromErrorHandlerShouldAckTransactionalMessage()
        {
            var transactionalMessage = CreateMessage("Message1");
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => transactionalMessage, e => null);

            ExecuteHandler((m, c) => throw new Exception(), (o, exception) => false);

            transactionalMessage.Received(0).Ack();
            transactionalMessage.Received(1).NAck();
        }

        private void ExecuteHandler(Action<object, CancellationToken> messageHandler, Action<object, Exception> errorHandler = null, Action heartbeatHandler = null)
        {
            using (var cut = CreateQueueHandler())
            {
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, messageHandler, errorHandler, heartbeatHandler, 100, false, true, _cancellationToken);
            }
        }

        private void ExecuteHandler(Action<object, CancellationToken> messageHandler, Func<object, Exception, bool> errorHandler)
        {
            using (var cut = CreateQueueHandler())
            {
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, messageHandler, errorHandler, null, 100, false, true, _cancellationToken);
            }
        }

        private IQueueHandler CreateQueueHandler()
        {
            return new QueueHandler(NullLogger.Instance, _queueFactory, _taskFactory);
        }

        private static ITransactionalMessage CreateMessage(object body)
        {
            var message = Substitute.For<ITransactionalMessage>();

            message.Message.Returns(body);
            message.Body.Returns(body.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));
            message.Type.Returns(body.GetType());

            return message;
        }
    }
}
