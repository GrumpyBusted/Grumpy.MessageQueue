using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using Grumpy.Common.Extensions;
using Grumpy.Common.Threading;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.MessageQueue.Msmq.Extensions;
using Grumpy.MessageQueue.Msmq.Interfaces;

namespace Grumpy.MessageQueue.Msmq
{
    /// <inheritdoc />
    public class MessageQueueManager : IMessageQueueManager
    {
        private readonly object _lock;

        /// <inheritdoc />
        public MessageQueueManager()
        {
            _lock = new object();
        }

        private const string PrivatePrefix = @"private$\";
        private static string Path(string serverName, string name, bool privateQueue) => (Locale(serverName) ? "." : "FormatName:DIRECT=OS:" + serverName.ToLower()) + @"\" + Prefix(privateQueue) + name.ToLower();
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
            catch (MessageQueueException)
            {
                // Ignore
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
            catch (MessageQueueException)
            {
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
            catch (MessageQueueException)
            {
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
            catch (MessageQueueException)
            {
                return false;
            }
        }

        private static bool Locale(string serverName) => serverName.NullOrWhiteSpace() || serverName.In(".", Environment.GetEnvironmentVariable("COMPUTERNAME"));
    }
}
