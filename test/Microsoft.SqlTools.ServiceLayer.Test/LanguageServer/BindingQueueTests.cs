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
            this.BindingLocked = new ManualResetEvent(initialState: false);
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
    public class BindingQueuTests
    {
        private IBindingContext GetMockBindingContext()
        {
            return new TestBindingContext();
        }

        private Task TestBindOperation(
            IBindingContext bindContext, 
            EventContext eventContext, 
            CancellationToken cancelToken)
        {
            return null;
        }

        //  Func<IBindingContext, EventContext, CancellationToken, Task> bindOperation,
        //     Func<IBindingContext, EventContext, Task> timeoutOperation = null)

        [Fact]
        public void LatestSqlParserIsUsedByDefault()
        {
            var bindingContext = GetMockBindingContext();
            
            var bindingQueue = new BindingQueue<TestBindingContext>();
            bindingQueue.QueueBindingOperation(
                key: "testkey",
                eventContext: null,
                bindOperation: null,
                timeoutOperation: null);
            
            bindingQueue.StopQueueProcessor(15000);                
        }


    }
}
