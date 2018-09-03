//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

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
        public void LoggerVerifyScriptAsCreateTable()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = System.Reflection.MethodInfo.GetCurrentMethod().Name,
                EventType = System.Diagnostics.TraceEventType.Information,
                TracingLevel = System.Diagnostics.SourceLevels.Information,
            };

            test.Initialize();
            VerifyScriptAsCreateTable(); // This should emit log.
            test.LogMessage = "An expected log message based on running of the VerifyAllSqlObjects() test";
            test.Verify(); // The log message should be absent since the tracing level is set to Off.
            test.Cleanup();

        }

        [Fact]
        public async void VerifyScriptAsCreateTable()
        {
            string query = @"CREATE TABLE testTable1 (c1 int)
                            GO
                            CREATE CLUSTERED INDEX [ClusteredIndex-1] ON [dbo].[testTable1]
                            (
	                            [c1] ASC
                            )
                            GO
                            ";
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Create;
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testTable1",
                Schema = "dbo",
                Type = "Table"
            };
            List<string> expectedScripts = new List<string> { "CREATE TABLE [dbo].[testTable1]", "CREATE CLUSTERED INDEX [ClusteredIndex-1] ON [dbo].[testTable1]" };


            await VerifyScriptAsForMultipleObjects(query, new List<ScriptingObject> { scriptingObject }, scriptCreateDrop, expectedScripts);
        }

        [Fact]
        public async void VerifyScriptAsExecuteTableFailes()
        {
            string query = "CREATE TABLE testTable1 (c1 int)";
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Execute;
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testTable1",
                Schema = "dbo",
                Type = "Table"
            };
            string expectedScript = null;
            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsAlter()
        {
            string query = @"CREATE PROCEDURE testSp1 @StartProductID [int] AS  BEGIN Select * from sys.all_columns END
                            GO
                            CREATE VIEW testView1 AS SELECT * from sys.all_columns
                            GO
                            CREATE FUNCTION testFun1() RETURNS [int] AS BEGIN RETURN 1 END
                            GO";
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Alter;

            List<ScriptingObject> scriptingObjects = new List<ScriptingObject>
            {
                new ScriptingObject
                {
                    Name = "testSp1",
                    Schema = "dbo",
                    Type = "StoredProcedure"
                },
                new ScriptingObject
                {
                    Name = "testView1",
                    Schema = "dbo",
                    Type = "View"
                },
                new ScriptingObject
                {
                    Name = "testFun1",
                    Schema = "dbo",
                    Type = "UserDefinedFunction"
                }
            };
            List<string> expectedScripts = new List<string>
            {
                "ALTER PROCEDURE [dbo].[testSp1]",
                "ALTER VIEW [dbo].[testView1]",
                "ALTER FUNCTION [dbo].[testFun1]"
            };

            await VerifyScriptAsForMultipleObjects(query, scriptingObjects, scriptCreateDrop, expectedScripts);
        }

        // TODO: Fix flaky test. See https://github.com/Microsoft/sqltoolsservice/issues/631
        // [Fact]
        public async void VerifyScriptAsExecuteStoredProcedure()
        {
            string query = @"CREATE PROCEDURE testSp1
                @BusinessEntityID [int],
                @JobTitle [nvarchar](50),
                @HireDate [datetime],
                @RateChangeDate [datetime],
                @Rate [money],
                @PayFrequency [tinyint]
                AS
                BEGIN Select * from sys.all_columns END";
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Execute;
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testSp1",
                Schema = "dbo",
                Type = "StoredProcedure"
            };
            string expectedScript = "EXECUTE @RC = [dbo].[testSp1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsSelectTable()
        {
            string query = "CREATE TABLE testTable1 (c1 int)";
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Select;
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testTable1",
                Schema = "dbo",
                Type = "Table"
            };
            string expectedScript = "SELECT TOP (1000) [c1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        [Fact]
        public async void VerifyScriptAsCreateView()
        {
            string query = "CREATE VIEW testView1 AS SELECT * from sys.all_columns";
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Create;
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
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Create;
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
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Delete;
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
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Delete;
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
            ScriptingOperationType scriptCreateDrop = ScriptingOperationType.Delete;
            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "testSp1",
                Schema = "dbo",
                Type = "StoredProcedure"
            };
            string expectedScript = "DROP PROCEDURE [dbo].[testSp1]";

            await VerifyScriptAs(query, scriptingObject, scriptCreateDrop, expectedScript);
        }

        private async Task VerifyScriptAs(string query, ScriptingObject scriptingObject, ScriptingOperationType operation, string expectedScript)
        {
            await VerifyScriptAsForMultipleObjects(query, new List<ScriptingObject> { scriptingObject }, operation, new List<string> { expectedScript });
        }

        private async Task VerifyScriptAsForMultipleObjects(string query, List<ScriptingObject> scriptingObjects, ScriptingOperationType operation, List<string> expectedScripts)
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
                        ScriptDestination = "ToEditor",
                        Operation = operation
                    };

                    string scriptCreateOperation = "ScriptCreate";
                    if(operation == ScriptingOperationType.Delete)
                    {
                        scriptCreateOperation = "ScriptDrop";
                    }
                    else
                    {
                        scriptCreateOperation = $"Script{operation}";
                    }

                    scriptingParams.ScriptOptions = new ScriptOptions
                    {
                        ScriptCreateDrop = scriptCreateOperation,
                    };

                    scriptingParams.ScriptingObjects = scriptingObjects;


                    ScriptingService service = new ScriptingService();
                    await service.HandleScriptExecuteRequest(scriptingParams, requestContext.Object);
                    Thread.Sleep(2000);
                    await service.ScriptingTask;

                    requestContext.Verify(x => x.SendResult(It.Is<ScriptingResult>(r => VerifyScriptingResult(r, expectedScripts))));
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

        private static bool VerifyScriptingResult(ScriptingResult result, List<string> expectedScripts)
        {
            if (expectedScripts == null || (expectedScripts.Count > 0 && expectedScripts.All(x => x == null)))
            {
                return string.IsNullOrEmpty(result.Script);
            }

            foreach (string expectedScript in expectedScripts)
            {
                if (!result.Script.Contains(expectedScript))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
