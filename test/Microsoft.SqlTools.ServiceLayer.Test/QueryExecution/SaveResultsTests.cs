// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    /// <summary>
    /// Tests for saving a result set to a file
    /// </summary>
    public class SaveResultsTests
    {
        /// <summary>
        /// Test save results to a file as CSV with correct parameters
        /// </summary>
        [Fact]
        public async void SaveResultsAsCsvSuccessTest()
        {
            // Execute a query
            var workplaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new [] {Common.StandardTestData}, true, false, workplaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, executeParams, executeRequest.Object);

            // Request to save the results as csv with correct parameters
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_1.csv",
                IncludeHeaders = true
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddResultValidation(r =>
                {
                    Assert.Null(r.Messages);
                }).Complete();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            await selectedResultSet.GetSaveTask(saveParams.FilePath);

            // Expect to see a file successfully created in filepath and a success message
            saveRequest.Validate();
            Assert.True(File.Exists(saveParams.FilePath));

            // Delete temp file after test
            if (File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        /// <summary>
        /// Test save results to a file as CSV with a selection of cells and correct parameters
        /// </summary>
        [Fact]
        public async void SaveResultsAsCsvWithSelectionSuccessTest()
        {
            // Execute a query
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new []{Common.StandardTestData}, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument , OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, executeParams, executeRequest.Object);

            // Request to save the results as csv with correct parameters
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_2.csv",
                IncludeHeaders = true,
                RowStartIndex = 0,
                RowEndIndex = 0,
                ColumnStartIndex = 0,
                ColumnEndIndex = 0
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddResultValidation(r =>
                {
                    Assert.Null(r.Messages);
                }).Complete();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);         
            await saveTask;

            // Expect to see a file successfully created in filepath and a success message
            saveRequest.Validate();
            Assert.True(File.Exists(saveParams.FilePath));

            // Delete temp file after test
            if (File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        /// <summary>
        /// Test handling exception in saving results to CSV file
        /// </summary>
        [Fact]
        public async void SaveResultsAsCsvExceptionTest()
        {
            // Execute a query
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new[] {Common.StandardTestData}, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, executeParams, executeRequest.Object);

            // Request to save the results as csv with incorrect filepath
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "G:\\test.csv" : "/test.csv"
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddErrorValidation<SaveResultRequestError>(e =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(e.message));
                }).Complete();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);           
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            await selectedResultSet.GetSaveTask(saveParams.FilePath);

            // Expect to see error message
            saveRequest.Validate();
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test saving results to CSV file when the requested result set is no longer active
        /// </summary>
        [Fact]
        public async Task SaveResultsAsCsvQueryNotFoundTest()
        {
            // Create a query execution service
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);

            // Request to save the results as csv with query that is no longer active
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = "falseuri",
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_3.csv"
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddResultValidation(r =>
                {
                    Assert.NotNull(r.Messages);
                }).Complete();
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);

            // Expect message that save failed
            saveRequest.Validate();
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test save results to a file as JSON with correct parameters
        /// </summary>
        [Fact]
        public async void SaveResultsAsJsonSuccessTest()
        {
            // Execute a query
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new[] {Common.StandardTestData}, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, executeParams, executeRequest.Object);

            // Request to save the results as json with correct parameters
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_4.json"
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddResultValidation(r =>
                {
                    Assert.Null(r.Messages);
                }).Complete();
            
            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            await selectedResultSet.GetSaveTask(saveParams.FilePath);
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
            // Expect to see a file successfully created in filepath and a success message
            saveRequest.Validate();
            Assert.True(File.Exists(saveParams.FilePath));

            // Delete temp file after test
            if (File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        /// <summary>
        /// Test save results to a file as JSON with a selection of cells and correct parameters
        /// </summary>
        [Fact]
        public async void SaveResultsAsJsonWithSelectionSuccessTest()
        {
            // Execute a query
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new[] { Common.StandardTestData }, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument , OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, executeParams, executeRequest.Object);

            // Request to save the results as json with correct parameters
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_5.json",          
                RowStartIndex = 0,
                RowEndIndex = 1,
                ColumnStartIndex = 0,
                ColumnEndIndex = 1             
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddResultValidation(r =>
                {
                    Assert.Null(r.Messages);
                }).Complete();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            await selectedResultSet.GetSaveTask(saveParams.FilePath);

            // Expect to see a file successfully created in filepath and a success message
            saveRequest.Validate();
            Assert.True(File.Exists(saveParams.FilePath));

            // Delete temp file after test
            if (File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        /// <summary>
        /// Test handling exception in saving results to JSON file
        /// </summary>
        [Fact]
        public async void SaveResultsAsJsonExceptionTest()
        {
            // Execute a query
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new [] {Common.StandardTestData}, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, executeParams, executeRequest.Object);

            // Request to save the results as json with incorrect filepath
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "G:\\test.json" : "/test.json"
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddErrorValidation<SaveResultRequestError>(e =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(e.message));
                }).Complete();
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            
            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            await selectedResultSet.GetSaveTask(saveParams.FilePath);

            // Expect to see error message
            saveRequest.Validate();
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test saving results to JSON file when the requested result set is no longer active
        /// </summary>
        [Fact]
        public async Task SaveResultsAsJsonQueryNotFoundTest()
        {
            // Create a query service
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);

            // Request to save the results as json with query that is no longer active
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = "falseuri",
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_6.json"
            };
            var saveRequest = new EventFlowValidator<SaveResultRequestResult>()
                .AddResultValidation(r =>
                {
                    Assert.Equal("Failed to save results, ID not found.", r.Messages);
                }).Complete();
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);

            // Expect message that save failed
            saveRequest.Validate();
            Assert.False(File.Exists(saveParams.FilePath));
        }
    }
}
