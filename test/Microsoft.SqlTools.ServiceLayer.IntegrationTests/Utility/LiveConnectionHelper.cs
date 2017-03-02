using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility
{
    public class LiveConnectionHelper
    {
        public static string GetTestSqlFile()
        {
            string filePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "sqltest.sql");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, "SELECT * FROM sys.objects\n");
            return filePath;

        }

        public static TestConnectionResult InitLiveConnectionInfo()
        {
            string sqlFilePath = GetTestSqlFile();
            ScriptFile scriptFile = TestServiceProvider.Instance.WorkspaceService.Workspace.GetFile(sqlFilePath);
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem);

            string ownerUri = scriptFile.ClientFilePath;
            var connectionService = GetLiveTestConnectionService();
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
            return new TestConnectionResult() { ConnectionInfo = connInfo, ScriptFile = scriptFile };
        }

        public static ConnectionInfo InitLiveConnectionInfoForDefinition(string databaseName = null)
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
            const string ScriptUriTemplate = "file://some/{0}.sql";
            string ownerUri = string.Format(CultureInfo.InvariantCulture, ScriptUriTemplate, string.IsNullOrEmpty(databaseName) ? "file" : databaseName);
            var connectionService = GetLiveTestConnectionService();
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

            Assert.NotNull(connInfo);
            return connInfo;
        }

        public static ServerConnection InitLiveServerConnectionForDefinition(ConnectionInfo connInfo)
        {
            SqlConnection sqlConn = new SqlConnection(ConnectionService.BuildConnectionString(connInfo.ConnectionDetails));
            return new ServerConnection(sqlConn);
        }

        /// <summary>
        /// Creates a test sql connection factory instance
        /// </summary>
        public static ISqlConnectionFactory GetLiveTestSqlConnectionFactory()
        {
            // connect to a real server instance
            return ConnectionService.Instance.ConnectionFactory;
        }

        public static ConnectionService GetLiveTestConnectionService()
        {
            // connect to a real server instance
            return ConnectionService.Instance;
        }

        public class TestConnectionResult
        {
            public ConnectionInfo ConnectionInfo { get; set; }

            public ScriptFile ScriptFile { get; set; }

            public TextDocumentPosition TextDocumentPosition { get; set; }
        }
    }
}
