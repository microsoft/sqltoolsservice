//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
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
                    Character = 16
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

            List<Declaration> declarations = new List<Declaration>();
            DefinitionResult result = peekDefinition.GetScript(declarations, objectName, null);

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
                (key, bindOperation, timeoutOperation, blah, blah2) => 
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
