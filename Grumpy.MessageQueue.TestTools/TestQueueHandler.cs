using System;
using System.Collections.Generic;
using System.Threading;
using Grumpy.Common.Threading;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.MessageQueue.TestTools
{
    /// <inheritdoc />
    /// <summary>
    /// Test Implementation of Queue Handler
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TestQueueHandler : IQueueHandler
    {
        private readonly ICollection<object> _messages;
        private bool _disposed;

        /// <inheritdoc />
        public TestQueueHandler(ICollection<object> messages)
        {
            _messages = messages;
        }

        /// <inheritdoc />
        public bool Idle => false;

        /// <inheritdoc />
        public void Start(string queueName, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, Action<object, CancellationToken> messageHandler, Action<object, Exception> errorHandler, Action heartbeatHandler, int heartRateMilliseconds, bool multiThreadedHandler, bool syncMode, CancellationToken cancellationToken)
        {
            if (heartbeatHandler != null)
            {
                var timerTask = new TimerTask();

                timerTask.Start(heartbeatHandler, heartRateMilliseconds, cancellationToken);
            }

            foreach (var message in _messages)
            {
                try
                {
                    messageHandler(message, cancellationToken);
                }
                catch (Exception exception)
                {
                    errorHandler(message, exception);
                }
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose locale objects
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = disposing;
        }
    }
}