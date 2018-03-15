//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Xunit;

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
        [Fact]
        public void QueueOneBindingOperationTest()
        {
            InitializeTestSettings();

            this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindOperation: TestBindOperation,
                timeoutOperation: TestTimeoutOperation);    

            Thread.Sleep(1000);      
            
            this.bindingQueue.StopQueueProcessor(15000);     

            Assert.Equal(1, this.bindCallCount);
            Assert.Equal(0, this.timeoutCallCount);  
            Assert.False(this.isCancelationRequested);
        }

        /// <summary>
        /// Queues a single task
        /// </summary>
        [Fact]
        public void QueueWithUnhandledExceptionTest()
        {
            InitializeTestSettings();
            ManualResetEvent mre = new ManualResetEvent(false);
            bool isExceptionHandled = false;
            object defaultReturnObject = new object();
            var queueItem = this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindOperation: (context, CancellationToken) => { throw new Exception("Unhandled!!"); },
                timeoutOperation: TestTimeoutOperation,
                errorHandler: (exception) => {
                    isExceptionHandled = true;
                    mre.Set();
                    return defaultReturnObject;
                });

            mre.WaitOne(10000);
            
            this.bindingQueue.StopQueueProcessor(15000);

            Assert.True(isExceptionHandled);
            var result = queueItem.GetResultAsT<object>();
            Assert.Equal(defaultReturnObject, result);
        }

        /// <summary>
        /// Queue a 100 short tasks
        /// </summary>
        // Disable flaky test (mairvine - 3/15/2018)
        // [Fact]
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

            Assert.Equal(100, this.bindCallCount);
            Assert.Equal(0, this.timeoutCallCount);
            Assert.False(this.isCancelationRequested);
        }

        /// <summary>
        /// Queue an task with a long operation causing a timeout
        /// </summary>
        [Fact]
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

            Assert.Equal(0, this.bindCallCount);
            Assert.Equal(1, this.timeoutCallCount);
            Assert.True(this.isCancelationRequested);
        }
    }
}
