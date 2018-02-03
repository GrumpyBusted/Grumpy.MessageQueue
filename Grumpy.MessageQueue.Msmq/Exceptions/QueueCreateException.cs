using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Exception creating message queue
    /// </summary>
    [Serializable]
    public sealed class QueueCreateException : Exception
    {
        private QueueCreateException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Exception creating message queue
        /// </summary>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Is queue private</param>
        /// <param name="exception">Inner Exception</param>
        public QueueCreateException(string name, bool privateQueue, Exception exception) : base($"Exception creating message queue ({name})", exception)
        {
            Data.Add(nameof(name), name);
            Data.Add(nameof(privateQueue), privateQueue);
        }
    }
}