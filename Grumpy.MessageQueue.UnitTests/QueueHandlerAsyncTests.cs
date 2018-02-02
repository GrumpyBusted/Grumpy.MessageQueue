using System;
using System.Threading;
using FluentAssertions;
using Grumpy.Common.Interfaces;
using Grumpy.Common.Threading;
using Grumpy.Json;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Grumpy.MessageQueue.UnitTests
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class QueueHandlerAsyncTests : IDisposable
    {
        private readonly IQueueFactory _queueFactory;
        private readonly ITaskFactory _taskFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly ILocaleQueue _queue;
        private bool _disposed;

        public QueueHandlerAsyncTests()
        {
            _queue = Substitute.For<ILocaleQueue>();
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("Message1"), e => CreateMessage("Message2"), e => CreateMessage("Message3"), e => null);

            _queueFactory = Substitute.For<IQueueFactory>();
            _queueFactory.CreateLocale(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(_queue);
            _taskFactory = new TaskFactory();
            
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }
        
        [Fact]
        public void CanStopQueue()
        {
            using (var cut = CreateQueueHandler())
            {
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, (m,c) => { }, null, null, 100, false, false, _cancellationToken);
                cut.Stop();
            }
        }

        [Fact]
        public void InvalidHeartbeatRateShouldThrow()
        {
            using (var cut = CreateQueueHandler())
            {
                Assert.Throws<ArgumentException>(() => cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, (m,c) => { }, null, () => { }, -1, false, false, _cancellationToken));
            }
        }

        [Fact]
        public void MultiStartShouldThrowException()
        {
            using (var cut = CreateQueueHandler())
            {
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, (m,c) => { }, null, null, 100, false, false, _cancellationToken);
                Assert.Throws<ArgumentException>(() => cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, (m,c) => { }, null, null, 100, false, false, _cancellationToken));
            }
        }

        [Fact]
        public void QueueBeforeShouldBeIdle()
        {
            using (var cut = CreateQueueHandler())
            {
                cut.Idle.Should().BeTrue();
            }
        }

        [Fact]
        public void QueueAfterShouldNotBeIdle()
        {
            using (var cut = CreateQueueHandler())
            {
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, (m,c) => { }, null, null, 100, false, false, _cancellationToken);
                cut.Idle.Should().BeFalse();
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

        [Fact]
        public void HeartbeatAreCalled()
        {
            using (var cut = CreateQueueHandler())
            {
                var i = 0;
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, (m,c) => { }, null, () => { ++i; }, 1, false, false, _cancellationToken);
                Thread.Sleep(1000);
                i.Should().BeGreaterOrEqualTo(1);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Dispose();
                    _queue?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
