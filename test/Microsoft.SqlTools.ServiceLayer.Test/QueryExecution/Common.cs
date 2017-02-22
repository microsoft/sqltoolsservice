// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Moq.Protected;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class Common
    {
        #region Constants

        public const string InvalidQuery = "SELECT *** FROM sys.objects";

        public const string NoOpQuery = "-- No ops here, just us chickens.";

        public const int Ordinal = 100;     // We'll pick something other than default(int)

        public const string OwnerUri = "testFile";

        public const int StandardColumns = 5;

        public const string StandardQuery = "SELECT * FROM sys.objects";

        public const int StandardRows = 5;

        public const string UdtQuery = "SELECT hierarchyid::Parse('/')";

        public const SelectionData WholeDocument = null;

        public static readonly ConnectionDetails StandardConnectionDetails = new ConnectionDetails
        {
            DatabaseName = "123",
            Password = "456",
            ServerName = "789",
            UserName = "012"
        };

        public static readonly SelectionData SubsectionDocument = new SelectionData(0, 0, 2, 2);

        #endregion

        public static TestResultSet StandardTestResultSet => new TestResultSet(StandardColumns, StandardRows);

        public static TestResultSet[] StandardTestDataSet => new[] {StandardTestResultSet};

        public static TestResultSet[] ExecutionPlanTestDataSet
        {
            get
            {
                DbColumn[] columns = { new TestDbColumn("Microsoft SQL Server 2005 XML Showplan") };
                object[][] rows = { new object[] { "Execution Plan" } };
                return new[] {new TestResultSet(columns, rows)};
            }
        }

        #region Public Methods

        public static Batch GetBasicExecutedBatch()
        {
            Batch batch = new Batch(StandardQuery, SubsectionDocument, 1, GetFileStreamFactory(new Dictionary<string, byte[]>()));
            batch.Execute(CreateTestConnection(StandardTestDataSet, false), CancellationToken.None).Wait();
            return batch;
        }

        public static Batch GetExecutedBatchWithExecutionPlan()
        {
            Batch batch = new Batch(StandardQuery, SubsectionDocument, 1, GetFileStreamFactory(new Dictionary<string, byte[]>()));
            batch.Execute(CreateTestConnection(ExecutionPlanTestDataSet, false), CancellationToken.None).Wait();
            return batch;
        }

        public static Query GetBasicExecutedQuery()
        {
            ConnectionInfo ci = CreateTestConnectionInfo(StandardTestDataSet, false);

            // Query won't be able to request a new query DbConnection unless the ConnectionService has a 
            // ConnectionInfo with the same URI as the query, so we will manually set it
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            Query query = new Query(StandardQuery, ci, new QueryExecutionSettings(), GetFileStreamFactory(new Dictionary<string, byte[]>()));
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }

        public static Query GetBasicExecutedQuery(QueryExecutionSettings querySettings)
        {
            ConnectionInfo ci = CreateTestConnectionInfo(StandardTestDataSet, false);

            // Query won't be able to request a new query DbConnection unless the ConnectionService has a 
            // ConnectionInfo with the same URI as the query, so we will manually set it
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            Query query = new Query(StandardQuery, ci, querySettings, GetFileStreamFactory(new Dictionary<string, byte[]>()));
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }

        public static TestResultSet[] GetTestDataSet(int dataSets)
        {
            return Enumerable.Repeat(StandardTestResultSet, dataSets).ToArray();
        }

        public static async Task AwaitExecution(QueryExecutionService service, ExecuteDocumentSelectionParams qeParams,
            RequestContext<ExecuteRequestResult> requestContext)
        {
            await service.HandleExecuteRequest(qeParams, requestContext);
            if (service.ActiveQueries.ContainsKey(qeParams.OwnerUri) && service.ActiveQueries[qeParams.OwnerUri].ExecutionTask != null)
            {
                await service.ActiveQueries[qeParams.OwnerUri].ExecutionTask;
            }
        }

        #endregion

        #region FileStreamWriteMocking 

        public static IFileStreamFactory GetFileStreamFactory(Dictionary<string, byte[]> storage)
        {
            Mock<IFileStreamFactory> mock = new Mock<IFileStreamFactory>();
            mock.Setup(fsf => fsf.CreateFile())
                .Returns(() =>
                {
                    string fileName = Guid.NewGuid().ToString();
                    storage.Add(fileName, new byte[8192]);
                    return fileName;
                });
            mock.Setup(fsf => fsf.GetReader(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output]), new QueryExecutionSettings()));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamWriter(new MemoryStream(storage[output]), new QueryExecutionSettings()));

            return mock.Object;
        }

        #endregion

        #region DbConnection Mocking

        public static DbCommand CreateTestCommand(TestResultSet[] data, bool throwOnRead)
        {
            var commandMock = new Mock<DbCommand> { CallBase = true };
            var commandMockSetup = commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>());

            // Setup the expected behavior
            if (throwOnRead)
            {
                var mockException = new Mock<DbException>();
                mockException.SetupGet(dbe => dbe.Message).Returns("Message");
                commandMockSetup.Throws(mockException.Object);
            }
            else
            {
                commandMockSetup.Returns(new TestDbDataReader(data));
            }
                

            return commandMock.Object;
        }

        public static DbConnection CreateTestConnection(TestResultSet[] data, bool throwOnRead)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(() => CreateTestCommand(data, throwOnRead));
            connectionMock.Setup(dbc => dbc.Open())
                .Callback(() => connectionMock.SetupGet(dbc => dbc.State).Returns(ConnectionState.Open));
            connectionMock.Setup(dbc => dbc.Close())
                .Callback(() => connectionMock.SetupGet(dbc => dbc.State).Returns(ConnectionState.Closed));

            return connectionMock.Object;
        }

        public static ISqlConnectionFactory CreateMockFactory(TestResultSet[] data, bool throwOnRead)
        {
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(() => CreateTestConnection(data, throwOnRead));

            return mockFactory.Object;
        }

        public static ConnectionInfo CreateTestConnectionInfo(TestResultSet[] data, bool throwOnRead)
        {
            // Create a connection info and add the default connection to it
            ISqlConnectionFactory factory = CreateMockFactory(data, throwOnRead);
            ConnectionInfo ci = new ConnectionInfo(factory, OwnerUri, StandardConnectionDetails);
            ci.ConnectionTypeToConnectionMap[ConnectionType.Default] = factory.CreateSqlConnection(null);
            return ci;
        }

        public static ConnectionInfo CreateConnectedConnectionInfo(TestResultSet[] data, bool throwOnRead, string type = ConnectionType.Default)
        {
            ConnectionService connectionService = ConnectionService.Instance;
            connectionService.OwnerToConnectionMap.Clear();
            connectionService.ConnectionFactory = CreateMockFactory(data, throwOnRead);

            ConnectParams connectParams = new ConnectParams
            {
                Connection = StandardConnectionDetails,
                OwnerUri = OwnerUri,
                Type = type
            };

            connectionService.Connect(connectParams).Wait();
            return connectionService.OwnerToConnectionMap[OwnerUri];
        }

        #endregion

        #region Service Mocking

        public static QueryExecutionService GetPrimedExecutionService(TestResultSet[] data,
            bool isConnected, bool throwOnRead, WorkspaceService<SqlToolsSettings> workspaceService,
            out Dictionary<string, byte[]> storage)
        {
            // Create a place for the temp "files" to be written
            storage = new Dictionary<string, byte[]>();

            // Mock the connection service
            var connectionService = new Mock<ConnectionService>();
            ConnectionInfo ci = CreateConnectedConnectionInfo(data, throwOnRead);
            ConnectionInfo outValMock;
            connectionService
                .Setup(service => service.TryFindConnection(It.IsAny<string>(), out outValMock))
                .OutCallback((string owner, out ConnectionInfo connInfo) => connInfo = isConnected ? ci : null)
                .Returns(isConnected);

            return new QueryExecutionService(connectionService.Object, workspaceService) { BufferFileStreamFactory = GetFileStreamFactory(storage) };
        }

        public static QueryExecutionService GetPrimedExecutionService(TestResultSet[] data, bool isConnected, bool throwOnRead, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            Dictionary<string, byte[]> storage;
            return GetPrimedExecutionService(data, isConnected, throwOnRead, workspaceService, out storage);
        }

        public static WorkspaceService<SqlToolsSettings> GetPrimedWorkspaceService(string query)
        {
            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(query);
           
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            return workspaceService.Object;
        }

        #endregion
    }
}
