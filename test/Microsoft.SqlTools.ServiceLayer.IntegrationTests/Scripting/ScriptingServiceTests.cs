//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Moq;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Scripting
{
    /// <summary>
    /// Tests for the scripting service component
    /// </summary>
    public class ScriptingServiceTests
    {
        private const string SchemaName = "dbo";
        private const string TableName = "spt_monitor";
        private const string ViewName = "test";
        private const string DatabaseName = "test-db";
        private const string StoredProcName = "test-sp";
        private string[] objects = new string[5] { "Table", "View", "Schema", "Database", "SProc" };
        private string[] selectObjects = new string[2] { "Table", "View" };

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Test.Common.Constants.OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            result.TextDocumentPosition = textDocument;
            return result;
        }

        private async Task<Mock<RequestContext<ScriptingResult>>> SendAndValidateScriptRequest(bool isSelectScript)
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<ScriptingResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingResult>())).Returns(Task.FromResult(new object()));

            var scriptingParams = new ScriptingParams
            {
                OwnerUri = ConnectionService.BuildConnectionString(result.ConnectionInfo.ConnectionDetails)
            };
            if (isSelectScript)
            {
                scriptingParams.ScriptOptions = new ScriptOptions { ScriptCreateDrop = "ScriptSelect" };
                List<ScriptingObject> scriptingObjects = new List<ScriptingObject>();
                scriptingObjects.Add(new ScriptingObject { Type = "View", Name = "sysobjects", Schema = "sys" });
                scriptingParams.ScriptingObjects = scriptingObjects;
            }
            ScriptingService service = new ScriptingService();
            await service.HandleScriptExecuteRequest(scriptingParams, requestContext.Object);

            return requestContext;
        }

        /// <summary>
        /// Verify the script object request
        /// </summary>
        [Fact]
        public async void ScriptingScript()
        {
            foreach (string obj in objects)
            {
                Assert.NotNull(await SendAndValidateScriptRequest(false));
                Assert.NotNull(await SendAndValidateScriptRequest(true));
            }
        }

        [Fact]
        public async void VerifyScriptAsCreateTable()
        {
            string query = "CREATE TABLE testTable1 (c1 int)";
            string scriptCreateDrop = "ScriptCreate";
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testTable1",
                Schema = "dbo",
                Type = "Table"
            };
            string expectedScript = "CREATE TABLE [dbo].[testTable1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsCreateView()
        {
            string query = "CREATE VIEW testView1 AS SELECT * from sys.all_columns";
            string scriptCreateDrop = "ScriptCreate";
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testView1",
                Schema = "dbo",
                Type = "View"
            };
            string expectedScript = "CREATE VIEW [dbo].[testView1] AS";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsCreateStoredProcedure()
        {
            string query = "CREATE PROCEDURE testSp1 AS  BEGIN Select * from sys.all_columns END";
            string scriptCreateDrop = "ScriptCreate";
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testSp1",
                Schema = "dbo",
                Type = "StoredProcedure"
            };
            string expectedScript = "CREATE PROCEDURE [dbo].[testSp1] AS";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsDropTable()
        {
            string query = "CREATE TABLE testTable1 (c1 int)";
            string scriptCreateDrop = "ScriptDrop";
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testTable1",
                Schema = "dbo",
                Type = "Table"
            };
            string expectedScript = "DROP TABLE [dbo].[testTable1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsDropView()
        {
            string query = "CREATE VIEW testView1 AS SELECT * from sys.all_columns";
            string scriptCreateDrop = "ScriptDrop";
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testView1",
                Schema = "dbo",
                Type = "View"
            };
            string expectedScript = "DROP VIEW [dbo].[testView1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsDropStoredProcedure()
        {
            string query = "CREATE PROCEDURE testSp1 AS  BEGIN Select * from sys.all_columns END";
            string scriptCreateDrop = "ScriptDrop";
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testSp1",
                Schema = "dbo",
                Type = "StoredProcedure"
            };
            string expectedScript = "DROP PROCEDURE [dbo].[testSp1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        private async Task VerifyScriptAs(string query, ScriptingObject scriptingObject, string scriptCreateDrop, string expectedScript)
        {
            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, "ScriptingTests");
            try
            {
                var requestContext = new Mock<RequestContext<ScriptingResult>>();
                requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingResult>())).Returns(Task.FromResult(new object()));
                ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    //Opening a connection to db to lock the db
                    TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, queryTempFile.FilePath, ConnectionType.Default);
                    var scriptingParams = new ScriptingParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        ScriptDestination = "ToEditor"
                    };

                    scriptingParams.ScriptOptions = new ScriptOptions
                    {
                        ScriptCreateDrop = scriptCreateDrop,

                    };

                    scriptingParams.ScriptingObjects = new List<ScriptingObject>
                     {
                        scriptingObject
                    };


                    ScriptingService service = new ScriptingService();
                    await service.HandleScriptingScriptAsRequest(scriptingParams, requestContext.Object);
                    Thread.Sleep(2000);
                    await service.ScriptingTask;

                    requestContext.Verify(x => x.SendResult(It.Is<ScriptingResult>(r => VerifyScriptingResult(r, expectedScript))));
                    connectionService.Disconnect(new ServiceLayer.Connection.Contracts.DisconnectParams
                    {
                        OwnerUri = queryTempFile.FilePath
                    });
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                await testDb.CleanupAsync();
            }
        }

        private static bool VerifyScriptingResult(ScriptingResult result, string expected)
        {
            return !string.IsNullOrEmpty(result.Script) && result.Script.Contains(expected);
        }
    }
}
