using System;
using System.Messaging;
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
        /// <param name="messageQueue"></param>
        /// <param name="timeout"></param>
        /// <param name="messageQueueTransaction"></param>
        /// <param name="exception"></param>
        public MessageQueueReceiveException(System.Messaging.MessageQueue messageQueue, TimeSpan timeout, MessageQueueTransaction messageQueueTransaction, Exception exception) : base("Unable to receive from Message Queue Exception", exception)
        {
            Data.Add("Name", messageQueue?.QueueName);
            Data.Add(nameof(messageQueue), messageQueue.TrySerializeToJson());
            Data.Add(nameof(timeout), timeout);
            Data.Add(nameof(messageQueueTransaction), messageQueueTransaction.TrySerializeToJson());
        }

        /// <inheritdoc />
        /// <summary>
        /// Unable to receive from Message Queue Exception
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <param name="correlationId"></param>
        /// <param name="timeout"></param>
        /// <param name="messageQueueTransaction"></param>
        /// <param name="exception"></param>
        public MessageQueueReceiveException(System.Messaging.MessageQueue messageQueue, string correlationId, TimeSpan timeout, MessageQueueTransaction messageQueueTransaction, Exception exception) : base("Unable to receive from Message Queue Exception", exception)
        {
            Data.Add("QueueName", messageQueue?.QueueName);
            Data.Add(nameof(messageQueue), messageQueue.TrySerializeToJson());
            Data.Add(nameof(correlationId), correlationId);
            Data.Add(nameof(timeout), timeout);
            Data.Add(nameof(messageQueueTransaction), messageQueueTransaction.TrySerializeToJson());
        }
    }
}