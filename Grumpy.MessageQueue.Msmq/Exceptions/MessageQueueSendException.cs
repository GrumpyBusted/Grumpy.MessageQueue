using System;
using System.Messaging;
using System.Runtime.Serialization;
using Grumpy.Json;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Message Queue Send Exception
    /// </summary>
    [Serializable]
    public sealed class MessageQueueSendException : Exception
    {
        private MessageQueueSendException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Message Queue Send Exception
        /// </summary>
        public MessageQueueSendException(System.Messaging.MessageQueue messageQueue, Message message, Exception exception) : base($"Message Queue Send Exception ({messageQueue?.QueueName ?? ""})", exception)
        {
            Data.Add("QueueName", messageQueue?.QueueName ?? "");
            Data.Add(nameof(messageQueue), messageQueue.TrySerializeToJson());
            Data.Add(nameof(message), message.TrySerializeToJson());
        }
    }
}