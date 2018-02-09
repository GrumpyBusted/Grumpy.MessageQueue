using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class MissingChunkException : Exception
    {
        private MissingChunkException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public MissingChunkException(string queueName, int messageNumber) : base("Chunk of message missing")
        {
            Data.Add(nameof(queueName), queueName);
            Data.Add(nameof(messageNumber), messageNumber);
        }
    }
}