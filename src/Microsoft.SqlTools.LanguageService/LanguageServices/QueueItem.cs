//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Class that stores the state of a binding queue request item
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Default allowance, in milliseconds, for the time an item may spend queued behind other
        /// items on the same key before <see cref="WaitForCompletionAsync"/> gives up on it.
        /// </summary>
        public const int DefaultQueueWaitBudgetMs = 30_000;

        private static long nextId;

        private int abandoned;

        /// <summary>
        /// QueueItem constructor
        /// </summary>
        public QueueItem()
        {
            this.Id = Interlocked.Increment(ref nextId);
            this.Lifetime = Stopwatch.StartNew();
            this.ItemProcessed = new ManualResetEvent(initialState: false);
        }

        /// <summary>
        /// Gets an identifier used to correlate this item in queue logs.
        /// </summary>
        internal long Id { get; }

        /// <summary>
        /// Gets elapsed time since the item was queued.
        /// </summary>
        internal Stopwatch Lifetime { get; }

        /// <summary>
        /// Gets or sets the queue item key
        /// </summary>
#pragma warning disable IDE0370 // Suppression is unnecessary — null! is required here to satisfy CS8618 for properties set by callers before use
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets or sets the bind operation callback method
        /// </summary>
        public Func<IBindingContext, CancellationToken, object?> BindOperation { get; set; } = null!;

        /// <summary>
        /// Gets or sets an asynchronous binding callback. An item uses either this callback or
        /// <see cref="BindOperation"/>.
        /// </summary>
        internal Func<IBindingContext, CancellationToken, Task<object?>>? BindOperationAsync { get; set; }

        /// <summary>
        /// Gets or sets the timeout operation to call if the bind operation doesn't finish within timeout period
        /// </summary>
#pragma warning restore IDE0370
        public Func<IBindingContext, object>? TimeoutOperation { get; set; }

        /// <summary>
        /// Gets or sets the operation to call if the bind operation encounters an unexpected exception.
        /// Supports returning an object in case of the exception occurring since in some cases we need to be
        /// tolerant of error cases and still return some value
        /// </summary>
#pragma warning disable IDE0370
        public Func<Exception, object> ErrorHandler { get; set; } = null!;
#pragma warning restore IDE0370

        /// <summary>
        /// Gets or sets an event to signal when this queue item has been processed
        /// </summary>
        public virtual ManualResetEvent ItemProcessed { get; set; }

        /// <summary>
        /// Gets or sets the result of the queued task
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Gets or sets whether the binding operation started. A false value after
        /// <see cref="ItemProcessed"/> is signaled means the item was not executed.
        /// </summary>
        internal bool WasExecuted { get; set; }

        /// <summary>
        /// Gets or sets whether the item completed through a lock or operation timeout.
        /// </summary>
        internal bool TimedOut { get; set; }

        /// <summary>
        /// Gets or sets the slow-operation threshold in milliseconds. When
        /// <see cref="HardTimeout"/> is not set, this is also the hard timeout.
        /// </summary>
        public int? BindingTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum time the caller waits for the binding operation.
        /// </summary>
        public int? HardTimeout { get; set; }

        /// <summary>
        /// Gets or sets the timeout for how long to wait for the binding lock
        /// </summary>
        public int? WaitForLockTimeout { get; set; }

        /// <summary>
        /// Gets whether the waiter gave up on this item before the queue processed it. The queue
        /// drops abandoned items without running their operation, so a backlog left behind by
        /// waiters that timed out drains immediately instead of being executed for nobody.
        /// </summary>
        public bool Abandoned => Volatile.Read(ref this.abandoned) != 0;

        /// <summary>
        /// Marks an item as no longer eligible to execute and releases every waiter. This is used
        /// both when a caller's bounded wait expires and when the queue explicitly discards work.
        /// </summary>
        internal void Abandon()
        {
            Interlocked.Exchange(ref this.abandoned, 1);
            this.ItemProcessed.Set();
        }

        /// <summary>
        /// Waits for completion with the item's timeout budget when the caller must remain synchronous.
        /// Prefer <see cref="WaitForCompletionAsync"/> in asynchronous callers.
        /// </summary>
        /// <returns>True if the queue processed the item, false if the wait timed out.</returns>
        public bool WaitForCompletion(int queueWaitBudgetMs = DefaultQueueWaitBudgetMs)
        {
            bool completed = this.ItemProcessed.WaitOne(GetCompletionTimeout(queueWaitBudgetMs));
            if (!completed)
            {
                this.Abandon();
            }

            return completed;
        }

        /// <summary>
        /// Waits for the queue to process this item without occupying a thread.
        /// </summary>
        /// <remarks>
        /// Callers must not block a thread pool thread on <see cref="ItemProcessed"/>. The queue
        /// dispatches items on the thread pool, so a burst of blocked waiters can exhaust the pool
        /// and leave nothing to signal them, which deadlocks the whole service. This method parks
        /// only a registered wait, and it is bounded: the item's own timeouts plus
        /// <paramref name="queueWaitBudgetMs"/> for time spent queued. On timeout the item is
        /// marked <see cref="Abandoned"/> so the queue skips it.
        /// </remarks>
        /// <param name="queueWaitBudgetMs">
        /// Allowance for time spent queued behind other items, or <see cref="Timeout.Infinite"/>
        /// to wait without bound.
        /// </param>
        /// <returns>True if the queue processed the item, false if the wait timed out.</returns>
        public async Task<bool> WaitForCompletionAsync(
            int queueWaitBudgetMs = DefaultQueueWaitBudgetMs,
            CancellationToken cancellationToken = default)
        {
            ManualResetEvent processed = this.ItemProcessed;
            if (processed.WaitOne(0))
            {
                return true;
            }

            bool completed;
            try
            {
                completed = await processed.WaitOneAsync(
                    GetCompletionTimeout(queueWaitBudgetMs),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                this.Abandon();
                throw;
            }
            if (!completed)
            {
                this.Abandon();
            }

            return completed;
        }

        private int GetCompletionTimeout(int queueWaitBudgetMs)
        {
            if (queueWaitBudgetMs == Timeout.Infinite)
            {
                return Timeout.Infinite;
            }

            long budget = (long)(this.WaitForLockTimeout ?? 0)
                + (this.HardTimeout ?? this.BindingTimeout ?? ConnectedBindingQueue.DefaultBindingTimeout)
                + queueWaitBudgetMs;
            return (int)Math.Min(budget, int.MaxValue);
        }

        /// <summary>
        /// Converts the result of the execution to type T
        /// </summary>
        public T? GetResultAsT<T>() where T : class
        {
            //var task = this.ResultsTask;
            return (this.Result != null)
                ? this.Result as T
                : null;
        }
    }
}
