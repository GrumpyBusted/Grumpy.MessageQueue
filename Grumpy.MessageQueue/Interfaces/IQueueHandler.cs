using System;
using System.Threading;
using Grumpy.MessageQueue.Enum;

namespace Grumpy.MessageQueue.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Queue Handler Receive Message from Message Queue and invoke Message Handler Functionality 
    /// </summary>
    public interface IQueueHandler : IDisposable
    {
        /// <summary>
        /// Start the Queue handler
        /// </summary>
        /// <param name="queueName">Message Queue Name</param>
        /// <param name="privateQueue">Is Message Queue Private</param>
        /// <param name="localeQueueMode">Message Queue Create Mode</param>
        /// <param name="transactional">Is Message Queue transactional</param>
        /// <param name="messageHandler">Message Handler</param>
        /// <param name="errorHandler">Error Handler</param>
        /// <param name="heartbeatHandler">Heartbeat Handler</param>
        /// <param name="heartRateMilliseconds">Heart rate in milliseconds</param>
        /// <param name="multiThreadedHandler">Handler executed multi threaded</param>
        /// <param name="syncMode">Should the handler be running in synchronous mode</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        void Start(string queueName, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, Action<object, CancellationToken> messageHandler, Action<object, Exception> errorHandler, Action heartbeatHandler, int heartRateMilliseconds, bool multiThreadedHandler, bool syncMode, CancellationToken cancellationToken);

        /// <summary>
        /// Indicate if the Queue handler is idle or managing active tasks
        /// </summary>
        bool Idle { get; }

        /// <summary>
        /// Stop the queue handler
        /// </summary>
        void Stop();
    }
}