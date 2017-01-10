//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//#define USE_LIVE_CONNECTION

using System.Data.Common;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecuteTests
    {

#if USE_LIVE_CONNECTION
        [Fact]
        public void QueryUdtShouldNotRetry()
        {
            // If:
            // ... I create a query with a udt column in the result set
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            Query query = new Query(Common.UdtQuery, connectionInfo, new QueryExecutionSettings(), Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));

            // If:
            // ... I then execute the query
            DateTime startTime = DateTime.Now;
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... The query should complete within 2 seconds since retry logic should not kick in
            Assert.True(DateTime.Now.Subtract(startTime) < TimeSpan.FromSeconds(2), "Query completed slower than expected, did retry logic execute?");

            // Then:
            // ... There should be an error on the batch
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(1, query.BatchSummaries.Length);
            Assert.True(query.BatchSummaries[0].HasError);
            Assert.NotEmpty(query.BatchSummaries[0].Messages);
        }
#endif
    }
}
