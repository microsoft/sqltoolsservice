//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using GlobalCommon = Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Metadata;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using System.IO;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the language service autocomplete component
    /// </summary>
    public class MetadataServiceTests
    {
        private const int TaskTimeout = 60000;

        private readonly string testScriptUri = TestObjects.ScriptUri;

        private readonly string testConnectionKey = "testdbcontextkey";

        private Mock<ConnectedBindingQueue> bindingQueue;

        private Mock<WorkspaceService<SqlToolsSettings>> workspaceService;

        private Mock<RequestContext<CompletionItem[]>> requestContext;

        private Mock<ScriptFile> scriptFile;

        private Mock<IBinder> binder; 

        private ScriptParseInfo scriptParseInfo;  

        private TextDocumentPosition textDocument;

        private void InitializeTestObjects()
        {
            // initial cursor position in the script file
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier {Uri = this.testScriptUri},
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };                    

            // default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // set up file for returning the query
            scriptFile = new Mock<ScriptFile>();
            scriptFile.SetupGet(file => file.Contents).Returns(GlobalCommon.Constants.StandardQuery);
            scriptFile.SetupGet(file => file.ClientFilePath).Returns(this.testScriptUri);

            // set up workspace mock
            workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(scriptFile.Object);
        
            // setup binding queue mock
            bindingQueue = new Mock<ConnectedBindingQueue>();
            bindingQueue.Setup(q => q.AddConnectionContext(It.IsAny<ConnectionInfo>(), It.IsAny<bool>()))
                .Returns(this.testConnectionKey);

            // inject mock instances into the Language Service
            LanguageService.WorkspaceServiceInstance = workspaceService.Object;         
            LanguageService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();     
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo(); 
            LanguageService.ConnectionServiceInstance.OwnerToConnectionMap.Add(this.testScriptUri, connectionInfo); 
            LanguageService.Instance.BindingQueue = bindingQueue.Object;

            // setup the mock for SendResult
            requestContext = new Mock<RequestContext<CompletionItem[]>>();            
            requestContext.Setup(rc => rc.SendResult(It.IsAny<CompletionItem[]>()))
                .Returns(Task.FromResult(0));     

            // setup the IBinder mock
            binder = new Mock<IBinder>();
            binder.Setup(b => b.Bind(
                It.IsAny<IEnumerable<ParseResult>>(),
                It.IsAny<string>(),
                It.IsAny<BindMode>()));
            
            scriptParseInfo = new ScriptParseInfo();        
            LanguageService.Instance.AddOrUpdateScriptParseInfo(this.testScriptUri, scriptParseInfo);      
            scriptParseInfo.IsConnected = true;            
            scriptParseInfo.ConnectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(connectionInfo);

            // setup the binding context object
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            bindingContext.Binder = binder.Object;
            bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
            LanguageService.Instance.BindingQueue.BindingContextMap.Add(scriptParseInfo.ConnectionKey, bindingContext);                
        }

        public static string GetTestSqlFile()
        {
            string filePath = "sqltest.sql"; 
            
            // Path.Combine(
            //     Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
            //     "/xplat/sqltest.sql");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, "SELECT * FROM sys.objects\n");
            return filePath;

        }
        
        public static ConnectionInfo InitLiveConnectionInfo()
        {
            string sqlFilePath = GetTestSqlFile();
            ScriptFile scriptFile = TestServiceProvider.Instance.WorkspaceService.Workspace.GetFile(sqlFilePath);
            //ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem);

            ConnectParams connectParams = new ConnectParams();
            connectParams.Connection = new ConnectionDetails();
            connectParams.Connection.ServerName = "192.168.0.12";
            connectParams.Connection.DatabaseName = "master";
            connectParams.Connection.UserName = "sa";
            connectParams.Connection.Password = "Yukon900";
            connectParams.Connection.AuthenticationType = AuthenticationType.SqlLogin.ToString();

            // if (key == DefaultSqlAzureInstanceKey || key == DefaultSqlAzureV12InstanceKey)
            // {
            //     connectParams.Connection.ConnectTimeout = 30;
            //     connectParams.Connection.Encrypt = true;
            //     connectParams.Connection.TrustServerCertificate = false;
            // }

            string ownerUri = scriptFile.ClientFilePath;
            var connectionService = ConnectionService.Instance;
            var connectionResult =
                connectionService
                .Connect(new ConnectParams
                {
                    OwnerUri = ownerUri,
                    Connection = connectParams.Connection
                });

            connectionResult.Wait();

            ConnectionInfo connInfo = null;
            connectionService.TryFindConnection(ownerUri, out connInfo);
            return connInfo;
            //return new TestConnectionResult() { ConnectionInfo = connInfo, ScriptFile = scriptFile };
        }


        [Fact]
        public async void HandleCompletionRequestDisabled()
        {
            var connInfo = InitLiveConnectionInfo();

            var sqlConn = MetadataService.OpenMetadataConnection(connInfo);

            Assert.NotNull(sqlConn);

            var metadata = new List<ObjectMetadata>();
            MetadataService.ReadMetadata(sqlConn, metadata);

            Assert.NotNull(metadata.Count > 0);


            // await MetadataService.HandleMetadataListRequest(
            //     null, null
            // );

            // InitializeTestObjects();



            // WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.SqlTools.IntelliSense.EnableIntellisense = false;            
            // Assert.NotNull(LanguageService.HandleCompletionRequest(null, null));
        }

    }
}
