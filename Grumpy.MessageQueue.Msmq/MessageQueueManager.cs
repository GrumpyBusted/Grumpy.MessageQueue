using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using Grumpy.Common.Extensions;
using Grumpy.Common.Threading;
using Grumpy.Logging;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Extensions;
using Grumpy.MessageQueue.Msmq.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public class MessageQueueManager : IMessageQueueManager
    {
        private readonly ILogger _logger;
        private readonly object _lock;
        private readonly string _serverName;

        /// <inheritdoc />
        public MessageQueueManager(ILogger logger)
        {
            _logger = logger;
            _lock = new object();
            _serverName = Environment.GetEnvironmentVariable("COMPUTERNAME");
        }

        private const string PrivatePrefix = @"private$\";
        private string Path(string serverName, string name, bool privateQueue) => (Locale(serverName) ? "" : "FormatName:DIRECT=OS:") + Name(name, privateQueue);

        private string Name(string name, bool privateQueue)
        {
            var res = _serverName.ToLower() + @"\" + Prefix(privateQueue) + name.ToLower();

            if (res.Length > 124)
                throw new QueueNameException(res, 124);

            return res;
        }
        
        private static string Prefix(bool privateQueue) => privateQueue ? PrivatePrefix.ToLower() : "";

        /// <inheritdoc />
        public System.Messaging.MessageQueue Create(string name, bool privateQueue, bool transactional)
        {
            try
            {
                var queue = System.Messaging.MessageQueue.Create(Path(".", name, privateQueue), transactional);

                TimerUtility.WaitForIt(() => Exists(".", name, privateQueue), privateQueue ? 1000 : 20000);

                return queue;
            }
            catch (Exception exception)
            {
                _logger.Information(exception, "Error Creating Message Queue {}");

                throw new QueueCreateException(name, privateQueue, exception);
            }
        }

        /// <inheritdoc />
        public void Delete(string name, bool privateQueue)
        {
            try
            {
                System.Messaging.MessageQueue.Delete(Path(".", name, privateQueue));
            }
            catch (MessageQueueException exception)
            {
                _logger.Information(exception, "Error Deleting Message Queue");
            }
        }

        /// <inheritdoc />
        public bool Exists(string name, bool privateQueue)
        {
            return System.Messaging.MessageQueue.Exists(Path(".", name, privateQueue));
        }

        /// <inheritdoc />
        public System.Messaging.MessageQueue Get(string serverName, string name, bool privateQueue, QueueAccessMode mode)
        {
            try
            {
                lock (_lock)
                {
                    System.Messaging.MessageQueue.ClearConnectionCache();

                    return Exists(serverName, name, privateQueue) ? new System.Messaging.MessageQueue(Path(serverName, name, privateQueue), mode) : null;
                }
            }
            catch (MessageQueueException exception)
            {
                _logger.Warning(exception, "Error Getting Message Queue");

                return null;
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> List(string serverName, bool privateQueue)
        {
            try
            {
                lock (_lock)
                {
                    System.Messaging.MessageQueue.ClearConnectionCache();
                    
                    return privateQueue ? System.Messaging.MessageQueue.GetPrivateQueuesByMachine(serverName).SelectNames(Prefix(true)) : System.Messaging.MessageQueue.GetPublicQueuesByMachine(serverName).SelectNames(Prefix(false));
                }
            }
            catch (MessageQueueException exception)
            {
                _logger.Warning(exception, "Error Listing Message Queues");

                return Enumerable.Empty<string>();
            }
        }

        /// <inheritdoc />
        public void Send(System.Messaging.MessageQueue messageQueue, Message message, System.Messaging.MessageQueueTransaction messageQueueTransaction)
        {
            try
            {
                if (messageQueueTransaction == null)
                    messageQueue.Send(message);
                else
                    messageQueue.Send(message, messageQueueTransaction);
            }
            catch (Exception exception)
            {
                _logger.Debug(exception, "Error Sending Message to Message Queues");

                throw new MessageQueueSendException(messageQueue, message, exception);
            }
        }

        /// <inheritdoc />
        public Message Receive(System.Messaging.MessageQueue messageQueue, TimeSpan timeout, System.Messaging.MessageQueueTransaction messageQueueTransaction)
        {
            try
            {
                return messageQueueTransaction == null ? messageQueue?.Receive(timeout) : messageQueue?.Receive(timeout, messageQueueTransaction);
            }
            catch (Exception exception)
            {
                if (exception is MessageQueueException messageQueueException && messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                _logger.Debug(exception, "Error Receiving Message from Message Queues");

                throw new MessageQueueReceiveException(messageQueue, timeout, exception);
            }
        }

        /// <inheritdoc />
        public Message ReceiveByCorrelationId(System.Messaging.MessageQueue messageQueue, string correlationId, TimeSpan timeout, System.Messaging.MessageQueueTransaction messageQueueTransaction)
        {
            try
            {
                return messageQueueTransaction == null ? messageQueue?.ReceiveByCorrelationId(correlationId, timeout) : messageQueue.ReceiveByCorrelationId(correlationId, timeout, messageQueueTransaction);
            }
            catch (Exception exception)
            {
                if (exception is MessageQueueException messageQueueException && messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                _logger.Debug(exception, "Error Receiving by Correlation Id Message from Message Queues");

                throw new MessageQueueReceiveException(messageQueue, correlationId, timeout, exception);
            }
        }

        /// <inheritdoc />
        public IAsyncResult BeginPeek(System.Messaging.MessageQueue messageQueue, TimeSpan timeout)
        {
            try
            {
                return messageQueue.BeginPeek(timeout, MessageQueueTransactionType.Automatic);
            }
            catch (Exception exception)
            {
                _logger.Debug(exception, "Error Peeking Head of Message Queues");

                throw new MessageQueuePeekException("Begin", messageQueue, timeout, exception);
            }
        }

        /// <inheritdoc />
        public Message EndPeek(System.Messaging.MessageQueue messageQueue, IAsyncResult asyncResult)
        {
            try
            {
                return messageQueue.EndPeek(asyncResult);
            }
            catch (Exception exception)
            {
                _logger.Debug(exception, "Error ending Peek on Message Queues");

                if (exception is MessageQueueException messageQueueException && messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                throw new MessageQueuePeekException("End", messageQueue, asyncResult, exception);
            }
        }

        private bool Exists(string serverName, string name, bool privateQueue)
        {
            try
            {
                return Locale(serverName) ? System.Messaging.MessageQueue.Exists(Path(".", name, privateQueue)) : List(serverName, privateQueue).Any(n => n == name);
            }
            catch (MessageQueueException exception)
            {
                _logger.Debug(exception, "Error Checking if queue exist");

                return false;
            }
        }

        private bool Locale(string serverName)
        {
            return serverName.NullOrWhiteSpace() ||serverName.In(".", _serverName);
        }
    }
}
