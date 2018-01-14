using System;
using System.Runtime.Serialization;
using Grumpy.Json;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Invalid Message Type received
    /// </summary>
    [Serializable]
    public sealed class InvalidMessageTypeReceivedException : Exception
    {
        private InvalidMessageTypeReceivedException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Invalid Message Type received
        /// </summary>
        /// <param name="queueName">Queue Name</param>
        /// <param name="privateQueue">Is Queue Private</param>
        /// <param name="message">The Message</param>
        /// <param name="expectedType">Expected Message Type</param>
        /// <param name="actualType">Actual Message Type</param>
        public InvalidMessageTypeReceivedException(string queueName, bool privateQueue, object message, Type expectedType, Type actualType) : base("Invalid Message Type Received")
        {
            Data.Add(nameof(queueName), queueName);
            Data.Add(nameof(privateQueue), privateQueue);
            Data.Add(nameof(message), message.TrySerializeToJson());
            Data.Add(nameof(expectedType), expectedType);
            Data.Add(nameof(actualType), actualType);
        }
    }
}