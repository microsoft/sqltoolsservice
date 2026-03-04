//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
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
        /// <summary>
        /// Test that session-level query execution options are persisted across executions on an active 
        /// connection.  This runs a query with the default options, changes the options and runs query
        /// in a batch, then finally runs a query to confirm the options changes are still applied.
        /// </summary>
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

        /// <summary>
        /// Tests that verifies that the session-level query execution options persist after changing
        /// the connection URI.  The connection URI changes when a script document is renamed.
        /// </summary>
        [Test]
        public async Task QueryExecutionSessionOptionsPersistsAfterUriChange()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            { // check the initial ansi_nulls option value
                var queryResult = await ExecuteAndVerifyQuery(
                    "SELECT ansi_nulls FROM sys.dm_exec_sessions WHERE session_id=@@SPID;",
                    connectionInfo.OwnerUri);

                Assert.AreEqual(queryResult.RowCount, 1);
                bool ansiNullsOn = (bool)queryResult.Rows[0][0].RawObject;
                Assert.AreEqual(true, ansiNullsOn);
            }

            { // change the ansi_nulls value to OFF
                var queryResult = await ExecuteAndVerifyQuery(
                    "SET ANSI_NULLS OFF; SELECT ansi_nulls FROM sys.dm_exec_sessions WHERE session_id=@@SPID;",
                    connectionInfo.OwnerUri);

                Assert.AreEqual(queryResult.RowCount, 1);
                bool ansiNullsOn = (bool)queryResult.Rows[0][0].RawObject;
                Assert.AreEqual(false, ansiNullsOn);
            }

            string newOwnerUri = "renamed_" + connectionInfo.OwnerUri + "_renamed";
            var executeParams = new ConnectionUriChangedParams
            {
                OriginalOwnerUri = connectionInfo.OwnerUri,
                NewOwnerUri = newOwnerUri
            };

            // change the connection URI
            var requestContext = new Mock<EventContext>();
            await QueryExecutionService.Instance.HandleConnectionUriChangedNotification(executeParams, requestContext.Object);

            { // rerun the query with new URI and confirm previous option persists
                var queryResult = await ExecuteAndVerifyQuery(
                    "SELECT ansi_nulls FROM sys.dm_exec_sessions WHERE session_id=@@SPID;",
                    newOwnerUri);

                Assert.AreEqual(queryResult.RowCount, 1);
                bool ansiNullsOn = (bool)queryResult.Rows[0][0].RawObject;
                Assert.AreEqual(false, ansiNullsOn);
            }
        }

        private async Task<ResultSetSubset> ExecuteAndVerifyQuery(string query, string ownerUri)
        {
            var requestContext = new Mock<RequestContext<ExecuteRequestResult>>();
            ManualResetEvent sendResultEvent = new ManualResetEvent(false);
            ExecuteRequestResult? result = null;
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
            SubsetResult? subsetResult = null;
            subsetRequestContext.Setup(x => x.SendResult(It.IsAny<SubsetResult>()))
                .Callback<SubsetResult>(r =>
                {
                    subsetResult = r;
                })
                .Returns(Task.FromResult(new object()));


            await QueryExecutionService.Instance.HandleResultSubsetRequest(subsetParams, subsetRequestContext.Object);

            return subsetResult.ResultSubset;
        }

        /// <summary>
        /// Test that SimpleExecuteRequest collects and returns SQL error messages for queries with errors
        /// </summary>
        [Test]
        public async Task SimpleExecuteRequestReturnsErrorMessages()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            var requestContext = new Mock<RequestContext<SimpleExecuteResult>>();
            ManualResetEvent errorEvent = new ManualResetEvent(false);
            string? errorMessage = null;

            requestContext.Setup(x => x.SendError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback<string, int, string>((error, code, data) =>
                {
                    errorMessage = error;
                    errorEvent.Set();
                })
                .Returns(Task.FromResult(new object()));

            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "SELECT * FROM NonExistentTable123456;"
            };

            await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams, requestContext.Object);

            // Wait for error to be sent
            bool gotError = errorEvent.WaitOne(TimeSpan.FromSeconds(10));
            Assert.IsTrue(gotError, "Expected error message was not received");
            Assert.IsNotNull(errorMessage, "Error message should not be null");
            Assert.IsTrue(errorMessage!.Contains("NonExistentTable123456") || errorMessage!.Contains("Invalid object name"),
                $"Error message should contain table name or invalid object error. Got: {errorMessage}");
        }

        /// <summary>
        /// Test that SimpleExecuteRequest returns success with empty columns for queries with no result sets (UPDATE, DELETE, etc.)
        /// </summary>
        [Test]
        public async Task SimpleExecuteRequestSucceedsForNoResultSetQueries()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            var requestContext = new Mock<RequestContext<SimpleExecuteResult>>();
            ManualResetEvent resultEvent = new ManualResetEvent(false);
            SimpleExecuteResult? result = null;

            requestContext.Setup(x => x.SendResult(It.IsAny<SimpleExecuteResult>()))
                .Callback<SimpleExecuteResult>(r =>
                {
                    result = r;
                    resultEvent.Set();
                })
                .Returns(Task.FromResult(new object()));

            // Use a query that doesn't return a result set - DECLARE creates a variable
            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "DECLARE @TestVar INT; SET @TestVar = 1;"
            };

            await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams, requestContext.Object);

            // Wait for result to be sent
            bool gotResult = resultEvent.WaitOne(TimeSpan.FromSeconds(10));
            Assert.IsTrue(gotResult, "Expected result was not received");
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result!.RowCount, "Row count should be 0 for queries without result sets");
            Assert.IsNotNull(result!.ColumnInfo, "Column info should not be null");
            Assert.AreEqual(0, result!.ColumnInfo.Length, "Column info should be empty for queries without result sets");
        }

        /// <summary>
        /// Test that SimpleExecuteRequest captures and returns PRINT statement messages
        /// </summary>
        [Test]
        public async Task SimpleExecuteRequestReturnsPrintMessages()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            var requestContext = new Mock<RequestContext<SimpleExecuteResult>>();
            ManualResetEvent resultEvent = new ManualResetEvent(false);
            SimpleExecuteResult? result = null;

            requestContext.Setup(x => x.SendResult(It.IsAny<SimpleExecuteResult>()))
                .Callback<SimpleExecuteResult>(r =>
                {
                    result = r;
                    resultEvent.Set();
                })
                .Returns(Task.FromResult(new object()));

            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "PRINT 'Test message from PRINT'; SELECT 1 AS Value;"
            };

            await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams, requestContext.Object);

            // Wait for result to be sent
            bool gotResult = resultEvent.WaitOne(TimeSpan.FromSeconds(10));
            Assert.IsTrue(gotResult, "Expected result was not received");
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result!.Messages, "Messages should not be null");
            Assert.IsTrue(result!.Messages.Length > 0, "Messages array should contain at least one message");
            
            var printMessage = result!.Messages.FirstOrDefault(m => m.Message.Contains("Test message from PRINT"));
            Assert.IsNotNull(printMessage, "PRINT message should be captured in Messages array");
            Assert.IsFalse(printMessage!.IsError, "PRINT message should not be marked as error");
        }

        /// <summary>
        /// Test that SimpleExecuteRequest captures both PRINT messages and result completion messages
        /// </summary>
        [Test]
        public async Task SimpleExecuteRequestReturnsMultipleMessages()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            var requestContext = new Mock<RequestContext<SimpleExecuteResult>>();
            ManualResetEvent resultEvent = new ManualResetEvent(false);
            SimpleExecuteResult? result = null;

            requestContext.Setup(x => x.SendResult(It.IsAny<SimpleExecuteResult>()))
                .Callback<SimpleExecuteResult>(r =>
                {
                    result = r;
                    resultEvent.Set();
                })
                .Returns(Task.FromResult(new object()));

            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "PRINT 'First message'; PRINT 'Second message'; SELECT 1 AS Value;"
            };

            await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams, requestContext.Object);

            // Wait for result to be sent
            bool gotResult = resultEvent.WaitOne(TimeSpan.FromSeconds(10));
            Assert.IsTrue(gotResult, "Expected result was not received");
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result!.Messages, "Messages should not be null");
            Assert.IsTrue(result!.Messages.Length >= 2, $"Messages array should contain at least 2 PRINT messages, found {result!.Messages.Length}");
            
            var firstMessage = result!.Messages.FirstOrDefault(m => m.Message.Contains("First message"));
            var secondMessage = result!.Messages.FirstOrDefault(m => m.Message.Contains("Second message"));
            
            Assert.IsNotNull(firstMessage, "First PRINT message should be captured");
            Assert.IsNotNull(secondMessage, "Second PRINT message should be captured");
            Assert.IsFalse(firstMessage!.IsError, "First PRINT message should not be marked as error");
            Assert.IsFalse(secondMessage!.IsError, "Second PRINT message should not be marked as error");
        }
    }
}
