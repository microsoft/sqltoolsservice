//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Runs binding operations against binding contexts that are keyed by connection.
    /// Operations on the same context run one at a time, because a context's connection and
    /// binder are not thread safe. Operations on different contexts run concurrently.
    /// Waiting for a context or for an operation never holds a thread.
    /// </summary>
    public class BindingQueue<T> : IDisposable where T : IBindingContext, new()
    {
        internal const string DisconnectedBindingContextKey = "disconnected_binding_context";

        // Cancelled by ClearQueuedItems and Dispose to abandon items still waiting for their context.
        // ClearQueuedItems replaces it; once the queue is disposed it stays cancelled.
        private volatile CancellationTokenSource pendingItemsCancellation = new CancellationTokenSource();

        private readonly object pendingItemsLock = new object();

        private volatile bool disposed;

        public delegate void UnhandledExceptionDelegate(string connectionKey, Exception ex);

        /// <summary>
        /// Raised when a binding operation fails with a connection error. The context that
        /// failed has already been removed, so the next operation for the key starts afresh.
        /// </summary>
        public event UnhandledExceptionDelegate? OnUnhandledException;

        /// <summary>
        /// Map from context keys to binding context instances.
        /// Internal for testing purposes only.
        /// </summary>
        internal ConcurrentDictionary<string, IBindingContext> BindingContextMap { get; } = new ConcurrentDictionary<string, IBindingContext>();

        /// <summary>
        /// Queues a synchronous binding operation. See the asynchronous overload for details.
        /// </summary>
        public Task<QueueItem> QueueBindingOperationAsync(
            string key,
            Func<IBindingContext, CancellationToken, object?> bindOperation,
            Func<IBindingContext, object?>? timeoutOperation = null,
            Func<Exception, object?>? errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null,
            int? hardTimeout = null)
        {
            return QueueBindingOperationAsync(
                key,
                bindOperationAsync: (context, cancellationToken) => Task.FromResult(bindOperation(context, cancellationToken)),
                timeoutOperation,
                errorHandler,
                bindingTimeout,
                waitForLockTimeout,
                hardTimeout);
        }

        /// <summary>
        /// Queues a binding operation for the context with the given key. The returned task
        /// completes when the item is done and never faults: check <see cref="QueueItem.Result"/>,
        /// <see cref="QueueItem.TimedOut"/> and <see cref="QueueItem.WasExecuted"/>.
        /// </summary>
        /// <param name="bindOperation">Runs on the thread pool while the item holds the context.</param>
        /// <param name="timeoutOperation">Supplies the result when the item times out.</param>
        /// <param name="errorHandler">Supplies the result when the operation throws.</param>
        /// <param name="bindingTimeout">
        /// Milliseconds after which the operation is logged as slow. It is also the hard timeout
        /// when <paramref name="hardTimeout"/> is not set. Defaults to the context's timeout.
        /// </param>
        /// <param name="waitForLockTimeout">
        /// Milliseconds to wait for an earlier operation on the context to finish. Defaults to 0,
        /// which times the item out at once if the context is busy.
        /// </param>
        /// <param name="hardTimeout">
        /// Milliseconds after which the caller gets the timeout result and the operation is asked
        /// to cancel. The context stays unavailable until the operation actually ends.
        /// </param>
        public virtual Task<QueueItem> QueueBindingOperationAsync(
            string key,
            Func<IBindingContext, CancellationToken, Task<object?>> bindOperationAsync,
            Func<IBindingContext, object?>? timeoutOperation = null,
            Func<Exception, object?>? errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null,
            int? hardTimeout = null)
        {
            return RunAsync(new QueueItem
            {
                Key = key,
                BindOperation = bindOperationAsync,
                TimeoutOperation = timeoutOperation,
                ErrorHandler = errorHandler,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout,
                HardTimeout = hardTimeout
            });
        }

        /// <summary>
        /// Checks if a particular binding context is connected or not
        /// </summary>
        public bool IsBindingContextConnected(string key)
        {
            return this.BindingContextMap.TryGetValue(key, out IBindingContext? context) && context.IsConnected;
        }

        /// <summary>
        /// Completes every item still waiting for its context without running it or its timeout
        /// handler. Items that are
        /// already running are not affected.
        /// </summary>
        public void ClearQueuedItems()
        {
            CancellationTokenSource cleared;
            lock (this.pendingItemsLock)
            {
                if (this.disposed)
                {
                    return;
                }

                cleared = this.pendingItemsCancellation;
                this.pendingItemsCancellation = new CancellationTokenSource();
            }

            // Not disposed: an item that read the old source just before the swap may still pass
            // its token to a wait, which would throw if the source were disposed.
            cleared.Cancel();
        }

        /// <summary>
        /// Gets or creates a binding context for the provided context key
        /// </summary>
        protected IBindingContext GetOrCreateBindingContext(string key)
        {
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(key))
            {
                key = DisconnectedBindingContextKey;
            }

            return this.BindingContextMap.GetOrAdd(key, _ => new T());
        }

        protected IReadOnlyList<IBindingContext> GetBindingContexts(string keyPrefix)
        {
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                keyPrefix = DisconnectedBindingContextKey;
            }

            return this.BindingContextMap
                .Where(entry => entry.Key.StartsWith(keyPrefix, StringComparison.Ordinal))
                .Select(entry => entry.Value)
                .ToList();
        }

        /// <summary>
        /// Checks if a binding context already exists for the provided context key
        /// </summary>
        protected bool BindingContextExists(string key)
        {
            return this.BindingContextMap.ContainsKey(key);
        }

        /// <summary>
        /// Publishes a context under a key, closing the connection of any context it replaces.
        /// Operations already running keep the context they started with.
        /// </summary>
        protected void ReplaceBindingContext(string key, IBindingContext context)
        {
            while (true)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext? existing))
                {
                    if (this.BindingContextMap.TryUpdate(key, context, existing))
                    {
                        CloseConnectionWhenIdle(existing);
                        return;
                    }
                }
                else if (this.BindingContextMap.TryAdd(key, context))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Removes the binding context for a key and closes its connection.
        /// </summary>
        protected void RemoveBindingContext(string key)
        {
            if (this.BindingContextMap.TryRemove(key, out IBindingContext? context))
            {
                CloseConnectionWhenIdle(context);
            }
        }

        /// <summary>
        /// Removes a context only if it is still the one published for the key, so a failed
        /// operation on a context that has since been replaced does not remove its replacement.
        /// </summary>
        private bool RemoveBindingContext(string key, IBindingContext context)
        {
            var entry = new KeyValuePair<string, IBindingContext>(key, context);
            if (((ICollection<KeyValuePair<string, IBindingContext>>)this.BindingContextMap).Remove(entry))
            {
                CloseConnectionWhenIdle(context);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels whatever the context's connection is running, then disconnects it once the
        /// operation using the context, if any, has ended.
        /// </summary>
        private static void CloseConnectionWhenIdle(IBindingContext context)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (context.ServerConnection is { IsOpen: true } running)
                    {
                        running.Cancel();
                    }

                    await RunWhenIdleAsync(context, Timeout.Infinite, connection =>
                    {
                        if (connection.IsOpen)
                        {
                            connection.Disconnect();
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to close a removed binding context connection: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Runs an action on a context's connection once no operation is using the context, and
        /// does nothing if the context stays busy for the whole timeout or has no connection.
        /// </summary>
        protected static async Task RunWhenIdleAsync(IBindingContext context, int millisecondsTimeout, Action<ServerConnection> action)
        {
            if (!await context.BindingLock.WaitAsync(millisecondsTimeout).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                // Read under the lock: a context that is still being populated has no connection
                // until its population releases the lock.
                ServerConnection? connection = context.ServerConnection;
                if (connection != null)
                {
                    action(connection);
                }
            }
            finally
            {
                context.BindingLock.Release();
            }
        }

        private async Task<QueueItem> RunAsync(QueueItem item)
        {
            try
            {
                IBindingContext context = GetOrCreateBindingContext(item.Key);
                int bindTimeout = item.BindingTimeout ?? context.BindingTimeout;
                int hardTimeout = item.HardTimeout ?? bindTimeout;
                int lockTimeout = item.WaitForLockTimeout ?? 0;

                bool acquired;
                try
                {
                    acquired = await context.BindingLock.WaitAsync(lockTimeout, this.pendingItemsCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return Abandon(item);
                }

                // Disposal can start between reading the token above and taking a free context.
                if (acquired && this.disposed)
                {
                    context.BindingLock.Release();
                    return Abandon(item);
                }

                if (!acquired)
                {
                    Logger.Warning($"Binding queue item {item.Id} for key '{item.Key}' gave up after {item.Lifetime.ElapsedMilliseconds} ms waiting for its binding context");
                    item.TimedOut = true;
                    item.Result = RunTimeoutOperation(item, context);
                    return item;
                }

                // The operation releases the context when it actually ends, which can be after
                // the caller has been given the timeout result.
                var operationCancellation = new CancellationTokenSource();
                Task<object?> operation = ExecuteAndReleaseAsync(item, context, operationCancellation.Token);

                int slowThreshold = Math.Min(bindTimeout, hardTimeout);
                bool completed = await CompletesWithinAsync(operation, slowThreshold).ConfigureAwait(false);
                if (!completed && hardTimeout > slowThreshold)
                {
                    Logger.Warning($"Binding queue item {item.Id} exceeded the {bindTimeout} ms slow-operation threshold");
                    completed = await CompletesWithinAsync(operation, hardTimeout - slowThreshold).ConfigureAwait(false);
                }

                if (completed)
                {
                    operationCancellation.Dispose();
                    item.Result = await operation.ConfigureAwait(false);
                }
                else
                {
                    Logger.Warning($"Binding queue item {item.Id} reached its {hardTimeout} ms hard timeout; cancelling it");
                    operationCancellation.Cancel();
                    _ = operation.ContinueWith(
                        _ => operationCancellation.Dispose(),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    item.TimedOut = true;
                    item.Result = RunTimeoutOperation(item, context);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Binding queue item {item.Id} failed unexpectedly: {ex}");
            }

            Logger.Verbose($"Binding queue item {item.Id} finished after {item.Lifetime.ElapsedMilliseconds} ms; executed: {item.WasExecuted}, timed out: {item.TimedOut}");
            return item;
        }

        /// <summary>
        /// Runs the operation on the thread pool and releases the context when it ends. Never faults.
        /// </summary>
        private async Task<object?> ExecuteAndReleaseAsync(QueueItem item, IBindingContext context, CancellationToken cancellationToken)
        {
            try
            {
                item.WasExecuted = true;
                return await Task.Run(() => item.BindOperation(context, cancellationToken)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected exception on the binding queue for key '{item.Key}': {ex}");
                object? result = null;
                if (item.ErrorHandler != null)
                {
                    try
                    {
                        result = item.ErrorHandler(ex);
                    }
                    catch (Exception handlerException)
                    {
                        Logger.Error($"Unexpected exception in binding queue error handler: {handlerException}");
                    }
                }

                if (IsConnectionException(ex) && RemoveBindingContext(item.Key, context))
                {
                    RaiseUnhandledException(item.Key, ex);
                }

                return result;
            }
            finally
            {
                context.BindingLock.Release();
            }
        }

        /// <summary>
        /// Completes an item that <see cref="ClearQueuedItems"/> or <see cref="Dispose"/> stopped
        /// before it ran
        /// </summary>
        private static QueueItem Abandon(QueueItem item)
        {
            Logger.Verbose($"Binding queue item {item.Id} for key '{item.Key}' was abandoned before it ran");
            item.TimedOut = true;
            return item;
        }

        private static object? RunTimeoutOperation(QueueItem item, IBindingContext context)
        {
            try
            {
                return item.TimeoutOperation?.Invoke(context);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception running binding queue timeout handler: {ex}");
                return null;
            }
        }

        private void RaiseUnhandledException(string key, Exception ex)
        {
            try
            {
                this.OnUnhandledException?.Invoke(key, ex);
            }
            catch (Exception handlerException)
            {
                Logger.Error($"Unexpected exception in binding queue unhandled exception handler: {handlerException}");
            }
        }

        /// <summary>
        /// Waits for a task with a timeout, without holding a thread
        /// </summary>
        private static async Task<bool> CompletesWithinAsync(Task task, int millisecondsTimeout)
        {
            if (task.IsCompleted)
            {
                return true;
            }

            using var delayCancellation = new CancellationTokenSource();
            Task finished = await Task.WhenAny(task, Task.Delay(millisecondsTimeout, delayCancellation.Token)).ConfigureAwait(false);
            delayCancellation.Cancel();
            return finished == task;
        }

        private static bool IsConnectionException(Exception ex)
        {
            return ex is SqlException || ex is SocketException
                || ex.InnerException is SqlException || ex.InnerException is SocketException;
        }

        /// <summary>
        /// Completes waiting items without running them, stops new items from running, and
        /// closes the contexts' connections. Operations already running are not stopped.
        /// </summary>
        public void Dispose()
        {
            lock (this.pendingItemsLock)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
            }

            this.pendingItemsCancellation.Cancel();

            foreach (IBindingContext context in this.BindingContextMap.Values)
            {
                try
                {
                    context.ServerConnection?.SqlConnectionObject?.Close();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to close a binding context connection: {ex.Message}");
                }
            }
        }
    }
}
