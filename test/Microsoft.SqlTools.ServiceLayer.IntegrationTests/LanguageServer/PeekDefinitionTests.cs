//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServices
{
    /// <summary>
    /// Tests for the language service peek definition/ go to definition feature
    /// </summary>
    public class PeekDefinitionTests
    {
        private const int TaskTimeout = 30000;

        private readonly string testScriptUri = TestObjects.ScriptUri;

        private readonly string testConnectionKey = "testdbcontextkey";

        private Mock<ConnectedBindingQueue> bindingQueue;

        private Mock<WorkspaceService<SqlToolsSettings>> workspaceService;

        private Mock<RequestContext<Location[]>> requestContext;

        private Mock<IBinder> binder;

        private TextDocumentPosition textDocument;

        private const string OwnerUri = "testFile1";

        /// <summary>
        /// Test get definition for a table object with active connection
        /// </summary>
        [Fact]
        public void GetValidTableDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetTableDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetTableDefinitionWithSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetUnsupportedDefinitionErrorTest()
        {
            ScriptFile scriptFile;
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    // test for 'dbo'
                    Character = 18
                }
            };
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
            scriptFile.Contents = "alter trigger test_trigger";
            var languageService = new LanguageService();
            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            // When I call the language service
            var result = languageService.GetDefinition(textDocument, scriptFile, connInfo);

            // Then I expect null locations and an error to be reported
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Get Definition for a object with no definition. Expect a error result
        /// </summary>
        [Fact]
        public void GetDefinitionWithNoResultsFoundError()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
                It.IsAny<int?>(),
                It.IsAny<int?>()))
            .Callback<string, Func<IBindingContext, CancellationToken, object>, Func<IBindingContext, object>, int?, int?>(
                (key, bindOperation, timeoutOperation, t1, t2) =>
            {
                timeoutResult = (DefinitionResult) timeoutOperation((IBindingContext)null);
                itemMock.Object.Result = timeoutResult;
            })
            .Returns(() => itemMock.Object);

            ScriptFile scriptFile;
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 20
                }
            };
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
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
        public void GetValidViewDefinitionTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetViewDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetStoredProcedureDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetStoredProcedureDefinitionFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetStoredProcedureDefinitionWithoutSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetScalarValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "addTwo";
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
        public void GetTableValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "returnTable";
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
        public void GetScalarValuedFunctionDefinitionWithNonExistantFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetTableValuedFunctionDefinitionWithNonExistantObjectFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetScalarValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "addTwo";
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
        public void GetTableValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "returnTable";
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
        public void GetUserDefinedDataTypeDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "SSN";
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
        public void GetUserDefinedDataTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "SSN";
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
        public void GetUserDefinedDataTypeDefinitionWithNonExistantFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
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
        public void GetUserDefinedTableTypeDefinitionWithSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "LocationTableType";
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
        public void GetUserDefinedTableTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "LocationTableType";
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
        public void GetUserDefinedTableTypeDefinitionWithNonExistantFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
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
