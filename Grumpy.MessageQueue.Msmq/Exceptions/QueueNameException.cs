using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class QueueNameException : Exception
    {
        /// <inheritdoc />
        private QueueNameException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public QueueNameException(string name, int maxLength) : base("Queue Name too long")
        {
            Data.Add(nameof(name), name);
            Data.Add(nameof(maxLength), maxLength);
        }
    }
}