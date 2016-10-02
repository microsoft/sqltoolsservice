//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
{

    /// <summary>
    /// Test class for the test binding context
    /// </summary>
    public class TestBindingContext : IBindingContext
    {
        public TestBindingContext()
        {
            this.BindingLocked = new ManualResetEvent(initialState: true);
            this.BindingTimeout = 3000;
        }

        public bool IsConnected { get; set; }

        public ServerConnection ServerConnection { get; set; }

        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        public SmoMetadataProvider SmoMetadataProvider { get; set; }

        public IBinder Binder { get; set; }

        public ManualResetEvent BindingLocked { get; set; } 

        public int BindingTimeout { get; set; } 
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
        private Task<object> TestBindOperation(
            IBindingContext bindContext, 
            CancellationToken cancelToken)
        {            
            return  Task.Run(() => 
            {
                cancelToken.WaitHandle.WaitOne(this.bindCallbackDelay);
                this.isCancelationRequested = cancelToken.IsCancellationRequested;
                if (!this.isCancelationRequested)
                {
                    ++this.bindCallCount;
                }
                return new CompletionItem[0] as object;
            });
        }

        /// <summary>
        /// Test callback for the bind timeout operation
        /// </summary>
        private Task<object> TestTimeoutOperation(
            IBindingContext bindingContext)
        {
            ++this.timeoutCallCount;
            return  Task.FromResult(new CompletionItem[0] as object);
        }

        /// <summary>
        /// Runs for a few seconds to allow the queue to pump any requests
        /// </summary>
        private void WaitForQueue(int delay = 5000)
        {
            int step = 50;
            int steps = delay / step + 1;
            for (int i = 0; i < steps; ++i)
            {
                Thread.Sleep(step);
            }
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

            WaitForQueue();        
            
            this.bindingQueue.StopQueueProcessor(15000);     

            Assert.True(this.bindCallCount == 1);
            Assert.True(this.timeoutCallCount == 0);  
            Assert.False(this.isCancelationRequested);
        }

        /// <summary>
        /// Queue a 100 short tasks
        /// </summary>
        [Fact]
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
            
            WaitForQueue();

            this.bindingQueue.StopQueueProcessor(15000);     

            Assert.True(this.bindCallCount == 100);
            Assert.True(this.timeoutCallCount == 0);
            Assert.False(this.isCancelationRequested);
        }

        /// <summary>
        /// Queue an task with a long operation causing a timeout
        /// </summary>
        [Fact]
        public void QueueWithTimeout()
        {
            InitializeTestSettings();

            this.bindCallbackDelay = 10000;

            this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindOperation: TestBindOperation,
                timeoutOperation: TestTimeoutOperation);

            WaitForQueue(this.bindCallbackDelay + 2000);
            
            this.bindingQueue.StopQueueProcessor(15000);

            Assert.True(this.bindCallCount == 0);
            Assert.True(this.timeoutCallCount == 1);
            Assert.True(this.isCancelationRequested);
        }
    }
}
