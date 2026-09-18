//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
#pragma warning disable CS8632

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Main class for the Binding Queue
    /// </summary>
    public class BindingQueue<T> : IDisposable where T : IBindingContext, new()
    {
        internal const int QueueThreadStackSize = 5 * 1024 * 1024;

        private CancellationTokenSource processQueueCancelToken = null;

        private ManualResetEvent itemQueuedEvent = new ManualResetEvent(initialState: false);

        private object bindingQueueLock = new();

        private LinkedList<QueueItem> bindingQueue = new LinkedList<QueueItem>();

        private object bindingContextLock = new();

        private Task queueProcessorTask;

        private bool disposed;

        public delegate void UnhandledExceptionDelegate(string connectionKey, Exception ex);

        public event UnhandledExceptionDelegate OnUnhandledException;

        /// <summary>
        /// Map from context keys to binding context instances
        /// Internal for testing purposes only
        /// </summary>
        internal ConcurrentDictionary<string, IBindingContext> BindingContextMap { get; set; }

        internal ConcurrentDictionary<IBindingContext, Task> BindingContextTasks { get; set; } = new();

        /// <summary>
        /// Scheduler used to dispatch queue items and run their binding operations. Defaults to
        /// the thread pool. Tests substitute a scheduler with a fixed, small number of threads to
        /// prove that waiting on a queue item never consumes the threads the queue needs.
        /// </summary>
        internal TaskScheduler DispatchScheduler { get; set; } = TaskScheduler.Default;

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
            int? hardTimeout = null)
        {
            QueueItem queueItem = new QueueItem()
            {
                Key = key,
                BindOperation = bindOperation,
                TimeoutOperation = timeoutOperation,
                ErrorHandler = errorHandler,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout,
                HardTimeout = hardTimeout
            };

            return this.EnqueueOrAbandon(queueItem);
        }

        /// <summary>
        /// Queues an asynchronous binding operation so waiting for dedicated-thread work does not
        /// retain one of the scheduler threads needed to dispatch other items.
        /// </summary>
        public virtual QueueItem QueueBindingOperationAsync(
            string key,
            Func<IBindingContext, CancellationToken, Task<object?>> bindOperation,
            Func<IBindingContext, object>? timeoutOperation = null,
            Func<Exception, object>? errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null,
            int? hardTimeout = null)
        {
            QueueItem queueItem = new QueueItem
            {
                Key = key,
                BindOperationAsync = bindOperation,
                TimeoutOperation = timeoutOperation,
                ErrorHandler = errorHandler,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout,
                HardTimeout = hardTimeout,
            };

            return this.EnqueueOrAbandon(queueItem);
        }

        /// <summary>
        /// Adds an item to the queue and wakes the processor, unless the queue has been disposed.
        /// </summary>
        /// <remarks>
        /// A disposed queue has no processor left to dispatch the item, and
        /// <see cref="itemQueuedEvent"/> may already be torn down. Abandoning the item releases its
        /// waiters immediately rather than blocking them on work that can never run.
        /// </remarks>
        private QueueItem EnqueueOrAbandon(QueueItem queueItem)
        {
            bool queued;
            lock (this.bindingQueueLock)
            {
                queued = !this.disposed;
                if (queued)
                {
                    this.bindingQueue.AddLast(queueItem);

                    // Signalled under the lock that guards disposal and the processor's Reset, so a
                    // concurrent Dispose cannot tear the handle down between the add and the set.
                    this.itemQueuedEvent.Set();
                }
            }

            if (queued)
            {
                Logger.Verbose($"Binding queue item {queueItem.Id} queued for key '{queueItem.Key}'");
            }
            else
            {
                Logger.Verbose($"Binding queue item {queueItem.Id} abandoned for key '{queueItem.Key}'; the binding queue is disposed");
                queueItem.Abandon();
            }

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
                    this.BindingContextTasks.TryAdd(bindingContext, Task.CompletedTask);
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

                        if (queueItem.Abandoned)
                        {
                            // Nobody is waiting for this item any more, so running it would only
                            // delay the items behind it.
                            Logger.Verbose($"Binding queue item {queueItem.Id} was abandoned by its waiter after {queueItem.Lifetime.ElapsedMilliseconds} ms queued; skipping");
                            queueItem.ItemProcessed.Set();
                            continue;
                        }

                        try
                        {
                            IBindingContext bindingContext;
                            Task bindingContextTask;

                            // Context replacement/removal uses the same lock. Keep context lookup,
                            // task-chain lookup, and chain replacement atomic so RemoveBindingContext
                            // cannot delete the task entry between these steps and kill this consumer.
                            lock (this.bindingContextLock)
                            {
                                bindingContext = GetOrCreateBindingContext(queueItem.Key);
                                if (bindingContext == null)
                                {
                                    queueItem.ItemProcessed.Set();
                                    continue;
                                }

                                if (!this.BindingContextTasks.TryGetValue(bindingContext, out bindingContextTask))
                                {
                                    // Recover a consistent chain if external test setup or an older
                                    // lifecycle path supplied a context without its initial task.
                                    bindingContextTask = Task.CompletedTask;
                                }

                                // Chain dispatches per context so that an item waits for the previous
                                // item's dispatch, including its wait for the context's binding lock.
                                this.BindingContextTasks[bindingContext] = bindingContextTask.ContinueWith(
                                    task => DispatchQueueItemAsync(bindingContext, queueItem),
                                    CancellationToken.None,
                                    TaskContinuationOptions.RunContinuationsAsynchronously,
                                    this.DispatchScheduler).Unwrap();
                            }
                        }
                        catch (Exception ex)
                        {
                            // A malformed item or lifecycle race must fail this item, not terminate
                            // the dedicated consumer and strand every later queue item.
                            Logger.Error($"Binding queue failed to schedule item {queueItem.Id}: {ex}");
                            try
                            {
                                if (queueItem.ErrorHandler != null)
                                {
                                    queueItem.Result = queueItem.ErrorHandler(ex);
                                }
                            }
                            catch (Exception errorHandlerException)
                            {
                                Logger.Error($"Binding queue error handler failed for item {queueItem.Id}: {errorHandlerException}");
                            }
                            finally
                            {
                                queueItem.ItemProcessed.Set();
                            }
                        }

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

        /// <summary>
        /// Acquires the context's binding lock, starts the item's operation, and hands the item
        /// to the timeout monitor. Completes once the operation has been started or the item has
        /// been resolved without running, not when the operation finishes.
        /// </summary>
        private async Task DispatchQueueItemAsync(IBindingContext bindingContext, QueueItem queueItem)
        {
            if (queueItem.Abandoned)
            {
                // The waiter gave up while this item sat behind earlier items for the same context.
                Logger.Verbose($"Binding queue item {queueItem.Id} was abandoned by its waiter after {queueItem.Lifetime.ElapsedMilliseconds} ms; skipping dispatch");
                queueItem.ItemProcessed.Set();
                return;
            }

            bool lockTaken = false;
            Stopwatch bindingLockStopwatch = Stopwatch.StartNew();
            try
            {
                // prefer the queue item binding item, otherwise use the context default timeout - timeout is in milliseconds
                int bindTimeoutInMs = queueItem.BindingTimeout ?? bindingContext.BindingTimeout;
                int hardTimeoutInMs = queueItem.HardTimeout ?? bindTimeoutInMs;
                int waitForLockTimeoutInMs = queueItem.WaitForLockTimeout ?? 0;
                Logger.Verbose($"Binding queue item {queueItem.Id} dispatch started after {queueItem.Lifetime.ElapsedMilliseconds} ms queued; lock wait timeout: {waitForLockTimeoutInMs} ms, slow threshold: {bindTimeoutInMs} ms, hard timeout: {hardTimeoutInMs} ms");

                // Handle the case a previous binding operation is still running. The wait holds
                // no thread: dispatch runs on the thread pool, and a blocked dispatch thread is
                // one fewer thread for the operations that would release this lock.
                if (!await bindingContext.BindingLock.WaitOneAsync(waitForLockTimeoutInMs))
                {
                    try
                    {
                        Logger.Warning($"Binding queue item {queueItem.Id} timed out after {bindingLockStopwatch.ElapsedMilliseconds} ms waiting for BindingLock");
                        queueItem.TimedOut = true;
                        queueItem.Result = queueItem.TimeoutOperation != null
                            ? queueItem.TimeoutOperation(bindingContext)
                            : null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Exception running binding queue lock timeout handler: " + ex.ToString());
                    }
                    finally
                    {
                        Logger.Verbose($"Binding queue item {queueItem.Id} signalling ItemProcessed after lock-wait timeout at {queueItem.Lifetime.ElapsedMilliseconds} ms");
                        queueItem.ItemProcessed.Set();
                    }

                    // This item never acquired BindingLock, so stop after completing its timeout path.
                    return;
                }

                bindingContext.BindingLock.Reset();

                lockTaken = true;
                bindingLockStopwatch.Restart();
                Logger.Verbose($"Binding queue item {queueItem.Id} acquired BindingLock after {queueItem.Lifetime.ElapsedMilliseconds} ms total");

                // execute the binding operation
                object result = null;
                CancellationTokenSource cancelToken = new CancellationTokenSource();

                // run the operation in a separate thread
                var bindTask = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        queueItem.WasExecuted = true;
                        Logger.Verbose($"Binding queue item {queueItem.Id} operation started at {queueItem.Lifetime.ElapsedMilliseconds} ms");
                        result = queueItem.BindOperationAsync != null
                            ? await queueItem.BindOperationAsync(bindingContext, cancelToken.Token).ConfigureAwait(false)
                            : queueItem.BindOperation(bindingContext, cancelToken.Token);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Unexpected exception on the binding queue: " + ex.ToString());
                        if (queueItem.ErrorHandler != null)
                        {
                            try
                            {
                                result = queueItem.ErrorHandler(ex);
                            }
                            catch (Exception ex2)
                            {
                                Logger.Error("Unexpected exception in binding queue error handler: " + ex2.ToString());
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
                    finally
                    {
                        Logger.Verbose($"Binding queue item {queueItem.Id} operation finished at {queueItem.Lifetime.ElapsedMilliseconds} ms; cancellation requested: {cancelToken.IsCancellationRequested}");
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                this.DispatchScheduler).Unwrap();

                // From here on the async monitor owns the lock and the item's completion. It
                // enforces the timeouts with timers rather than by blocking a thread, so timeouts
                // still fire when every dispatch thread is busy.
                _ = CompleteQueueItemAsync(
                    bindingContext,
                    queueItem,
                    bindTask,
                    cancelToken,
                    bindTimeoutInMs,
                    hardTimeoutInMs,
                    bindingLockStopwatch,
                    () => result);
                lockTaken = false;
            }
            catch (Exception ex)
            {
                // catch and log any exceptions raised in the binding calls
                // set item processed to avoid deadlocks
                Logger.Error("Binding queue threw exception " + ex.ToString());
                // set item processed to avoid deadlocks
                if (lockTaken)
                {
                    bindingContext.BindingLock.Set();
                    Logger.Verbose($"Binding queue item {queueItem.Id} released BindingLock after dispatch failure; lock held for {bindingLockStopwatch.ElapsedMilliseconds} ms");
                }
                Logger.Verbose($"Binding queue item {queueItem.Id} signalling ItemProcessed after dispatch failure at {queueItem.Lifetime.ElapsedMilliseconds} ms");
                queueItem.ItemProcessed.Set();
            }
        }

        /// <summary>
        /// Waits for a dispatched binding operation, applying the slow-operation threshold and the
        /// hard timeout, then releases the context's binding lock and signals the item.
        /// </summary>
        private async Task CompleteQueueItemAsync(
            IBindingContext bindingContext,
            QueueItem queueItem,
            Task bindTask,
            CancellationTokenSource cancelToken,
            int bindTimeoutInMs,
            int hardTimeoutInMs,
            Stopwatch bindingLockStopwatch,
            Func<object> getResult)
        {
            bool releaseLock = true;
            try
            {
                int slowWaitInMs = Math.Min(bindTimeoutInMs, hardTimeoutInMs);

                // The first timeout is only a slow-operation threshold when a later hard timeout is set.
                if (await CompletesWithinAsync(bindTask, slowWaitInMs).ConfigureAwait(false))
                {
                    queueItem.Result = getResult();
                }
                else
                {
                    if (slowWaitInMs == bindTimeoutInMs)
                    {
                        Logger.Warning($"Binding queue item {queueItem.Id} exceeded the {bindTimeoutInMs} ms slow-operation threshold at {queueItem.Lifetime.ElapsedMilliseconds} ms");
                    }

                    int remainingWaitInMs = hardTimeoutInMs - slowWaitInMs;
                    if (remainingWaitInMs > 0 && await CompletesWithinAsync(bindTask, remainingWaitInMs).ConfigureAwait(false))
                    {
                        queueItem.Result = getResult();
                    }
                    else
                    {
                        Logger.Warning($"Binding queue item {queueItem.Id} reached its {hardTimeoutInMs} ms hard timeout at {queueItem.Lifetime.ElapsedMilliseconds} ms");
                        queueItem.TimedOut = true;

                        // Keep this context unavailable until the old operation actually exits.
                        _ = bindTask.ContinueWith(
                            task =>
                            {
                                bindingContext.BindingLock.Set();
                                Logger.Verbose($"Binding queue item {queueItem.Id} released BindingLock after its timed-out operation ended; lock held for {bindingLockStopwatch.ElapsedMilliseconds} ms");
                            },
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                        releaseLock = false;

                        Logger.Verbose($"Binding queue item {queueItem.Id} requesting cancellation at {queueItem.Lifetime.ElapsedMilliseconds} ms");
                        cancelToken.Cancel();
                        if (queueItem.TimeoutOperation != null)
                        {
                            queueItem.Result = queueItem.TimeoutOperation(bindingContext);
                        }

                        _ = bindTask.ContinueWithOnFaulted(t => Logger.Error("Binding queue threw exception " + t.Exception.ToString()));
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
                if (releaseLock)
                {
                    bindingContext.BindingLock.Set();
                    Logger.Verbose($"Binding queue item {queueItem.Id} released BindingLock after holding it for {bindingLockStopwatch.ElapsedMilliseconds} ms");
                }
                Logger.Verbose($"Binding queue item {queueItem.Id} signalling ItemProcessed at {queueItem.Lifetime.ElapsedMilliseconds} ms; timed out: {queueItem.TimedOut}, executed: {queueItem.WasExecuted}");
                queueItem.ItemProcessed.Set();
            }
        }

        /// <summary>
        /// Waits up to <paramref name="millisecondsTimeout"/> for <paramref name="task"/> to
        /// finish, using a timer rather than a blocked thread.
        /// </summary>
        private static async Task<bool> CompletesWithinAsync(Task task, int millisecondsTimeout)
        {
            if (task.IsCompleted)
            {
                return true;
            }

            if (millisecondsTimeout == Timeout.Infinite)
            {
                await task.ConfigureAwait(false);
                return true;
            }

            if (millisecondsTimeout <= 0)
            {
                return false;
            }

            using var delayCancellation = new CancellationTokenSource();
            Task delay = Task.Delay(millisecondsTimeout, delayCancellation.Token);
            Task first = await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (first == task)
            {
                delayCancellation.Cancel();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear queued items
        /// </summary>
        public void ClearQueuedItems()
        {
            QueueItem[] removedItems;
            lock (this.bindingQueueLock)
            {
                removedItems = this.bindingQueue.ToArray();
                this.bindingQueue.Clear();
            }

            // Clearing a linked list is not completion. Release callers whose work was discarded;
            // otherwise parameterless legacy waits can remain blocked forever.
            foreach (QueueItem queueItem in removedItems)
            {
                Logger.Verbose($"Binding queue item {queueItem.Id} was removed before dispatch");
                queueItem.Abandon();
            }
        }

        public void Dispose()
        {
            // Set under the queueing lock so an in-flight enqueue either lands before disposal
            // (and is abandoned by ClearQueuedItems below) or observes the flag and abandons itself.
            lock (this.bindingQueueLock)
            {
                if (this.disposed)
                {
                    return;
                }
                this.disposed = true;
            }

            // Work still in the linked list has no consumer after disposal. Complete it before
            // tearing down the wait handles so callers cannot remain stranded.
            this.ClearQueuedItems();

            bool processorStopped = true;
            if (this.processQueueCancelToken != null)
            {
                this.processQueueCancelToken.Cancel();
                try
                {
                    processorStopped = this.queueProcessorTask == null
                        || this.queueProcessorTask.Wait(5_000);
                }
                catch (AggregateException ex)
                {
                    processorStopped = true;
                    Logger.Error($"Binding queue processor faulted during disposal: {ex}");
                }

                if (processorStopped)
                {
                    this.processQueueCancelToken.Dispose();
                }
            }

            if (processorStopped && itemQueuedEvent != null)
            {
                lock (this.bindingQueueLock)
                {
                    itemQueuedEvent.Dispose();
                }
            }
            else if (!processorStopped)
            {
                // A live processor may still be waiting on these handles. Leaving them allocated
                // is safer than disposing under that thread; the processor logs make this visible.
                Logger.Warning("Binding queue processor did not stop within 5000 ms during disposal.");
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
