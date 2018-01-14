using System;
using System.Runtime.Serialization;
using Grumpy.Json;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Unable to Peek Message Queue Exception
    /// </summary>
    [Serializable]
    public sealed class MessageQueuePeekException : Exception
    {
        private MessageQueuePeekException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Unable to Peek Message Queue Exception
        /// </summary>
        /// <param name="function">Peek Function</param>
        /// <param name="messageQueue">Message Queue</param>
        /// <param name="timeout">Peek Timeout</param>
        /// <param name="exception">Inner Exception</param>
        /// <exception cref="T:System.NotImplementedException"></exception>
        public MessageQueuePeekException(string function, System.Messaging.MessageQueue messageQueue, TimeSpan timeout, Exception exception) : base("Unable to Peek Message Queue Exception", exception)
        {
            Data.Add(nameof(function), function);
            Data.Add("Name", messageQueue?.QueueName ?? "");
            Data.Add(nameof(messageQueue), messageQueue?.TrySerializeToJson());
            Data.Add(nameof(timeout), timeout);
        }

        /// <inheritdoc />
        /// <summary>
        /// Unable to Peek Message Queue Exception
        /// </summary>
        /// <param name="function">Peek Function</param>
        /// <param name="messageQueue">Message Queue</param>
        /// <param name="asyncResult">Async Result</param>
        /// <param name="exception">Inner Exception</param>
        public MessageQueuePeekException(string function, System.Messaging.MessageQueue messageQueue, IAsyncResult asyncResult, Exception exception) : base("Unable to Peek Message Queue Exception", exception)
        {
            Data.Add(nameof(function), function);
            Data.Add("QueueName", messageQueue?.QueueName ?? "");
            Data.Add(nameof(messageQueue), messageQueue?.TrySerializeToJson());
            Data.Add(nameof(asyncResult), asyncResult?.TrySerializeToJson());
        }
    }
}