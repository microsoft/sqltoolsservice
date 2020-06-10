//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Moq;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Utility
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
            return new ConnectionService(new TestDataSourceConnectionFactory());
        }

        public static ConnectParams GetTestConnectionParams(bool useConnectionString = false)
        {
            return new ConnectParams()
            {
                OwnerUri = ScriptUri,
                Connection = GetTestConnectionDetails(useConnectionString)
            };
        }

        /// <summary>
        /// Creates a test connection details object
        /// </summary>
        public static ConnectionDetails GetTestConnectionDetails(bool useConnectionString = false)
        {
            if (useConnectionString)
            {
                return new ConnectionDetails()
                {
                    ConnectionString = $"User ID=user;PWD={Guid.NewGuid().ToString()};Database=databaseName;Server=serverName"
                };
            }

            return new ConnectionDetails()
            {
                UserName = "user",
                Password = Guid.NewGuid().ToString(),
                DatabaseName = "databaseName",
                ServerName = "serverName",
                AzureAccountToken = "azureAccountToken"
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

        public static ConnectionInfo GetTestConnectionInfo()
        {
            return new ConnectionInfo(
                new TestDataSourceConnectionFactory(),
                ScriptUri,
                GetTestConnectionDetails());
        }
        
        public static ServerInfo GetTestServerInfo()
        {
            return new ServerInfo()
            {
                ServerVersion = "14.0.1.0",
                ServerMajorVersion = 14,
                ServerMinorVersion = 0,
                EngineEditionId = 3,
                OsVersion = "Linux (Ubuntu 15.10)",
                IsCloud = false,
                ServerEdition = "Developer Edition",
                ServerLevel = ""
            };
        }
    }

    public class TestReliableDataSourceConnection : ReliableDataSourceConnection
    {
        public TestReliableDataSourceConnection(string connectionString, string azureAccountToken) : base(connectionString, new TestRetryPolicy(), new TestRetryPolicy(), azureAccountToken)
        {
        }
    }

    public class TestRetryPolicy : RetryPolicy
    {
        private static readonly Mock<IErrorDetectionStrategy> MockErrorDetectionStrategy = new Mock<IErrorDetectionStrategy>();
        
        public TestRetryPolicy() : base(MockErrorDetectionStrategy.Object)
        {
        }

        protected override bool ShouldRetryImpl(RetryState retryState)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Test mock class for IDataSourceConnectionFactory factory
    /// </summary>
    public class TestDataSourceConnectionFactory : IDataSourceConnectionFactory
    {
        public ReliableDataSourceConnection CreateDataSourceConnection(string connectionString, string azureAccountToken)
        {
            return new TestReliableDataSourceConnection(connectionString, azureAccountToken);
        }
    }
}
