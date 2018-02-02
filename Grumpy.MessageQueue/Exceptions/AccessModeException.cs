using System;
using System.Runtime.Serialization;
using Grumpy.MessageQueue.Enum;

namespace Grumpy.MessageQueue.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class AccessModeException : Exception
    {
        /// <inheritdoc />
        private AccessModeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public AccessModeException(string function, AccessMode accessMode) : base($"Invalid Access mode for Function {function}")
        {
            Data.Add(nameof(accessMode), accessMode);
        }
    }
}