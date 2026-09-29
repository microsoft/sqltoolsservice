//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using NUnit.Framework;

namespace Microsoft.SqlTools.LanguageService.UnitTests.LanguageServices
{

    /// <summary>
    /// Test class for the test binding context
    /// </summary>
    public class TestBindingContext : IBindingContext
    {
        public TestBindingContext()
        {
            this.BindingTimeout = 3000;
        }

        public bool IsConnected { get; set; }

        public ServerConnection ServerConnection { get; set; }

        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        public SmoMetadataProvider SmoMetadataProvider { get; set; }

        public IBinder Binder { get; set; }

        public SemaphoreSlim BindingLock { get; } = new SemaphoreSlim(1, 1);

        public int BindingTimeout { get; set; }

        public ParseOptions ParseOptions { get; }

        public ServerVersion ServerVersion { get; }

        public DatabaseEngineType DatabaseEngineType {  get; }

        public TransactSqlVersion TransactSqlVersion { get; }

        public DatabaseCompatibilityLevel DatabaseCompatibilityLevel { get; }
    }

    /// <summary>
    /// A binding queue that exposes context replacement and removal to the tests
    /// </summary>
    public class TestBindingQueue : BindingQueue<TestBindingContext>
    {
        public void Replace(string key, IBindingContext context) => ReplaceBindingContext(key, context);

        public void Remove(string key) => RemoveBindingContext(key);

        public static Task RunIdle(IBindingContext context, int millisecondsTimeout, Action<ServerConnection> action)
            => RunWhenIdleAsync(context, millisecondsTimeout, action);
    }

    /// <summary>
    /// Tests for the Binding Queue
    /// </summary>
    public class BindingQueueTests
    {
        private TestBindingQueue bindingQueue;

        [SetUp]
        public void CreateQueue()
        {
            this.bindingQueue = new TestBindingQueue();
        }

        [TearDown]
        public void DisposeQueue()
        {
            this.bindingQueue.Dispose();
        }

        /// <summary>
        /// Starts an operation that holds its context until <paramref name="release"/> completes,
        /// and returns once the operation is running.
        /// </summary>
        private async Task<Task<QueueItem>> StartBlockingOperation(string key, Task<object> release)
        {
            var started = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<QueueItem> item = this.bindingQueue.QueueBindingOperationAsync(
                key,
                bindingTimeout: 30_000,
                bindOperationAsync: async (context, cancellationToken) =>
                {
                    started.TrySetResult(null);
                    return await release;
                });
            await started.Task;
            return item;
        }

        [Test]
        public async Task QueuedOperationReturnsItsResultWithoutRunningOnTheCallersThread()
        {
            object expected = new object();
            int callerThread = Environment.CurrentManagedThreadId;
            int operationThread = callerThread;

            QueueItem item = await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) =>
                {
                    operationThread = Environment.CurrentManagedThreadId;
                    return expected;
                });

