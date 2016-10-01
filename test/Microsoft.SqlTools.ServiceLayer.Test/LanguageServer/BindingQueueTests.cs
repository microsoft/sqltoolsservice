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
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
{

    public class TestBindingContext : IBindingContext
    {
        public TestBindingContext()
        {
            this.BindingLocked = new ManualResetEvent(initialState: true);
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
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class BindingQueueTests
    {
        private int bindCallCount = 0;
        
        private int timeoutCallCount = 0;

        private IBindingContext GetMockBindingContext()
        {
            return new TestBindingContext();
        }

        private Task TestBindOperation(
            IBindingContext bindContext, 
            EventContext eventContext, 
            CancellationToken cancelToken)
        {
            ++this.bindCallCount;
            return  Task.FromResult(0);
        }

        private Task TestTimeoutOperation(
            IBindingContext bindingContext, 
            EventContext eventContext)
        {
            ++this.timeoutCallCount;
            return  Task.FromResult(0);
        }

     
        [Fact]
        public void QueueOneBindingOperationTest()
        {
            this.bindCallCount = 0;
            this.timeoutCallCount = 0;

            var bindingContext = GetMockBindingContext();
            
            var bindingQueue = new BindingQueue<TestBindingContext>();
            bindingQueue.QueueBindingOperation(
                key: "testkey",
                eventContext: null,
                bindOperation: TestBindOperation,
                timeoutOperation: TestTimeoutOperation);

            for (int i = 0; i < 60; ++i)
            {
                Thread.Sleep(50);
            }
            
            bindingQueue.StopQueueProcessor(15000);     

            Assert.True(this.bindCallCount == 1);
            Assert.False(this.timeoutCallCount == 0);       
        }

        [Fact]
        public void Queue100BindingOperationTest()
        {
            this.bindCallCount = 0;
            this.timeoutCallCount = 0;

            var bindingContext = GetMockBindingContext();
            
            var bindingQueue = new BindingQueue<TestBindingContext>();

            for (int i = 0; i < 100; ++i)
            {
                bindingQueue.QueueBindingOperation(
                    key: "testkey",
                    eventContext: null,
                    bindOperation: TestBindOperation,
                    timeoutOperation: TestTimeoutOperation);
            }
            for (int i = 0; i < 60; ++i)
            {
                Thread.Sleep(50);
            }
            
            bindingQueue.StopQueueProcessor(15000);     

            Assert.True(this.bindCallCount == 100);
            Assert.False(this.timeoutCallCount == 0);       
        }


    }
}
