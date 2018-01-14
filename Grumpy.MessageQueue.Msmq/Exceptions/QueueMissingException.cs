using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Queue not found exception
    /// </summary>
    [Serializable]
    public sealed class QueueMissingException : Exception
    {
        private QueueMissingException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Queue not found exception
        /// </summary>
        /// <param name="name">Queue Name</param>
        public QueueMissingException(string name) : base("Queue not found exception")
        {
            Data.Add(nameof(name), name);
        }
    }
}