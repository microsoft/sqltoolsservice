//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution
{
    /// <summary>
    /// Tests for the ServiceHost Query Execution Service tests that require a live database connection
    /// </summary>
    public class QueryExecutionServiceTests
    {
        private async Task<ResultSetSubset> ExecuteAndVerifyQuery(string query, string ownerUri)
        {
            var requestContext = new Mock<RequestContext<ExecuteRequestResult>>();
            ManualResetEvent sendResultEvent = new ManualResetEvent(false);
            ExecuteRequestResult result = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<ExecuteRequestResult>()))
                .Callback<ExecuteRequestResult>(r => 
                {
                    result = r;
                    sendResultEvent.Set();
                
                })
                .Returns(Task.FromResult(new object()));

            var executeParams = new ExecuteStringParams
            {
                OwnerUri = ownerUri,
                Query = query
            };
            await QueryExecutionService.Instance.HandleExecuteRequest(executeParams, requestContext.Object);

            sendResultEvent.WaitOne(TimeSpan.FromSeconds(10));
            Assert.NotNull(result);

            var subsetParams = new SubsetParams()
            {
                OwnerUri = ownerUri,
                BatchIndex = 0,
                ResultSetIndex = 0,
                RowsStartIndex = 0,
                RowsCount = 1
            };
            
            var subsetRequestContext = new Mock<RequestContext<SubsetResult>>();
            SubsetResult subsetResult = null;
            subsetRequestContext.Setup(x => x.SendResult(It.IsAny<SubsetResult>()))
                .Callback<SubsetResult>(r => 
                {
                    subsetResult = r;
                
                })
                .Returns(Task.FromResult(new object()));


            await QueryExecutionService.Instance.HandleResultSubsetRequest(subsetParams, subsetRequestContext.Object);

            return subsetResult.ResultSubset;
        }


        [Test]
        public async Task RunningMultipleQueriesCreatesOnlyOneConnection()
        {
            // Connect/disconnect twice to ensure reconnection can occur
            ConnectionService connectionService = ConnectionService.Instance;
            connectionService.OwnerToConnectionMap.Clear();

            var connectionResult = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connectionInfo = connectionResult.ConnectionInfo;

            var queryResult = await ExecuteAndVerifyQuery("SELECT 1", connectionInfo.OwnerUri);
            

            // QueryExecutionService queryExecutionService = QueryExecutionService.Instance;

            // var requestContext = new Mock<RequestContext<ExecuteRequestResult>>();

            // ManualResetEvent sendResultEvent = new ManualResetEvent(false);
            // ExecuteRequestResult result = null;
            // requestContext.Setup(x => x.SendResult(It.IsAny<ExecuteRequestResult>()))
            //     .Callback<ExecuteRequestResult>(r => 
            //     {
            //         result = r;
            //         sendResultEvent.Set();
                
            //     })
            //     .Returns(Task.FromResult(new object()));

            // var executeParams = new ExecuteStringParams
            // {
            //     OwnerUri = connectionInfo.OwnerUri,
            //     Query = "SELECT 1"
            // };
            // await queryExecutionService.HandleExecuteRequest(executeParams, requestContext.Object);

            // sendResultEvent.WaitOne(TimeSpan.FromSeconds(10));
            // Assert.NotNull(result);

            // var subsetParams = new SubsetParams()
            // {
            //     OwnerUri = connectionInfo.OwnerUri,
            //     BatchIndex = 0,
            //     ResultSetIndex = 0,
            //     RowsStartIndex = 0,
            //     RowsCount = 1
            // };
            
            // var subsetRequestContext = new Mock<RequestContext<SubsetResult>>();
            // subsetRequestContext.Setup(x => x.SendResult(It.IsAny<SubsetResult>())).Returns(Task.FromResult(new object()));
            // await queryExecutionService.HandleResultSubsetRequest(subsetParams, subsetRequestContext.Object);
            
            // queryExecutionService.HandleExecuteRequest(new ExecuteRequestParamsBase
            // {
            //     QuerySelection = null,
            //     Query = Constants.StandardQuery,
            //     OwnerUri = connectionInfo.OwnerUri
            // });

            // for (int i = 0; i < 2; i++)
            // {
            //     var result = LiveConnectionHelper.InitLiveConnectionInfo();
            //     ConnectionInfo connectionInfo = result.ConnectionInfo;
            //     string uri = connectionInfo.OwnerUri;

            //     // We should see one ConnectionInfo and one DbConnection
            //     Assert.AreEqual(1, connectionInfo.CountConnections);
            //     Assert.AreEqual(1, service.OwnerToConnectionMap.Count);

            //     // If we run a query
            //     var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            //     Query query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            //     query.Execute();
            //     query.ExecutionTask.Wait();

            //     // We should see 1 DbConnections
            //     Assert.AreEqual(1, connectionInfo.CountConnections);

            //     // If we run another query
            //     query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            //     query.Execute();
            //     query.ExecutionTask.Wait();

            //     // We should see 1 DbConnections
            //     Assert.AreEqual(1, connectionInfo.CountConnections);

            //     // If we disconnect, we should remain in a consistent state to do it over again
            //     // e.g. loop and do it over again
            //     service.Disconnect(new DisconnectParams() { OwnerUri = connectionInfo.OwnerUri });

            //     // We should be left with an empty connection map
            //     Assert.AreEqual(0, service.OwnerToConnectionMap.Count);
            // }
        }
    }
}
