﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution
{
    public class ExecuteTests
    {
        [Test]
        public void RollbackTransactionFailsWithoutBeginTransaction()
        {
            const string refactorText = "ROLLBACK TRANSACTION";

            // Given a connection to a live database
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();

            // If I run a "ROLLBACK TRANSACTION" query
            Query query = new Query(refactorText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // There should be an error
            Assert.True(query.Batches[0].HasError);
        }

        [Test]
        public void TransactionsSucceedAcrossQueries()
        {
            const string beginText = "BEGIN TRANSACTION";
            const string rollbackText = "ROLLBACK TRANSACTION";

            // Given a connection to a live database
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();

            // If I run a "BEGIN TRANSACTION" query
            CreateAndExecuteQuery(beginText, connInfo, fileStreamFactory);

            // Then I run a "ROLLBACK TRANSACTION" query, there should be no errors
            Query rollbackQuery = CreateAndExecuteQuery(rollbackText, connInfo, fileStreamFactory);
            Assert.False(rollbackQuery.Batches[0].HasError);
        }

        [Test]
        public void TempTablesPersistAcrossQueries()
        {
            const string createTempText = "CREATE TABLE #someTempTable (id int)";
            const string insertTempText = "INSERT INTO #someTempTable VALUES(1)";

            // Given a connection to a live database
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory(new ConcurrentDictionary<string, byte[]>());

            // If I run a query creating a temp table
            CreateAndExecuteQuery(createTempText, connInfo, fileStreamFactory);

            // Then I run a different query using that temp table, there should be no errors
            Query insertTempQuery = CreateAndExecuteQuery(insertTempText, connInfo, fileStreamFactory);
            Assert.False(insertTempQuery.Batches[0].HasError);
        }

        [Test]
        public void DatabaseChangesWhenCallingUseDatabase()
        {
            const string master = "master";
            const string tempdb = "tempdb";
            const string useQuery = "USE {0}";

            // Given a connection to a live database
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            DbConnection connection;
            connInfo.TryGetConnection(ConnectionType.Default, out connection);

            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory(new ConcurrentDictionary<string, byte[]>());

            // If I use master, the current database should be master
            CreateAndExecuteQuery(string.Format(useQuery, master), connInfo, fileStreamFactory);
            Assert.AreEqual(master, connInfo.ConnectionDetails.DatabaseName);

            // If I use tempdb, the current database should be tempdb
            CreateAndExecuteQuery(string.Format(useQuery, tempdb), connInfo, fileStreamFactory);
            Assert.AreEqual(tempdb, connInfo.ConnectionDetails.DatabaseName);

            // If I switch back to master, the current database should be master
            CreateAndExecuteQuery(string.Format(useQuery, master), connInfo, fileStreamFactory);
            Assert.AreEqual(master, connInfo.ConnectionDetails.DatabaseName);
        }

        public static Query CreateAndExecuteQuery(string queryText, ConnectionInfo connectionInfo, IFileStreamFactory fileStreamFactory, bool IsSqlCmd = false)
        {
            var settings = new QueryExecutionSettings() { IsSqlCmdMode = IsSqlCmd };
            Query query = new Query(queryText, connectionInfo, settings, fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }

    }
}
