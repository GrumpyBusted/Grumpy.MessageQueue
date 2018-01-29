using System;
using System.Messaging;
using System.Threading;
using Grumpy.Common.Extensions;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Interfaces;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc cref="ILocaleQueue" />
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class LocaleQueue : Queue, ILocaleQueue
    {
        private readonly LocaleQueueMode _localeQueueMode;

        private bool _disposed;

        /// <inheritdoc />
        public LocaleQueue(IMessageQueueManager messageQueueManager, IMessageQueueTransactionFactory messageQueueTransactionFactory, string name, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional) : base(messageQueueManager, messageQueueTransactionFactory, name, privateQueue, localeQueueMode.In(LocaleQueueMode.Durable, LocaleQueueMode.DurableCreate), transactional)
        {
            _localeQueueMode = localeQueueMode;

            if (localeQueueMode.In(LocaleQueueMode.DurableCreate, LocaleQueueMode.TemporaryMaster))
                Connect();
        }
        
        /// <inheritdoc />
        public override void Connect(AccessMode accessMode)
        {
            try
            {
                if (!Exists())
                    CreateQueue();

                base.Connect(accessMode);
            }
            catch (QueueMissingException)
            {
                CreateQueue();

                base.Connect(accessMode);
            }
        }

        /// <inheritdoc />
        public void Create()
        {
            MessageQueueManager.Create(Name, Private, Transactional);
        }

        /// <inheritdoc />
        public void Delete()
        {
            MessageQueueManager.Delete(Name, Private);
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

        private void CreateQueue()
        {
            switch (_localeQueueMode)
            {
                case LocaleQueueMode.DurableCreate:
                    if (!Exists())
                    {
                        using (var mutex = new Mutex(true, $@"Global\Grumpy.MessageQueue.{Name}"))
                        {
                            mutex.WaitOne(10000);

                            if (!Exists())
                                Create();

                            mutex.ReleaseMutex();
                        }
                    }

                    break;
                case LocaleQueueMode.TemporaryMaster:
                    Create();
                    break;
                case LocaleQueueMode.TemporarySlave:
                    break;
                case LocaleQueueMode.Durable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}