// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
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

        public const int Ordinal = 0;

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

        public static Dictionary<string, string>[] StandardTestData
        {
            get { return GetTestData(StandardRows, StandardColumns); }
        }

        #region Public Methods

        public static Batch GetBasicExecutedBatch()
        {
            Batch batch = new Batch(StandardQuery, SubsectionDocument, 1, GetFileStreamFactory(new Dictionary<string, byte[]>()));
            batch.Execute(CreateTestConnection(new[] {StandardTestData}, false), CancellationToken.None).Wait();
            return batch;
        }

        public static Query GetBasicExecutedQuery()
        {
            ConnectionInfo ci = CreateTestConnectionInfo(new[] {StandardTestData}, false);
            Query query = new Query(StandardQuery, ci, new QueryExecutionSettings(), GetFileStreamFactory(new Dictionary<string, byte[]>()));
            query.Execute();
            query.ExecutionTask.Wait();
            return query;
        }

        public static Dictionary<string, string>[] GetTestData(int columns, int rows)
        {
            Dictionary<string, string>[] output = new Dictionary<string, string>[rows];
            for (int row = 0; row < rows; row++)
            {
                Dictionary<string, string> rowDictionary = new Dictionary<string, string>();
                for (int column = 0; column < columns; column++)
                {
                    rowDictionary.Add(string.Format("column{0}", column), string.Format("val{0}{1}", column, row));
                }
                output[row] = rowDictionary;
            }

            return output;
        }

        public static async Task AwaitExecution(QueryExecutionService service, QueryExecuteParams qeParams,
            RequestContext<QueryExecuteResult> requestContext)
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
                .Returns<string>(output => new ServiceBufferFileStreamReader(new InMemoryWrapper(storage[output]), output));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamWriter(
                    new InMemoryWrapper(storage[output]), output, 1024, 1024));

            return mock.Object;
        }

        public class InMemoryWrapper : IFileStreamWrapper
        {
            private readonly MemoryStream memoryStream;
            private bool readingOnly;

            public InMemoryWrapper(byte[] storage)
            {
                memoryStream = new MemoryStream(storage);
            }

            public void Close()
            {
                memoryStream.Dispose();
            }

            public void Dispose()
            {
                // We'll dispose this via a special method
            }

            public void Flush()
            {
                if (readingOnly) { throw new InvalidOperationException(); }
            }

            public void Init(string fileName, int bufferSize, FileAccess fAccess)
            {
                readingOnly = fAccess == FileAccess.Read;
            }

            public int ReadData(byte[] buffer, int bytes)
            {
                return ReadData(buffer, bytes, memoryStream.Position);
            }

            public int ReadData(byte[] buffer, int bytes, long fileOffset)
            {
                memoryStream.Seek(fileOffset, SeekOrigin.Begin);
                return memoryStream.Read(buffer, 0, bytes);
            }

            public int WriteData(byte[] buffer, int bytes)
            {
                if (readingOnly) { throw new InvalidOperationException(); }
                memoryStream.Write(buffer, 0, bytes);
                memoryStream.Flush();
                return bytes;
            }
        }

        #endregion

        #region DbConnection Mocking

        public static DbCommand CreateTestCommand(Dictionary<string, string>[][] data, bool throwOnRead)
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

        public static DbConnection CreateTestConnection(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(CreateTestCommand(data, throwOnRead));
            connectionMock.Setup(dbc => dbc.Open())
                .Callback(() => connectionMock.SetupGet(dbc => dbc.State).Returns(ConnectionState.Open));
            connectionMock.Setup(dbc => dbc.Close())
                .Callback(() => connectionMock.SetupGet(dbc => dbc.State).Returns(ConnectionState.Closed));

            return connectionMock.Object;
        }

        public static ISqlConnectionFactory CreateMockFactory(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(CreateTestConnection(data, throwOnRead));

            return mockFactory.Object;
        }

        public static ConnectionInfo CreateTestConnectionInfo(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            return new ConnectionInfo(CreateMockFactory(data, throwOnRead), OwnerUri, StandardConnectionDetails);
        }

        #endregion

        #region Service Mocking

        public static QueryExecutionService GetPrimedExecutionService(Dictionary<string, string>[][] data,
            bool isConnected, bool throwOnRead, WorkspaceService<SqlToolsSettings> workspaceService,
            out Dictionary<string, byte[]> storage)
        {
            // Create a place for the temp "files" to be written
            storage = new Dictionary<string, byte[]>();

            // Create the connection factory with the dataset
            var factory = CreateTestConnectionInfo(data, throwOnRead).Factory;

            // Mock the connection service
            var connectionService = new Mock<ConnectionService>();
            ConnectionInfo ci = new ConnectionInfo(factory, OwnerUri, StandardConnectionDetails);
            ConnectionInfo outValMock;
            connectionService
                .Setup(service => service.TryFindConnection(It.IsAny<string>(), out outValMock))
                .OutCallback((string owner, out ConnectionInfo connInfo) => connInfo = isConnected ? ci : null)
                .Returns(isConnected);

            return new QueryExecutionService(connectionService.Object, workspaceService) { BufferFileStreamFactory = GetFileStreamFactory(storage) };
        }

        public static QueryExecutionService GetPrimedExecutionService(Dictionary<string, string>[][] data, bool isConnected, bool throwOnRead, WorkspaceService<SqlToolsSettings> workspaceService)
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
