using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public abstract class Queue : IQueue
    {
        private const int MaxMsmqMessageSize = 4096000;
        private readonly IMessageQueueTransactionFactory _messageQueueTransactionFactory;
        private readonly object _messageQueueLock;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        /// <inheritdoc />
        protected Queue(ILogger logger, IMessageQueueManager messageQueueManager, IMessageQueueTransactionFactory messageQueueTransactionFactory, string name, bool privateQueue, bool durable, bool transactional, AccessMode accessMode)
        {
            if (name.Length > 124)
                throw new ArgumentException("Queue name too long", nameof(name));

            _messageQueueTransactionFactory = messageQueueTransactionFactory;

            Logger = logger;
            MessageQueueManager = messageQueueManager;
            Name = name;
            Private = privateQueue;
            Transactional = transactional;
            Durable = durable;
            AccessMode = accessMode;

            _messageQueueLock = new object();
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Message Queue Manager
        /// </summary>
        protected readonly IMessageQueueManager MessageQueueManager;

        /// <summary>
        /// Get the existing MSMQ Queue
        /// </summary>
        /// <param name="accessMode">Queue Access Mode</param>
        /// <returns>The MSMQ Queue</returns>
        protected abstract System.Messaging.MessageQueue GetQueue(AccessMode accessMode);

        /// <summary>
        /// The message queue
        /// </summary>
        protected System.Messaging.MessageQueue MessageQueue;

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

            lock (_messageQueueLock)
            {
                return CountInternal();
            }
        }

        private int CountInternal()
        {
            try
            {
                ConnectInternal();

                if (MessageQueue != null)
                {
                    var count = 0;

                    using (var messageEnumerator = MessageQueue.GetMessageEnumerator2())
                    {
                        while (messageEnumerator.MoveNext())
                            ++count;
                    }

                    return count;
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
        public void Connect()
        {
            lock (_messageQueueLock)
            {
                ConnectInternal();
            }
        }

        /// <summary>
        /// Internal Connection
        /// </summary>
        protected virtual void ConnectInternal()
        {
            if (MessageQueue == null)
            {
                MessageQueue = GetQueue(AccessMode);

                if (MessageQueue == null)
                    throw new UnableToGetQueueException(Name, AccessMode);

                switch (AccessMode)
                {
                    case AccessMode.Receive:
                        MessageQueue.MessageReadPropertyFilter = new MessagePropertyFilter { AppSpecific = true, Id = true, Body = true };
                        MessageQueue.Formatter = new StringMessageFormatter();
                        break;
                    case AccessMode.Send:
                        MessageQueue.DefaultPropertiesToSend = new DefaultPropertiesToSend { Recoverable = Durable };
                        break;
                    case AccessMode.SendAndReceive:
                        MessageQueue.MessageReadPropertyFilter = new MessagePropertyFilter { AppSpecific = true, Id = true, Body = true };
                        MessageQueue.Formatter = new StringMessageFormatter();
                        MessageQueue.DefaultPropertiesToSend = new DefaultPropertiesToSend { Recoverable = Durable };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(AccessMode), AccessMode, "Unknown Access Mode");
                }

                _stopwatch.Restart();

                Logger.Information("Connected to Message Queue {@Queue}", this);
            }
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            lock (_messageQueueLock)
            {
                DisconnectInternal();
            }
        }

        /// <summary>
        /// Disconnect from queue
        /// </summary>
        protected void DisconnectInternal()
        {
            MessageQueue?.Close();
            MessageQueue?.Dispose();
            MessageQueue = null;

            Logger.Information("Disconnect from Message Queue {@Queue}", this);
        }

        /// <inheritdoc />
        public void Reconnect()
        {
            lock (_messageQueueLock)
            {
                ReconnectInternal();
            }
        }

        /// <summary>
        /// Reconnect to queue
        /// </summary>
        private void ReconnectInternal()
        {
            DisconnectInternal();

            ConnectInternal();
        }

        private void AutoReconnect()
        {
            if (_stopwatch.ElapsedMilliseconds > 10000 && MessageQueue != null)
                DisconnectInternal();

            ConnectInternal();
        }

        /// <inheritdoc />
        public void Send<T>(T message)
        {
            if (!AccessMode.In(AccessMode.Send, AccessMode.SendAndReceive))
                throw new AccessModeException(nameof(Send), AccessMode);

            lock (_messageQueueLock)
            {
                try
                {
                    SendInternal(message);
                }
                catch (Exception exception)
                {
                    Logger.Warning(exception, "Error sending, retrying once {@Queue}", this);

                    DisconnectInternal();

                    SendInternal(message);
                }
            }
        }

        private void SendInternal<T>(T message)
        {
            AutoReconnect();

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

        /// <inheritdoc />
        public T Receive<T>(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            using (var message = Receive(millisecondsTimeout, cancellationToken))
            {
                if (message != null)
                {
                    if (message.Type != null && message.Type != typeof(T))
                    {
                        message.NAck();

                        throw new InvalidMessageTypeReceivedException(Name, Private, message.Message, typeof(T), message.Type);
                    }

                    if (message.Message is T res)
                    {
                        message.Ack();

                        return res;
                    }

                    message.NAck();
                }

                return default(T);
            }
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
                Logger.Information(exception, "Error receiving message {@Queue}", this);

                if (exception.InnerException?.GetType() == typeof(TaskCanceledException))
                    return new TransactionalMessage();

                if (exception.InnerException != null)
                    throw exception.InnerException;

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ITransactionalMessage> ReceiveAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (!AccessMode.In(AccessMode.Receive, AccessMode.SendAndReceive))
                throw new AccessModeException(nameof(Send), AccessMode);

            try
            {
                return await ReceiveAsyncInternal(millisecondsTimeout, cancellationToken);
            }
            catch (Exception exception)
            {
                Logger.Warning(exception, "Error receiving message, retrying once {@Queue}", this);

                DisconnectInternal();

                return await ReceiveAsyncInternal(millisecondsTimeout, cancellationToken);
            }
        }

        private async Task<ITransactionalMessage> ReceiveAsyncInternal(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            AutoReconnect();

            var asyncResult = MessageQueueManager.BeginPeek(MessageQueue, TimeSpan.FromMilliseconds(millisecondsTimeout));

            await WaitForMessageAsync(cancellationToken, asyncResult);

            lock (_messageQueueLock)
            {
                return ReceiveMessage();
            }
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
                    DisconnectInternal();

                _disposed = true;
            }
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

                Logger.Debug("Sending message in {Chunks} Chucks on {QueueName} {Transactional}", numberOfChunks, Name, Transactional);
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

                    MessageQueueManager.Send(MessageQueue, queueMessage, messageQueueTransaction?.Transaction);

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
                    var message = MessageQueueManager.Receive(MessageQueue, TimeSpan.Zero, messageQueueTransaction?.Transaction);

                    Logger.Debug("Received message chunk ({Chunk}/{NumberOfChunks}) from {QueueName} {@Message}", 1, message?.AppSpecific ?? 0, Name, message);

                    var messageNumber = 0;

                    while (message != null && ++messageNumber <= message.AppSpecific)
                    {
                        message.BodyStream.CopyTo(memoryStream);

                        if (messageNumber < message.AppSpecific)
                        {
                            message = MessageQueueManager.ReceiveByCorrelationId(MessageQueue, message.Id, TimeSpan.Zero, messageQueueTransaction?.Transaction);

                            Logger.Debug("Received message chunk ({Chunk}/{NumberOfChunks}) from {QueueName} {@Message}", messageNumber + 1, message?.AppSpecific ?? 0, Name, message);
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