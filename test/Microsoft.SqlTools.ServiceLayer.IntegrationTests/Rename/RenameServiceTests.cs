//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.BatchParser.Utility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Rename;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Rename
{
    public class RenameServiceTests
    {
        private readonly string table_query = @"CREATE TABLE testTable1_RenamingTable (c1 int)
                            GO
                            ";
        private readonly string ownerUri = "testDB";
        private readonly string startTableName = "testTable1_RenamingTable";
        private readonly string startColumnName = "c1";
        private RenameService renameService;
        private SqlTestDb testDb;
        private Mock<RequestContext<bool>> requestContextMock;

        [SetUp]
        public async Task TestInitialize()
        {
            this.testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, table_query, "RenameTest");

            requestContextMock = new Mock<RequestContext<bool>>();
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, this.ownerUri, ConnectionType.Default);

            RenameService.ConnectionServiceInstance = connectionService;
            this.renameService = new RenameService();
        }

        [Test]
        public async Task TestRenameTable()
        {
            await renameService.HandleProcessRenameEditRequest(this.InitRequestParams(testDb, "RenamingTable", this.startTableName, this.startTableName, "dbo", ChangeType.TABLE), requestContextMock.Object);
            Thread.Sleep(2000);
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => VerifySendedFeedback(r, true))));
            await CheckGeneratedScript(testDb.DatabaseName, "CREATE TABLE [dbo].[RenamingTable]");

            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameColumn()
        {
            await renameService.HandleProcessRenameEditRequest(this.InitRequestParams(testDb, "RenameColumn", this.startTableName, this.startColumnName, "dbo", ChangeType.COLUMN), requestContextMock.Object);
            Thread.Sleep(2000);
            requestContextMock.Verify(x => x.SendResult(It.Is<bool>(r => VerifySendedFeedback(r, true))));
            await CheckGeneratedScript(testDb.DatabaseName, "[RenameColumn] [int] NULL");

            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameColumnNotExisting()
        {
            await renameService.HandleProcessRenameEditRequest(this.InitRequestParams(testDb, "RenameColumn", this.startTableName, this.startColumnName + "_NOT", "dbo", ChangeType.COLUMN), requestContextMock.Object);
            Thread.Sleep(2000);

            VerifyErrorSent(requestContextMock);
            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        [Test]
        public async Task TestRenameTableNotExisting()
        {
            await renameService.HandleProcessRenameEditRequest(this.InitRequestParams(testDb, "RenameColumn", this.startTableName, this.startTableName + "_NOT", "dbo", ChangeType.TABLE), requestContextMock.Object);
            Thread.Sleep(2000);

            VerifyErrorSent(requestContextMock);
            await SqlTestDb.DropDatabase(testDb.DatabaseName);
        }

        private static bool VerifySendedFeedback(bool result, bool expectedResult)
        {
            return result == expectedResult;
        }

        private ProcessRenameEditRequestParams InitRequestParams(SqlTestDb testDb, string newName, string TableName, string OldName, string schema, ChangeType type)
        {
            RenameTableChangeInfo changeInfo = new RenameTableChangeInfo
            {
                NewName = newName,
                Type = type
            };
            RenameTableInfo info = new RenameTableInfo
            {
                Database = testDb.DatabaseName,
                Id = "1",
                OldName = OldName,
                Schema = schema,
                OwnerUri = this.ownerUri,
                TableName = TableName
            };

            return new ProcessRenameEditRequestParams
            {
                ChangeInfo = changeInfo,
                TableInfo = info
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
            contextMock.Verify(c => c.SendError(It.IsAny<InvalidOperationException>()), Times.Once);
        }

    }
}