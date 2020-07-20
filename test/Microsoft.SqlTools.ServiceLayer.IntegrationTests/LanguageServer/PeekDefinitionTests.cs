//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using Xunit;
using ConnectionType = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServices
{
    /// <summary>
    /// Tests for the language service peek definition/ go to definition feature
    /// </summary>
    public class PeekDefinitionTests
    {
        private const string OwnerUri = "testFile1";
        private const string TestUri = "testFile2";
        private const string ReturnTableFunctionName = "pd_returnTable";
        private const string ReturnTableTableFunctionQuery = @"
CREATE FUNCTION [dbo].[" + ReturnTableFunctionName + @"] ()
RETURNS TABLE
AS
RETURN
(
    select * from master.dbo.spt_monitor
);

GO";

        private const string AddTwoFunctionName = "pd_addTwo";
        private const string AddTwoFunctionQuery = @"
CREATE FUNCTION[dbo].[" + AddTwoFunctionName + @"](@number int)
RETURNS int
AS
BEGIN
    RETURN @number + 2;
        END;

GO";


        private const string SsnTypeName = "pd_ssn";
        private const string SsnTypeQuery = @"
CREATE TYPE [dbo].[" + SsnTypeName + @"] FROM [varchar](11) NOT NULL
GO";

        private const string LocationTableTypeName = "pd_locationTableType";

        private const string LocationTableTypeQuery = @"
CREATE TYPE [dbo].[" + LocationTableTypeName + @"] AS TABLE(
    [LocationName] [varchar](50) NULL,
    [CostRate] [int] NULL
)
GO";

        private const string TestTableSynonymName = "pd_testTable";
        private const string TestTableSynonymQuery = @"
CREATE SYNONYM [dbo].[pd_testTable] FOR master.dbo.spt_monitor
GO";

        private const string TableValuedFunctionTypeName = "TableValuedFunction";
        private const string ScalarValuedFunctionTypeName = "UserDefinedFunction";
        private const string UserDefinedDataTypeTypeName = "UserDefinedDataType";
        private const string UserDefinedTableTypeTypeName = "UserDefinedTableType";
        private const string SynonymTypeName = "Synonym";
        private const string StoredProcedureTypeName = "StoredProcedure";
        private const string ViewTypeName = "View";
        private const string TableTypeName = "Table";

        /// <summary>
        /// Test get definition for a table object with active connection
        /// </summary>
        [Fact]
        public void GetValidTableDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "spt_monitor";

            string schemaName = null;
            string objectType = "Table";

            // Get locations for valid table object
            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        [Fact]
        public void LoggerGetValidTableDefinitionTest()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = System.Reflection.MethodInfo.GetCurrentMethod().Name,
                EventType = System.Diagnostics.TraceEventType.Information,
                TracingLevel = System.Diagnostics.SourceLevels.All,
            };

            test.Initialize();
            GetValidTableDefinitionTest(); // This should emit log.from SMO code base
            test.LogMessage = "OnScriptingProgress ScriptingCompleted"; //Log message to verify. This message comes from SMO code.
            test.Verify(); // The log message should be absent since the tracing level is set to Off.
            test.Cleanup();

        }

        /// <summary>
        /// Test get definition for a invalid table object with active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "test_invalid";
            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for invalid table object
            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a valid table object with schema and active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionWithSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "spt_monitor";

            string schemaName = "dbo";
            string objectType = "Table";

            // Get locations for valid table object with schema name
            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test GetDefinition with an unsupported type(schema - dbo). Expect a error result.
        /// </summary>
        [Fact]
        public void GetUnsupportedDefinitionErrorTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            // When I try to get definition for 'Collation'
            DefinitionResult result = scripter.GetDefinitionUsingDeclarationType(DeclarationType.Collation, "master.sys.objects", objectName, schemaName);
            // Then I expect non null result with error flag set
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Get Definition for a object with no definition. Expect a error result
        /// </summary>
        [Fact]
        public void GetDefinitionWithNoResultsFoundError()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "from";
            Position position = new Position()
            {
                Line = 1,
                Character = 14
            };
            ScriptParseInfo scriptParseInfo = new ScriptParseInfo() { IsConnected = true };
            Mock<IBindingContext> bindingContextMock = new Mock<IBindingContext>();
            DefinitionResult result = scripter.GetScript(scriptParseInfo.ParseResult, position, bindingContextMock.Object.MetadataDisplayInfoProvider, objectName, null);

            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
            Assert.Equal(SR.PeekDefinitionNoResultsError, result.Message);
        }

        /// <summary>
        /// Test GetDefinition with a forced timeout. Expect a error result.
        /// </summary>
        [Fact]
        public void GetDefinitionTimeoutTest()
        {
            // Given a binding queue that will automatically time out
            var languageService = new LanguageService();
            Mock<ConnectedBindingQueue> queueMock = new Mock<ConnectedBindingQueue>();
            languageService.BindingQueue = queueMock.Object;
            ManualResetEvent mre = new ManualResetEvent(true); // Do not block
            Mock<QueueItem> itemMock = new Mock<QueueItem>();
            itemMock.Setup(i => i.ItemProcessed).Returns(mre);

            DefinitionResult timeoutResult = null;

            queueMock.Setup(q => q.QueueBindingOperation(
                It.IsAny<string>(),
                It.IsAny<Func<IBindingContext, CancellationToken, object>>(),
                It.IsAny<Func<IBindingContext, object>>(),
                It.IsAny<Func<Exception, object>>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()))
            .Callback<string, Func<IBindingContext, CancellationToken, object>, Func<IBindingContext, object>, Func<Exception, object>, int?, int?>(
                (key, bindOperation, timeoutOperation, errHandler, t1, t2) =>
                {
                    if(timeoutOperation != null)
                    {
                        timeoutResult = (DefinitionResult)timeoutOperation(null);
                    }
                    
                    itemMock.Object.Result = timeoutResult;
                })
            .Returns(() => itemMock.Object);

            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 20
                }
            };
            LiveConnectionHelper.TestConnectionResult connectionResult = LiveConnectionHelper.InitLiveConnectionInfo();
            ScriptFile scriptFile = connectionResult.ScriptFile;
            scriptFile.Contents = "select * from dbo.func ()";

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(scriptFile.ClientUri, scriptInfo);

            // Pass in null connection info to force doing a local parse since that hits the BindingQueue timeout
            // before we want it to (this is testing the timeout trying to fetch the definitions after the parse)
            var result = languageService.GetDefinition(textDocument, scriptFile, null);

            // Then I expect null locations and an error to be reported
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
            // Check timeout message
            Assert.Equal(SR.PeekDefinitionTimedoutError, result.Message);
        }

        /// <summary>
        /// Test get definition for a view object with active connection
        /// </summary>
        [Fact]
        public void GetValidViewDefinitionTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            string objectType = "View";

            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for an invalid view object with no schema name and with active connection
        /// </summary>
        [Fact]
        public void GetViewDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = null;
            string objectType = "VIEW";

            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "sp_columns";

            string schemaName = "sys";
            string objectType = "StoredProcedure";

            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object that does not exist with active connection
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "SP2";
            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection and no schema
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionWithoutSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "sp_MSrepl_startup";
            string schemaName = null;
            string objectType = "StoredProcedure";

            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetScalarValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(AddTwoFunctionQuery, AddTwoFunctionName, ScalarValuedFunctionTypeName);
        }

        private async Task ExecuteAndValidatePeekTest(string query, string objectName, string objectType, string schemaName = "dbo")
        {
            if (!string.IsNullOrEmpty(query))
            {
                SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query);
                ValidatePeekTest(testDb.DatabaseName, objectName, objectType, schemaName, true);
                await testDb.CleanupAsync();
            }
            else
            {
                ValidatePeekTest(null, objectName, objectType, schemaName, false);
            }
        }

        private void ValidatePeekTest(string databaseName, string objectName, string objectType, string schemaName, bool shouldReturnValidResult)
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition(databaseName);
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);

            Location[] locations = scripter.GetSqlObjectDefinition(objectName, schemaName, objectType);
            if (shouldReturnValidResult)
            {
                Assert.NotNull(locations);
                Cleanup(locations);
            }
            else
            {
                Assert.Null(locations);
            }

            var connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
            connectionService.Disconnect(new DisconnectParams
            {
                    OwnerUri = connInfo.OwnerUri
            });
        }

        /// <summary>
        /// Test get definition for a table valued function object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetTableValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(ReturnTableTableFunctionQuery, ReturnTableFunctionName, ScalarValuedFunctionTypeName);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetScalarValuedFunctionDefinitionWithNonExistentFailureTest()
        {
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = ScalarValuedFunctionTypeName;

            await ExecuteAndValidatePeekTest(null, objectName, objectType, schemaName);
        }

        /// <summary>
        /// Test get definition for a table valued function object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetTableValuedFunctionDefinitionWithNonExistentObjectFailureTest()
        {
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = TableValuedFunctionTypeName;
            await ExecuteAndValidatePeekTest(null, objectName, objectType, schemaName);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetScalarValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(AddTwoFunctionQuery, AddTwoFunctionName, ScalarValuedFunctionTypeName, null);
        }

        /// <summary>
        /// Test get definition for a table valued function object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetTableValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(ReturnTableTableFunctionQuery, ReturnTableFunctionName, ScalarValuedFunctionTypeName, null);
        }


        /// <summary>
        /// Test get definition for a user defined data type object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedDataTypeDefinitionWithSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(SsnTypeQuery, SsnTypeName, UserDefinedDataTypeTypeName);
        }

        /// <summary>
        /// Test get definition for a user defined data type object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedDataTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(SsnTypeQuery, SsnTypeName, UserDefinedDataTypeTypeName, null);
        }

        /// <summary>
        /// Test get definition for a user defined data type object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedDataTypeDefinitionWithNonExistentFailureTest()
        {
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = UserDefinedDataTypeTypeName;
            await ExecuteAndValidatePeekTest(null, objectName, objectType, schemaName);
        }

        /// <summary>
        /// Test get definition for a user defined table type object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedTableTypeDefinitionWithSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(LocationTableTypeQuery, LocationTableTypeName, UserDefinedTableTypeTypeName);
        }

        /// <summary>
        /// Test get definition for a user defined table type object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedTableTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(LocationTableTypeQuery, LocationTableTypeName, UserDefinedTableTypeTypeName, null);
        }

        /// <summary>
        /// Test get definition for a user defined table type object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedTableTypeDefinitionWithNonExistentFailureTest()
        {
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = UserDefinedTableTypeTypeName;
            await ExecuteAndValidatePeekTest(null, objectName, objectType, schemaName);

        }

        /// <summary>
        /// Test get definition for a synonym object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetSynonymDefinitionWithSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(TestTableSynonymQuery, TestTableSynonymName, SynonymTypeName);
        }


        /// <summary>
        /// Test get definition for a Synonym object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetSynonymDefinitionWithoutSchemaNameSuccessTest()
        {
            await ExecuteAndValidatePeekTest(TestTableSynonymQuery, TestTableSynonymName, SynonymTypeName, null);
        }

        /// <summary>
        /// Test get definition for a Synonym object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetSynonymDefinitionWithNonExistentFailureTest()
        {
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Synonym";
            await ExecuteAndValidatePeekTest(null, objectName, objectType, schemaName);
        }

        /// <summary>
        /// Test get definition using declaration type for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingDeclarationTypeWithValidObjectTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";

            DefinitionResult result = scripter.GetDefinitionUsingDeclarationType(DeclarationType.View, "master.sys.objects", objectName, schemaName);
            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.False(result.IsErrorResult);
            Cleanup(result.Locations);

        }

        /// <summary>
        /// Test get definition using declaration type for a non existent view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingDeclarationTypeWithNonexistentObjectTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "sys";

            DefinitionResult result = scripter.GetDefinitionUsingDeclarationType(DeclarationType.View, "master.sys.objects", objectName, schemaName);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Test get definition using quickInfo text for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingQuickInfoTextWithValidObjectTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            string quickInfoText = "view master.sys.objects";

            DefinitionResult result = scripter.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, schemaName);
            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.False(result.IsErrorResult);
            Cleanup(result.Locations);

        }

        /// <summary>
        /// Test get definition using quickInfo text for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingQuickInfoTextWithNonexistentObjectTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);

            Scripter scripter = new Scripter(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "sys";
            string quickInfoText = "view master.sys.objects";

            DefinitionResult result = scripter.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, schemaName);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Test if peek definition default database name is the default server connection database name
        /// Given that there is no query connection
        /// Expect database name to be "master"
        /// </summary>
        [Fact]
        public void GetDatabaseWithNoQueryConnectionTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);
            DbConnection connection;
            //Check if query connection is present
            Assert.False(connInfo.TryGetConnection(ConnectionType.Query, out connection));

            Scripter scripter = new Scripter(serverConnection, connInfo);
            //Check if database name is the default server connection database name
            Assert.Equal("master", scripter.Database.Name);
        }

        /// <summary>
        /// Test if the peek definition database name changes to the query connection database name
        /// Give that there is a query connection
        /// Expect database name to be query connection's database name
        /// </summary>
        [Fact]
        public void GetDatabaseWithQueryConnectionTest()
        {
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = LiveConnectionHelper.InitLiveServerConnectionForDefinition(connInfo);
            //Mock a query connection object
            var mockQueryConnection = new Mock<DbConnection> { CallBase = true };
            mockQueryConnection.SetupGet(x => x.Database).Returns("testdb");
            connInfo.ConnectionTypeToConnectionMap[ConnectionType.Query] = mockQueryConnection.Object;
            DbConnection connection;
            //Check if query connection is present
            Assert.True(connInfo.TryGetConnection(ConnectionType.Query, out connection));

            Scripter scripter = new Scripter(serverConnection, connInfo);
            //Check if database name is the database name in the query connection
            Assert.Equal("testdb", scripter.Database.Name);

            // remove mock from ConnectionInfo
            Assert.True(connInfo.ConnectionTypeToConnectionMap.TryRemove(ConnectionType.Query, out connection));
        }

        // Temporily commented out until a fix is pushed.

        /// <summary>
        /// Get Definition for a object by putting the cursor on 3 different
        /// objects
        /// </summary>
        [Fact]
        public async void GetDefinitionFromChildrenAndParents()
        {
            string queryString = "select * from master.sys.objects";
            // place the cursor on every token

            //cursor on objects
            TextDocumentPosition objectDocument = CreateTextDocPositionWithCursor(26, OwnerUri);

            //cursor on sys
            TextDocumentPosition sysDocument = CreateTextDocPositionWithCursor(22, OwnerUri);

            //cursor on master
            TextDocumentPosition masterDocument = CreateTextDocPositionWithCursor(17, OwnerUri);
            
            LiveConnectionHelper.TestConnectionResult connectionResult = LiveConnectionHelper.InitLiveConnectionInfo(null);
            ScriptFile scriptFile = connectionResult.ScriptFile;
            ConnectionInfo connInfo = connectionResult.ConnectionInfo;
            connInfo.RemoveAllConnections();
            var bindingQueue = new ConnectedBindingQueue();
            bindingQueue.AddConnectionContext(connInfo);
            scriptFile.Contents = queryString;

            var service = new LanguageService();
            service.RemoveScriptParseInfo(OwnerUri);
            service.BindingQueue = bindingQueue;
            await service.UpdateLanguageServiceOnConnection(connectionResult.ConnectionInfo);
            Thread.Sleep(2000);

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            service.ParseAndBind(scriptFile, connInfo);
            scriptInfo.ConnectionKey = bindingQueue.AddConnectionContext(connInfo);
            service.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            // When I call the language service
            var objectResult = service.GetDefinition(objectDocument, scriptFile, connInfo);
            var sysResult = service.GetDefinition(sysDocument, scriptFile, connInfo);
            var masterResult = service.GetDefinition(masterDocument, scriptFile, connInfo);

            // Then I expect the results to be non-null
            Assert.NotNull(objectResult);
            Assert.NotNull(sysResult);
            Assert.NotNull(masterResult);

            // And I expect the all results to be the same
            Assert.True(CompareLocations(objectResult.Locations, sysResult.Locations));
            Assert.True(CompareLocations(objectResult.Locations, masterResult.Locations));

            Cleanup(objectResult.Locations);
            Cleanup(sysResult.Locations);
            Cleanup(masterResult.Locations);
            service.ScriptParseInfoMap.Remove(OwnerUri);
            connInfo.RemoveAllConnections();
        }

        [Fact]
        public async void GetDefinitionFromProcedures()
        {

            string queryString = "EXEC master.dbo.sp_MSrepl_startup";

            // place the cursor on every token

            //cursor on objects
            TextDocumentPosition fnDocument = CreateTextDocPositionWithCursor(30, TestUri);

            //cursor on sys
            TextDocumentPosition dboDocument = CreateTextDocPositionWithCursor(14, TestUri);

            //cursor on master
            TextDocumentPosition masterDocument = CreateTextDocPositionWithCursor(10, TestUri);

            LiveConnectionHelper.TestConnectionResult connectionResult = LiveConnectionHelper.InitLiveConnectionInfo(null);
            ScriptFile scriptFile = connectionResult.ScriptFile;
            ConnectionInfo connInfo = connectionResult.ConnectionInfo;
            connInfo.RemoveAllConnections();
            var bindingQueue = new ConnectedBindingQueue();
            bindingQueue.AddConnectionContext(connInfo);
            scriptFile.Contents = queryString;

            var service = new LanguageService();
            service.RemoveScriptParseInfo(OwnerUri);
            service.BindingQueue = bindingQueue;
            await service.UpdateLanguageServiceOnConnection(connectionResult.ConnectionInfo);
            Thread.Sleep(2000);

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            service.ParseAndBind(scriptFile, connInfo);
            scriptInfo.ConnectionKey = bindingQueue.AddConnectionContext(connInfo);
            service.ScriptParseInfoMap.Add(TestUri, scriptInfo);

            // When I call the language service
            var fnResult = service.GetDefinition(fnDocument, scriptFile, connInfo);
            var sysResult = service.GetDefinition(dboDocument, scriptFile, connInfo);
            var masterResult = service.GetDefinition(masterDocument, scriptFile, connInfo);

            // Then I expect the results to be non-null
            Assert.NotNull(fnResult);
            Assert.NotNull(sysResult);
            Assert.NotNull(masterResult);

            // And I expect the all results to be the same
            Assert.True(CompareLocations(fnResult.Locations, sysResult.Locations));
            Assert.True(CompareLocations(fnResult.Locations, masterResult.Locations));

            Cleanup(fnResult.Locations);
            Cleanup(sysResult.Locations);
            Cleanup(masterResult.Locations);
            service.ScriptParseInfoMap.Remove(TestUri);
            connInfo.RemoveAllConnections();
        }


        /// <summary>
        /// Helper method to clean up script files
        /// </summary>
        private void Cleanup(Location[] locations)
        {
            try
            {
                string filePath = locations[0].Uri;
                Uri fileUri = null;
                if (Uri.IsWellFormedUriString(filePath, UriKind.Absolute))
                {
                    fileUri = new Uri(filePath);
                }
                else 
                {
                    filePath = filePath.Replace("file:/", "file://");
                    if (Uri.IsWellFormedUriString(filePath, UriKind.Absolute))
                    {
                        fileUri = new Uri(filePath);
                    }
                }
                if (fileUri != null && File.Exists(fileUri.LocalPath))
                {
                    File.Delete(fileUri.LocalPath);                    
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Helper method to compare 2 Locations arrays
        /// </summary>
        /// <param name="locationsA"></param>
        /// <param name="locationsB"></param>
        /// <returns></returns>
        private bool CompareLocations(Location[] locationsA, Location[] locationsB)
        {
            HashSet<Location> locationSet = new HashSet<Location>();
            foreach (var location in locationsA)
            {
                locationSet.Add(location);
            }
            foreach (var location in locationsB)
            {
                if (!locationSet.Contains(location))
                {
                    return false;
                }
            }
            return true;
        }

        private TextDocumentPosition CreateTextDocPositionWithCursor(int column, string OwnerUri)
        {
            TextDocumentPosition textDocPos = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = column
                }
            };
            return textDocPos;
        }
    }
}