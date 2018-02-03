using System;
using System.Runtime.Serialization;
using Grumpy.Json;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Unable to receive from Message Queue Exception
    /// </summary>
    [Serializable]
    public sealed class MessageQueueReceiveException : Exception
    {
        private MessageQueueReceiveException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Unable to receive from Message Queue Exception
        /// </summary>
        /// <param name="messageQueue">Message Queue</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="exception">Inner Exception</param>
        public MessageQueueReceiveException(System.Messaging.MessageQueue messageQueue, TimeSpan timeout, Exception exception) : base($"Unable to receive from Message Queue Exception ({messageQueue?.QueueName ?? ""})", exception)
        {
            Data.Add("Name", messageQueue?.QueueName);
            Data.Add(nameof(messageQueue), messageQueue.TrySerializeToJson());
            Data.Add(nameof(timeout), timeout);
        }

        /// <inheritdoc />
        /// <summary>
        /// Unable to receive from Message Queue Exception
        /// </summary>
        /// <param name="messageQueue">Message Queue</param>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="exception">Inner Exception</param>
        public MessageQueueReceiveException(System.Messaging.MessageQueue messageQueue, string correlationId, TimeSpan timeout, Exception exception) : base($"Unable to receive from Message Queue Exception ({messageQueue?.QueueName ?? ""})", exception)
        {
            Data.Add("QueueName", messageQueue?.QueueName);
            Data.Add(nameof(messageQueue), messageQueue.TrySerializeToJson());
            Data.Add(nameof(correlationId), correlationId);
            Data.Add(nameof(timeout), timeout);
        }
    }
}