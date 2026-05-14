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
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
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
        /// and make sure subsequent tasks can continue on a fresh context
        /// </summary>
        [Test]
        public void QueueWithTimeoutDoesNotBlockNextTask()
        {
            string operationKey = "testkey";
            ManualResetEvent firstEventStarted = new ManualResetEvent(false);
            ManualResetEvent releaseFirstEvent = new ManualResetEvent(false);
            ManualResetEvent secondEventExecuted = new ManualResetEvent(false);
            IBindingContext firstContext = null;
            IBindingContext secondContext = null;
            bool firstOperationCanceled = false;
            bool secondOperationExecuted = false;
            InitializeTestSettings();

            this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                bindingTimeout: 100,
                bindOperation: (bindingContext, cancellationToken) =>
                {
                    firstContext = bindingContext;
                    firstEventStarted.Set();
                    releaseFirstEvent.WaitOne(2000);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        firstOperationCanceled = true;
                    }
                    return null;
                },
                timeoutOperation: TestTimeoutOperation);

            this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                bindingTimeout: 1000,
                bindOperation: (bindingContext, cancellationToken) =>
                {
                    secondContext = bindingContext;
                    secondOperationExecuted = true;
                    secondEventExecuted.Set();
                    return null;
                }
            );

            Assert.True(firstEventStarted.WaitOne(1000));
            Assert.True(secondEventExecuted.WaitOne(1000));
            releaseFirstEvent.Set();

            this.bindingQueue.StopQueueProcessor(15000);

            Assert.AreEqual(1, this.timeoutCallCount);
            Assert.True(firstOperationCanceled);
            Assert.True(secondOperationExecuted);
            Assert.False(ReferenceEquals(firstContext, secondContext));
        }

        [Test]
        public void QueueLockTimeoutDoesNotRunBindOperation()
        {
            string operationKey = "testkey";
            InitializeTestSettings();
            TestBindingContext lockedContext = new TestBindingContext();
            lockedContext.BindingLock.Reset();
            this.bindingQueue.BindingContextMap.TryAdd(operationKey, lockedContext);
            this.bindingQueue.BindingContextTasks.TryAdd(operationKey, Task.FromResult(0));

            bool bindOperationExecuted = false;
            QueueItem queueItem = this.bindingQueue.QueueBindingOperation(
                key: operationKey,
                bindingTimeout: 1000,
                waitForLockTimeout: 50,
                bindOperation: (bindingContext, cancellationToken) =>
                {
                    bindOperationExecuted = true;
                    return null;
                },
                timeoutOperation: TestTimeoutOperation);

            Assert.True(queueItem.ItemProcessed.WaitOne(1000));
            lockedContext.BindingLock.Set();
            this.bindingQueue.StopQueueProcessor(15000);

            Assert.False(bindOperationExecuted);
            Assert.AreEqual(1, this.timeoutCallCount);
        }
    }
}
