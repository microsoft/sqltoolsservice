//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using NUnit.Framework;

namespace Microsoft.SqlTools.LanguageService.UnitTests.LanguageServices
{
    /// <summary>
    /// Reproduces the thread pool starvation behind microsoft/vscode-mssql#22920 and verifies
    /// the fix.
    ///
    /// The binding queue dispatches items, runs their operations, and signals their completion
    /// on the thread pool. The language service used to wait for an item by blocking the thread
    /// it was on, which was also a pool thread. A diagnostics sweep over a large SQL project
    /// created thousands of such waiters at once; once they held every pool thread, the queue had
    /// no thread left to signal any of them, and the whole service, including the stdin reader,
    /// stopped. These tests shrink the pool to a couple of dedicated threads so the same sequence
    /// plays out deterministically in milliseconds.
    /// </summary>
    public class BindingQueueStarvationTests
    {
        /// <summary>
        /// Pauses the queue precisely between resolving a binding context and looking up that
        /// context's task chain. This makes the removal race deterministic instead of relying on
        /// timing or repeatedly hammering the queue.
        /// </summary>
        private sealed class PausingBindingContext : TestBindingContext
        {
            private int hashCalls;

            internal static ManualResetEventSlim AtTaskLookup { get; } = new ManualResetEventSlim(false);

            internal static ManualResetEventSlim ResumeTaskLookup { get; } = new ManualResetEventSlim(false);

            internal static void ResetGates()
            {
                AtTaskLookup.Reset();
                ResumeTaskLookup.Reset();
            }

            public override int GetHashCode()
            {
                // The first hash inserts this instance into BindingContextTasks. The second hash
                // is ProcessQueue's task-chain lookup after GetOrCreateBindingContext released its
                // lock, which is the vulnerable window this test needs.
                if (Interlocked.Increment(ref this.hashCalls) == 2 && !ResumeTaskLookup.IsSet)
                {
                    AtTaskLookup.Set();
                    if (!ResumeTaskLookup.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("Timed out waiting to resume the task-chain lookup.");
                    }
                }

                return base.GetHashCode();
            }
        }

        private sealed class RemovableBindingQueue : BindingQueue<PausingBindingContext>
        {
            internal void Remove(string key) => this.RemoveBindingContext(key);
        }

        /// <summary>
        /// A task scheduler backed by a fixed number of dedicated threads, standing in for a
        /// thread pool that cannot grow. Never inlines work, so a blocked thread stays blocked.
        /// </summary>
        private sealed class FixedThreadScheduler : TaskScheduler, IDisposable
        {
            private readonly BlockingCollection<Task> pending = new BlockingCollection<Task>();
            private readonly Thread[] threads;

            public FixedThreadScheduler(int threadCount)
            {
                this.threads = new Thread[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    this.threads[i] = new Thread(() =>
                    {
                        foreach (Task task in this.pending.GetConsumingEnumerable())
                        {
                            this.TryExecuteTask(task);
                        }
                    })
                    {
                        IsBackground = true,
                        Name = $"{nameof(FixedThreadScheduler)}-{i}"
                    };
                    this.threads[i].Start();
                }
            }

            public override int MaximumConcurrencyLevel => this.threads.Length;

            protected override void QueueTask(Task task) => this.pending.Add(task);

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

            protected override IEnumerable<Task> GetScheduledTasks() => this.pending.ToArray();

            public void Dispose() => this.pending.CompleteAdding();
        }

        private static Task<T> RunOn<T>(TaskScheduler scheduler, Func<Task<T>> work)
        {
            return Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler).Unwrap();
        }

        private static void Shutdown(BindingQueue<TestBindingContext> queue)
        {
            queue.StopQueueProcessor(5_000);
            queue.Dispose();
        }

        /// <summary>
        /// The failure as the customer hit it. Waiters block the only pool threads; the queue's
        /// dispatch needs one of those threads; nothing ever completes.
        /// </summary>
        [Test]
        [Timeout(20_000)]
        public async Task BlockingWaitersThatExhaustThePoolDeadlockTheQueue()
        {
            using var pool = new FixedThreadScheduler(threadCount: 2);
            var queue = new BindingQueue<TestBindingContext> { DispatchScheduler = pool };
            try
            {
                // One blocking waiter per pool thread, waiting the way ParseAndBind used to.
                Task<bool>[] waiters = Enumerable.Range(0, pool.MaximumConcurrencyLevel)
                    .Select(_ => Task.Factory.StartNew(
                        () =>
                        {
                            QueueItem item = queue.QueueBindingOperation("key", (context, token) => null);
                            return item.ItemProcessed.WaitOne(1_500);
                        },
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        pool))
                    .ToArray();

                bool[] processedInTime = await Task.WhenAll(waiters);

                Assert.That(processedInTime, Is.All.False,
                    "with every pool thread blocked in a waiter, the queue has no thread to dispatch on, so no item completes");
            }
            finally
            {
                Shutdown(queue);
            }
        }

