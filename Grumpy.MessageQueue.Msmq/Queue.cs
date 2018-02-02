using System;
using System.IO;
using System.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common.Extensions;
using Grumpy.Json;
using Grumpy.Logging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Exceptions;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Dto;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Extensions;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public abstract class Queue : IQueue
    {
        private const int MaxMsmqMessageSize = 4096000;
        private System.Messaging.MessageQueue _messageQueue;
        private readonly object _messageQueueLock;
        private readonly Timer _disconnectTimer;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <inheritdoc />
        protected Queue(ILogger logger, IMessageQueueManager messageQueueManager, IMessageQueueTransactionFactory messageQueueTransactionFactory, string name, bool privateQueue, bool durable, bool transactional, AccessMode accessMode)
        {
            if (name.Length > 124)
                throw new ArgumentException("Queue name too long", nameof(name));

            Logger = logger;
            MessageQueueManager = messageQueueManager;
            _messageQueueTransactionFactory = messageQueueTransactionFactory;
            Name = name;
            Private = privateQueue;
            Transactional = transactional;
            Durable = durable;
            _messageQueueLock = new object();
            _disconnectTimer = new Timer(Disconnect, null, 3600000, 3600000);
            AccessMode = accessMode;
        }

        /// <summary>
        /// Message Queue Manager
        /// </summary>
        protected readonly IMessageQueueManager MessageQueueManager;

        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory;

        private bool _disposed;

        /// <summary>
        /// Get the existing MSMQ Queue
        /// </summary>
        /// <param name="accessMode">Queue Access Mode</param>
        /// <returns>The MSMQ Queue</returns>
        protected abstract System.Messaging.MessageQueue GetQueue(AccessMode accessMode);

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool Private { get; }

        /// <inheritdoc />
        public bool Durable { get; }

        /// <inheritdoc />
        public bool Transactional { get; }

        /// <inheritdoc />
        public AccessMode AccessMode { get; }

        /// <inheritdoc />
        public int Count()
        {
            if (!AccessMode.In(AccessMode.Receive, AccessMode.SendAndReceive))
                throw new AccessModeException(nameof(Count), AccessMode);

            try
            {
                lock (_messageQueueLock)
                {
                    Connect();

                    if (_messageQueue != null)
                    {
                        var count = 0;

                        using (var messageEnumerator = _messageQueue.GetMessageEnumerator2())
                        {
                            while (messageEnumerator.MoveNext())
                                ++count;
                        }

                        return count;
                    }
                }

                Logger.Warning("Unable to count messages {@Queue}", this);

                return -1;
            }
            catch (Exception exception)
            {
                Logger.Warning(exception, "Unable to count messages {@Queue}", this);

                return -1;
            }
        }

        /// <inheritdoc />
        public virtual void Connect()
        {
            lock (_messageQueueLock)
            {
                if (_messageQueue == null)
                {
                    _messageQueue = GetQueue(AccessMode);

                    if (_messageQueue != null)
                    {
                        switch (AccessMode)
                        {
                            case AccessMode.Receive:
                                _messageQueue.MessageReadPropertyFilter = new MessagePropertyFilter { AppSpecific = true, Id = true, Body = true };
                                _messageQueue.Formatter = new StringMessageFormatter();
                                break;
                            case AccessMode.Send:
                                _messageQueue.DefaultPropertiesToSend = new DefaultPropertiesToSend { Recoverable = Durable };
                                break;
                            case AccessMode.SendAndReceive:
                                _messageQueue.MessageReadPropertyFilter = new MessagePropertyFilter { AppSpecific = true, Id = true, Body = true };
                                _messageQueue.Formatter = new StringMessageFormatter();
                                _messageQueue.DefaultPropertiesToSend = new DefaultPropertiesToSend { Recoverable = Durable };
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(AccessMode), AccessMode, "Unknown Access Mode");
                        }
                    }

                    Logger.Information("Connected to Message Queue {@Queue}", this);
                }

                if (_messageQueue == null)
                    throw new QueueMissingException(Name);
            }
        }

        /// <inheritdoc />
        public void Reconnect()
        {
            lock (_messageQueueLock)
            {
                if (_messageQueue != null)
                    Disconnect();

                Connect();
            }
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            lock (_messageQueueLock)
            {
                _messageQueue?.Close();
                _messageQueue?.Dispose();
                _messageQueue = null;

                Logger.Information("Disconnect from Message Queue {@Queue}", this);
            }
        }

        /// <inheritdoc />
        public void Send<T>(T message)
        {
            if (!AccessMode.In(AccessMode.Send, AccessMode.SendAndReceive))
                throw new AccessModeException(nameof(Send), AccessMode);

            try
            {
                SendInternal(message);
            }
            catch
            {
                Disconnect();

                SendInternal(message);
            }
        }

        private void SendInternal<T>(T message)
        {
            lock (_messageQueueLock)
            {
                Connect();

                var messageQueueTransaction = CreateTransaction();

                try
                {
                    messageQueueTransaction?.Begin();

                    SendMessage(message, messageQueueTransaction);

                    messageQueueTransaction?.Commit();
                }
                catch
                {
                    messageQueueTransaction?.Abort();

                    throw;
                }
                finally
                {
                    messageQueueTransaction?.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public async Task<ITransactionalMessage> ReceiveAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            try
            {
                return await ReceiveAsyncInternal(millisecondsTimeout, cancellationToken);
            }
            catch
            {
                Disconnect();

                return await ReceiveAsyncInternal(millisecondsTimeout, cancellationToken);
            }
        }

        private async Task<ITransactionalMessage> ReceiveAsyncInternal(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (!AccessMode.In(AccessMode.Receive, AccessMode.SendAndReceive))
                throw new AccessModeException(nameof(Send), AccessMode);

            var timeout = TimeSpan.FromMilliseconds(millisecondsTimeout);

            IAsyncResult asyncResult;
            
            Connect();

            lock (_messageQueueLock)
            {
                asyncResult = MessageQueueManager.BeginPeek(_messageQueue, timeout);
            }

            await WaitForMessageAsync(cancellationToken, asyncResult);

            Connect();

            return ReceiveMessage();
        }

        /// <inheritdoc />
        public ITransactionalMessage Receive(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            try
            {
                return ReceiveAsync(millisecondsTimeout, cancellationToken).Result;
            }
            catch (AggregateException exception)
            {
                if (exception.InnerException?.GetType() == typeof(TaskCanceledException))
                    return new TransactionalMessage();

                if (exception.InnerException != null)
                    throw exception.InnerException;

                throw;
            }
        }

        /// <inheritdoc />
        public T Receive<T>(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var message = Receive(millisecondsTimeout, cancellationToken);

            if (message != null)
            {
                if (message.Type != null && message.Type != typeof(T))
                    throw new InvalidMessageTypeReceivedException(Name, Private, message.Message, typeof(T), message.Type);

                if (message.Message is T res)
                {
                    message.Ack();

                    return res;
                }
            }
            else
                return default(T);

            return default(T);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~Queue()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose locale objects
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Disconnect();

                _disposed = true;
                _disconnectTimer.Dispose();
            }
        }

        private void Disconnect(object state)
        {
            Disconnect();
        }

        private void SendMessage<T>(T message, IMessageQueueTransaction messageQueueTransaction)
        {
            var jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var body = new QueueMessage { MessageBody = message.SerializeToJson(jsonSerializerSettings), MessageType = typeof(T) }.SerializeToJson(jsonSerializerSettings);

            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
            {
                var buffer = new byte[MaxMsmqMessageSize];
                var correlationId = "";
                var messageSize = memoryStream.Length;
                var numberOfChunks = Convert.ToInt32(Math.Ceiling((double)messageSize / buffer.Length));
                int bytes;
                var chunk = 0;

                Logger.Debug("Sending message in {Chunks} Chucks on {QueueName} ({Transactional})", numberOfChunks, Name, Transactional ? "Transactional" : "Non-Transactional");
                Logger.Debug($"Message: {body}");

                if (!Transactional && numberOfChunks > 1)
                    throw new MessageSizeException(memoryStream.Length, buffer.Length, Transactional);

                while ((bytes = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var queueMessage = new Message
                    {
                        AppSpecific = numberOfChunks,
                        CorrelationId = correlationId
                    };

                    queueMessage.BodyStream.Write(buffer, 0, bytes);

                    Logger.Debug("Sending message chunk ({Chunk}/{NumberOfChunks}) on {QueueName} {CorrelationId}", ++chunk, numberOfChunks, Name, correlationId);

                    MessageQueueManager.Send(_messageQueue, queueMessage, messageQueueTransaction?.Transaction);

                    correlationId = queueMessage.Id;
                }
            }
        }

        private ITransactionalMessage ReceiveMessage()
        {
            var messageQueueTransaction = CreateTransaction();

            messageQueueTransaction?.Begin();

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    lock (_messageQueue)
                    {
                        var message = MessageQueueManager.Receive(_messageQueue, TimeSpan.Zero, messageQueueTransaction?.Transaction);

                        Logger.Debug("Received message chunk ({Chunk}/{NumberOfChunks}) from {QueueName} {@Message}", 1, message?.AppSpecific ?? 0, Name, message);

                        var messageNumber = 0;

                        while (message != null && ++messageNumber <= message.AppSpecific)
                        {
                            message.BodyStream.CopyTo(memoryStream);

                            if (messageNumber < message.AppSpecific)
                            {
                                message = MessageQueueManager.ReceiveByCorrelationId(_messageQueue, message.Id, TimeSpan.Zero, messageQueueTransaction?.Transaction);

                                Logger.Debug("Received message chunk ({Chunk}/{NumberOfChunks}) from {QueueName} {@Message}", messageNumber + 1, message?.AppSpecific ?? 0, Name, message);
                            }
                        }
                    }

                    return CreateTransactionalMessage(memoryStream, messageQueueTransaction);
                }
            }
            catch (Exception)
            {
                messageQueueTransaction?.Abort();
                messageQueueTransaction?.Dispose();

                throw;
            }
        }

        private IMessageQueueTransaction CreateTransaction()
        {
            return Transactional ? _messageQueueTransactionFactory.Create() : null;
        }

        private static ITransactionalMessage CreateTransactionalMessage(Stream stream, IMessageQueueTransaction messageQueueTransaction)
        {
            stream.Position = 0;

            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                var queueMessage = streamReader.ReadToEnd().DeserializeFromJson<QueueMessage>();

                if (queueMessage == null)
                {
                    messageQueueTransaction?.Commit();
                    messageQueueTransaction?.Dispose();
                    messageQueueTransaction = null;
                }

                return new TransactionalMessage(queueMessage, messageQueueTransaction);
            }
        }

        private static async Task WaitForMessageAsync(CancellationToken cancellationToken, IAsyncResult asyncResult)
        {
            if (asyncResult?.AsyncWaitHandle != null)
                await asyncResult.AsyncWaitHandle.ToTask(cancellationToken);
        }
    }
}