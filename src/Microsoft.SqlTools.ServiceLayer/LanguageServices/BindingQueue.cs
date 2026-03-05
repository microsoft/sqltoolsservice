//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for the Binding Queue
    /// </summary>
    public class BindingQueue<T> : IDisposable where T : IBindingContext, new()
    {
        internal const int QueueThreadStackSize = 5 * 1024 * 1024;
        private const int CanceledBindTaskWaitTimeoutMs = 1000;

        private CancellationTokenSource processQueueCancelToken = null;

        private ManualResetEvent itemQueuedEvent = new ManualResetEvent(initialState: false);

        private object bindingQueueLock = new object();

        private LinkedList<QueueItem> bindingQueue = new LinkedList<QueueItem>();

        private object bindingContextLock = new object();

        private Task queueProcessorTask;

        public delegate void UnhandledExceptionDelegate(string connectionKey, Exception ex);

        public event UnhandledExceptionDelegate OnUnhandledException;

        /// <summary>
        /// Map from context keys to binding context instances
        /// Internal for testing purposes only
        /// </summary>
        internal ConcurrentDictionary<string, IBindingContext> BindingContextMap { get; set; }

        internal ConcurrentDictionary<IBindingContext, Task> BindingContextTasks { get; set; } = new();

        /// <summary>
        /// Constructor for a binding queue instance
        /// </summary>
        public BindingQueue()
        {
            this.BindingContextMap = new();
            this.StartQueueProcessor();
        }

        public void StartQueueProcessor()
        {
            this.queueProcessorTask = StartQueueProcessorAsync();
        }

        /// <summary>
        /// Stops the binding queue by sending cancellation request
        /// </summary>
        /// <param name="timeout"></param>
        public bool StopQueueProcessor(int timeout)
        {
            this.processQueueCancelToken.Cancel();
            return this.queueProcessorTask.Wait(timeout);
        }

        /// <summary>
        /// Returns true if cancellation is requested
        /// </summary>
        /// <returns></returns>
        public bool IsCancelRequested
        {
            get
            {
                return this.processQueueCancelToken.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Queue a binding request item
        /// </summary>
        public virtual QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, object?> bindOperation,
            Func<IBindingContext, object>? timeoutOperation = null,
            Func<Exception, object>? errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null,
            CancellationToken callerCancellation = default)
        {
            QueueItem queueItem = new QueueItem()
            {
                Key = key,
                BindOperation = bindOperation,
                TimeoutOperation = timeoutOperation,
                ErrorHandler = errorHandler,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout,
                CallerCancellation = callerCancellation
            };

            int queueDepth;
            lock (this.bindingQueueLock)
            {
                this.bindingQueue.AddLast(queueItem);
                queueDepth = this.bindingQueue.Count;
            }

            Logger.Verbose($"BindingQueue: queued operation (hasKey={!string.IsNullOrWhiteSpace(key)}, bindingTimeoutMs={bindingTimeout?.ToString() ?? "default"}, waitForLockTimeoutMs={waitForLockTimeout?.ToString() ?? "default"}, queueDepth={queueDepth}, callerCancelled={callerCancellation.IsCancellationRequested}).");

            this.itemQueuedEvent.Set();

            return queueItem;
        }

        /// <summary>
        /// Checks if a particular binding context is connected or not
        /// </summary>
        /// <param name="key"></param>
        public bool IsBindingContextConnected(string key)
        {
            lock (this.bindingContextLock)
            {
                IBindingContext context;
                if (this.BindingContextMap.TryGetValue(key, out context))
                {
                    return context.IsConnected;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets or creates a binding context for the provided context key
        /// </summary>
        /// <param name="key"></param>
        protected IBindingContext GetOrCreateBindingContext(string key)
        {
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "disconnected_binding_context";
            }

            lock (this.bindingContextLock)
            {
                if (!this.BindingContextMap.ContainsKey(key))
                {
                    var bindingContext = new T();
                    this.BindingContextMap.TryAdd(key, bindingContext);
                    this.BindingContextTasks.TryAdd(bindingContext, Task.Run(() => null));
                }

                return this.BindingContextMap[key];
            }
        }

        protected IEnumerable<IBindingContext> GetBindingContexts(string keyPrefix)
        {
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                keyPrefix = "disconnected_binding_context";
            }

            lock (this.bindingContextLock)
            {
                return this.BindingContextMap.Where(x => x.Key.StartsWith(keyPrefix)).Select(v => v.Value);
            }
        }

        /// <summary>
        /// Checks if a binding context already exists for the provided context key
        /// </summary>
        protected bool BindingContextExists(string key)
        {
            lock (this.bindingContextLock)
            {
                return this.BindingContextMap.ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove the binding queue entry
        /// </summary>
        protected void RemoveBindingContext(string key)
        {
            lock (this.bindingContextLock)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext? bindingContext))
                {
                    // disconnect existing connection
                    if (bindingContext.ServerConnection != null && bindingContext.ServerConnection.IsOpen)
                    {
                        // Disconnecting can take some time so run it in a separate task so that it doesn't block removal
                        Task.Run(() =>
                        {
                            bindingContext.ServerConnection.Cancel();
                            bindingContext.ServerConnection.Disconnect();
                        });
                    }

                    // remove key from the map
                    this.BindingContextMap.TryRemove(key, out _);
                    this.BindingContextTasks.TryRemove(bindingContext, out _);
                }
            }
        }

        public bool HasPendingQueueItems
        {
            get
            {
                lock (this.bindingQueueLock)
                {
                    return this.bindingQueue.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets the next pending queue item
        /// </summary>
        private QueueItem GetNextQueueItem()
        {
            lock (this.bindingQueueLock)
            {
                if (this.bindingQueue.Count == 0)
                {
                    return null;
                }

                QueueItem queueItem = this.bindingQueue.First.Value;
                this.bindingQueue.RemoveFirst();
                return queueItem;
            }
        }

        /// <summary>
        /// Starts the queue processing thread
        /// </summary>        
        private Task StartQueueProcessorAsync()
        {
            if (this.processQueueCancelToken != null)
            {
                this.processQueueCancelToken.Dispose();
            }
            this.processQueueCancelToken = new CancellationTokenSource();

            return Task.Factory.StartNew(
                ProcessQueue,
                this.processQueueCancelToken.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        /// The core queue processing method
        /// </summary>
        /// <param name="state"></param>
        private void ProcessQueue()
        {
            CancellationToken token = this.processQueueCancelToken.Token;
            WaitHandle[] waitHandles = new WaitHandle[2]
            {
                this.itemQueuedEvent,
                token.WaitHandle
            };

            while (true)
            {
                // wait for with an item to be queued or the cancellation request
                WaitHandle.WaitAny(waitHandles);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // dispatch all pending queue items
                    while (this.HasPendingQueueItems)
                    {
                        QueueItem queueItem = GetNextQueueItem();
                        if (queueItem == null)
                        {
                            continue;
                        }

                        // Skip items that the caller has already cancelled (e.g. superseded completion requests)
                        if (queueItem.CallerCancellation.IsCancellationRequested)
                        {
                            Logger.Verbose("BindingQueue: skipping canceled queue item before dispatch.");
                            queueItem.SignalCompleted();
                            continue;
                        }

                        IBindingContext bindingContext = GetOrCreateBindingContext(queueItem.Key);
                        if (bindingContext == null)
                        {
                            Logger.Warning("BindingQueue: no binding context available for queued item; signaling completion.");
                            queueItem.SignalCompleted();
                            continue;
                        }

                        var bindingContextTask = this.BindingContextTasks[bindingContext];

                        // Run in the binding context task in case this task has to wait for a previous binding operation
                        this.BindingContextTasks[bindingContext] = bindingContextTask.ContinueWith(
                            task => DispatchQueueItem(bindingContext, queueItem)
                        , TaskContinuationOptions.RunContinuationsAsynchronously);

                        // if a queue processing cancellation was requested then exit the loop
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    lock (this.bindingQueueLock)
                    {
                        // verify the binding queue is still empty
                        if (this.bindingQueue.Count == 0)
                        {
                            // reset the item queued event since we've processed all the pending items
                            this.itemQueuedEvent.Reset();
                        }
                    }
                }
            }
        }

        private void DispatchQueueItem(IBindingContext bindingContext, QueueItem queueItem)
        {
            bool releaseBindingLock = false;
            CancellationTokenSource cancelToken = null;
            Task bindTask = null;
            bool bindTimedOut = false;
            try
            {
                // If the caller already cancelled this request, skip it entirely
                if (queueItem.CallerCancellation.IsCancellationRequested)
                {
                    Logger.Verbose("BindingQueue: dispatch skipped because caller cancellation was already requested.");
                    return;
                }

                // prefer the queue item binding item, otherwise use the context default timeout - timeout is in milliseconds
                int bindTimeoutInMs = queueItem.BindingTimeout ?? bindingContext.BindingTimeout;
                int waitForLockTimeoutInMs = queueItem.WaitForLockTimeout ?? bindTimeoutInMs;

                Logger.Verbose($"BindingQueue: dispatch starting (bindingTimeoutMs={bindTimeoutInMs}, waitForLockTimeoutMs={waitForLockTimeoutInMs}).");

                // handle the case a previous binding operation is still running
                if (!bindingContext.BindingLock.WaitOne(waitForLockTimeoutInMs))
                {
                    try
                    {
                        Logger.Warning($"BindingQueue: operation timed out waiting for previous operation to finish after {waitForLockTimeoutInMs} ms.");
                        queueItem.Result = queueItem.TimeoutOperation != null
                            ? queueItem.TimeoutOperation(bindingContext)
                            : null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("BindingQueue: exception running lock-timeout handler: " + ex.ToString());
                    }

                    return;
                }

                bindingContext.BindingLock.Reset();
                releaseBindingLock = true;

                // execute the binding operation
                object result = null;
                cancelToken = new CancellationTokenSource();

                // run the operation in a separate thread
                bindTask = Task.Run(() =>
                {
                    try
                    {
                        result = queueItem.BindOperation(
                                            bindingContext,
                                            cancelToken.Token);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("BindingQueue: unexpected exception in bind operation: " + ex.ToString());
                        if (queueItem.ErrorHandler != null)
                        {
                            try
                            {
                                result = queueItem.ErrorHandler(ex);
                            }
                            catch (Exception ex2)
                            {
                                Logger.Error("BindingQueue: unexpected exception in error handler: " + ex2.ToString());
                            }
                        }
                        if (IsExceptionOfType(ex, typeof(SqlException)) || IsExceptionOfType(ex, typeof(SocketException)))
                        {
                            if (this.OnUnhandledException != null)
                            {
                                this.OnUnhandledException(queueItem.Key, ex);
                            }
                            RemoveBindingContext(queueItem.Key);
                        }
                    }
                });

                try
                {
                    // check if the binding task completed within the binding timeout
                    if (bindTask.Wait(bindTimeoutInMs))
                    {
                        queueItem.Result = result;
                        Logger.Verbose($"BindingQueue: dispatch completed within timeout. ResultType={queueItem.Result?.GetType().Name ?? "null"}.");
                    }
                    else
                    {
                        bindTimedOut = true;
                        cancelToken.Cancel();
                        Logger.Warning($"BindingQueue: dispatch timed out after {bindTimeoutInMs} ms; cancellation requested.");

                        // if the task didn't complete then call the timeout callback
                        if (queueItem.TimeoutOperation != null)
                        {
                            queueItem.Result = queueItem.TimeoutOperation(bindingContext);
                        }

                        bindTask.ContinueWithOnFaulted(t => Logger.Error("BindingQueue: bind task faulted after timeout: " + t.Exception.ToString()));

                        // Give the task a short chance to complete, but don't block the queue indefinitely
                        if (!bindTask.Wait(CanceledBindTaskWaitTimeoutMs))
                        {
                            Logger.Warning($"BindingQueue: task did not complete within the post-cancel grace period ({CanceledBindTaskWaitTimeoutMs} ms). Removing binding context.");
                            RemoveBindingContext(queueItem.Key);

                            // Keep the old context lock unavailable while the timed-out task is still running.
                            // Already-scheduled continuations captured with this context should fail fast on lock wait
                            // instead of executing concurrently against a stale context.
                            releaseBindingLock = false;

                            // Defer CTS disposal until task completion to avoid races with late cancellation checks.
                            CancellationTokenSource ctsToDispose = cancelToken;
                            bindTask.ContinueWith(
                                _ => ctsToDispose.Dispose(),
                                TaskScheduler.Default);
                            cancelToken = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("BindingQueue: exception while waiting for bind task completion: " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                // catch and log any exceptions raised in the binding calls
                // set item processed to avoid deadlocks 
                Logger.Error("BindingQueue: unhandled dispatch exception: " + ex.ToString());
            }
            finally
            {
                cancelToken?.Dispose();

                // release the binding lock if we took it
                if (releaseBindingLock)
                {
                    bindingContext.BindingLock.Set();
                }

                // always signal completion to avoid deadlocks
                queueItem.SignalCompleted();

                if (bindTimedOut)
                {
                    Logger.Verbose("BindingQueue: completion signaled after timeout path.");
                }
            }
        }

        /// <summary>
        /// Clear queued items
        /// </summary>
        public void ClearQueuedItems()
        {
            lock (this.bindingQueueLock)
            {
                if (this.bindingQueue.Count > 0)
                {
                    this.bindingQueue.Clear();
                }
            }
        }

        public void Dispose()
        {
            if (this.processQueueCancelToken != null)
            {
                this.processQueueCancelToken.Dispose();
            }

            if (itemQueuedEvent != null)
            {
                itemQueuedEvent.Dispose();
            }

            if (this.BindingContextMap != null)
            {
                foreach (var item in this.BindingContextMap)
                {
                    if (item.Value != null && item.Value.ServerConnection != null && item.Value.ServerConnection.SqlConnectionObject != null)
                    {
                        item.Value.ServerConnection.SqlConnectionObject.Close();
                    }
                }
            }
        }

        private bool IsExceptionOfType(Exception ex, Type t)
        {
            return ex.GetType() == t || (ex.InnerException != null && ex.InnerException.GetType() == t);
        }
    }
}