        /// <summary>
        /// The fix. Awaiting an item's completion parks a registered wait, not a thread, so even
        /// a pool of two threads services many more waiters than it has threads. The items share
        /// one context and wait for its binding lock, so that wait must not hold a thread either.
        /// </summary>
        [Test]
        [Timeout(20_000)]
        public async Task AsyncWaitersLeaveThePoolFreeForTheQueue()
        {
            const int waiterCount = 64;
            using var pool = new FixedThreadScheduler(threadCount: 2);
            var queue = new BindingQueue<TestBindingContext> { DispatchScheduler = pool };
            int executed = 0;
            try
            {
                var items = new ConcurrentBag<QueueItem>();
                Task<bool>[] waiters = Enumerable.Range(0, waiterCount)
                    .Select(_ => RunOn(pool, async () =>
                    {
                        QueueItem item = queue.QueueBindingOperation(
                            "key",
                            (context, token) =>
                            {
                                Interlocked.Increment(ref executed);
                                return null;
                            },
                            waitForLockTimeout: 10_000);
                        items.Add(item);
                        return await item.WaitForCompletionAsync(queueWaitBudgetMs: 10_000);
                    }))
                    .ToArray();

                bool[] processedInTime = await Task.WhenAll(waiters);

                Assert.Multiple(() =>
                {
                    Assert.That(processedInTime, Is.All.True, "every waiter is signalled");
                    Assert.That(Volatile.Read(ref executed), Is.EqualTo(waiterCount), "every operation ran");
                    Assert.That(items.Select(i => i.Abandoned), Is.All.False);
                });
            }
            finally
            {
                Shutdown(queue);
            }
        }

        /// <summary>
        /// A waiter that gives up marks its item abandoned, and the queue drops abandoned items
        /// instead of running them, so a backlog left behind by a stall drains immediately.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        [Timeout(20_000)]
        public async Task AbandonedItemsAreDroppedWithoutRunning(bool synchronous)
        {
            var queue = new BindingQueue<TestBindingContext>();
            bool operationRan = false;
            try
            {
                QueueItem ready = queue.QueueBindingOperation("startup", (context, token) => null);
                Assert.That(await ready.WaitForCompletionAsync(queueWaitBudgetMs: 5_000), Is.True,
                    "the processor must have started before it is stopped");
                Assert.That(queue.StopQueueProcessor(5_000), Is.True, "nothing dispatches while the processor is stopped");

                QueueItem item = queue.QueueBindingOperation("key", (context, token) =>
                {
                    operationRan = true;
                    return null;
                });

                bool processed = synchronous
                    ? item.WaitForCompletion(queueWaitBudgetMs: 100)
                    : await item.WaitForCompletionAsync(queueWaitBudgetMs: 100);

                Assert.That(processed, Is.False, "the wait is bounded even though the queue never got to the item");
                Assert.That(item.Abandoned, Is.True);

                queue.StartQueueProcessor();

                Assert.That(item.ItemProcessed.WaitOne(5_000), Is.True,
                    "the queue still signals an abandoned item so nothing else can hang on it");
                Assert.Multiple(() =>
                {
                    Assert.That(item.WasExecuted, Is.False);
                    Assert.That(operationRan, Is.False, "an abandoned item's operation must not run");
                });
            }
            finally
            {
                Shutdown(queue);
            }
        }

