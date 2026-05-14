//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
#pragma warning disable CS8632

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for the Binding Queue
    /// </summary>
    public class BindingQueue<T> : IDisposable where T : IBindingContext, new()
    {
        internal const int QueueThreadStackSize = 5 * 1024 * 1024;

        private const string DisconnectedBindingContextKey = "disconnected_binding_context";

        private CancellationTokenSource processQueueCancelToken = null;

        private ManualResetEvent itemQueuedEvent = new ManualResetEvent(initialState: false);

        private Lock bindingQueueLock = new();

        private LinkedList<QueueItem> bindingQueue = new LinkedList<QueueItem>();

        private Lock bindingContextLock = new();

        private Task queueProcessorTask;

        public delegate void UnhandledExceptionDelegate(string connectionKey, Exception ex);

        public event UnhandledExceptionDelegate OnUnhandledException;

        /// <summary>
        /// Map from context keys to binding context instances
        /// Internal for testing purposes only
        /// </summary>
        internal ConcurrentDictionary<string, IBindingContext> BindingContextMap { get; set; }

        internal ConcurrentDictionary<string, Task> BindingContextTasks { get; set; } = new();

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
            int? waitForLockTimeout = null)
        {
            QueueItem queueItem = new QueueItem()
            {
                Key = key,
                BindOperation = bindOperation,
                TimeoutOperation = timeoutOperation,
                ErrorHandler = errorHandler,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout
            };

            lock (this.bindingQueueLock)
            {
                this.bindingQueue.AddLast(queueItem);
            }

            this.itemQueuedEvent.Set();

            return queueItem;
        }

        /// <summary>
        /// Checks if a particular binding context is connected or not
        /// </summary>
        /// <param name="key"></param>
        public bool IsBindingContextConnected(string key)
        {
            key = NormalizeBindingContextKey(key);
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
        protected virtual IBindingContext GetOrCreateBindingContext(string key)
        {
            key = NormalizeBindingContextKey(key);

            lock (this.bindingContextLock)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext existingContext))
                {
                    return existingContext;
                }
            }

            IBindingContext bindingContext = CreateBindingContext(key);

            lock (this.bindingContextLock)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext existingContext))
                {
                    DisconnectBindingContextAsync(bindingContext, disconnectImmediately: true);
                    return existingContext;
                }

                this.BindingContextMap.TryAdd(key, bindingContext);
                this.BindingContextTasks.TryAdd(key, Task.CompletedTask);
                return bindingContext;
            }
        }

        protected virtual IBindingContext CreateBindingContext(string key)
        {
            return new T();
        }

        protected IEnumerable<IBindingContext> GetBindingContexts(string keyPrefix)
        {
            keyPrefix = NormalizeBindingContextKey(keyPrefix);

            lock (this.bindingContextLock)
            {
                return this.BindingContextMap.Where(x => x.Key.StartsWith(keyPrefix)).Select(v => v.Value).ToList();
            }
        }

        /// <summary>
        /// Checks if a binding context already exists for the provided context key
        /// </summary>
        protected bool BindingContextExists(string key)
        {
            key = NormalizeBindingContextKey(key);
            lock (this.bindingContextLock)
            {
                return this.BindingContextMap.ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove the binding queue entry
        /// </summary>
        protected void RemoveBindingContext(string key)
            => RemoveBindingContext(key, removeTaskChain: true, disconnectImmediately: true);

        protected void RemoveBindingContext(string key, bool removeTaskChain, bool disconnectImmediately)
        {
            key = NormalizeBindingContextKey(key);
            lock (this.bindingContextLock)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext? bindingContext))
                {
                    DisconnectBindingContextAsync(bindingContext, disconnectImmediately);

                    // remove key from the map
                    this.BindingContextMap.TryRemove(key, out _);
                    if (removeTaskChain)
                    {
                        this.BindingContextTasks.TryRemove(key, out _);
                    }
                }
            }
        }

        private bool RemoveBindingContextIfCurrent(
            string key,
            IBindingContext bindingContext,
            bool removeTaskChain,
            bool disconnectImmediately)
        {
            key = NormalizeBindingContextKey(key);
            lock (this.bindingContextLock)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext? currentContext) &&
                    ReferenceEquals(currentContext, bindingContext))
                {
                    DisconnectBindingContextAsync(bindingContext, disconnectImmediately);
                    this.BindingContextMap.TryRemove(key, out _);
                    if (removeTaskChain)
                    {
                        this.BindingContextTasks.TryRemove(key, out _);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool IsCurrentBindingContext(string key, IBindingContext bindingContext)
        {
            key = NormalizeBindingContextKey(key);
            lock (this.bindingContextLock)
            {
                return this.BindingContextMap.TryGetValue(key, out IBindingContext? currentContext) &&
                       ReferenceEquals(currentContext, bindingContext);
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

                        string bindingContextKey = NormalizeBindingContextKey(queueItem.Key);
                        Task bindingContextTask = this.BindingContextTasks.GetOrAdd(bindingContextKey, Task.CompletedTask);

                        // Run in the binding context task in case this task has to wait for a previous binding operation.
                        // Resolve the context inside the continuation so a timed-out operation can evict the old SMO
                        // objects before the next queued item starts.
                        this.BindingContextTasks[bindingContextKey] = bindingContextTask.ContinueWith(
                            task =>
                            {
                                IBindingContext bindingContext = GetOrCreateBindingContext(bindingContextKey);
                                if (bindingContext == null)
                                {
                                    queueItem.ItemProcessed.Set();
                                    return;
                                }

                                DispatchQueueItem(bindingContextKey, bindingContext, queueItem);
                            }
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

        private void DispatchQueueItem(string bindingContextKey, IBindingContext bindingContext, QueueItem queueItem)
        {
            bool lockTaken = false;
            CancellationTokenSource cancelToken = null;
            Task bindTask = null;
            try
            {
                // prefer the queue item binding item, otherwise use the context default timeout - timeout is in milliseconds
                int bindTimeoutInMs = queueItem.BindingTimeout ?? bindingContext.BindingTimeout;

                // handle the case a previous binding operation is still running
                if (!bindingContext.BindingLock.WaitOne(queueItem.WaitForLockTimeout ?? 0))
                {
                    Logger.Warning("Binding queue operation timed out waiting for previous operation to finish");
                    queueItem.Result = RunTimeoutOperation(bindingContext, queueItem, "lock wait timeout");
                    queueItem.ItemProcessed.Set();
                    return;
                }

                bindingContext.BindingLock.Reset();
                lockTaken = true;

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
                        Logger.Error("Unexpected exception on the binding queue: " + ex.ToString());
                        result = RunErrorHandler(queueItem, ex);
                        if (ShouldRemoveBindingContextOnException(ex) && IsCurrentBindingContext(bindingContextKey, bindingContext))
                        {
                            if (this.OnUnhandledException != null)
                            {
                                this.OnUnhandledException(bindingContextKey, ex);
                            }
                            Logger.Warning($"Removing binding context '{bindingContextKey}' after binding exception of type {ex.GetType().Name}");
                            RemoveBindingContextIfCurrent(
                                bindingContextKey,
                                bindingContext,
                                removeTaskChain: false,
                                disconnectImmediately: true);
                        }
                    }
                });

                try
                {
                    // check if the binding task completed within the binding timeout
                    if (bindTask.Wait(bindTimeoutInMs))
                    {
                        queueItem.Result = result;
                    }
                    else
                    {
                        Logger.Warning($"Binding queue operation for '{bindingContextKey}' timed out after {bindTimeoutInMs} ms");
                        cancelToken.Cancel();
                        queueItem.Result = RunTimeoutOperation(bindingContext, queueItem, "bind timeout");

                        if (ShouldEvictBindingContextOnTimeout(bindingContext, queueItem))
                        {
                            bool removed = RemoveBindingContextIfCurrent(
                                bindingContextKey,
                                bindingContext,
                                removeTaskChain: false,
                                disconnectImmediately: false);
                            if (removed)
                            {
                                Logger.Warning($"Evicted binding context '{bindingContextKey}' after bind timeout");
                            }
                            ScheduleOrphanedContextCleanup(bindingContext, bindTask, cancelToken);
                            cancelToken = null;
                        }
                        else
                        {
                            bindTask.Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Binding queue task completion threw exception " + ex.ToString());
                }
                finally
                {
                    // set item processed to avoid deadlocks
                    if (lockTaken)
                    {
                        bindingContext.BindingLock.Set();
                    }
                    queueItem.ItemProcessed.Set();
                    cancelToken?.Dispose();
                }
            }
            catch (Exception ex)
            {
                // catch and log any exceptions raised in the binding calls
                // set item processed to avoid deadlocks
                Logger.Error("Binding queue threw exception " + ex.ToString());
                if (lockTaken)
                {
                    bindingContext.BindingLock.Set();
                }
                queueItem.ItemProcessed.Set();
                cancelToken?.Dispose();
            }
        }

        protected virtual bool ShouldEvictBindingContextOnTimeout(IBindingContext bindingContext, QueueItem queueItem)
        {
            return true;
        }

        private object RunTimeoutOperation(IBindingContext bindingContext, QueueItem queueItem, string reason)
        {
            if (queueItem.TimeoutOperation == null)
            {
                return null;
            }

            try
            {
                Logger.Verbose($"Running binding queue timeout handler due to {reason}");
                return queueItem.TimeoutOperation(bindingContext);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception running binding queue timeout handler: " + ex.ToString());
                return null;
            }
        }

        private object RunErrorHandler(QueueItem queueItem, Exception ex)
        {
            if (queueItem.ErrorHandler == null)
            {
                return null;
            }

            try
            {
                return queueItem.ErrorHandler(ex);
            }
            catch (Exception ex2)
            {
                Logger.Error("Unexpected exception in binding queue error handler: " + ex2.ToString());
                return null;
            }
        }

        private void ScheduleOrphanedContextCleanup(
            IBindingContext bindingContext,
            Task bindTask,
            CancellationTokenSource cancelToken)
        {
            bindTask.ContinueWith(
                task =>
                {
                    try
                    {
                        if (task.IsFaulted && task.Exception != null)
                        {
                            Logger.Error("Binding queue threw exception after timeout " + task.Exception);
                        }
                        DisconnectBindingContextAsync(bindingContext, disconnectImmediately: true);
                    }
                    finally
                    {
                        cancelToken.Dispose();
                    }
                },
                TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        private static string NormalizeBindingContextKey(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? DisconnectedBindingContextKey
                : key;
        }

        private void DisconnectBindingContextAsync(IBindingContext bindingContext, bool disconnectImmediately)
        {
            if (bindingContext?.ServerConnection == null || !bindingContext.ServerConnection.IsOpen)
            {
                return;
            }

            if (!disconnectImmediately)
            {
                return;
            }

            // Disconnecting can take some time so run it in a separate task so that it doesn't block removal.
            Task.Run(() =>
            {
                try
                {
                    bindingContext.ServerConnection.Cancel();
                    bindingContext.ServerConnection.Disconnect();
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception disconnecting binding context: " + ex.ToString());
                }
            });
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

        private bool ShouldRemoveBindingContextOnException(Exception ex)
        {
            return IsExceptionOfType(ex, typeof(SqlException)) ||
                   IsExceptionOfType(ex, typeof(SocketException)) ||
                   IsExceptionOfType(ex, typeof(ConnectionException)) ||
                   IsExceptionOfType(ex, typeof(SqlParserInternalBinderError));
        }

        private bool IsExceptionOfType(Exception ex, Type t)
        {
            return ex.GetType() == t || (ex.InnerException != null && ex.InnerException.GetType() == t);
        }
    }
}
