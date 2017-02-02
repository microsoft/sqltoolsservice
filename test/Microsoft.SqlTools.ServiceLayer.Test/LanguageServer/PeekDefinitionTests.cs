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
using Microsoft.SqlTools.ServiceLayer.Test.Common;

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
            fileMock.SetupGet(file => file.Contents).Returns(QueryExecution.Common.StandardQuery);
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
            requestContext.Setup(rc => rc.SendError(It.IsAny<DefinitionError>())).Returns(Task.FromResult(0));;
            requestContext.Setup(r => r.SendEvent(It.IsAny<EventType<TelemetryParams>>(), It.IsAny<TelemetryParams>())).Returns(Task.FromResult(0));;
            requestContext.Setup(r => r.SendEvent(It.IsAny<EventType<StatusChangeParams>>(), It.IsAny<StatusChangeParams>())).Returns(Task.FromResult(0));;

            // setup the IBinder mock
            binder = new Mock<IBinder>();
            binder.Setup(b => b.Bind(
                It.IsAny<IEnumerable<ParseResult>>(),
                It.IsAny<string>(),
                It.IsAny<BindMode>()));

            var testScriptParseInfo = new ScriptParseInfo();
            LanguageService.Instance.AddOrUpdateScriptParseInfo(this.testScriptUri, testScriptParseInfo);
            testScriptParseInfo.IsConnected = false;
            testScriptParseInfo.ConnectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(connectionInfo);

            // setup the binding context object
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            bindingContext.Binder = binder.Object;
            bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
            LanguageService.Instance.BindingQueue.BindingContextMap.Add(testScriptParseInfo.ConnectionKey, bindingContext);
        }


        /// <summary>
        /// Tests the definition event handler. When called with no active connection, an error is sent
        /// </summary>
        [Fact]
        public async Task DefinitionsHandlerWithNoConnectionTest()
        {
            InitializeTestObjects();
            // request definition
            var definitionTask = await Task.WhenAny(LanguageService.HandleDefinitionRequest(textDocument, requestContext.Object), Task.Delay(TaskTimeout));
            await definitionTask;
            // verify that send result was not called and send error was called
            requestContext.Verify(m => m.SendResult(It.IsAny<Location[]>()), Times.Never());
            requestContext.Verify(m => m.SendError(It.IsAny<DefinitionError>()), Times.Once());
        }

        /// <summary>
        /// Tests creating location objects on windows and non-windows systems
        /// </summary>
        [Fact]
        public void GetLocationFromFileForValidFilePathTest()
        {
            String filePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\test\\script.sql" : "/test/script.sql";
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            Location[] locations = peekDefinition.GetLocationFromFile(filePath, 0);

            String expectedFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "file:///C:/test/script.sql" : "file:/test/script.sql";
            Assert.Equal(locations[0].Uri, expectedFilePath);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a valid database name
        /// </summary>
        [Fact]
        public void GetSchemaFromDatabaseQualifiedNameWithValidNameTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string validDatabaseQualifiedName = "master.test.test_table";
            string objectName = "test_table";
            string expectedSchemaName = "test";

            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.Equal(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a valid object name and no schema
        /// </summary>

        [Fact]
        public void GetSchemaFromDatabaseQualifiedNameWithNoSchemaTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string validDatabaseQualifiedName = "test_table";
            string objectName = "test_table";
            string expectedSchemaName = "dbo";

            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.Equal(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a invalid database name
        /// </summary>
        [Fact]
        public void GetSchemaFromDatabaseQualifiedNameWithInvalidNameTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string validDatabaseQualifiedName = "x.y.z";
            string objectName = "test_table";
            string expectedSchemaName = "dbo";

            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.Equal(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test Deletion of peek definition scripts for a valid temp folder that exists
        /// </summary>
        [Fact]
        public void DeletePeekDefinitionScriptsTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            var languageService = LanguageService.Instance;
            Assert.True(Directory.Exists(ServiceLayer.QueryExecution.FileUtils.PeekDefinitionTempFolder));
            languageService.DeletePeekDefinitionScripts();
            Assert.False(Directory.Exists(ServiceLayer.QueryExecution.FileUtils.PeekDefinitionTempFolder));
        }

        /// <summary>
        /// Test Deletion of peek definition scripts for a temp folder that does not exist
        /// </summary>
        [Fact]
        public void DeletePeekDefinitionScriptsWhenFolderDoesNotExistTest()
        {
            var languageService = LanguageService.Instance;
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            ServiceLayer.QueryExecution.FileUtils.SafeDirectoryDelete(ServiceLayer.QueryExecution.FileUtils.PeekDefinitionTempFolder, true);
            Assert.False(Directory.Exists(ServiceLayer.QueryExecution.FileUtils.PeekDefinitionTempFolder));
            // Expected not to throw any exception
            languageService.DeletePeekDefinitionScripts();
        }

        /// <summary>
        /// Test Extracting the full object name from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string containing the object name
        /// Expect the full object name (database.schema.objectName)
        /// </summary>
        [Fact]
        public void GetFullObjectNameFromQuickInfoWithValidStringsTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string objectName = "testTable";
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName);
            string expected = "master.dbo.testTable";
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test Extracting the full object name from quickInfoText.
        /// Given a null object name string and a vaild quickInfo string containing the object name( and vice versa)
        /// Expect null
        /// </summary>
        [Fact]
        public void GetFullObjectNameFromQuickInfoWithNullStringsTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string expected = null;

            string objectName = null;
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName);
            Assert.Equal(expected, result);

            quickInfoText = null;
            objectName = "tableName";
            result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName);
            Assert.Equal(expected, result);

            quickInfoText = null;
            objectName = null;
            result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test Extracting the full object name from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string that does not contain the object name
        /// Expect null
        /// </summary>
        [Fact]
        public void GetFullObjectNameFromQuickInfoWithIncorrectObjectNameTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string objectName = "test";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName);
            string expected = null;
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test Extracting the object type from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string containing the object name
        /// Expect correct object type
        /// </summary>
        [Fact]
        public void GetTokenTypeFromQuickInfoWithValidStringsTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string objectName = "tableName";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName);
            string expected = "table";
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test Extracting theobject type from quickInfoText.
        /// Given a null object name string and a vaild quickInfo string containing the object name( and vice versa)
        /// Expect null
        /// </summary>
        [Fact]
        public void GetTokenTypeFromQuickInfoWithNullStringsTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string expected = null;

            string objectName = null;
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName);
            Assert.Equal(expected, result);

            quickInfoText = null;
            objectName = "tableName";
            result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName);
            Assert.Equal(expected, result);

            quickInfoText = null;
            objectName = null;
            result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test Extracting the object type from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string that does not containthe object name
        /// Expect null
        /// </summary>
        [Fact]
        public void GetTokenTypeFromQuickInfoWithIncorrectObjectNameTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string objectName = "test";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName);
            string expected = null;
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// test Getting definition using quickInfo text without a live connection
        /// Expect an error result( because you cannot script without a live connection)
        /// </summary>
        [Fact]
        public void GetDefinitionUsingQuickInfoWithoutConnectionTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string objectName = "tableName";
            string quickInfoText = "table master.dbo.tableName";
            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, null);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// test Getting definition using declarration Type without a live connection
        /// Expect an error result( because you cannot script without a live connection)
        /// </summary>
        [Fact]
        public void GetDefinitionUsingDeclarationItemWithoutConnectionTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null, null);
            string objectName = "tableName";
            string fullObjectName = "master.dbo.tableName";
            DefinitionResult result = peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.Table, fullObjectName, objectName, null);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }
    }
}
