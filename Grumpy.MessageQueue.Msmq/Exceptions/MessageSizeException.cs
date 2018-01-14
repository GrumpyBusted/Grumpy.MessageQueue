using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Message Size Exception - Message to Large
    /// </summary>
    [Serializable]
    public sealed class MessageSizeException : Exception
    {
        private MessageSizeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Message Size Exception - Message to Large
        /// </summary>
        /// <param name="messageSize">Message Size</param>
        /// <param name="maxSize">Max Message Size</param>
        /// <param name="transactional">Transactional Queue</param>
        public MessageSizeException(long messageSize, int maxSize, bool transactional) : base("Message size to Large Exception")
        {
            Data.Add(nameof(messageSize), messageSize);
            Data.Add(nameof(maxSize), maxSize);
            Data.Add(nameof(transactional), transactional);
        }
    }
}