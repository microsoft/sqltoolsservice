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
        


        [Test]
        public async Task QueryExecutionSessionOptionsPersistAcrossQueries()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            { // check the initial default transaction level
                var queryResult = await ExecuteAndVerifyQuery(
                    "SELECT transaction_isolation_level FROM sys.dm_exec_sessions WHERE session_id=@@SPID;",
                    connectionInfo.OwnerUri);

                Assert.AreEqual(queryResult.RowCount, 1);
                Int16 transactionLevel = (Int16)queryResult.Rows[0][0].RawObject;
                Assert.AreEqual(1, transactionLevel);
            }

            { // change the transaction level and run a query
                var queryResult = await ExecuteAndVerifyQuery(
                    "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; SELECT transaction_isolation_level FROM sys.dm_exec_sessions WHERE session_id=@@SPID;",
                    connectionInfo.OwnerUri);

                Assert.AreEqual(queryResult.RowCount, 1);
                Int16 transactionLevel = (Int16)queryResult.Rows[0][0].RawObject;
                Assert.AreEqual(4, transactionLevel);
            }

            { // rerun the query without setting execution option and confirm previous option persists
                var queryResult = await ExecuteAndVerifyQuery(
                    "SELECT transaction_isolation_level FROM sys.dm_exec_sessions WHERE session_id=@@SPID;",
                    connectionInfo.OwnerUri);

                Assert.AreEqual(queryResult.RowCount, 1);
                Int16 transactionLevel = (Int16)queryResult.Rows[0][0].RawObject;
                Assert.AreEqual(4, transactionLevel);
            }
        }

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
            Thread.Sleep(TimeSpan.FromSeconds(1));

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
    }
}
