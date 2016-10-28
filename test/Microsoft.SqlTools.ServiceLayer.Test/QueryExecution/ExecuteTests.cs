//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//#define USE_LIVE_CONNECTION

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Moq;

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
            Query query = new Query(Common.UdtQuery, connectionInfo, new QueryExecutionSettings(), Common.GetFileStreamFactory());

            // If:
            // ... I then execute the query
            DateTime startTime = DateTime.Now;
            query.Execute().Wait();

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

        private static void VerifyQueryExecuteCallCount(Mock<RequestContext<QueryExecuteResult>> mock, Times sendResultCalls, Times sendEventCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type),
                It.IsAny<QueryExecuteCompleteParams>()), sendEventCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        private static DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
        }

        private static async Task AwaitExecution(QueryExecutionService service, QueryExecuteParams qeParams,
            RequestContext<QueryExecuteResult> requestContext)
        {
            await service.HandleExecuteRequest(qeParams, requestContext);
            await service.ActiveQueries[qeParams.OwnerUri].ExecutionTask;
        }
    }
}
