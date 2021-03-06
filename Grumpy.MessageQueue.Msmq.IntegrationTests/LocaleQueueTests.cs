﻿using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using Grumpy.Common;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.IntegrationTests.Helper;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using NullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger;

namespace Grumpy.MessageQueue.Msmq.IntegrationTests
{
    public class LocaleQueueTests
    {
        private readonly IMessageQueueManager _messageQueueManager = new MessageQueueManager(Substitute.For<ILogger>());
        private readonly CancellationToken _cancellationToken = new CancellationToken();
        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory = new MessageQueueTransactionFactory();

        private ILocaleQueue CreateLocalQueue(string name, bool privateQueue, LocaleQueueMode localeQueueMode = LocaleQueueMode.TemporaryMaster, AccessMode accessMode = AccessMode.Receive, bool transactional = true)
        {
            return new LocaleQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, name, privateQueue, localeQueueMode, transactional, accessMode);
        }

        [Fact]
        public void ConnectReconnectAndDisconnectShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Exists(name, true).Should().BeFalse();

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.Send))
                {
                    queue.Disconnect();
                    queue.Connect();
                    queue.Reconnect();
                    queue.Disconnect();
                    queue.Connect();
                    queue.Reconnect();
                    queue.Disconnect();
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void CanSendAndReceiveFromPrivateQueue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.Send))
                {
                    queue.Send("Hallo");
                }

                using (var queue = new LocaleQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, name, true, LocaleQueueMode.DurableCreate, true, AccessMode.Receive))
                {
                    var message = (string)queue.Receive(100, _cancellationToken).Message;

                    message.Should().Be("Hallo");
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void ReceiveAsyncFromEmptyQueueShouldTimeout()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = new LocaleQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, name, true, LocaleQueueMode.DurableCreate, true, AccessMode.Receive))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var task = queue.ReceiveAsync(1000, _cancellationToken);
                    stopwatch.ElapsedMilliseconds.Should().BeLessThan(900);
                    task.Result?.Message.Should().BeNull();
                    stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(900);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void CanSendAndReceiveAsync()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = new LocaleQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, name, true, LocaleQueueMode.DurableCreate, true, AccessMode.SendAndReceive))
                {
                    queue.Send("Hallo");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var task = queue.ReceiveAsync(1000, _cancellationToken);
                    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
                    task.Result.Message.Should().Be("Hallo");
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void CancelReceiveAsyncShouldStopBeforeTimeout()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = new LocaleQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, name, true, LocaleQueueMode.DurableCreate, true, AccessMode.Receive))
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var stopwatch = new Stopwatch();

                    stopwatch.Start();
                    var task = queue.ReceiveAsync(1000, cancellationTokenSource.Token);
                    cancellationTokenSource.CancelAfter(500);
                    try
                    {
                        task.Result?.Message.Should().BeNull();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    stopwatch.Stop();
                    stopwatch.ElapsedMilliseconds.Should().BeInRange(400, 711);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void CancelReceiveShouldStopBeforeTimeout()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = new LocaleQueue(NullLogger.Instance, _messageQueueManager, _messageQueueTransactionFactory, name, true, LocaleQueueMode.DurableCreate, true, AccessMode.Receive))
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var stopwatch = new Stopwatch();

                    stopwatch.Start();
                    cancellationTokenSource.CancelAfter(500);
                    var message = queue.Receive<string>(1000, cancellationTokenSource.Token);
                    message.Should().BeNull();
                    stopwatch.Stop();
                    stopwatch.ElapsedMilliseconds.Should().BeInRange(400, 700);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void NoneDurableQueueShouldNotExistAfterUse()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true))
                {
                    queue.Receive(1, _cancellationToken);
                }

                _messageQueueManager.Exists(name, true).Should().BeFalse();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void DurableQueueShouldExistAfterUse()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    queue.Receive(1, _cancellationToken);
                }

                _messageQueueManager.Exists(name, true).Should().BeTrue();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void SendToNoneExistingQueueShouldThrow()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.Durable, AccessMode.Send))
                {
                    Assert.Throws<QueueMissingException>(() => queue.Send("Message"));
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void ReceiveFromNoneExistingQueueShouldThrow()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.Durable))
                {
                    Assert.Throws<QueueMissingException>(() => queue.Receive(1, _cancellationToken));
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void SendAndReceiveLargeMessageShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.SendAndReceive))
                {
                    queue.Send(new string('A', 5000000));

                    queue.Count().Should().Be(2);
                    queue.Transactional.Should().BeTrue();
                    queue.AccessMode.Should().Be(AccessMode.SendAndReceive);
                }

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.Durable))
                {
                    var message = (string)queue.Receive(100, _cancellationToken).Message;

                    message.Length.Should().Be(5000000);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }

        }

        [Fact]
        public void SendAndReceiveStringMessageShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.Send))
                {
                    queue.Send("ABC");
                }

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    ((string)queue.Receive(100, _cancellationToken).Message).Should().Be("ABC");
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void SendAfterQueueDeleteShouldRecreateQueue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Exists(name, true).Should().BeFalse();

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.SendAndReceive))
                {
                    queue.Send("ABC");
                    _messageQueueManager.Exists(name, true).Should().BeTrue();
                    _messageQueueManager.Delete(name, true);
                    _messageQueueManager.Exists(name, true).Should().BeFalse();
                    queue.Disconnect();
                    queue.Send("ABC");
                    _messageQueueManager.Exists(name, true).Should().BeTrue();
                    queue.Reconnect();
                    _messageQueueManager.Exists(name, true).Should().BeTrue();
                    queue.Disconnect();
                    queue.Connect();
                    _messageQueueManager.Exists(name, true).Should().BeTrue();
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void SendAndReceiveDtoMessageShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.Send))
                {
                    queue.Send(new MyDto { S = "ABC", I = 2 });
                }

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    var dto = (MyDto)queue.Receive(100, _cancellationToken).Message;

                    dto.S.Should().Be("ABC");
                    dto.I.Should().Be(2);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }


        [Fact]
        public void SendAndReceiveAsyncDtoMessageShouldWork()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.Send))
                {
                    queue.Send(new MyDto { S = "ABC", I = 2 });
                }

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    var dto = (MyDto)queue.ReceiveAsync(100, _cancellationToken).Result.Message;

                    dto.S.Should().Be("ABC");
                    dto.I.Should().Be(2);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }
        [Fact]
        public void SendMessageToReceiveWithCancellationShouldReceiveMessage()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.Send))
                {
                    queue.Send("Message");
                }

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate)
                )
                {
                    var message = queue.Receive(1000, _cancellationToken);

                    message.Should().NotBeNull();
                    var dto = message.Message;


                    dto.GetType().Should().Be(typeof(string));
                    dto.Should().Be("Message");
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void NoMessageToReceiveWithCancellationShouldTimeout()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var message = queue.Receive(1000, new CancellationToken());
                    var dto = message.Message;

                    stopwatch.Stop();
                    stopwatch.ElapsedMilliseconds.Should().BeInRange(900, 1998);
                    dto.Should().BeNull();
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void CancelReceiveWithCancellationShouldReturnNull()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    var stopwatch = new Stopwatch();
                    var cancellationTokenSource = new CancellationTokenSource();

                    stopwatch.Start();
                    cancellationTokenSource.CancelAfter(500);

                    var message = queue.Receive(1000, cancellationTokenSource.Token);
                    var dto = message.Message;

                    stopwatch.Stop();
                    stopwatch.ElapsedMilliseconds.Should().BeLessThan(900);
                    dto.Should().BeNull();
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void AckShouldRemoveMessage()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.SendAndReceive))
                {
                    queue.Send("Message");

                    queue.Count().Should().Be(1);

                    var message = queue.Receive(1000, new CancellationToken());
                    message.Ack();

                    queue.Count().Should().Be(0);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void NAckShouldLeaveMessage()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.SendAndReceive))
                {
                    queue.Send("Message");

                    queue.Count().Should().Be(1);

                    var message = queue.Receive(1000, new CancellationToken());
                    message.NAck();

                    queue.Count().Should().Be(1);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void UseTransactionalQueue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                var stopwatch = new Stopwatch();

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.SendAndReceive))
                {
                    stopwatch.Start();
                    
                    queue.Send("Message");
                    var message = queue.Receive(1000, new CancellationToken());
                    
                    stopwatch.Stop();
                    message.Message.Should().Be("Message");
                    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }

        [Fact]
        public void UseNonTransactionalQueue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                var stopwatch = new Stopwatch();

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate, AccessMode.SendAndReceive, false))
                {
                    stopwatch.Start();

                    queue.Send("Message");
                    var message = queue.Receive(1000, new CancellationToken());
                    
                    stopwatch.Stop();
                    message.Message.Should().Be("Message");
                    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }
        
        [Fact]
        public void CanCreate()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    queue.Delete();
                    queue.Create();
                }

                _messageQueueManager.Exists(name, true).Should().BeTrue();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }
        
        [Fact]
        public void CanDelete()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, true, false);

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    queue.Delete();
                }

                _messageQueueManager.Exists(name, true).Should().BeFalse();
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }
        
        [Fact]
        public void OnNoneExistingQueueExistShouldBeCreate()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    queue.Exists().Should().BeTrue();
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }
        
        [Fact]
        public void OnExistingQueueExistShouldReturnTrue()
        {
            var name = $"IntegrationTest_{UniqueKeyUtility.Generate()}";

            try
            {
                _messageQueueManager.Create(name, true, false);

                using (var queue = CreateLocalQueue(name, true, LocaleQueueMode.DurableCreate))
                {
                    queue.Exists().Should().BeTrue();
                }
            }
            finally
            {
                _messageQueueManager.Delete(name, true);
            }
        }
    }
}