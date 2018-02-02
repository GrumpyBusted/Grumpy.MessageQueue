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
        /// <inheritdoc />
        protected TaskCreationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public TaskCreationException(Exception exception) : base("Exception creating task", exception) { }
    }
}