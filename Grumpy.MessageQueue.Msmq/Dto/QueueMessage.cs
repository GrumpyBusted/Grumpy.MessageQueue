using System;

namespace Grumpy.MessageQueue.Msmq.Dto
{
    /// <summary>
    /// Message Queue Message
    /// </summary>
    public class QueueMessage
    {
        /// <summary>
        /// Message Body
        /// </summary>
        public string MessageBody { get; set; }

        /// <summary>
        /// Message Body Object Type
        /// </summary>
        public Type MessageType { get; set; }
    }
}