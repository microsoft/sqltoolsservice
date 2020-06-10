//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.SqlContext;
using Microsoft.Kusto.ServiceLayer.Workspace;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using GlobalCommon = Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the language service autocomplete component
    /// </summary>
    public abstract class LanguageServiceTestBase<T>
    {
        protected const int TaskTimeout = 60000;

        protected readonly string testScriptUri = TestObjects.ScriptUri;

        protected readonly string testConnectionKey = "testdbcontextkey";

        protected LanguageService langService;

        protected Mock<ConnectedBindingQueue> bindingQueue;

        protected Mock<WorkspaceService<SqlToolsSettings>> workspaceService;

        protected Mock<RequestContext<T[]>> requestContext;

        protected Mock<ScriptFile> scriptFile;

        protected Mock<IBinder> binder;

        internal ScriptParseInfo scriptParseInfo;

        protected TextDocumentPosition textDocument;

        protected void InitializeTestObjects()
        {
            // initial cursor position in the script file
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = this.testScriptUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 23
                }
            };

            // default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // set up file for returning the query
            scriptFile = new Mock<ScriptFile>();
            scriptFile.SetupGet(file => file.Contents).Returns(GlobalCommon.Constants.StandardQuery);
            scriptFile.SetupGet(file => file.ClientUri).Returns(this.testScriptUri);

            // set up workspace mock
            workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns((string filePath) => { return filePath == this.testScriptUri ? scriptFile.Object : null; });

            // setup binding queue mock
            bindingQueue = new Mock<ConnectedBindingQueue>();
            bindingQueue.Setup(q => q.AddConnectionContext(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(this.testConnectionKey);

            langService = new LanguageService();
            // inject mock instances into the Language Service
            langService.WorkspaceServiceInstance = workspaceService.Object;
            langService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            langService.ConnectionServiceInstance.OwnerToConnectionMap.Add(this.testScriptUri, connectionInfo);
            langService.BindingQueue = bindingQueue.Object;

            // setup the mock for SendResult
            requestContext = new Mock<RequestContext<T[]>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<T[]>()))
                .Returns(Task.FromResult(0));
            requestContext.Setup(rc => rc.SendError(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(0));
            requestContext.Setup(r => r.SendEvent(It.IsAny<EventType<TelemetryParams>>(), It.IsAny<TelemetryParams>())).Returns(Task.FromResult(0));
            requestContext.Setup(r => r.SendEvent(It.IsAny<EventType<StatusChangeParams>>(), It.IsAny<StatusChangeParams>())).Returns(Task.FromResult(0));

            // setup the IBinder mock
            binder = new Mock<IBinder>();
            binder.Setup(b => b.Bind(
                It.IsAny<IEnumerable<ParseResult>>(),
                It.IsAny<string>(),
                It.IsAny<BindMode>()));

            scriptParseInfo = new ScriptParseInfo();
            langService.AddOrUpdateScriptParseInfo(this.testScriptUri, scriptParseInfo);
            scriptParseInfo.IsConnected = true;
            scriptParseInfo.ConnectionKey = langService.BindingQueue.AddConnectionContext(connectionInfo);

            // setup the binding context object
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            bindingContext.Binder = binder.Object;
            bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
            langService.BindingQueue.BindingContextMap.Add(scriptParseInfo.ConnectionKey, bindingContext);
        }
    }
}