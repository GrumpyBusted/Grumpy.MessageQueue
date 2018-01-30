using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common.Interfaces;
using Grumpy.Logging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Exceptions;
using Grumpy.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grumpy.MessageQueue
{
    /// <inheritdoc />
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class QueueHandler : IQueueHandler
    {
        private readonly ILogger _logger;
        private readonly IQueueFactory _queueFactory;
        private readonly ITaskFactory _taskFactory;
        private readonly List<ITask> _workTasks;
        private readonly Stopwatch _heartRateMonitor;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        private IQueue _queue;
        private Action<object, CancellationToken> _messageHandler;
        private Action<object, Exception> _errorHandlerAction;
        private Func<object, Exception, bool> _errorHandlerFunc;
        private Action _heartbeatHandler;
        private ITask _processTask;
        private int _heartRateMilliseconds;
        private int _numberOfException;
        private bool _multiThreadedHandler;
        private bool _messageReceived;
        private bool _syncMode;
        private bool _disposed;
        private string _queueName;

        /// <inheritdoc />
        public QueueHandler(ILogger logger, IQueueFactory queueFactory, ITaskFactory taskFactory)
        {
            _queueFactory = queueFactory;
            _taskFactory = taskFactory;
            _logger = logger;

            _workTasks = new List<ITask>();
            _heartRateMonitor = new Stopwatch();
            _messageReceived = true;
        }

        /// <inheritdoc />
        public bool Idle => _workTasks.Count == 0 && !_messageReceived && _numberOfException == 0 || (_cancellationTokenSource?.Token.IsCancellationRequested ?? true);

        /// <inheritdoc />
        public void Start(string queueName, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, Action<object, CancellationToken> messageHandler, Action<object, Exception> errorHandler, Action heartbeatHandler, int heartRateMilliseconds, bool multiThreadedHandler, bool syncMode, CancellationToken cancellationToken)
        {
            _errorHandlerFunc = null;
            _errorHandlerAction = errorHandler;

            Start(queueName, privateQueue, localeQueueMode, transactional, messageHandler, heartbeatHandler, heartRateMilliseconds, multiThreadedHandler, syncMode, cancellationToken);
        }

        /// <inheritdoc />
        public void Start(string queueName, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, Action<object, CancellationToken> messageHandler, Func<object, Exception, bool> errorHandler, Action heartbeatHandler, int heartRateMilliseconds, bool multiThreadedHandler, bool syncMode, CancellationToken cancellationToken)
        {
            _errorHandlerFunc = errorHandler;
            _errorHandlerAction = null;

            Start(queueName, privateQueue, localeQueueMode, transactional, messageHandler, heartbeatHandler, heartRateMilliseconds, multiThreadedHandler, syncMode, cancellationToken);
        }

        private void Start(string queueName, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, Action<object, CancellationToken> messageHandler, Action heartbeatHandler, int heartRateMilliseconds, bool multiThreadedHandler, bool syncMode, CancellationToken cancellationToken)
        {
            if (_cancellationTokenSource != null)
                throw new ArgumentException("Handler not stopped");

            if (heartRateMilliseconds <= 0 && heartbeatHandler != null)
                throw new ArgumentException("Invalid Heart Rate", nameof(heartRateMilliseconds));

            _queueName = queueName;
            _cancellationTokenSource = new CancellationTokenSource();
            _syncMode = syncMode;
            _queue = _queueFactory.CreateLocale(_queueName, privateQueue, localeQueueMode, transactional);
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _heartbeatHandler = heartbeatHandler;
            _heartRateMilliseconds = heartRateMilliseconds;
            _multiThreadedHandler = multiThreadedHandler;
            _cancellationTokenRegistration = cancellationToken.Register(Stop);

            if (_syncMode)
                Process();
            else
            {
                _processTask = _taskFactory.Create();
                _processTask.Start(Process, _cancellationTokenSource.Token);
            }

            _logger.Information($"Queue Handler started {_queueName}");
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (!_cancellationTokenSource?.IsCancellationRequested ?? false)
                _cancellationTokenSource?.Cancel();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            Parallel.ForEach(_workTasks, t =>
            {
                t.Wait();
                t.Dispose();
            });

            _processTask?.Dispose();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenRegistration.Dispose();
            _queue?.Dispose();

            _logger.Information($"Queue Handler stopped {_queueName}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose locale objects
        /// </summary>
        /// <param name="disposing">Disposing</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                    Stop();
            }
        }

        private void Process()
        {
            try
            {
                _heartRateMonitor?.Start();

                while (!(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
                {
                    if (_heartRateMonitor != null && _numberOfException == 0 && _heartRateMonitor.ElapsedMilliseconds > _heartRateMilliseconds)
                        Heartbeat();

                    var message = ReceiveMessage();

                    if (message != null)
                        AddTask(message);
                    else if (_numberOfException == 0)
                    {
                        CleanUpTasks();

                        if (_syncMode)
                            Stop();
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, $"Error processing queue handler {_queueName}");

                if (_syncMode)
                    throw new QueueHandlerProcessException(exception);

                _cancellationTokenSource?.Token.WaitHandle?.WaitOne(TimeSpan.FromMilliseconds(60000));
            }
        }

        private ITransactionalMessage ReceiveMessage()
        {
            try
            {
                var transactionalMessage = _queue.Receive(_heartRateMilliseconds - (int)(_heartRateMonitor?.ElapsedMilliseconds ?? 0), _cancellationTokenSource.Token);

                _numberOfException = 0;
                _messageReceived = transactionalMessage?.Message != null;

                if (_messageReceived)
                    return transactionalMessage;
            }
            catch (Exception)
            {
                _messageReceived = false;

                if (++_numberOfException >= 3)
                    throw;
            }

            return null;
        }

        private void Handler(object message)
        {
            if (message is ITransactionalMessage transactionalMessage)
                Handler(transactionalMessage);
            else
                _logger.Warning("Invalid message type received {QueueName} {Type} {@Message}", _queueName, message.GetType().FullName, message);
        }

        private void Handler(ITransactionalMessage message)
        {
            _logger.Debug("Handler called {QueueName} {@Message}", _queueName, message);

            try
            {
                _messageHandler(message.Message, _cancellationTokenSource.Token);

                message.Ack();
            }
            catch (Exception exception)
            {
                ErrorHandler(message, exception);
            }
        }

        private void ErrorHandler(ITransactionalMessage message, Exception exception)
        {
            _logger.Debug(exception, "Error Handler called {QueueName} {@Message}", _queueName, message);

            try
            {
                if (CallErrorHandler(message.Message, exception))
                    message.Ack();
                else
                    message.NAck();
            }
            catch(Exception ex)
            {
                _logger.Information(ex, "Exception in Error Handler {QueueName} {@Message} {@Exception}", _queueName, message, exception);

                message.NAck();
            }
        }

        private bool CallErrorHandler(object message, Exception exception)
        {
            if (_errorHandlerFunc != null)
                return _errorHandlerFunc(message, exception);

            if (_errorHandlerAction == null)
                return false;

            _errorHandlerAction(message, exception);

            return true;
        }

        private void Heartbeat()
        {
            _logger.Debug("Heartbeat Handler called {QueueName}", _queueName);

            try
            {
                _heartbeatHandler?.Invoke();
            }
            catch(Exception exception)
            {
                _logger.Warning(exception, $"Error in Heartbeat Handler {_queueName}");
            }

            _heartRateMonitor?.Restart();
        }

        private void AddTask(ITransactionalMessage message)
        {
            try
            {
                if (_syncMode)
                    Handler(message);
                else
                {
                    var task = _taskFactory.Create();
                    task.Start(Handler, message, _cancellationTokenSource.Token);

                    _workTasks.Add(task);

                    if (!_multiThreadedHandler)
                        task.Wait();
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Error starting handler task {QueueName} {@Message}", _queueName, message);

                ErrorHandler(message, new TaskCreationException(exception));
            }
        }

        private void CleanUpTasks()
        {
            if (_numberOfException == 0)
            {
                _logger.Debug("Cleaning up dead tasks {QueueName}", _queueName);

                try
                {
                    foreach (var task in _workTasks.Where(t => t.IsCompleted || t.IsFaulted))
                    {
                        if (task.IsFaulted)
                            CallErrorHandler(task.AsyncState, task.Exception);

                        task.Dispose();
                    }

                    _workTasks.RemoveAll(t => t.IsCompleted || t.IsFaulted);
                }
                catch(Exception exception)
                {
                    _logger.Warning(exception, "Error in cleaning up dead tasks {QueueName}", _queueName);
                }
            }
        }
    }
}