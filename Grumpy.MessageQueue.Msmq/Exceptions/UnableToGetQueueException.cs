using System;
using System.Runtime.Serialization;
using Grumpy.MessageQueue.Enum;

namespace Grumpy.MessageQueue.Msmq.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class UnableToGetQueueException : Exception
    {
        private UnableToGetQueueException (SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public UnableToGetQueueException(string name, AccessMode accessMode) : base($"Unable to Get Queue Exception ({name})")
        {
            Data.Add(nameof(name), name);
            Data.Add(nameof(accessMode), accessMode);
        }
    }
}