using System;
using System.Runtime.Serialization;

namespace Grumpy.MessageQueue.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Exception creating task
    /// </summary>
    [Serializable]
    public class TaskCreationException : Exception
    {
        private TaskCreationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Exception creating task
        /// </summary>
        public TaskCreationException(Exception exception) : base("Exception creating task", exception) { }
    }
}