            Assert.That(item.Result, Is.SameAs(expected));
            Assert.That(item.WasExecuted, Is.True);
            Assert.That(item.TimedOut, Is.False);
            Assert.That(operationThread, Is.Not.EqualTo(callerThread));
        }

        [Test]
        [Timeout(30_000)]
        public async Task OperationsOnOneContextRunOneAtATime()
        {
            int running = 0;
            int maxRunning = 0;
            List<Task<QueueItem>> items = Enumerable.Range(0, 20).Select(_ => this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                waitForLockTimeout: 20_000,
                bindOperationAsync: async (context, cancellationToken) =>
                {
                    int now = Interlocked.Increment(ref running);
                    InterlockedMax(ref maxRunning, now);
                    await Task.Delay(10);
                    Interlocked.Decrement(ref running);
                    return null;
                })).ToList();

            QueueItem[] results = await Task.WhenAll(items);

            Assert.That(maxRunning, Is.EqualTo(1));
            Assert.That(results.All(r => r.WasExecuted && !r.TimedOut), Is.True);
        }

        [Test]
        [Timeout(30_000)]
        public async Task OperationsOnDifferentContextsRunConcurrently()
        {
            const int contextCount = 5;
            int running = 0;
            var allRunning = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            List<Task<QueueItem>> items = Enumerable.Range(0, contextCount).Select(i => this.bindingQueue.QueueBindingOperationAsync(
                "testkey" + i,
                bindingTimeout: 20_000,
                bindOperationAsync: async (context, cancellationToken) =>
                {
                    if (Interlocked.Increment(ref running) == contextCount)
                    {
                        allRunning.TrySetResult(null);
                    }
                    return await allRunning.Task;
                })).ToList();

            QueueItem[] results = await Task.WhenAll(items);

            Assert.That(results.All(r => r.WasExecuted && !r.TimedOut), Is.True);
        }

        /// <summary>
        /// Items wait for a busy context only as long as their lock timeout, which defaults to 0.
        /// An item that gives up gets its timeout result and never runs later.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task BusyContextTimesTheItemOutWithoutRunningIt()
        {
            object timeoutResult = new object();
            bool secondOperationRan = false;
            var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<QueueItem> first = await StartBlockingOperation("testkey", release.Task);

            QueueItem second = await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) =>
                {
                    secondOperationRan = true;
                    return null;
                },
                timeoutOperation: context => timeoutResult);

            Assert.That(second.TimedOut, Is.True);
            Assert.That(second.WasExecuted, Is.False);
            Assert.That(second.Result, Is.SameAs(timeoutResult));

            release.SetResult("first");
            Assert.That((await first).Result, Is.EqualTo("first"));
            await Task.Delay(100);
            Assert.That(secondOperationRan, Is.False);
        }

        [Test]
        [Timeout(10_000)]
        public async Task QueueWithUnhandledExceptionReturnsErrorHandlerResult()
        {
            object defaultReturnObject = new object();

            QueueItem queueItem = await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) => throw new InvalidOperationException("Unhandled!!"),
                errorHandler: exception => defaultReturnObject);

            Assert.That(queueItem.GetResultAsT<object>(), Is.SameAs(defaultReturnObject));
            Assert.That(this.bindingQueue.BindingContextMap.ContainsKey("testkey"), Is.True,
                "Only connection errors remove the context.");

            QueueItem next = await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) => "next");
            Assert.That(next.Result, Is.EqualTo("next"), "The context is released after a failed operation.");
        }

        [Test]
        [Timeout(10_000)]
        public async Task ConnectionErrorRemovesTheContextAndRaisesTheEvent()
        {
            string raisedKey = null;
            this.bindingQueue.OnUnhandledException += (key, ex) => raisedKey = key;

            await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) => throw new SocketException());

            Assert.That(this.bindingQueue.BindingContextMap.ContainsKey("testkey"), Is.False);
            Assert.That(raisedKey, Is.EqualTo("testkey"));
        }

        /// <summary>
        /// A failed operation on a context that has since been replaced (for example by an
        /// IntelliSense rebuild) must not remove the replacement.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task ConnectionErrorDoesNotRemoveAReplacementContext()
        {
            bool raised = false;
            this.bindingQueue.OnUnhandledException += (key, ex) => raised = true;
            var replacement = new TestBindingContext();

            await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) =>
                {
                    this.bindingQueue.Replace("testkey", replacement);
                    throw new SocketException();
                });

            Assert.That(this.bindingQueue.BindingContextMap.TryGetValue("testkey", out IBindingContext current), Is.True);
            Assert.That(current, Is.SameAs(replacement));
            Assert.That(raised, Is.False);
        }

        [Test]
        [Timeout(10_000)]
        public async Task ClearQueuedItemsCompletesWaitingItemsWithoutRunningThem()
        {
            bool waiterRan = false;
            var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<QueueItem> running = await StartBlockingOperation("testkey", release.Task);
            Task<QueueItem> waiting = this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                waitForLockTimeout: 30_000,
                bindOperation: (context, cancellationToken) =>
                {
                    waiterRan = true;
                    return null;
                },
                timeoutOperation: context => "timeout handler ran");

            this.bindingQueue.ClearQueuedItems();

            QueueItem abandoned = await waiting;
            Assert.That(abandoned.TimedOut, Is.True);
            Assert.That(abandoned.WasExecuted, Is.False);
            Assert.That(abandoned.Result, Is.Null, "An abandoned item does not run its timeout handler.");
            Assert.That(running.IsCompleted, Is.False, "Running items are not affected.");

            release.SetResult("done");
            Assert.That((await running).Result, Is.EqualTo("done"));
            Assert.That(waiterRan, Is.False);

            QueueItem after = await this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindOperation: (context, cancellationToken) => "after");
            Assert.That(after.Result, Is.EqualTo("after"), "The queue keeps working after it is cleared.");
        }

        [Test]
        [Timeout(10_000)]
        public async Task DisposeCompletesWaitingAndLaterItemsWithoutRunningThem()
        {
            var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<QueueItem> running = await StartBlockingOperation("testkey", release.Task);
            Task<QueueItem> waiting = this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                waitForLockTimeout: 30_000,
                bindOperation: (context, cancellationToken) => null);

            this.bindingQueue.Dispose();

            Assert.That((await waiting).WasExecuted, Is.False);
            QueueItem late = await this.bindingQueue.QueueBindingOperationAsync(
                "otherkey",
                bindOperation: (context, cancellationToken) => "ran");
            Assert.That(late.WasExecuted, Is.False);

            release.SetResult(null);
            await running;
        }

        /// <summary>
        /// Neither waiting for a context nor waiting for a running operation holds a thread.
        /// </summary>
        [Test]
        [Timeout(60_000)]
        public async Task WaitingItemsDoNotHoldThreads()
        {
            // Cap the pool well below the number of items, so they can only all be waiting at
            // once if none of them holds a thread.
            ThreadPool.GetMinThreads(out int minWorkerThreads, out _);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);
            int cappedWorkerThreads = minWorkerThreads + 4;
            int itemCount = cappedWorkerThreads * 4;
            int operationsStarted = 0;
            var releaseOperations = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var items = new List<Task<QueueItem>>();

            Assert.That(ThreadPool.SetMaxThreads(cappedWorkerThreads, maxIoThreads), Is.True);
            try
            {
                for (int i = 0; i < itemCount; i++)
                {
                    // Half share one context and wait for it, half run on their own contexts.
                    items.Add(this.bindingQueue.QueueBindingOperationAsync(
                        i % 2 == 0 ? "sharedkey" : "testkey" + i,
                        bindingTimeout: 30_000,
                        waitForLockTimeout: 30_000,
                        bindOperationAsync: async (context, cancellationToken) =>
                        {
                            Interlocked.Increment(ref operationsStarted);
                            return await releaseOperations.Task;
                        }));
                }

                int expectedStarted = itemCount / 2 + 1;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (Volatile.Read(ref operationsStarted) < expectedStarted && stopwatch.ElapsedMilliseconds < 10_000)
                {
                    await Task.Delay(20);
                }
                Assert.That(Volatile.Read(ref operationsStarted), Is.EqualTo(expectedStarted),
                    "every context is in use while the rest of the items wait");

                releaseOperations.SetResult(null);
                QueueItem[] results = await Task.WhenAll(items);
                Assert.That(results.All(r => r.WasExecuted && !r.TimedOut), Is.True);
            }
            finally
            {
                releaseOperations.TrySetResult(null);
                ThreadPool.SetMaxThreads(maxWorkerThreads, maxIoThreads);
            }
        }

        /// <summary>
        /// Verifies that crossing the slow-operation threshold does not select the timeout result.
        /// An operation which finishes before its hard timeout must return its real result, even
        /// though it was reported as slow. This protects the #21930 large-dbo reproduction, where
        /// about 36,900 suggestions took 650-760 ms and were previously discarded at 500 ms.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task QueueSlowOperationCanCompleteBeforeHardTimeout()
        {
            object successResult = new object();
            object timeoutResult = new object();

            QueueItem queueItem = await this.bindingQueue.QueueBindingOperationAsync(
                "slow-operation-test",
                bindingTimeout: 50,
                hardTimeout: 2_000,
                bindOperation: (context, cancellationToken) =>
                {
                    Thread.Sleep(300);
                    return successResult;
                },
                timeoutOperation: context => timeoutResult);

            Assert.That(queueItem.Result, Is.SameAs(successResult));
            Assert.That(queueItem.TimedOut, Is.False);
        }

        /// <summary>
        /// Verifies that the hard timeout returns to the caller while a non-cooperative operation is
        /// still blocked, but keeps the binding context locked until that operation really ends.
        /// A second item must time out waiting for the lock instead of running concurrently, and
        /// the late result from the first operation must not replace its timeout result. This
        /// models the #22236 repro where SMO's sys.all_columns query waited on LCK_M_S behind a
        /// schema-modification lock and did not observe queue cancellation.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task QueueHardTimeoutReturnsAndRetainsBindingLock()
        {
            const string operationKey = "hard-timeout-test";
            object timeoutResult = new object();
            using var operationFinished = new ManualResetEventSlim(false);
            using var releaseOperation = new ManualResetEventSlim(false);
            bool cancellationRequested = false;
            bool secondOperationStarted = false;
            var bindingContext = new TestBindingContext();
            this.bindingQueue.BindingContextMap.TryAdd(operationKey, bindingContext);

            try
            {
                QueueItem firstItem = await this.bindingQueue.QueueBindingOperationAsync(
                    operationKey,
                    bindingTimeout: 50,
                    hardTimeout: 150,
                    bindOperation: (context, cancellationToken) =>
                    {
                        using (cancellationToken.Register(() => cancellationRequested = true))
                        {
                            releaseOperation.Wait();
                        }
                        operationFinished.Set();
                        return new object();
                    },
                    timeoutOperation: context => timeoutResult);

                Assert.That(firstItem.Result, Is.SameAs(timeoutResult));
                Assert.That(firstItem.TimedOut, Is.True);
                Assert.That(cancellationRequested, Is.True);
                Assert.That(operationFinished.IsSet, Is.False, "The caller must not wait for the blocked operation.");
                Assert.That(bindingContext.BindingLock.CurrentCount, Is.Zero,
                    "The context must remain unavailable while the timed-out operation is still running.");

                QueueItem secondItem = await this.bindingQueue.QueueBindingOperationAsync(
                    operationKey,
                    waitForLockTimeout: 50,
                    bindOperation: (context, cancellationToken) =>
                    {
                        secondOperationStarted = true;
                        return null;
                    },
                    timeoutOperation: context => timeoutResult);

                Assert.That(secondItem.Result, Is.SameAs(timeoutResult));
                Assert.That(secondItem.WasExecuted, Is.False);
                Assert.That(secondOperationStarted, Is.False, "A second operation must not use the same context concurrently.");

                releaseOperation.Set();

                Assert.That(operationFinished.Wait(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(await bindingContext.BindingLock.WaitAsync(TimeSpan.FromSeconds(1)), Is.True);
                bindingContext.BindingLock.Release();
                Assert.That(firstItem.Result, Is.SameAs(timeoutResult),
                    "A late operation result must not replace the hard-timeout result.");
            }
            finally
            {
                releaseOperation.Set();
            }
        }

        /// <summary>
        /// A context that is still being populated holds its lock and has no connection yet. Acting
        /// on it must wait for population and then use the connection it assigned, or a removed
        /// context leaks its connection and a restore can run while the context connects.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task RunWhenIdleUsesTheConnectionAssignedWhileItWaited()
        {
            var bindingContext = new TestBindingContext();
            Assert.That(bindingContext.BindingLock.Wait(0), Is.True, "hold the lock as population does");
            ServerConnection seen = null;

            Task run = TestBindingQueue.RunIdle(bindingContext, 5_000, connection => seen = connection);
            await Task.Delay(100);
            Assert.That(run.IsCompleted, Is.False, "It waits for the context to be populated.");

            var populated = new ServerConnection();
            bindingContext.ServerConnection = populated;
            bindingContext.BindingLock.Release();
            await run;

            Assert.That(seen, Is.SameAs(populated));
            Assert.That(bindingContext.BindingLock.CurrentCount, Is.EqualTo(1));
        }

        [Test]
        [Timeout(10_000)]
        public async Task RunWhenIdleGivesUpOnABusyContext()
        {
            var bindingContext = new TestBindingContext { ServerConnection = new ServerConnection() };
            Assert.That(bindingContext.BindingLock.Wait(0), Is.True);
            bool acted = false;

            await TestBindingQueue.RunIdle(bindingContext, 50, connection => acted = true);

            Assert.That(acted, Is.False);
            Assert.That(bindingContext.BindingLock.CurrentCount, Is.Zero, "It must not release a lock it did not take.");
            bindingContext.BindingLock.Release();
        }

        /// <summary>
        /// Contexts are replaced (project saves, IntelliSense rebuilds) and removed (connection
        /// errors, closed sessions) while items are queued for them. The old queue's processor
        /// died on that race and every later item hung, so every item must still complete.
        /// </summary>
        [Test]
        [Timeout(30_000)]
        public async Task ItemsCompleteWhileTheirContextsAreReplacedAndRemoved()
        {
            string[] keys = { "a", "b", "c", "d" };
            using var stopChurn = new CancellationTokenSource();
            Task churn = Task.Run(() =>
            {
                var random = new Random(1);
                while (!stopChurn.IsCancellationRequested)
                {
                    string key = keys[random.Next(keys.Length)];
                    if (random.Next(2) == 0)
                    {
                        this.bindingQueue.Replace(key, new TestBindingContext());
                    }
                    else
                    {
                        this.bindingQueue.Remove(key);
                    }
                }
            });

            try
            {
                Task<QueueItem[]> items = Task.WhenAll(Enumerable.Range(0, 400).Select(i => Task.Run(() =>
                    this.bindingQueue.QueueBindingOperationAsync(
                        keys[i % keys.Length],
                        waitForLockTimeout: 20_000,
                        bindOperationAsync: async (context, cancellationToken) =>
                        {
                            await Task.Yield();
                            return "ran";
                        }))));

                QueueItem[] results = await items;

                Assert.That(results.All(r => r.WasExecuted && !r.TimedOut && (string)r.Result == "ran"), Is.True);
            }
            finally
            {
                stopChurn.Cancel();
                await churn;
            }
        }

        /// <summary>
        /// Clearing abandons items that are waiting for a context. Racing it against items being
        /// queued must leave every item either run once or abandoned before running, never both,
        /// never stranded, and never running alongside another item on the same context.
        /// </summary>
        [Test]
        [Timeout(30_000)]
        public async Task ClearingWhileItemsAreQueuedNeverStrandsOrOverlapsThem()
        {
            int running = 0;
            int maxRunning = 0;
            using var stopClearing = new CancellationTokenSource();
            Task clearing = Task.Run(async () =>
            {
                while (!stopClearing.IsCancellationRequested)
                {
                    this.bindingQueue.ClearQueuedItems();
                    await Task.Delay(20);
                }
            });

            try
            {
                // Several producers queue items over time, so clears land among waiting and running items.
                QueueItem[][] produced = await Task.WhenAll(Enumerable.Range(0, 4).Select(producer => Task.Run(async () =>
                {
                    var queued = new List<Task<QueueItem>>();
                    for (int i = 0; i < 75; i++)
                    {
                        queued.Add(this.bindingQueue.QueueBindingOperationAsync(
                            "testkey",
                            bindingTimeout: 20_000,
                            waitForLockTimeout: 20_000,
                            bindOperationAsync: async (context, cancellationToken) =>
                            {
                                InterlockedMax(ref maxRunning, Interlocked.Increment(ref running));
                                await Task.Yield();
                                Interlocked.Decrement(ref running);
                                return "ran";
                            }));
                        await Task.Delay(1);
                    }
                    return await Task.WhenAll(queued);
                })));
                QueueItem[] results = produced.SelectMany(items => items).ToArray();

                Assert.That(maxRunning, Is.EqualTo(1));
                Assert.That(results.All(r => r.WasExecuted
                    ? !r.TimedOut && (string)r.Result == "ran"
                    : r.TimedOut && r.Result == null), Is.True,
                    "Each item either ran or was abandoned before running.");
            }
            finally
            {
                stopClearing.Cancel();
                await clearing;
            }
        }

        /// <summary>
        /// Disposing while other threads are still queueing must complete every item, and the
        /// queue must not run anything once it is disposed.
        /// </summary>
        [Test]
        [Timeout(30_000)]
        public async Task DisposingWhileItemsAreQueuedCompletesEveryItem()
        {
            var items = new List<Task<QueueItem>>();
            var startQueueing = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task producers = Task.WhenAll(Enumerable.Range(0, 4).Select(producer => Task.Run(async () =>
            {
                await startQueueing.Task;
                for (int i = 0; i < 100; i++)
                {
                    Task<QueueItem> item = this.bindingQueue.QueueBindingOperationAsync(
                        "key" + (i % 3),
                        waitForLockTimeout: 20_000,
                        bindOperationAsync: async (context, cancellationToken) =>
                        {
                            await Task.Delay(1);
                            return null;
                        });
                    lock (items)
                    {
                        items.Add(item);
                    }
                }
            })));

            startQueueing.SetResult(null);
            await Task.Delay(5);
            this.bindingQueue.Dispose();
            await producers;

            Task<QueueItem>[] queued;
            lock (items)
            {
                queued = items.ToArray();
            }
            await Task.WhenAll(queued);

            bool ranAfterDispose = false;
            QueueItem late = await this.bindingQueue.QueueBindingOperationAsync(
                "key0",
                bindOperation: (context, cancellationToken) =>
                {
                    ranAfterDispose = true;
                    return null;
                });
            Assert.That(late.WasExecuted, Is.False);
            Assert.That(ranAfterDispose, Is.False);
        }

        /// <summary>
        /// Hard timeouts hand the caller a result while a non-cooperative operation keeps the
        /// context. Under contention, no two operations may overlap on a context, and each
        /// context lock must be released exactly once, after its operation really ends.
        /// </summary>
        [Test]
        [Timeout(60_000)]
        public async Task HardTimeoutsUnderContentionNeverOverlapOperationsOrOverReleaseTheContext()
        {
            var bindingContext = new TestBindingContext();
            this.bindingQueue.BindingContextMap.TryAdd("testkey", bindingContext);
            var random = new Random(7);
            int[] delays = Enumerable.Range(0, 60).Select(_ => random.Next(0, 40)).ToArray();
            int running = 0;
            int maxRunning = 0;
            int finished = 0;

            QueueItem[] results = await Task.WhenAll(delays.Select(delay => this.bindingQueue.QueueBindingOperationAsync(
                "testkey",
                bindingTimeout: 5,
                hardTimeout: 10,
                waitForLockTimeout: 30_000,
                bindOperation: (context, cancellationToken) =>
                {
                    // Ignores cancellation, like a SMO query blocked on a lock.
                    InterlockedMax(ref maxRunning, Interlocked.Increment(ref running));
                    Thread.Sleep(delay);
                    Interlocked.Decrement(ref running);
                    Interlocked.Increment(ref finished);
                    return null;
                })));

            int executed = results.Count(r => r.WasExecuted);
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (Volatile.Read(ref finished) < executed && stopwatch.ElapsedMilliseconds < 20_000)
            {
                await Task.Delay(20);
            }

            Assert.That(results.Any(r => r.WasExecuted && r.TimedOut), Is.True, "Some operations outlive their hard timeout.");
            Assert.That(maxRunning, Is.EqualTo(1));
            Assert.That(Volatile.Read(ref finished), Is.EqualTo(executed));
            Assert.That(bindingContext.BindingLock.CurrentCount, Is.EqualTo(1),
                "The context is free once every operation has ended, and released no more than once.");
        }

        private static void InterlockedMax(ref int target, int value)
        {
            int current;
            while ((current = Volatile.Read(ref target)) < value
                && Interlocked.CompareExchange(ref target, value, current) != current)
            {
            }
        }
    }
}
