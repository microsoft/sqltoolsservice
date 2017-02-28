//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.Test.Utility
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class TestObjects
    {
        public const string ScriptUriTemplate = "file://some/{0}.sql";
        public const string ScriptUri = "file://some/file.sql";
        private static TestServiceProvider _serviceProvider = TestServiceProvider.Instance;

        /// <summary>
        /// Creates a test connection service
        /// </summary>
        public static ConnectionService GetTestConnectionService()
        {
            // use mock database connection
            return new ConnectionService(new TestSqlConnectionFactory());
        }

        public static ConnectionService GetLiveTestConnectionService()
        {
            // connect to a real server instance
            return ConnectionService.Instance;
        }

        /// <summary>
        /// Creates a test connection info instance.
        /// </summary>
        public static ConnectionInfo GetTestConnectionInfo()
        {
            return new ConnectionInfo(
                GetTestSqlConnectionFactory(),
                ScriptUri,
                GetTestConnectionDetails());
        }

        public static ConnectParams GetTestConnectionParams()
        {
            return new ConnectParams() 
            {
                OwnerUri = ScriptUri,
                Connection = GetTestConnectionDetails()
            };
        }

        /// <summary>
        /// Creates a test connection details object
        /// </summary>
        public static ConnectionDetails GetTestConnectionDetails()
        {
            return new ConnectionDetails()
            {
                UserName = "user",
                Password = "password",
                DatabaseName = "databaseName",
                ServerName = "serverName"
            };
        }

        /// <summary>
        /// Create a test language service instance
        /// </summary>
        /// <returns></returns>
        public static LanguageService GetTestLanguageService()
        {
            return new LanguageService();
        }

        /// <summary>
        /// Creates a test sql connection factory instance
        /// </summary>
        public static ISqlConnectionFactory GetTestSqlConnectionFactory()
        {
            // use mock database connection
            return new TestSqlConnectionFactory();
        }

         /// <summary>
        /// Creates a test sql connection factory instance
        /// </summary>
        public static ISqlConnectionFactory GetLiveTestSqlConnectionFactory()
        {
            // connect to a real server instance
            return ConnectionService.Instance.ConnectionFactory; 
        }

       

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
            var connectionService = TestObjects.GetLiveTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = connectParams.Connection
                });
            
            connectionResult.Wait();

            ConnectionInfo connInfo = null;
            connectionService.TryFindConnection(ownerUri, out connInfo);
            return new TestConnectionResult () { ConnectionInfo = connInfo, ScriptFile = scriptFile };
        }

        public static ConnectionInfo InitLiveConnectionInfoForDefinition(string databaseName = null)
        {
            ConnectParams connectParams = _serviceProvider.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);

            string ownerUri = string.Format(CultureInfo.InvariantCulture, ScriptUriTemplate, string.IsNullOrEmpty(databaseName) ? "file" :  databaseName);
            var connectionService = TestObjects.GetLiveTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
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

        /// <summary>
        /// Creates and returns a dummy TextDocumentPosition
        /// </summary>
        public static TextDocumentPosition GetTestDocPosition()
        {
            return new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = ScriptUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };
        }

        public static ServerConnection InitLiveServerConnectionForDefinition(ConnectionInfo connInfo)
        {
            SqlConnection sqlConn = new SqlConnection(ConnectionService.BuildConnectionString(connInfo.ConnectionDetails));                                
            return new ServerConnection(sqlConn);
        }
    }

    /// <summary>
    /// Test mock class for IDbCommand
    /// </summary>
    public class TestSqlCommand : DbCommand
    {
        internal TestSqlCommand(TestResultSet[] data)
        {
            Data = data;

            var mockParameterCollection = new Mock<DbParameterCollection>();
            mockParameterCollection.Setup(c => c.Add(It.IsAny<object>()))
                .Callback<object>(d => listParams.Add((DbParameter)d));
            mockParameterCollection.Setup(c => c.Count)
                .Returns(() => listParams.Count);
            DbParameterCollection = mockParameterCollection.Object;
        }

        internal TestResultSet[] Data { get; set; }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public override object ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public override void Prepare()
        {
            throw new NotImplementedException();
        }

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; }
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new TestDbDataReader(Data);
        }

        private List<DbParameter> listParams = new List<DbParameter>();
    }

    /// <summary>
    /// Test mock class for SqlConnection wrapper
    /// </summary>
    public class TestSqlConnection : DbConnection
    {
        internal TestSqlConnection(TestResultSet[] data)
        {
            Data = data;
        }
        
        internal TestResultSet[] Data { get; set; }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            // No Op
        }

        public override void Open()
        {
            // No Op, unless credentials are bad
            if(ConnectionString.Contains("invalidUsername"))
            {
                throw new Exception("Invalid credentials provided");
            }
        }

        public override string ConnectionString { get; set; }
        public override string Database { get; }
        public override ConnectionState State { get; }
        public override string DataSource { get; }
        public override string ServerVersion { get; }

        protected override DbCommand CreateDbCommand()
        {
            return new TestSqlCommand(Data);
        }

        public override void ChangeDatabase(string databaseName)
        {
            // No Op
        }
    }

    /// <summary>
    /// Test mock class for SqlConnection factory
    /// </summary>
    public class TestSqlConnectionFactory : ISqlConnectionFactory
    {
        public DbConnection CreateSqlConnection(string connectionString)
        {
            return new TestSqlConnection(null)
            {
                ConnectionString = connectionString
            };
        }
    }

    public class TestConnectionResult
    {
        public ConnectionInfo ConnectionInfo { get; set; }

        public ScriptFile ScriptFile { get; set; }

        public TextDocumentPosition TextDocumentPosition { get; set; }
    }
}
