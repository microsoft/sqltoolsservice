//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
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
            this.BindingLock = new ManualResetEvent(true);
            this.BindingTimeout = 3000;
        }

        public bool IsConnected { get; set; }

        public ServerConnection ServerConnection { get; set; }

        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        public SmoMetadataProvider SmoMetadataProvider { get; set; }

        public IBinder Binder { get; set; }

        public ManualResetEvent BindingLock { get; set; } 

        public int BindingTimeout { get; set; } 

        public ParseOptions ParseOptions { get; }

        public ServerVersion ServerVersion { get; }

        public DatabaseEngineType DatabaseEngineType {  get; }

        public TransactSqlVersion TransactSqlVersion { get; }

        public DatabaseCompatibilityLevel DatabaseCompatibilityLevel { get; }  
    }

    /// <summary>
    /// Tests for the Binding Queue
    /// </summary>
    public class BindingQueueTests
    {
        private int bindCallCount = 0;
        
        private int timeoutCallCount = 0;

        private int bindCallbackDelay = 0;

        private bool isCancelationRequested = false;

        private IBindingContext bindingContext = null;

        private BindingQueue<TestBindingContext> bindingQueue = null;

        private void InitializeTestSettings()
        {
            this.bindCallCount = 0;
            this.timeoutCallCount = 0;
            this.bindCallbackDelay = 10;
            this.isCancelationRequested = false;
            this.bindingContext = GetMockBindingContext();
            this.bindingQueue = new BindingQueue<TestBindingContext>();
        }

        private IBindingContext GetMockBindingContext()
        {
            return new TestBindingContext();
        }

        /// <summary>
        /// Test bind operation callback
        /// </summary>
        private object TestBindOperation(
            IBindingContext bindContext, 
            CancellationToken cancelToken)
        {
            cancelToken.WaitHandle.WaitOne(this.bindCallbackDelay);
            this.isCancelationRequested = cancelToken.IsCancellationRequested;
            if (!this.isCancelationRequested)
            {
                ++this.bindCallCount;
            }
            return new CompletionItem[0];
        }

        /// <summary>
        /// Test callback for the bind timeout operation
        /// </summary>
        private object TestTimeoutOperation(
            IBindingContext bindingContext)
        {
            ++this.timeoutCallCount;
            return new CompletionItem[0];
        }

        /// <summary>
        /// Queues a single task
        /// </summary>
        [Test]
        public void QueueOneBindingOperationTest()
        {
            InitializeTestSettings();

            this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindOperation: TestBindOperation,
                timeoutOperation: TestTimeoutOperation);    

            Thread.Sleep(1000);      
            
            this.bindingQueue.StopQueueProcessor(15000);     

            Assert.AreEqual(1, this.bindCallCount);
            Assert.AreEqual(0, this.timeoutCallCount);  
            Assert.False(this.isCancelationRequested);
        }

        /// <summary>
        /// Queues a single task
        /// </summary>
        [Test]
        public void QueueWithUnhandledExceptionTest()
        {
            InitializeTestSettings();
            bool isExceptionHandled = false;
            object defaultReturnObject = new object();
            var queueItem = this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindOperation: (context, CancellationToken) => { throw new Exception("Unhandled!!"); },
                timeoutOperation: TestTimeoutOperation,
                errorHandler: (exception) => {
                    isExceptionHandled = true;
                    return defaultReturnObject;
                });

            queueItem.ItemProcessed.WaitOne(10000);
            
            this.bindingQueue.StopQueueProcessor(15000);

            Assert.True(isExceptionHandled);
            var result = queueItem.GetResultAsT<object>();
            Assert.AreEqual(defaultReturnObject, result);
        }

        /// <summary>
        /// Queue a 100 short tasks
        /// </summary>
        // Disable flaky test (mairvine - 3/15/2018)
        // [Test]
        public void Queue100BindingOperationTest()
        {
            InitializeTestSettings();

            for (int i = 0; i < 100; ++i)
            {
                this.bindingQueue.QueueBindingOperation(
                    key: "testkey",
                    bindOperation: TestBindOperation,
                    timeoutOperation: TestTimeoutOperation);
            }
            
            Thread.Sleep(2000);

            this.bindingQueue.StopQueueProcessor(15000);     

            Assert.AreEqual(100, this.bindCallCount);
            Assert.AreEqual(0, this.timeoutCallCount);
            Assert.False(this.isCancelationRequested);
        }

        /// <summary>
        /// Queue an task with a long operation causing a timeout
        /// </summary>
        [Test]
        public void QueueWithTimeout()
        {
            InitializeTestSettings();

            this.bindCallbackDelay = 1000;

            this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindingTimeout: bindCallbackDelay / 2,
                bindOperation: TestBindOperation,
                timeoutOperation: TestTimeoutOperation);

            Thread.Sleep(this.bindCallbackDelay + 100);
            
            this.bindingQueue.StopQueueProcessor(15000);

            Assert.AreEqual(0, this.bindCallCount);
            Assert.AreEqual(1, this.timeoutCallCount);
            Assert.True(this.isCancelationRequested);
        }

        /// <summary>
        /// Queue a task with a long operation causing a timeout 
        /// and make sure subsequent tasks don't execute while task is completing
        /// </summary>
        [Test]
        public void QueueWithTimeoutDoesNotRunNextTask()
        {
            string operationKey = "testkey";
            ManualResetEvent firstEventExecuted = new ManualResetEvent(false);
            ManualResetEvent secondEventExecuted = new ManualResetEvent(false);
            bool firstOperationCanceled = false;
            bool secondOperationExecuted = false;
            InitializeTestSettings();

            this.bindCallbackDelay = 1000;
            var totalTimeout = (this.bindCallbackDelay + this.bindingContext.BindingTimeout) * 2;

            this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                bindingTimeout: bindCallbackDelay / 2,
                bindOperation: (bindingContext, cancellationToken) =>
                {
                    secondEventExecuted.WaitOne();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        firstOperationCanceled = true;
                    }
                    firstEventExecuted.Set();
                    return null;
                },
                timeoutOperation: TestTimeoutOperation);

            this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                bindingTimeout: bindCallbackDelay,
                bindOperation: (bindingContext, cancellationToken) =>
                {
                    secondOperationExecuted = true;
                    secondEventExecuted.Set();
                    return null;
                },
                waitForLockTimeout: totalTimeout
            );

            var result = firstEventExecuted.WaitOne(totalTimeout);
            Assert.False(result);

            this.bindingQueue.StopQueueProcessor(15000);

            Assert.AreEqual(1, this.timeoutCallCount);
            Assert.False(firstOperationCanceled);
            Assert.False(secondOperationExecuted);
        }

        /// <summary>
        /// Verifies that an item which times out waiting for the binding lock completes with its
        /// timeout result and returns from dispatch immediately. The context lock is held before
        /// the item is queued so the test deterministically exercises the lock-wait timeout path.
        /// The binding callback must never run, even after the timeout has been reported, and a
        /// late success result must not replace the terminal timeout result.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public void QueueLockWaitTimeoutDoesNotExecuteBindingOperation()
        {
            const string operationKey = "lock-timeout-test";
            object timeoutResult = new object();
            object successResult = new object();
            using var operationStarted = new ManualResetEvent(false);
            InitializeTestSettings();

            var lockedContext = new TestBindingContext();
            lockedContext.BindingLock.Reset();
            this.bindingQueue.BindingContextMap.TryAdd(operationKey, lockedContext);
            this.bindingQueue.BindingContextTasks.TryAdd(lockedContext, Task.CompletedTask);

            QueueItem queueItem = this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                waitForLockTimeout: 50,
                bindOperation: (context, cancellationToken) =>
                {
                    operationStarted.Set();
                    return successResult;
                },
                timeoutOperation: context => timeoutResult);

            try
            {
                Assert.That(queueItem.ItemProcessed.WaitOne(TimeSpan.FromSeconds(2)), Is.True);
                Assert.That(queueItem.Result, Is.SameAs(timeoutResult));
                Assert.That(operationStarted.WaitOne(TimeSpan.FromMilliseconds(500)), Is.False);
                Assert.That(queueItem.Result, Is.SameAs(timeoutResult));
            }
            finally
            {
                lockedContext.BindingLock.Set();
                this.bindingQueue.StopQueueProcessor(2_000);
                this.bindingQueue.Dispose();
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
        public void QueueSlowOperationCanCompleteBeforeHardTimeout()
        {
            object successResult = new object();
            object timeoutResult = new object();
            using var operationStarted = new ManualResetEvent(false);
            using var releaseOperation = new ManualResetEvent(false);
            InitializeTestSettings();

            QueueItem queueItem = this.bindingQueue.QueueBindingOperation(
                key: "slow-operation-test",
                bindingTimeout: 50,
                hardTimeout: 2_000,
                bindOperation: (context, cancellationToken) =>
                {
                    operationStarted.Set();
                    releaseOperation.WaitOne();
                    return successResult;
                },
                timeoutOperation: context => timeoutResult);

            try
            {
                Assert.That(operationStarted.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(queueItem.ItemProcessed.WaitOne(TimeSpan.FromMilliseconds(200)), Is.False,
                    "The slow threshold must not complete the queue item.");

                releaseOperation.Set();

                Assert.That(queueItem.ItemProcessed.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(queueItem.Result, Is.SameAs(successResult));
                Assert.That(queueItem.TimedOut, Is.False);
            }
            finally
            {
                releaseOperation.Set();
                this.bindingQueue.StopQueueProcessor(2_000);
                this.bindingQueue.Dispose();
            }
        }

        /// <summary>
        /// Verifies that the hard timeout signals the caller while a non-cooperative operation is
        /// still blocked, but keeps the binding context locked until that operation really ends.
        /// A second item must time out waiting for the lock instead of running concurrently, and
        /// the late result from the first operation must not replace its timeout result. This
        /// models the #22236 repro where SMO's sys.all_columns query waited on LCK_M_S behind a
        /// schema-modification lock and did not observe queue cancellation.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public void QueueHardTimeoutSignalsCallerAndRetainsBindingLock()
        {
            const string operationKey = "hard-timeout-test";
            object timeoutResult = new object();
            object lateResult = new object();
            using var operationStarted = new ManualResetEvent(false);
            using var operationFinished = new ManualResetEvent(false);
            using var releaseOperation = new ManualResetEvent(false);
            using var cancellationRequested = new ManualResetEvent(false);
            using var secondOperationStarted = new ManualResetEvent(false);
            InitializeTestSettings();

            var bindingContext = new TestBindingContext();
            this.bindingQueue.BindingContextMap.TryAdd(operationKey, bindingContext);
            this.bindingQueue.BindingContextTasks.TryAdd(bindingContext, Task.CompletedTask);

            QueueItem firstItem = this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                bindingTimeout: 50,
                hardTimeout: 150,
                bindOperation: (context, cancellationToken) =>
                {
                    using CancellationTokenRegistration registration = cancellationToken.Register(
                        () => cancellationRequested.Set());
                    operationStarted.Set();
                    releaseOperation.WaitOne();
                    operationFinished.Set();
                    return lateResult;
                },
                timeoutOperation: context => timeoutResult);

            try
            {
                Assert.That(operationStarted.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(firstItem.ItemProcessed.WaitOne(TimeSpan.FromSeconds(1)), Is.True,
                    "The caller must not wait for the blocked operation after the hard timeout.");
                Assert.That(firstItem.Result, Is.SameAs(timeoutResult));
                Assert.That(firstItem.TimedOut, Is.True);
                Assert.That(cancellationRequested.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(bindingContext.BindingLock.WaitOne(0), Is.False,
                    "The context must remain unavailable while the timed-out operation is still running.");

                QueueItem secondItem = this.bindingQueue.QueueBindingOperation(
                    key: operationKey,
                    waitForLockTimeout: 50,
                    bindOperation: (context, cancellationToken) =>
                    {
                        secondOperationStarted.Set();
                        return null;
                    },
                    timeoutOperation: context => timeoutResult);

                Assert.That(secondItem.ItemProcessed.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(secondItem.Result, Is.SameAs(timeoutResult));
                Assert.That(secondOperationStarted.WaitOne(TimeSpan.FromMilliseconds(200)), Is.False,
                    "A second operation must not use the same context concurrently.");

                releaseOperation.Set();

                Assert.That(operationFinished.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(bindingContext.BindingLock.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                Assert.That(firstItem.Result, Is.SameAs(timeoutResult),
                    "A late operation result must not replace the hard-timeout result.");
            }
            finally
            {
                releaseOperation.Set();
                operationFinished.WaitOne(TimeSpan.FromSeconds(1));
                bindingContext.BindingLock.Set();
                this.bindingQueue.StopQueueProcessor(2_000);
                this.bindingQueue.Dispose();
            }
        }

    }
}
