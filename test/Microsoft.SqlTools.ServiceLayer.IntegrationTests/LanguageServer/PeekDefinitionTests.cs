//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;
using ConnectionType = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServices
{
    /// <summary>
    /// Tests for the language service peek definition/ go to definition feature
    /// </summary>
    public class PeekDefinitionTests
    {
        private const string OwnerUri = "testFile1";

        /// <summary>
        /// Test get definition for a table object with active connection
        /// </summary>
        [Fact]
        public async Task GetValidTableDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "spt_monitor";

            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for valid table object
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a invalid table object with active connection
        /// </summary>
        [Fact]
        public async Task GetTableDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "test_invalid";
            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for invalid table object
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a valid table object with schema and active connection
        /// </summary>
        [Fact]
        public async Task GetTableDefinitionWithSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "spt_monitor";

            string schemaName = "dbo";
            string objectType = "TABLE";

            // Get locations for valid table object with schema name
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test GetDefinition with an unsupported type(schema - dbo). Expect a error result.
        /// </summary>
        [Fact]
        public async Task GetUnsupportedDefinitionErrorTest()
        {
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    // test for 'dbo'
                    Character = 15
                }
            };

            TestConnectionResult connectionResult = await TestObjects.InitLiveConnectionInfo();
            connectionResult.ScriptFile.Contents = "select * from dbo.func ()";
            var languageService = new LanguageService();
            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            // When I call the language service
            var result = languageService.GetDefinition(textDocument, connectionResult.ScriptFile, connectionResult.ConnectionInfo);

            // Then I expect null locations and an error to be reported
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Get Definition for a object with no definition. Expect a error result
        /// </summary>
        [Fact]
        public async Task GetDefinitionWithNoResultsFoundError()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "from";
            Position position = new Position()
            {
                Line = 1,
                Character = 14
            };
            ScriptParseInfo scriptParseInfo = new ScriptParseInfo() { IsConnected = true };
            Mock<IBindingContext> bindingContextMock = new Mock<IBindingContext>();
            DefinitionResult result = peekDefinition.GetScript(scriptParseInfo.ParseResult, position, bindingContextMock.Object.MetadataDisplayInfoProvider, objectName, null);

            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
            Assert.Equal(SR.PeekDefinitionNoResultsError, result.Message);
        }

        /// <summary>
        /// Test GetDefinition with a forced timeout. Expect a error result.
        /// </summary>
        [Fact]
        public async Task GetDefinitionTimeoutTest()
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
                It.IsAny<int?>(),
                It.IsAny<int?>()))
            .Callback<string, Func<IBindingContext, CancellationToken, object>, Func<IBindingContext, object>, int?, int?>(
                (key, bindOperation, timeoutOperation, t1, t2) =>
            {
                timeoutResult = (DefinitionResult) timeoutOperation((IBindingContext)null);
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
            TestConnectionResult connectionResult = await TestObjects.InitLiveConnectionInfo();
            ScriptFile scriptFile = connectionResult.ScriptFile;
            ConnectionInfo connInfo = connectionResult.ConnectionInfo;
            scriptFile.Contents = "select * from dbo.func ()";

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            // When I call the language service
            var result = languageService.GetDefinition(textDocument, scriptFile, connInfo);

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
        public async Task GetValidViewDefinitionTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            string objectType = "VIEW";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetViewScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for an invalid view object with no schema name and with active connection
        /// </summary>
        [Fact]
        public async Task GetViewDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = null;
            string objectType = "VIEW";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetViewScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection
        /// </summary>
        [Fact]
        public async Task GetStoredProcedureDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "sp_MSrepl_startup";

            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object that does not exist with active connection
        /// </summary>
        [Fact]
        public async Task GetStoredProcedureDefinitionFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "SP2";
            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection and no schema
        /// </summary>
        [Fact]
        public async Task GetStoredProcedureDefinitionWithoutSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "sp_MSrepl_startup";
            string schemaName = null;
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetScalarValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_addTwo";
            string schemaName = "dbo";
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetScalarValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a table valued function object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetTableValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_returnTable";
            string schemaName = "dbo";
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetScalarValuedFunctionDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetScalarValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a table valued function object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetTableValuedFunctionDefinitionWithNonExistentObjectFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetScalarValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_addTwo";
            string schemaName = null;
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetScalarValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a table valued function object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetTableValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_returnTable";
            string schemaName = null;
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }


        /// <summary>
        /// Test get definition for a user defined data type object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedDataTypeDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_ssn";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedDataTypeScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a user defined data type object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedDataTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_ssn";
            string schemaName = null;
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedDataTypeScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a user defined data type object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedDataTypeDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedDataTypeScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a user defined table type object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedTableTypeDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_locationTableType";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a user defined table type object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedTableTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_locationTableType";
            string schemaName = null;
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a user defined table type object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetUserDefinedTableTypeDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a synonym object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetSynonymDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_testTable";
            string schemaName = "dbo";
            string objectType = "Synonym";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetSynonymScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }


        /// <summary>
        /// Test get definition for a Synonym object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public async Task GetSynonymDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "pd_testTable";
            string schemaName = null;
            string objectType = "Synonym";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetSynonymScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a Synonym object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public async Task GetSynonymDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Synonym";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetSynonymScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition using declaration type for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public async Task GetDefinitionUsingDeclarationTypeWithValidObjectTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";

            DefinitionResult result = peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.View, "master.sys.objects", objectName, schemaName);
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
        public async Task GetDefinitionUsingDeclarationTypeWithNonexistentObjectTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "sys";

            DefinitionResult result = peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.View, "master.sys.objects", objectName, schemaName);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Test get definition using quickInfo text for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public async Task GetDefinitionUsingQuickInfoTextWithValidObjectTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            string quickInfoText = "view master.sys.objects";

            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, schemaName);
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
        public async Task GetDefinitionUsingQuickInfoTextWithNonexistentObjectTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "sys";
            string quickInfoText = "view master.sys.objects";

            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, schemaName);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Test if peek definition default database name is the default server connection database name
        /// Given that there is no query connection
        /// Expect database name to be "master"
        /// </summary>
        [Fact]
        public async Task GetDatabaseWithNoQueryConnectionTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);
            DbConnection connection;
            //Check if query connection is present
            Assert.False(connInfo.TryGetConnection(ConnectionType.Query, out connection));

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            //Check if database name is the default server connection database name
            Assert.Equal(peekDefinition.Database.Name, "master");
        }

        /// <summary>
        /// Test if the peek definition database name changes to the query connection database name
        /// Give that there is a query connection
        /// Expect database name to be query connection's database name
        /// </summary>
        [Fact]
        public async Task GetDatabaseWithQueryConnectionTest()
        {
            ConnectionInfo connInfo = await TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);
            //Mock a query connection object
            var mockQueryConnection = new Mock<DbConnection> { CallBase = true };
            mockQueryConnection.SetupGet(x => x.Database).Returns("testdb");
            connInfo.ConnectionTypeToConnectionMap[ConnectionType.Query] = mockQueryConnection.Object;
            DbConnection connection;
            //Check if query connection is present
            Assert.True(connInfo.TryGetConnection(ConnectionType.Query, out connection));

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            //Check if database name is the database name in the query connection
            Assert.Equal(peekDefinition.Database.Name, "testdb");

            // remove mock from ConnectionInfo
            Assert.True(connInfo.ConnectionTypeToConnectionMap.TryRemove(ConnectionType.Query, out connection));
        }


        /// <summary>
        /// Helper method to clean up script files
        /// </summary>
        private void Cleanup(Location[] locations)
        {
            Uri fileUri = new Uri(locations[0].Uri);
            if (File.Exists(fileUri.LocalPath))
            {
                try
                {
                    File.Delete(fileUri.LocalPath);
                }
                catch(Exception)
                {

                }
            }
        }
    }
}
