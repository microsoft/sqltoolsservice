//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using System;
using System.IO;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution
{
    public class SqlCmdExecutionTest
    {
        [Fact]
        public void TestConnectSqlCmdCommand()
        {
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            ConnectionInfo connInfo = liveConnection.ConnectionInfo;
            string serverName = liveConnection.ConnectionInfo.ConnectionDetails.ServerName;
            string sqlCmdQuerySuccess = $@"
:Connect {serverName}
select * from sys.databases where name = 'master'
GO";

            Query query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQuerySuccess, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches.Length == 1, $"Expected: 1 parsed batch, actual : {query.Batches.Length}");
            Assert.True(query.Batches[0].HasExecuted && !query.Batches[0].HasError && query.Batches[0].ResultSets.Count == 1, "Query should be executed and have one result set");

            string sqlCmdQueryFilaure = $@"
:Connect SomeWrongName
select * from sys.databases where name = 'master'
GO";

            query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQueryFilaure, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches.Length == 1, $"Expected: 1 parsed batch, actual : {query.Batches.Length}");
            Assert.True(query.Batches[0].HasError, "Query should have error");
        }

        [Fact]
        public void TestOnErrorSqlCmdCommand()
        {
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            ConnectionInfo connInfo = liveConnection.ConnectionInfo;
            string sqlCmdQuerySuccess = $@"
:on error ignore
GO
select * from sys.databases_wrong where name = 'master'
GO
select * from sys.databases where name = 'master'
GO";

            Query query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQuerySuccess, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches[0].HasExecuted && query.Batches[0].HasError, "first batch should be executed and have error");
            Assert.True(query.Batches[1].HasExecuted, "last batch should be executed");


            string sqlCmdQueryFilaure = $@"
:on error exit
GO
select * from sys.databases_wrong where name = 'master'
GO
select * from sys.databases where name = 'master'
GO";

            query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQueryFilaure, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches[0].HasExecuted && query.Batches[0].HasError, "first batch should be executed and have error");
            Assert.False(query.Batches[1].HasExecuted, "last batch should NOT be executed");
        }

        [Fact]
        public void TestIncludeSqlCmdCommand()
        {
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            ConnectionInfo connInfo = liveConnection.ConnectionInfo;
            string path = Path.Combine(Environment.CurrentDirectory, "mysqlfile.sql");
            string sqlPath = "\"" + path + "\"";

            // correct sql file text
            string correctfileText = $@"
select * from sys.databases where name = 'msdb' or name = 'master'
GO";

            // incorrect sql file text
            string incorrectfileText = $@"
select * from sys.databases_wrong where name = 'msdb' or name = 'master'
GO";

            File.WriteAllText(path, correctfileText);

            string sqlCmdQuerySuccess = $@"
:on error exit
:setvar mypath {sqlPath}
GO
:r $(mypath)
GO
select * from sys.databases where name = 'master'
GO";

            Query query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQuerySuccess, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches.Length == 2, $"Batches should be parsed and should be 2, actual number {query.Batches.Length}");
            Assert.True(query.Batches[0].HasExecuted && !query.Batches[0].HasError && query.Batches[0].ResultSets.Count == 1 && query.Batches[0].ResultSets[0].RowCount == 2, "first batch should be executed and have 2 results");
            Assert.True(query.Batches[1].HasExecuted && !query.Batches[1].HasError && query.Batches[1].ResultSets.Count == 1 && query.Batches[1].ResultSets[0].RowCount == 1, "second batch should be executed and have 1 result");


            string sqlCmdQueryFilaure1 = $@"
:on error exit
:setvar mypath somewrongpath
GO
:r $(mypath)
GO
select * from sys.databases where name = 'master'
GO";

            query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQueryFilaure1, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches.Length == 0, $"Batches should be 0 since parsing was aborted, actual number {query.Batches.Length}");

            File.WriteAllText(path, incorrectfileText);

            string sqlCmdQueryFilaure2 = $@"
:on error exit
:setvar mypath {sqlPath}
GO
:r $(mypath)
GO
select * from sys.databases where name = 'master'
GO";

            query = ExecuteTests.CreateAndExecuteQuery(sqlCmdQueryFilaure2, connInfo, fileStreamFactory, IsSqlCmd: true);
            Assert.True(query.Batches.Length == 2, $"Batches should be parsed and should be 2, actual number {query.Batches.Length}");
            Assert.True(query.Batches[0].HasExecuted && query.Batches[0].HasError, "first batch should be executed and have error");
            Assert.True(!query.Batches[1].HasExecuted, "second batch should not get to be executed because of the first error");
        }
    }
}
