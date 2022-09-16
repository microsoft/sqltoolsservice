//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.BatchParser.Utility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    public class ObjectManagementServiceTests
    {
        private readonly string table_query = @"CREATE TABLE testTable1_RenamingTable (c1 int)
                            GO
                            ";
        private readonly string ownerUri = "testDB";
        private readonly string startTableName = "testTable1_RenamingTable";
        private readonly string startColumnName = "c1";
        private ObjectManagementService objectManagementService;
        private SqlTestDb testDb;
        private Mock<RequestContext<bool>> requestContextMock;

        [SetUp]
        public async Task TestInitialize()
        {
            this.testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, table_query, "RenameTest");

            requestContextMock = new Mock<RequestContext<bool>>();
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, this.ownerUri, ConnectionType.Default);

            ObjectManagementService.ConnectionServiceInstance = connectionService;
            this.objectManagementService = new ObjectManagementService();
        }

        [Test]
        public async Task TestRenameTable()
        {
            await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenamingTable", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']", testDb.DatabaseName)), requestContextMock.Object);
            Thread.Sleep(2000);
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => VerifySendedFeedback(r, true))));
            await CheckGeneratedScript(testDb.DatabaseName, "CREATE TABLE [dbo].[RenamingTable]");

            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameColumn()
        {
            await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenameColumn", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']/Column[@Name='C1']", testDb.DatabaseName)), requestContextMock.Object);
            Thread.Sleep(2000);
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => VerifySendedFeedback(r, true))));
            await CheckGeneratedScript(testDb.DatabaseName, "[RenameColumn] [int] NULL");

            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameColumnNotExisting()
        {
            Assert.ThrowsAsync<FailedOperationException>(async () =>
            {
                await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenameColumn", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_RenamingTable' and @Schema='dbo']/Column[@Name='C1_NOT']", testDb.DatabaseName)), requestContextMock.Object);
                Thread.Sleep(2000);
            });

            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameTableNotExisting()
        {
            Assert.ThrowsAsync<FailedOperationException>(async () =>
            {
                await objectManagementService.HandleRenameRequest(this.InitRequestParams("RenamingTable", String.Format("Server/Database[@Name='{0}']/Table[@Name='testTable1_Not' and @Schema='dbo']", testDb.DatabaseName)), requestContextMock.Object);
                Thread.Sleep(2000);
            });
            await SqlTestDb.DropDatabase(testDb.DatabaseName);
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
                Thread.Sleep(2000);
            });
            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        private static bool VerifySendedFeedback(bool result, bool expectedResult)
        {
            return result == expectedResult;
        }

        private RenameRequestParams InitRequestParams(string newName, string UrnOfObject)
        {
            return new RenameRequestParams
            {
                NewName = newName,
                ConnectionUri = this.ownerUri,
                ObjectUrn = UrnOfObject,
            };
        }

        private async Task CheckGeneratedScript(string databaseName, string expectedResult)
        {
            var requestContext = new Mock<RequestContext<ScriptingResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingResult>())).Returns(Task.FromResult(new object()));
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(databaseName, queryTempFile.FilePath, ConnectionType.Default);
                var scriptingParams = new ScriptingParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    ScriptDestination = "ToEditor",
                    Operation = ScriptingOperationType.Create
                };
                scriptingParams.ScriptOptions = new ScriptOptions
                {
                    ScriptCreateDrop = "ScriptCreate",
                };
                ScriptingService service = new ScriptingService();
                await service.HandleScriptExecuteRequest(scriptingParams, requestContext.Object);
                Thread.Sleep(2000);
                await service.ScriptingTask;
            }
            requestContext.Verify(x => x.SendResult(It.Is<ScriptingResult>(r =>
                VerifyScriptingResult(r, expectedResult)
            )));
        }
        private static bool VerifyScriptingResult(ScriptingResult result, string expectedScript)
        {
            Logger.Verbose(result.Script);
            if (!result.Script.Contains(expectedScript))
            {
                return false;
            }
            return true;
        }

        protected void VerifyErrorSent<T>(Mock<RequestContext<T>> contextMock)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Never);
            contextMock.Verify(c => c.SendError(It.IsAny<Exception>()), Times.Once);
        }

    }
}