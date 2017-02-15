//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
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
            
            Thread.Sleep(2000);

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

            this.bindCallbackDelay = 1000;

            this.bindingQueue.QueueBindingOperation(
                key: "testkey",
                bindingTimeout: bindCallbackDelay / 2,
                bindOperation: TestBindOperation,
                timeoutOperation: TestTimeoutOperation);

            Thread.Sleep(this.bindCallbackDelay + 100);
            
            this.bindingQueue.StopQueueProcessor(15000);

            Assert.True(this.bindCallCount == 0);
            Assert.True(this.timeoutCallCount == 1);
            Assert.True(this.isCancelationRequested);
        }

        /// <summary>
        /// Test overwriting the binding queue context
        /// </summary>
        [Fact]
        public void OverwriteBindingContext()
        {
            InitializeTestSettings();

            // default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(QueryExecution.Common.StandardQuery);
            fileMock.SetupGet(file => file.ClientFilePath).Returns("file://file1.sql");

            // set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            // setup binding queue mock
            // var bindingQueue = new Mock<ConnectedBindingQueue>();
            // bindingQueue.Setup(q => q.AddConnectionContext(It.IsAny<ConnectionInfo>(), It.IsAny<bool>()))
            //      .Returns("connectionKey");

            // inject mock instances into the Language Service
            LanguageService.WorkspaceServiceInstance = workspaceService.Object;
            LanguageService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            LanguageService.ConnectionServiceInstance.OwnerToConnectionMap.Add("file://file1.sql", connectionInfo);
           // LanguageService.Instance.BindingQueue =  bindingQueue.Object;


            var connectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(connectionInfo);
        }
    }
}
