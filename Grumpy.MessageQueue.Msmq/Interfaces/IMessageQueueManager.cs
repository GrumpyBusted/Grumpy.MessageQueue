using System;
using System.Collections.Generic;
using System.Messaging;

namespace Grumpy.MessageQueue.Msmq.Interfaces
{
    /// <summary>
    /// Message Queue Manager - Wraps the Microsoft Message Queue (MSMQ) Api
    /// Thr purpose of the class is to wrap the part of the MSMQ Api used in the library to be able to unit test the rest of the functionality
    /// </summary>
    public interface IMessageQueueManager
    {
        /// <summary>
        /// Create a Message Queue on MSMQ
        /// </summary>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Create private (true) or public (false) queue</param>
        /// <param name="transaction">Transaction Queue</param>
        /// <returns>The MSMQ Queue</returns>
        System.Messaging.MessageQueue Create(string name, bool privateQueue, bool transaction);

        /// <summary>
        /// Delete Message Queue from MSMQ
        /// </summary>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Use as private (true) or public (false) queue</param>
        void Delete(string name, bool privateQueue);

        /// <summary>
        /// Indicate if Message Queue Exists in MSMQ
        /// </summary>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Use as private (true) or public (false) queue</param>
        /// <returns>True is exists</returns>
        bool Exists(string name, bool privateQueue);

        /// <summary>
        /// Get Existing Message Queue from MSMQ
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Use as private (true) or public (false) queue</param>
        /// <param name="mode">Queue Access Mode</param>
        /// <returns>The MSMQ Queue</returns>
        System.Messaging.MessageQueue Get(string serverName, string name, bool privateQueue, QueueAccessMode mode);

        /// <summary>
        /// List queue names on MSMQ
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="privateQueue">Use as private (true) or public (false) queue</param>
        /// <returns>List of queue names</returns>
        IEnumerable<string> List(string serverName, bool privateQueue);

        /// <summary>
        /// Send Message to Message Queue on MSMQ
        /// </summary>
        /// <param name="messageQueue">The MSMQ Queue</param>
        /// <param name="message">The MSMQ Message</param>
        /// <param name="messageQueueTransaction">Message Queue Transaction</param>
        void Send(System.Messaging.MessageQueue messageQueue, Message message, System.Messaging.MessageQueueTransaction messageQueueTransaction);

        /// <summary>
        /// Receive Message from Message Queue on MSMQ
        /// </summary>
        /// <param name="messageQueue">The MSMQ Queue</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="messageQueueTransaction">Message Queue Transaction</param>
        /// <returns>The MSMQ Message, null if not received before timeout</returns>
        Message Receive(System.Messaging.MessageQueue messageQueue, TimeSpan timeout, System.Messaging.MessageQueueTransaction messageQueueTransaction);

        /// <summary>
        /// Receive Message from Message Queue on MSMQ for specific Correlation Id
        /// </summary>
        /// <param name="messageQueue">The MSMQ Queue</param>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="messageQueueTransaction">Message Queue Transaction</param>
        /// <returns>The MSMQ Message, null if not received before timeout</returns>
        Message ReceiveByCorrelationId(System.Messaging.MessageQueue messageQueue, string correlationId, TimeSpan timeout, System.Messaging.MessageQueueTransaction messageQueueTransaction);

        /// <summary>
        /// Start Asynchronous Peek Message from Message Queue on MSMQ
        /// </summary>
        /// <param name="messageQueue">The MSMQ Queue</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>The MSMQ Message, null if not received before timeout</returns>
        IAsyncResult BeginPeek(System.Messaging.MessageQueue messageQueue, TimeSpan timeout);

        /// <summary>
        /// End Asynchronous Peek Message from Message Queue on MSMQ
        /// </summary>
        /// <param name="messageQueue">The MSMQ Queue</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>The MSMQ Message, null if not received before timeout</returns>
        Message EndPeek(System.Messaging.MessageQueue messageQueue, IAsyncResult timeout);
    }
}