using System;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.MessageQueue.Enum;

namespace Grumpy.MessageQueue.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// The Message Queue
    /// </summary>
    public interface IQueue : IDisposable
    {
        /// <summary>
        /// Queue Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Private Queue
        /// </summary>
        bool Private { get; }

        /// <summary>
        /// Durable Queue
        /// </summary>
        bool Durable { get; }

        /// <summary>
        /// Transaction Queue
        /// </summary>
        bool Transactional { get; }

        /// <summary>
        /// Queue Access Mode
        /// </summary>
        AccessMode AccessMode { get; }

        /// <summary>
        /// Number of Messages in Queue
        /// </summary>
        int Count();

        /// <summary>
        /// Connect to Message Queue
        /// </summary>
        void Connect();

        /// <summary>
        /// Reconnect to Message Queue
        /// </summary>
        void Reconnect();

        /// <summary>
        /// Disconnect from Message Queue
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send Message to Queue
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T">Type of message object</typeparam>
        void Send<T>(T message);

        /// <summary>
        /// Asynchronous Receive Message from Queue
        /// </summary>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait for message</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Task containing the Message as Result</returns>
        Task<ITransactionalMessage> ReceiveAsync(int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Receive Message from Queue
        /// </summary>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait for message</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Message</returns>
        ITransactionalMessage Receive(int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Receive Message and Acknowledge 
        /// </summary>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait for message</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <typeparam name="T">Expected type of message, throws exception if message do not match</typeparam>
        /// <returns>Message</returns>
        T Receive<T>(int millisecondsTimeout, CancellationToken cancellationToken);
    }
}
