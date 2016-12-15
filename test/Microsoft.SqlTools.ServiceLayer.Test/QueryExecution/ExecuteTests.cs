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

        /// <summary>
        /// If we perform a ROLLBACK TRANSACTION without a BEGIN TRANSACTION,
        /// we should get an error.
        /// </summary>
        [Fact]
        public void RollbackTransactionFailsWithoutBeginTransaction()
        {
            const string refactorText = "ROLLBACK TRANSACTION";
            ScriptFile scriptFile;

            // Given a connection to a live database
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I run a "ROLLBACK TRANSACTION" query
            Query query = new Query(refactorText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // There should be an error
            Assert.True(query.Batches[0].HasError);
        }

        /// <summary>
        /// If we perform a BEGIN TRANSACTION in one query then a ROLLBACK TRANSACTION in another
        /// query, we should get no errors.
        /// </summary>
        [Fact]
        public void TransactionsSuceedAcrossQueries()
        {
            const string beginText = "BEGIN TRANSACTION";
            const string rollbackText = "ROLLBACK TRANSACTION";
            ScriptFile scriptFile;

            // Given a connection to a live database
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I run a "BEGIN TRANSACTION" query
            Query beginQuery = new Query(beginText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            beginQuery.Execute();
            beginQuery.ExecutionTask.Wait();

            // Then I run a "ROLLBACK TRANSACTION" query
            Query rollbackQuery = new Query(rollbackText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            rollbackQuery.Execute();
            rollbackQuery.ExecutionTask.Wait();

            // There should be no errors
            Assert.False(rollbackQuery.Batches[0].HasError);
        }

        /// <summary>
        /// If we create a tamp table in one query and reference it in another query,
        /// we should not get an error.
        /// </summary>

        [Fact]
        public void TempTablesPersistAcrossQueries()
        {
            const string createTempText = "CREATE TABLE #someTempTable (id int)";
            const string insertTempText = "INSERT INTO #someTempTable VALUES(1)";
            ScriptFile scriptFile;

            // Given a connection to a live database
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I run a "BEGIN TRANSACTION" query
            Query createTempQuery = new Query(createTempText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            createTempQuery.Execute();
            createTempQuery.ExecutionTask.Wait();

            // Then I run a "ROLLBACK TRANSACTION" query
            Query insertTempQuery = new Query(insertTempText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            insertTempQuery.Execute();
            insertTempQuery.ExecutionTask.Wait();

            // There should be no errors
            Assert.False(insertTempQuery.Batches[0].HasError);
        }

#endif
    }
}
