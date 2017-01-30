// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.SaveResults
{
    public class ServiceIntegrationTests
    {
        #region CSV Tests

        [Fact]
        public async Task SaveResultsCsvNonExistentQuery()

        {
            // Given: A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(null);
            QueryExecutionService qes = Common.GetPrimedExecutionService(null, false, false, ws);

            // If: I attempt to save a result set from a query that doesn't exist
            SaveResultsAsCsvRequestParams saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri  // Won't exist because nothing has executed
            };
            object error = null;
            var requestContext = RequestContextMocks.Create<SaveResultRequestResult>(null)
                .AddErrorHandling(o => error = o);
            await qes.HandleSaveResultsAsCsvRequest(saveParams, requestContext.Object);

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            VerifyResponseCalls(requestContext, false, true);
            Assert.IsType<SaveResultRequestError>(error);
            Assert.NotNull(error);
            Assert.NotNull(((SaveResultRequestError)error).message);
        }

        [Fact]
        public async Task SaveResultAsCsvFailure()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            Dictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(new[] {Common.StandardTestData}, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set and get it to throw because of invalid column selection
            SaveResultsAsCsvRequestParams saveParams = new SaveResultsAsCsvRequestParams
            {
                BatchIndex = 0,
                FilePath = "qqq",
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                ColumnStartIndex = -1,
                ColumnEndIndex = 100,
                RowStartIndex = 0,
                RowEndIndex = 5
            };
            qes.CsvFileFactory = GetCsvStreamFactory(storage, saveParams);
            object error = null;
            var requestContext = RequestContextMocks.Create<SaveResultRequestResult>(null)
                .AddErrorHandling(e => error = e);
            await qes.HandleSaveResultsAsCsvRequest(saveParams, requestContext.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            VerifyResponseCalls(requestContext, false, true);
            Assert.IsType<SaveResultRequestError>(error);
            Assert.NotNull(error);
            Assert.NotNull(((SaveResultRequestError)error).message);
        }

        [Fact]
        public async Task SaveResultsAsCsvSuccess()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            Dictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(new[] {Common.StandardTestData}, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set from a query
            SaveResultsAsCsvRequestParams saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri,
                FilePath = "qqq",
                BatchIndex = 0,
                ResultSetIndex = 0
            };
            qes.CsvFileFactory = GetCsvStreamFactory(storage, saveParams);
            SaveResultRequestResult result = null;
            var requestContext = RequestContextMocks.Create<SaveResultRequestResult>(r => result = r);
            await qes.HandleSaveResultsAsCsvRequest(saveParams, requestContext.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... I should have a successful result
            // ... There should not have been an error
            VerifyResponseCalls(requestContext, true, false);
            Assert.NotNull(result);
            Assert.Null(result.Messages);
        }

        #endregion

        #region JSON tests

        [Fact]
        public async Task SaveResultsJsonNonExistentQuery()

        {
            // Given: A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(null);
            QueryExecutionService qes = Common.GetPrimedExecutionService(null, false, false, ws);

            // If: I attempt to save a result set from a query that doesn't exist
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri  // Won't exist because nothing has executed
            };
            object error = null;
            var requestContext = RequestContextMocks.Create<SaveResultRequestResult>(null)
                .AddErrorHandling(o => error = o);
            await qes.HandleSaveResultsAsJsonRequest(saveParams, requestContext.Object);

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            VerifyResponseCalls(requestContext, false, true);
            Assert.IsType<SaveResultRequestError>(error);
            Assert.NotNull(error);
            Assert.NotNull(((SaveResultRequestError)error).message);
        }

        [Fact]
        public async Task SaveResultAsJsonFailure()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            Dictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(new[] { Common.StandardTestData }, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set and get it to throw because of invalid column selection
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams
            {
                BatchIndex = 0,
                FilePath = "qqq",
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                ColumnStartIndex = -1,
                ColumnEndIndex = 100,
                RowStartIndex = 0,
                RowEndIndex = 5
            };
            qes.JsonFileFactory = GetJsonStreamFactory(storage, saveParams);
            object error = null;
            var requestContext = RequestContextMocks.Create<SaveResultRequestResult>(null)
                .AddErrorHandling(e => error = e);
            await qes.HandleSaveResultsAsJsonRequest(saveParams, requestContext.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            VerifyResponseCalls(requestContext, false, true);
            Assert.IsType<SaveResultRequestError>(error);
            Assert.NotNull(error);
            Assert.NotNull(((SaveResultRequestError)error).message);
        }

        [Fact]
        public async Task SaveResultsAsJsonSuccess()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            Dictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(new[] { Common.StandardTestData }, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set from a query
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri,
                FilePath = "qqq",
                BatchIndex = 0,
                ResultSetIndex = 0
            };
            qes.JsonFileFactory = GetJsonStreamFactory(storage, saveParams);
            SaveResultRequestResult result = null;
            var requestContext = RequestContextMocks.Create<SaveResultRequestResult>(r => result = r);
            await qes.HandleSaveResultsAsJsonRequest(saveParams, requestContext.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... I should have a successful result
            // ... There should not have been an error
            VerifyResponseCalls(requestContext, true, false);
            Assert.NotNull(result);
            Assert.Null(result.Messages);
        }

        #endregion

        #region Private Helpers

        private static void VerifyResponseCalls(Mock<RequestContext<SaveResultRequestResult>> requestContext, bool successCalled, bool errorCalled)
        {
            requestContext.Verify(rc => rc.SendResult(It.IsAny<SaveResultRequestResult>()), 
                successCalled ? Times.Once() : Times.Never());
            requestContext.Verify(rc => rc.SendError(It.IsAny<object>()),
                errorCalled ? Times.Once() : Times.Never());
        }

        private static IFileStreamFactory GetCsvStreamFactory(IDictionary<string, byte[]> storage, SaveResultsAsCsvRequestParams saveParams)
        {
            Mock<IFileStreamFactory> mock = new Mock<IFileStreamFactory>();
            mock.Setup(fsf => fsf.GetReader(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output])));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output =>
                {
                    storage.Add(output, new byte[8192]);
                    return new SaveAsCsvFileStreamWriter(new MemoryStream(storage[output]), saveParams);
                });

            return mock.Object;
        }

        private static IFileStreamFactory GetJsonStreamFactory(IDictionary<string, byte[]> storage, SaveResultsAsJsonRequestParams saveParams)
        {
            Mock<IFileStreamFactory> mock = new Mock<IFileStreamFactory>();
            mock.Setup(fsf => fsf.GetReader(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output])));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output =>
                {
                    storage.Add(output, new byte[8192]);
                    return new SaveAsJsonFileStreamWriter(new MemoryStream(storage[output]), saveParams);
                });

            return mock.Object;
        }

        #endregion
    }
}
