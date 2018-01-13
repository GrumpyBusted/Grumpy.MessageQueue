﻿using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using Grumpy.Common.Interfaces;
using Grumpy.Common.Threading;
using Grumpy.Json;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
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
        private readonly Stopwatch _stopwatch;

        public QueueHandlerAsyncTests()
        {
            _queue = Substitute.For<ILocaleQueue>();
            _queue.Receive(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(e => CreateMessage("Message1"), e => CreateMessage("Message2"), e => CreateMessage("Message3"), e => null);

            _queueFactory = Substitute.For<IQueueFactory>();
            _queueFactory.CreateLocale(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>()).Returns(_queue);
            _taskFactory = new TaskFactory();
            
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _stopwatch = new Stopwatch();
        }

        [Fact]
        public void ReceiveOnSingleThreadedHandlerShouldBeSlow()
        {
            _stopwatch.Start();

            ExecuteHandler((m, c) => Thread.Sleep(1000), false);

            _stopwatch.Stop();
            _stopwatch.ElapsedMilliseconds.Should().BeInRange(2500, 3500);
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
            return new QueueHandler(_queueFactory, _taskFactory);
        }

        private void ExecuteHandler(Action<object, CancellationToken> messageHandler, bool multiThreadedHandler)
        {
            using (var cut = new QueueHandler(_queueFactory, _taskFactory))
            {
                cut.Start("MyQueue", true, LocaleQueueMode.TemporaryMaster, true, messageHandler, null, null, 100, multiThreadedHandler, false, _cancellationToken);

                // ReSharper disable once AccessToDisposedClosure
                TimerUtility.WaitForIt(() => cut.Idle, 6000);
            }
        }

        private static ITransactionalMessage CreateMessage(object body)
        {
            var message = Substitute.For<ITransactionalMessage>();

            message.Message.Returns(body);
            message.Body.Returns(body.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));
            message.Type.Returns(body.GetType());

            return message;
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
