//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using NUnit.Framework;
using StreamJsonRpc;

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
            await QueryExecutionService.Instance.HandleConnectionUriChangedNotification(executeParams);

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
            var executeParams = new ExecuteStringParams
            {
                OwnerUri = ownerUri,
                Query = query
            };
            ExecuteRequestResult result = await QueryExecutionService.Instance.HandleExecuteRequest(executeParams);

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

            SubsetResult subsetResult = await QueryExecutionService.Instance.HandleResultSubsetRequest(subsetParams);

            Assert.NotNull(subsetResult);
            return subsetResult.ResultSubset;
        }

        /// <summary>
        /// Test that SimpleExecuteRequest collects and returns SQL error messages for queries with errors
        /// </summary>
        [Test]
        public void SimpleExecuteRequestReturnsErrorMessages()
        {
            // Establish a new connection
            ConnectionService.Instance.OwnerToConnectionMap.Clear();
            ConnectionInfo connectionInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "SELECT * FROM NonExistentTable123456;"
            };

            LocalRpcException? ex = Assert.ThrowsAsync<LocalRpcException>(async () =>
                await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams));

            Assert.NotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("NonExistentTable123456") || ex.Message.Contains("Invalid object name"),
                $"Error message should contain table name or invalid object error. Got: {ex.Message}");
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

            // Use a query that doesn't return a result set - DECLARE creates a variable
            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "DECLARE @TestVar INT; SET @TestVar = 1;"
            };

            SimpleExecuteResult result = await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.RowCount, "Row count should be 0 for queries without result sets");
            Assert.IsNotNull(result.ColumnInfo, "Column info should not be null");
            Assert.AreEqual(0, result.ColumnInfo.Length, "Column info should be empty for queries without result sets");
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

            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "PRINT 'Test message from PRINT'; SELECT 1 AS Value;"
            };

            SimpleExecuteResult result = await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.Messages, "Messages should not be null");
            Assert.IsTrue(result.Messages.Length > 0, "Messages array should contain at least one message");
            
            var printMessage = result.Messages.FirstOrDefault(m => m.Message.Contains("Test message from PRINT"));
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

            var executeParams = new SimpleExecuteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                QueryString = "PRINT 'First message'; PRINT 'Second message'; SELECT 1 AS Value;"
            };

            SimpleExecuteResult result = await QueryExecutionService.Instance.HandleSimpleExecuteRequest(executeParams);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.Messages, "Messages should not be null");
            Assert.IsTrue(result.Messages.Length >= 2, $"Messages array should contain at least 2 PRINT messages, found {result.Messages.Length}");
            
            var firstMessage = result.Messages.FirstOrDefault(m => m.Message.Contains("First message"));
            var secondMessage = result.Messages.FirstOrDefault(m => m.Message.Contains("Second message"));
            
            Assert.IsNotNull(firstMessage, "First PRINT message should be captured");
            Assert.IsNotNull(secondMessage, "Second PRINT message should be captured");
            Assert.IsFalse(firstMessage!.IsError, "First PRINT message should not be marked as error");
            Assert.IsFalse(secondMessage!.IsError, "Second PRINT message should not be marked as error");
        }
    }
}
