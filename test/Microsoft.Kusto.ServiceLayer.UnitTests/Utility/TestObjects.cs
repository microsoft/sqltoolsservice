//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
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
        public static ConnectionService GetTestConnectionService(ReliableDataSourceConnection reliableDataSourceConnection = null)
        {
            var testConnectionCompleteParams = new ConnectionCompleteParams
            {
                ConnectionId = Guid.NewGuid().ToString()
            };

            ReliableDataSourceConnection testSourceConnection;
            if (reliableDataSourceConnection == null)
            {
                var connectionMock = new Mock<ReliableDataSourceConnection> { CallBase = true };
                connectionMock.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testConnectionCompleteParams));
                connectionMock.Setup(x => x.Database).Returns("fakeDatabaseName");
                testSourceConnection = connectionMock.Object;
            }
            else
            {
                testSourceConnection = reliableDataSourceConnection;
            }

            var mockFactory = new Mock<IDataSourceConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateDataSourceConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testSourceConnection);
            
            // use mock database connection
            return new ConnectionService(mockFactory.Object);
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
                return new ConnectionDetails
                {
                    ConnectionString = $"User ID=user;PWD={Guid.NewGuid().ToString()};Database=databaseName;Server=serverName"
                };
            }

            return new ConnectionDetails
            {
                UserName = "user",
                Password = Guid.NewGuid().ToString(),
                DatabaseName = "databaseName",
                ServerName = "serverName",
                AzureAccountToken = "azureAccountToken",
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
        public static readonly ConnectionCompleteParams TestConnectionCompleteParams = new ConnectionCompleteParams
        {
            ConnectionId = Guid.NewGuid().ToString()
        };
        
        public TestReliableDataSourceConnection(string connectionString, string azureAccountToken) : base(new TestDataSource())
        {
            
        }

        public override Task OpenAsync(CancellationToken token)
        {
            return Task.FromResult(TestConnectionCompleteParams);
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
    
    public class TestDataSource : IDataSource
    {
        public void Dispose()
        {
            
        }

        public DataSourceType DataSourceType { get; }
        public string ClusterName { get; }
        public string DatabaseName { get; set; }
        public Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            return Task.CompletedTask as Task<IDataReader>;
        }

        public Task<T> ExecuteScalarQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            return Task.CompletedTask as Task<T>;
        }

        public IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata)
        {
            return new List<DataSourceObjectMetadata>();
        }

        public IEnumerable<DataSourceObjectMetadata> GetChildFolders(DataSourceObjectMetadata parentMetadata)
        {
            return new List<DataSourceObjectMetadata>();
        }

        public void Refresh()
        {
            
        }

        public void Refresh(DataSourceObjectMetadata objectMetadata)
        {
            
        }

        public Task<bool> Exists()
        {
            return Task.CompletedTask as Task<bool>;
        }

        public bool Exists(DataSourceObjectMetadata objectMetadata)
        {
            return false;
        }
    }
}
