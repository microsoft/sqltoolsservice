using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution
{
    public class ExecuteTests
    {
        [Fact]
        public async Task RollbackTransactionFailsWithoutBeginTransaction()
        {
            const string refactorText = "ROLLBACK TRANSACTION";

            // Given a connection to a live database
            var result = await TestObjects.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I run a "ROLLBACK TRANSACTION" query
            Query query = new Query(refactorText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // There should be an error
            Assert.True(query.Batches[0].HasError);
        }

        [Fact]
        public async Task TransactionsSucceedAcrossQueries()
        {
            const string beginText = "BEGIN TRANSACTION";
            const string rollbackText = "ROLLBACK TRANSACTION";

            // Given a connection to a live database
            var result = await TestObjects.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I run a "BEGIN TRANSACTION" query
            CreateAndExecuteQuery(beginText, connInfo, fileStreamFactory);

            // Then I run a "ROLLBACK TRANSACTION" query, there should be no errors
            Query rollbackQuery = CreateAndExecuteQuery(rollbackText, connInfo, fileStreamFactory);
            Assert.False(rollbackQuery.Batches[0].HasError);
        }

        [Fact]
        public async Task TempTablesPersistAcrossQueries()
        {
            const string createTempText = "CREATE TABLE #someTempTable (id int)";
            const string insertTempText = "INSERT INTO #someTempTable VALUES(1)";

            // Given a connection to a live database
            var result = await TestObjects.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I run a query creating a temp table
            CreateAndExecuteQuery(createTempText, connInfo, fileStreamFactory);

            // Then I run a different query using that temp table, there should be no errors
            Query insertTempQuery = CreateAndExecuteQuery(insertTempText, connInfo, fileStreamFactory);
            Assert.False(insertTempQuery.Batches[0].HasError);
        }

        [Fact]
        public async Task DatabaseChangesWhenCallingUseDatabase()
        {
            const string master = "master";
            const string tempdb = "tempdb";
            const string useQuery = "USE {0}";

            // Given a connection to a live database
            var result = await TestObjects.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            DbConnection connection;
            connInfo.TryGetConnection(ConnectionType.Default, out connection);

            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());

            // If I use master, the current database should be master
            CreateAndExecuteQuery(string.Format(useQuery, master), connInfo, fileStreamFactory);
            Assert.Equal(master, connection.Database);

            // If I use tempdb, the current database should be tempdb
            CreateAndExecuteQuery(string.Format(useQuery, tempdb), connInfo, fileStreamFactory);
            Assert.Equal(tempdb, connection.Database);

            // If I switch back to master, the current database should be master
            CreateAndExecuteQuery(string.Format(useQuery, master), connInfo, fileStreamFactory);
            Assert.Equal(master, connection.Database);
        }

        public Query CreateAndExecuteQuery(string queryText, ConnectionInfo connectionInfo, IFileStreamFactory fileStreamFactory)
        {
            Query query = new Query(queryText, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }
    }
}
