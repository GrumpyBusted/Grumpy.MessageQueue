using System;

namespace Grumpy.MessageQueue.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Transactional Message Queue Message
    /// </summary>
    public interface ITransactionalMessage : IDisposable
    {
        /// <summary>
        /// Message Object
        /// </summary>
        object Message { get; }

        /// <summary>
        /// Message Body
        /// </summary>
        string Body { get; }

        /// <summary>
        /// Message Type
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Acknowledge
        /// </summary>
        void Ack();

        /// <summary>
        /// Not Acknowledge
        /// </summary>
        void NAck();
    }
}