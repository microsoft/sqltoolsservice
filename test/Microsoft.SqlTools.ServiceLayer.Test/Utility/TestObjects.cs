//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;

namespace Microsoft.SqlTools.Test.Utility
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class TestObjects
    {
        public const string ScriptUri = "file://some/file.sql";

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
                UserName = "sa",
                Password = "...",
                DatabaseName = "master",
                ServerName = "localhost"
            };
        }

        /// <summary>
        /// Gets a ConnectionDetails for connecting to localhost with integrated auth
        /// </summary>
        public static ConnectionDetails GetIntegratedTestConnectionDetails()
        {
            return new ConnectionDetails()
            {
                DatabaseName = "master",
                ServerName = "localhost",
                AuthenticationType = "Integrated"
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
    }

    /// <summary>
    /// Test mock class for IDbCommand
    /// </summary>
    public class TestSqlCommand : DbCommand
    {
        internal TestSqlCommand(Dictionary<string, string>[][] data)
        {
            Data = data;
        }

        internal Dictionary<string, string>[][] Data { get; set; }

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
    }

    /// <summary>
    /// Test mock class for SqlConnection wrapper
    /// </summary>
    public class TestSqlConnection : DbConnection
    {
        internal TestSqlConnection(Dictionary<string, string>[][] data)
        {
            Data = data;
        }
        
        internal Dictionary<string, string>[][] Data { get; set; }

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
}
