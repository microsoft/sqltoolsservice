//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    public class ObjectManagementServiceTests
    {
        private const string TableQuery = @"CREATE TABLE testTable1_RenamingTable (c1 int)";
        private const string OwnerUri = "testDB";
        private ObjectManagementService objectManagementService;
        private SqlTestDb testDb;
        private Mock<RequestContext<bool>> requestContextMock;

        [SetUp]
        public async Task TestInitialize()
        {
            this.testDb = await SqlTestDb.CreateNewAsync(serverType: TestServerType.OnPrem, query: TableQuery, dbNamePrefix: "RenameTest");

            requestContextMock = new Mock<RequestContext<bool>>();
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, OwnerUri, ConnectionType.Default);

            ObjectManagementService.ConnectionServiceInstance = connectionService;
            this.objectManagementService = new ObjectManagementService();
        }

        [TearDown]
        public async Task TearDownTestDatabase()
        {
            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameTable()
        {
            //arrange & act
            await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenamingTable", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']", testDb.DatabaseName)), requestContextMock.Object);

            //assert
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => r == true)));

            Query queryRenameObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.tables WHERE name='RenamingTable'");
            Assert.That(queryRenameObject.HasExecuted, Is.True, "The query to check for the renamed table was not executed");
            Assert.That(queryRenameObject.HasErrored, Is.False, "There were errors on the execution of the query to check for the renamed table");
            Assert.That(queryRenameObject.Batches[0].ResultSets[0].RowCount, Is.EqualTo(1), "Did not find the table with the new name after the rename operation");

            Query queryOldObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.tables WHERE name='testTable1_RenamingTable'");
            Assert.That(queryOldObject.HasExecuted, Is.True, "The query to check for the old table was not executed");
            Assert.That(queryOldObject.HasErrored, Is.False, "There were errors on the execution of the query to check for the old table");
            Assert.That(queryOldObject.Batches[0].ResultSets[0].RowCount, Is.EqualTo(0), "Did find the old table which should have been renamed");
        }

        [Test]
        public async Task TestRenameColumn()
        {
            //arrange & act
            await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenameColumn", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']/Column[@Name='C1']", testDb.DatabaseName)), requestContextMock.Object);

            //assert
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => r == true)));

            Query queryRenameObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.columns WHERE name='RenameColumn'");
            Assert.That(queryRenameObject.HasExecuted, Is.True, "The query to check for the renamed column was not executed");
            Assert.That(queryRenameObject.HasErrored, Is.False, "There were errors on the execution of the query to check for the renamed column");
            Assert.That(queryRenameObject.Batches[0].ResultSets[0].RowCount, Is.EqualTo(1), "Did not find the column with the new name after the rename operation");

            Query queryOldObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.columns WHERE name='C1'");
            Assert.That(queryOldObject.HasExecuted, Is.True, "The query to check for the old column was not executed");
            Assert.That(queryOldObject.HasErrored, Is.False, "There were errors on the execution of the query to check for the old column");
            Assert.That(queryOldObject.Batches[0].ResultSets[0].RowCount, Is.EqualTo(0), "Did find the old column which should have been renamed");
        }

        [Test]
        public async Task TestRenameColumnNotExisting()
        {
            Assert.That(async () =>
            {
                await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenameColumn", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']/Column[@Name='C1_NOT']", testDb.DatabaseName)), requestContextMock.Object);
            }, Throws.Exception.TypeOf<FailedOperationException>(), "Did find the column, which should not have existed");
        }

        [Test]
        public async Task TestRenameTableNotExisting()
        {
            Assert.That(async () =>
            {
                await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenamingTable", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_Not' and @Schema='dbo']", testDb.DatabaseName)), requestContextMock.Object);
            }, Throws.Exception.TypeOf<FailedOperationException>(), "Did find the table, which should not have existed");
        }

        [Test]
        public async Task TestConnectionNotFound()
        {
            var testRenameRequestParams = new RenameRequestParams
            {
                NewName = "RenamingTable",
                ConnectionUri = "NOT_EXISTING",
                ObjectUrn = String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_Not' and @Schema='dbo']", testDb.DatabaseName),
            };
            Assert.That(async () =>
            {
                await objectManagementService.HandleRenameRequest(testRenameRequestParams, requestContextMock.Object);

            }, Throws.Exception.TypeOf<Exception>(), "Did find the connection, which should not have existed");
        }

        private RenameRequestParams InitRequestParams(string newName, string UrnOfObject)
        {
            return new RenameRequestParams
            {
                NewName = newName,
                ConnectionUri = OwnerUri,
                ObjectUrn = UrnOfObject,
            };
        }

        private Query ExecuteQuery(string queryText)
        {
            TestConnectionResult conResult = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = conResult.ConnectionInfo;
            IFileStreamFactory fileStreamFactory = MemoryFileSystem.GetFileStreamFactory(new ConcurrentDictionary<string, byte[]>());

            QueryExecutionSettings settings = new QueryExecutionSettings() { IsSqlCmdMode = false };
            Query query = new Query(queryText, connInfo, settings, fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }
    }
}