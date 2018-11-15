// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Moq.Protected;
using HostingProtocol = Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public static class Common
    {
        #region Constants

        public const string InvalidQuery = "SELECT *** FROM sys.objects";

        public const string NoOpQuery = "-- No ops here, just us chickens.";

        public const int Ordinal = 100;     // We'll pick something other than default(int)

        public const int StandardColumns = 5;

        public const int StandardRows = 5;

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
            Batch batch = new Batch(Constants.StandardQuery, SubsectionDocument, 1,
                MemoryFileSystem.GetFileStreamFactory());
            batch.Execute(CreateTestConnection(StandardTestDataSet, false, false), CancellationToken.None).Wait();
            return batch;
        }

        public static Batch GetExecutedBatchWithExecutionPlan()
        {
            Batch batch = new Batch(Constants.StandardQuery, SubsectionDocument, 1,
                MemoryFileSystem.GetFileStreamFactory());
            batch.Execute(CreateTestConnection(ExecutionPlanTestDataSet, false, false), CancellationToken.None).Wait();
            return batch;
        }

        public static Query GetBasicExecutedQuery()
        {
            ConnectionInfo ci = CreateConnectedConnectionInfo(StandardTestDataSet, false, false);

            // Query won't be able to request a new query DbConnection unless the ConnectionService has a 
            // ConnectionInfo with the same URI as the query, so we will manually set it
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            Query query = new Query(Constants.StandardQuery, ci, new QueryExecutionSettings(), 
                MemoryFileSystem.GetFileStreamFactory());
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }

        public static Query GetBasicExecutedQuery(QueryExecutionSettings querySettings)
        {
            ConnectionInfo ci = CreateConnectedConnectionInfo(StandardTestDataSet, false, false);

            // Query won't be able to request a new query DbConnection unless the ConnectionService has a 
            // ConnectionInfo with the same URI as the query, so we will manually set it
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            Query query = new Query(Constants.StandardQuery, ci, querySettings, 
                MemoryFileSystem.GetFileStreamFactory());
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }

        public static TestResultSet[] GetTestDataSet(int dataSets)
        {
            return Enumerable.Repeat(StandardTestResultSet, dataSets).ToArray();
        }

        public static async Task AwaitExecution(QueryExecutionService service, ExecuteDocumentSelectionParams qeParams,
            HostingProtocol.RequestContext<ExecuteRequestResult> requestContext)
        {
            await service.HandleExecuteRequest(qeParams, requestContext);
            if (service.ActiveQueries.ContainsKey(qeParams.OwnerUri) && service.ActiveQueries[qeParams.OwnerUri].ExecutionTask != null)
            {
                await service.ActiveQueries[qeParams.OwnerUri].ExecutionTask;
            }
        }

        #endregion

        #region DbConnection Mocking

        private static DbCommand CreateTestCommand(TestResultSet[] data, bool throwOnExecute, bool throwOnRead)
        {
            var commandMock = new Mock<DbCommand> { CallBase = true };
            var commandMockSetup = commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>());

            // Setup the expected execute behavior
            if (throwOnExecute)
            {
                var mockException = new Mock<DbException>();
                mockException.SetupGet(dbe => dbe.Message).Returns("Message");
                commandMockSetup.Throws(mockException.Object);
            }
            else
            {
                commandMockSetup.Returns(new TestDbDataReader(data, throwOnRead));
            }
                

            return commandMock.Object;
        }

        private static DbConnection CreateTestConnection(TestResultSet[] data, bool throwOnExecute, bool throwOnRead)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(() => CreateTestCommand(data, throwOnExecute, throwOnRead));
            connectionMock.Setup(dbc => dbc.Open())
                .Callback(() => connectionMock.SetupGet(dbc => dbc.State).Returns(ConnectionState.Open));
            connectionMock.Setup(dbc => dbc.Close())
                .Callback(() => connectionMock.SetupGet(dbc => dbc.State).Returns(ConnectionState.Closed));

            return connectionMock.Object;
        }

        private static ISqlConnectionFactory CreateMockFactory(TestResultSet[] data, bool throwOnExecute, bool throwOnRead)
        {
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => CreateTestConnection(data, throwOnExecute, throwOnRead));

            return mockFactory.Object;
        }

        public static ConnectionInfo CreateTestConnectionInfo(TestResultSet[] data, bool throwOnExecute, bool throwOnRead)
        {
            // Create a connection info and add the default connection to it
            ISqlConnectionFactory factory = CreateMockFactory(data, throwOnExecute, throwOnRead);
            ConnectionInfo ci = new ConnectionInfo(factory, Constants.OwnerUri, StandardConnectionDetails);
            ci.ConnectionTypeToConnectionMap[ConnectionType.Default] = factory.CreateSqlConnection(null, null);
            return ci;
        }

        public static ConnectionInfo CreateConnectedConnectionInfo(
            TestResultSet[] data, 
            bool throwOnExecute,
            bool throwOnRead,
            string type = ConnectionType.Default)
        {
            ConnectionService connectionService = ConnectionService.Instance;
            connectionService.OwnerToConnectionMap.Clear();
            connectionService.ConnectionFactory = CreateMockFactory(data, throwOnExecute, throwOnRead);

            ConnectParams connectParams = new ConnectParams
            {
                Connection = StandardConnectionDetails,
                OwnerUri = Constants.OwnerUri,
                Type = type
            };

            connectionService.Connect(connectParams).Wait();
            return connectionService.OwnerToConnectionMap[connectParams.OwnerUri];
        }

        #endregion

        #region Service Mocking

        public static QueryExecutionService GetPrimedExecutionService(
            TestResultSet[] data,
            bool isConnected, 
            bool throwOnExecute, 
            bool throwOnRead,
            WorkspaceService<SqlToolsSettings> workspaceService,
            out ConcurrentDictionary<string, byte[]> storage)
        {
            // Create a place for the temp "files" to be written
            storage = new ConcurrentDictionary<string, byte[]>();

            // Mock the connection service
            var connectionService = new Mock<ConnectionService>();
            ConnectionInfo ci = CreateConnectedConnectionInfo(data, throwOnExecute, throwOnRead);
            ConnectionInfo outValMock;
            connectionService
                .Setup(service => service.TryFindConnection(It.IsAny<string>(), out outValMock))
                .OutCallback((string owner, out ConnectionInfo connInfo) => connInfo = isConnected ? ci : null)
                .Returns(isConnected);

            return new QueryExecutionService(connectionService.Object, workspaceService) { BufferFileStreamFactory = MemoryFileSystem.GetFileStreamFactory(storage) };
        }

        public static QueryExecutionService GetPrimedExecutionService(
            TestResultSet[] data, 
            bool isConnected, 
            bool throwOnExecute,
            bool throwOnRead,
            WorkspaceService<SqlToolsSettings> workspaceService)
        {
            ConcurrentDictionary<string, byte[]> storage;
            return GetPrimedExecutionService(data, isConnected, throwOnExecute, throwOnRead, workspaceService, out storage);
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
