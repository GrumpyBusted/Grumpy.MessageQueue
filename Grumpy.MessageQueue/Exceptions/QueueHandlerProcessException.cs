using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Exception in Queue Handler Process
    /// </summary>
    [Serializable]
    public class QueueHandlerProcessException : Exception
    {
        private QueueHandlerProcessException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Exception in Queue Handler Process
        /// </summary>
        public QueueHandlerProcessException(Exception exception) : base("Exception in Queue Handler Process", exception) { }
    }
}