// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.SaveResults
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
                OwnerUri = Constants.OwnerUri  // Won't exist because nothing has executed
            };
            var evf = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await qes.HandleSaveResultsAsCsvRequest(saveParams, evf.Object);

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            evf.Validate();
        }

        [Fact]
        public async Task SaveResultAsCsvFailure()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            ConcurrentDictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(Common.ExecutionPlanTestDataSet, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set and get it to throw because of invalid column selection
            SaveResultsAsCsvRequestParams saveParams = new SaveResultsAsCsvRequestParams
            {
                BatchIndex = 0,
                FilePath = "qqq",
                OwnerUri = Constants.OwnerUri,
                ResultSetIndex = 0,
                ColumnStartIndex = -1,
                ColumnEndIndex = 100,
                RowStartIndex = 0,
                RowEndIndex = 5
            };
            qes.CsvFileFactory = GetCsvStreamFactory(storage, saveParams);
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardErrorValidation()
                .Complete();


            await qes.HandleSaveResultsAsCsvRequest(saveParams, efv.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            efv.Validate();
        }

        [Fact]
        public async Task SaveResultsAsCsvSuccess()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            ConcurrentDictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(Common.ExecutionPlanTestDataSet, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = null, OwnerUri = Constants.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set from a query
            SaveResultsAsCsvRequestParams saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Constants.OwnerUri,
                FilePath = "qqq",
                BatchIndex = 0,
                ResultSetIndex = 0
            };
            qes.CsvFileFactory = GetCsvStreamFactory(storage, saveParams);
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardResultValidator()
                .Complete();

            await qes.HandleSaveResultsAsCsvRequest(saveParams, efv.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... I should have a successful result
            // ... There should not have been an error
            efv.Validate();
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
                OwnerUri = Constants.OwnerUri  // Won't exist because nothing has executed
            };
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await qes.HandleSaveResultsAsJsonRequest(saveParams, efv.Object);

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            efv.Validate();
        }

        [Fact]
        public async Task SaveResultAsJsonFailure()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            ConcurrentDictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set and get it to throw because of invalid column selection
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams
            {
                BatchIndex = 0,
                FilePath = "qqq",
                OwnerUri = Constants.OwnerUri,
                ResultSetIndex = 0,
                ColumnStartIndex = -1,
                ColumnEndIndex = 100,
                RowStartIndex = 0,
                RowEndIndex = 5
            };
            qes.JsonFileFactory = GetJsonStreamFactory(storage, saveParams);
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await qes.HandleSaveResultsAsJsonRequest(saveParams, efv.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            efv.Validate();
        }

        [Fact]
        public async Task SaveResultsAsJsonSuccess()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            ConcurrentDictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set from a query
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Constants.OwnerUri,
                FilePath = "qqq",
                BatchIndex = 0,
                ResultSetIndex = 0
            };
            qes.JsonFileFactory = GetJsonStreamFactory(storage, saveParams);
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardResultValidator()
                .Complete();
            await qes.HandleSaveResultsAsJsonRequest(saveParams, efv.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... I should have a successful result
            // ... There should not have been an error
            efv.Validate();
        }

        #endregion
        
        #region Excel Tests 
        
        [Fact]
        public async Task SaveResultsExcelNonExistentQuery()
        {
            // Given: A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(null);
            QueryExecutionService qes = Common.GetPrimedExecutionService(null, false, false, ws);

            // If: I attempt to save a result set from a query that doesn't exist
            SaveResultsAsExcelRequestParams saveParams = new SaveResultsAsExcelRequestParams
            {
                OwnerUri = Constants.OwnerUri  // Won't exist because nothing has executed
            };
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await qes.HandleSaveResultsAsExcelRequest(saveParams, efv.Object);

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            efv.Validate();
        }

        [Fact]
        public async Task SaveResultAsExcelFailure()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            ConcurrentDictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set and get it to throw because of invalid column selection
            SaveResultsAsExcelRequestParams saveParams = new SaveResultsAsExcelRequestParams
            {
                BatchIndex = 0,
                FilePath = "qqq",
                OwnerUri = Constants.OwnerUri,
                ResultSetIndex = 0,
                ColumnStartIndex = -1,
                ColumnEndIndex = 100,
                RowStartIndex = 0,
                RowEndIndex = 5
            };
            qes.JsonFileFactory = GetExcelStreamFactory(storage, saveParams);
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await qes.HandleSaveResultsAsExcelRequest(saveParams, efv.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... An error event should have been fired
            // ... No success event should have been fired
            efv.Validate();
        }

        [Fact]
        public async Task SaveResultsAsExcelSuccess()
        {
            // Given: 
            // ... A working query and workspace service
            WorkspaceService<SqlToolsSettings> ws = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            ConcurrentDictionary<string, byte[]> storage;
            QueryExecutionService qes = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, ws, out storage);

            // ... The query execution service has executed a query with results
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await qes.HandleExecuteRequest(executeParams, executeRequest.Object);
            await qes.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // If: I attempt to save a result set from a query
            SaveResultsAsExcelRequestParams saveParams = new SaveResultsAsExcelRequestParams
            {
                OwnerUri = Constants.OwnerUri,
                FilePath = "qqq",
                BatchIndex = 0,
                ResultSetIndex = 0
            };
            qes.ExcelFileFactory = GetExcelStreamFactory(storage, saveParams);
            var efv = new EventFlowValidator<SaveResultRequestResult>()
                .AddStandardResultValidator()
                .Complete();
            await qes.HandleSaveResultsAsExcelRequest(saveParams, efv.Object);
            await qes.ActiveQueries[saveParams.OwnerUri]
                .Batches[saveParams.BatchIndex]
                .ResultSets[saveParams.ResultSetIndex]
                .SaveTasks[saveParams.FilePath];

            // Then:
            // ... I should have a successful result
            // ... There should not have been an error
            efv.Validate();
        }
        
        #endregion

        #region Private Helpers

        private static IFileStreamFactory GetCsvStreamFactory(IDictionary<string, byte[]> storage, SaveResultsAsCsvRequestParams saveParams)
        {
            Mock<IFileStreamFactory> mock = new Mock<IFileStreamFactory>();
            mock.Setup(fsf => fsf.GetReader(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output]), new QueryExecutionSettings()));
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
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output]), new QueryExecutionSettings()));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output =>
                {
                    storage.Add(output, new byte[8192]);
                    return new SaveAsJsonFileStreamWriter(new MemoryStream(storage[output]), saveParams);
                });

            return mock.Object;
        }

        private static IFileStreamFactory GetExcelStreamFactory(IDictionary<string, byte[]> storage,
            SaveResultsAsExcelRequestParams saveParams)
        {
            Mock<IFileStreamFactory> mock = new Mock<IFileStreamFactory>();
            mock.Setup(fsf => fsf.GetReader(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output]), new QueryExecutionSettings()));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output =>
                {
                    storage.Add(output, new byte[8192]);
                    return new SaveAsExcelFileStreamWriter(new MemoryStream(storage[output]), saveParams);
                });
            
            return mock.Object;
        }

        #endregion
    }

    public static class SaveResultEventFlowValidatorExtensions
    {
        public static EventFlowValidator<SaveResultRequestResult> AddStandardResultValidator(
            this EventFlowValidator<SaveResultRequestResult> efv)
        {
            return efv.AddResultValidation(r =>
            {
                Assert.NotNull(r);
                Assert.Null(r.Messages);
            });
        }
    }
}
