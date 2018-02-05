using System;
using System.Messaging;
using System.Threading;
using Grumpy.Common.Extensions;
using Grumpy.Json;
using Grumpy.Logging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc cref="ILocaleQueue" />
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class LocaleQueue : Queue, ILocaleQueue
    {
        private readonly LocaleQueueMode _localeQueueMode;

        private bool _disposed;

        /// <inheritdoc />
        public LocaleQueue(ILogger logger, IMessageQueueManager messageQueueManager, IMessageQueueTransactionFactory messageQueueTransactionFactory, string name, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, AccessMode accessMode) : base(logger, messageQueueManager, messageQueueTransactionFactory, name, privateQueue, localeQueueMode.In(LocaleQueueMode.Durable, LocaleQueueMode.DurableCreate), transactional, accessMode)
        {
            _localeQueueMode = localeQueueMode;

            switch (_localeQueueMode)
            {
                case LocaleQueueMode.TemporaryMaster:
                    Create();
                    base.ConnectInternal();
                    break;
                case LocaleQueueMode.DurableCreate:
                    CreateIfNotExist();
                    break;
                case LocaleQueueMode.Durable:
                    break;
                case LocaleQueueMode.TemporarySlave:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        protected override void ConnectInternal()
        {
            if (MessageQueue == null)
                CreateIfNotExist();

            base.ConnectInternal();
        }

        /// <inheritdoc />
        public void Create()
        {
            MessageQueueManager.Create(Name, Private, Transactional);

            Logger.Debug("Queue created {Name} {Private} {Transactional}", Name, Private, Transactional);
        }

        /// <inheritdoc />
        public void Delete()
        {
            MessageQueueManager.Delete(Name, Private);

            Logger.Debug("Queue deleted {Name} {Private}", Name, Private);
        }

        /// <inheritdoc />
        public bool Exists()
        {
            return MessageQueueManager.Exists(Name, Private);
        }

        /// <inheritdoc />
        protected override System.Messaging.MessageQueue GetQueue(AccessMode accessMode)
        {
            return MessageQueueManager.Get(".", Name, Private, accessMode == AccessMode.Receive ? QueueAccessMode.Receive : accessMode == AccessMode.Send ? QueueAccessMode.Send : QueueAccessMode.SendAndReceive);
        }

        /// <inheritdoc />
        public override string ToJson()
        {
            return this.SerializeToJson();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    if (_localeQueueMode == LocaleQueueMode.TemporaryMaster)
                        Delete();
                }

                base.Dispose(disposing);
            }
        }

        private void CreateIfNotExist()
        {
            if (!Exists())
            {
                if (_localeQueueMode.In(LocaleQueueMode.DurableCreate, LocaleQueueMode.TemporaryMaster))
                {
                    using (var mutex = new Mutex(true, $@"Global\Grumpy.MessageQueue.{Name}"))
                    {
                        mutex.WaitOne(10000);

                        if (!Exists())
                        {
                            DisconnectInternal();
                            Create();
                        }

                        mutex.ReleaseMutex();
                    }
                }
                else
                    throw new QueueMissingException(Name);
            }
        }
    }
}