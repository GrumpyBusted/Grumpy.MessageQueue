using System;
using System.Threading;
using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.MessageQueue.UnitTests.Helper
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class SyncTestTask : ITask
    {
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        public void Start(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Exception = exception;

                throw;
            }
        }

        public void Start(Action action, CancellationToken cancellationToken)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Exception = exception;

                throw;
            }
        }

        public void Start(Action<object> action, object state, CancellationToken cancellationToken)
        {
            try
            {
                AsyncState = state;

                if (state is ITransactionalMessage transactionalMessage)
                {
                    if (transactionalMessage.Message as string == "Exception")
                        throw new Exception("Exception starting task");
                }

                action(state);
            }
            catch (Exception exception)
            {
                Exception = exception;

                throw;
            }
        }

        public bool Wait()
        {
            return true;
        }

        public void Stop()
        {
        }

        public bool IsCompleted => true;
        public bool IsFaulted => false;
        public object AsyncState { get; private set; }
        public Exception Exception { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = disposing;
        }
    }
}