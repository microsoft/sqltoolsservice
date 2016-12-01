//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#define LIVE_CONNECTION_TESTS
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
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

        private const string ViewOwnerUri = "testFile2";

        private const string TriggerOwnerUri = "testFile3";

        private void InitializeTestObjects()
        {
            // initial cursor position in the script file
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier {Uri = this.testScriptUri},
                Position = new Position
                {
                    Line = 0,
                    Character = 23
                }
            };

            // default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            fileMock.SetupGet(file => file.ClientFilePath).Returns(this.testScriptUri);

            // set up workspace mock
            workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            // setup binding queue mock
            bindingQueue = new Mock<ConnectedBindingQueue>();
            bindingQueue.Setup(q => q.AddConnectionContext(It.IsAny<ConnectionInfo>()))
                .Returns(this.testConnectionKey);

            // inject mock instances into the Language Service
            LanguageService.WorkspaceServiceInstance = workspaceService.Object;
            LanguageService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            LanguageService.ConnectionServiceInstance.OwnerToConnectionMap.Add(this.testScriptUri, connectionInfo);
            LanguageService.Instance.BindingQueue = bindingQueue.Object;

            // setup the mock for SendResult
            requestContext = new Mock<RequestContext<Location[]>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<Location[]>()))
                .Returns(Task.FromResult(0));

            // setup the IBinder mock
            binder = new Mock<IBinder>();
            binder.Setup(b => b.Bind(
                It.IsAny<IEnumerable<ParseResult>>(),
                It.IsAny<string>(),
                It.IsAny<BindMode>()));

            var testScriptParseInfo = new ScriptParseInfo();
            LanguageService.Instance.AddOrUpdateScriptParseInfo(this.testScriptUri, testScriptParseInfo);
            testScriptParseInfo.IsConnected = true;
            testScriptParseInfo.ConnectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(connectionInfo);

            // setup the binding context object
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            bindingContext.Binder = binder.Object;
            bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
            LanguageService.Instance.BindingQueue.BindingContextMap.Add(testScriptParseInfo.ConnectionKey, bindingContext);
        }


        /// <summary>
        /// Tests the definition event handler. When called with no active connection, no definition is sent
        /// </summary>
        [Fact]
        public void DefinitionsHandlerWithNoConnectionTest()
        {
            InitializeTestObjects();
            // request the completion list
            Task handleCompletion = LanguageService.HandleDefinitionRequest(textDocument, requestContext.Object);
            handleCompletion.Wait(TaskTimeout);

            // verify that send result was not called
            requestContext.Verify(m => m.SendResult(It.IsAny<Location[]>()), Times.Never());

        }

#if LIVE_CONNECTION_TESTS
        /// <summary>
        /// Test get definition for a table object with active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionTest()
        {
            // Get live connectionInfo
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
            string objectName = "test_table";
            string schemaName = null;
            string objectType = "TABLE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        [Fact]
        public void GetUnsupportedDefinitionForFullScript()
        {

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

            var languageService = LanguageService.Instance;
            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            var locations = languageService.GetDefinition(textDocument, scriptFile, connInfo);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a view object with active connection
        /// </summary>
        [Fact]
        public void GetViewDefinitionTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
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
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
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
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
            string objectName = "SP1";
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
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
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
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
            string objectName = "SP1";
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
#endif
    }
}
