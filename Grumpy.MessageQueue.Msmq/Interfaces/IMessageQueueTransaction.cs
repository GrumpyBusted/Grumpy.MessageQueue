using System;
using System.Messaging;

namespace Grumpy.MessageQueue.Msmq.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Message Queue Transaction
    /// </summary>
    public interface IMessageQueueTransaction : IDisposable
    {
        /// <summary>
        /// Commit Transaction
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollback/Abort Transaction
        /// </summary>
        void Abort();

        /// <summary>
        /// Begin Transaction
        /// </summary>
        void Begin();

        /// <summary>
        /// Transaction Status
        /// </summary>
        MessageQueueTransactionStatus Status { get; }

        /// <summary>
        /// Get MSMQ Message Queue Transaction
        /// </summary>
        System.Messaging.MessageQueueTransaction Transaction { get; }
    }
}