        /// <summary>
        /// Timeouts used to be enforced by a task that blocked a pool thread waiting on the
        /// operation. When the pool was saturated that task could not run, so no timeout ever
        /// fired during the stall. They are timer driven now and fire regardless.
        /// </summary>
        [Test]
        [Timeout(20_000)]
        public async Task HardTimeoutFiresWhileTheOnlyDispatchThreadIsHeldByTheOperation()
        {
            using var pool = new FixedThreadScheduler(threadCount: 1);
            var queue = new BindingQueue<TestBindingContext> { DispatchScheduler = pool };
            using var releaseOperation = new ManualResetEventSlim(false);
            try
            {
                QueueItem item = queue.QueueBindingOperation(
                    "key",
                    (context, token) =>
                    {
                        releaseOperation.Wait();
                        return null;
                    },
                    bindingTimeout: 200);

                bool processed = await item.WaitForCompletionAsync(queueWaitBudgetMs: 5_000);

                Assert.Multiple(() =>
                {
                    Assert.That(processed, Is.True, "the item completes through its timeout while the operation still holds the only dispatch thread");
                    Assert.That(item.TimedOut, Is.True);
                    Assert.That(releaseOperation.IsSet, Is.False, "the operation is still running");
                });
            }
            finally
            {
                releaseOperation.Set();
                Shutdown(queue);
            }
        }

        /// <summary>
        /// Removing a context used to race with ProcessQueue between its context lookup and its
        /// BindingContextTasks indexer access. The resulting KeyNotFoundException faulted the
        /// dedicated consumer, left the current item unsignalled, and stopped unrelated work too.
        /// </summary>
        [Test]
        [Timeout(20_000)]
        public async Task RemovingContextDuringDispatchDoesNotStopQueueConsumer()
        {
            const string removedKey = "context-being-replaced";
            PausingBindingContext.ResetGates();
            var queue = new RemovableBindingQueue();
            try
            {
                QueueItem racingItem = queue.QueueBindingOperation(removedKey, (context, token) => null);
                Assert.That(PausingBindingContext.AtTaskLookup.Wait(TimeSpan.FromSeconds(5)), Is.True,
                    "the consumer reached the context task-chain lookup");

                using var removalStarted = new ManualResetEventSlim(false);
                using var removalFinished = new ManualResetEventSlim(false);
                Task removal = Task.Run(() =>
                {
                    removalStarted.Set();
                    queue.Remove(removedKey);
                    removalFinished.Set();
                });

                Assert.That(removalStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);

                // Before the fix removal completes in this window and deletes the task-chain
                // entry. After the fix it waits for the same lifecycle lock as the consumer.
                removalFinished.Wait(TimeSpan.FromMilliseconds(250));
                PausingBindingContext.ResumeTaskLookup.Set();
                await removal.WaitAsync(TimeSpan.FromSeconds(5));

                QueueItem unrelatedItem = queue.QueueBindingOperation("unrelated-context", (context, token) => null);

                Assert.Multiple(() =>
                {
                    Assert.That(racingItem.ItemProcessed.WaitOne(TimeSpan.FromSeconds(5)), Is.True,
                        "the item racing with removal must reach a terminal state");
                    Assert.That(unrelatedItem.ItemProcessed.WaitOne(TimeSpan.FromSeconds(5)), Is.True,
                        "the consumer must remain alive for unrelated work");
                });
            }
            finally
            {
                PausingBindingContext.ResumeTaskLookup.Set();
                try { queue.StopQueueProcessor(5_000); } catch (AggregateException) { }
                queue.Dispose();
            }
        }

        /// <summary>
        /// Clearing queued work must complete every removed item. Previously ClearQueuedItems
        /// dropped the linked-list nodes without signalling ItemProcessed, so synchronous callers
        /// could remain blocked forever even though their work no longer existed.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task ClearingQueueCompletesRemovedWaiters()
        {
            var queue = new BindingQueue<TestBindingContext>();
            try
            {
                QueueItem ready = queue.QueueBindingOperation("startup", (context, token) => null);
                Assert.That(await ready.WaitForCompletionAsync(queueWaitBudgetMs: 5_000), Is.True);
                Assert.That(queue.StopQueueProcessor(5_000), Is.True);

                QueueItem removed = queue.QueueBindingOperation("pending", (context, token) => null);
                queue.ClearQueuedItems();

                Assert.Multiple(() =>
                {
                    Assert.That(queue.HasPendingQueueItems, Is.False);
                    Assert.That(removed.ItemProcessed.WaitOne(TimeSpan.FromSeconds(1)), Is.True,
                        "a waiter must be released when its queued item is discarded");
                    Assert.That(removed.WasExecuted, Is.False);
                    Assert.That(removed.Abandoned, Is.True);
                });
            }
            finally
            {
                queue.Dispose();
            }
        }
    }
}
