//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
        private const string OWNERURI = "testDB";
        private ObjectManagementService objectManagementService;
        private SqlTestDb testDb;
        private Mock<RequestContext<bool>> requestContextMock;

        [SetUp]
        public async Task TestInitialize()
        {
            this.testDb = await SqlTestDb.CreateNewAsync(serverType: TestServerType.OnPrem, query: TableQuery, dbNamePrefix: "RenameTest");

            requestContextMock = new Mock<RequestContext<bool>>();
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, OWNERURI, ConnectionType.Default);

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
            Assert.IsTrue(queryRenameObject.HasExecuted);
            Assert.IsFalse(queryRenameObject.HasErrored);
            Assert.AreEqual(1, queryRenameObject.Batches[0].ResultSets[0].RowCount);

            Query queryOldObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.tables WHERE name='testTable1_RenamingTable'");
            Assert.IsTrue(queryOldObject.HasExecuted);
            Assert.IsFalse(queryOldObject.HasErrored);
            Assert.AreEqual(0, queryOldObject.Batches[0].ResultSets[0].RowCount);
        }

        [Test]
        public async Task TestRenameColumn()
        {
            //arrange & act
            await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenameColumn", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']/Column[@Name='C1']", testDb.DatabaseName)), requestContextMock.Object);

            //assert
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => r == true)));

            Query queryRenameObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.columns WHERE name='RenameColumn'");
            Assert.IsTrue(queryRenameObject.HasExecuted);
            Assert.IsFalse(queryRenameObject.HasErrored);
            Assert.AreEqual(1, queryRenameObject.Batches[0].ResultSets[0].RowCount);

            Query queryOldObject = ExecuteQuery("SELECT * FROM " + testDb.DatabaseName + ".sys.columns WHERE name='C1'");
            Assert.IsTrue(queryOldObject.HasExecuted);
            Assert.IsFalse(queryOldObject.HasErrored);
            Assert.AreEqual(0, queryOldObject.Batches[0].ResultSets[0].RowCount);
        }

        [Test]
        public async Task TestRenameColumnNotExisting()
        {
            Assert.ThrowsAsync<FailedOperationException>(async () =>
            {
                await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenameColumn", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']/Column[@Name='C1_NOT']", testDb.DatabaseName)), requestContextMock.Object);
            });

        }

        [Test]
        public async Task TestRenameTableNotExisting()
        {
            Assert.ThrowsAsync<FailedOperationException>(async () =>
            {
                await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenamingTable", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_Not' and @Schema='dbo']", testDb.DatabaseName)), requestContextMock.Object);
            });
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
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await objectManagementService.HandleRenameRequest(testRenameRequestParams, requestContextMock.Object);

            });
        }

        private RenameRequestParams InitRequestParams(string newName, string UrnOfObject)
        {
            return new RenameRequestParams
            {
                NewName = newName,
                ConnectionUri = OWNERURI,
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