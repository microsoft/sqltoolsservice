// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
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
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // Request to save the results as csv with correct parameters
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_1.csv",
                IncludeHeaders = true
            };
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);          
            await saveTask;

            // Expect to see a file successfully created in filepath and a success message
            Assert.Null(result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

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
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument , OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

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
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);         
            await saveTask;

            // Expect to see a file successfully created in filepath and a success message
            Assert.Null(result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

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
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // Request to save the results as csv with incorrect filepath
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "G:\\test.csv" : "/test.csv"
            };

            string errMessage = null;
            var saveRequest = GetSaveResultsContextMock( null, err => errMessage = (string) err);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object);           
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);          
            await saveTask;

            // Expect to see error message
            Assert.NotNull(errMessage);
            VerifySaveResultsCallCount(saveRequest, Times.Never(), Times.Once());
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test saving results to CSV file when the requested result set is no longer active
        /// </summary>
        [Fact]
        public async void SaveResultsAsCsvQueryNotFoundTest()
        {
            // Create a query execution service
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);

            // Request to save the results as csv with query that is no longer active
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = "falseuri",
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_3.csv"
            };
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect message that save failed
            Assert.NotNull(result.Messages);
            Assert.False(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());
        }

        /// <summary>
        /// Test save results to a file as JSON with correct parameters
        /// </summary>
        [Fact]
        public async void SaveResultsAsJsonSuccessTest()
        {
            // Execute a query
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // Request to save the results as json with correct parameters
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_4.json"    
            };
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            
            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);         
            await saveTask;


                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
            // Expect to see a file successfully created in filepath and a success message
            Assert.Null(result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

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
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument , OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

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
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();

            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);         
            await saveTask;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
            // Expect to see a file successfully created in filepath and a success message
            Assert.Null(result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

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
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // Request to save the results as json with incorrect filepath
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = Common.OwnerUri,
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "G:\\test.json" : "/test.json"
            };


            string errMessage = null;
            var saveRequest = GetSaveResultsContextMock( null, err => errMessage = (string) err);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            
            // Call save results and wait on the save task
            await queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object);
            ResultSet selectedResultSet = queryService.ActiveQueries[saveParams.OwnerUri].Batches[saveParams.BatchIndex].ResultSets[saveParams.ResultSetIndex];
            Task saveTask = selectedResultSet.GetSaveTask(saveParams.FilePath);         
            await saveTask;

            // Expect to see error message
            Assert.NotNull(errMessage);
            VerifySaveResultsCallCount(saveRequest, Times.Never(), Times.Once());
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test saving results to JSON file when the requested result set is no longer active
        /// </summary>
        [Fact]
        public async void SaveResultsAsJsonQueryNotFoundTest()
        {

            // Create a query service
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);

            // Request to save the results as json with query that is no longer active
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = "falseuri",
                ResultSetIndex = 0,
                BatchIndex = 0,
                FilePath = "testwrite_6.json"
            };
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object).Wait();

            // Expect message that save failed
            Assert.Equal("Failed to save results, ID not found.", result.Messages);
            Assert.False(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());
        }

        #region Mocking

        /// <summary>
        /// Mock the requestContext for saving a result set
        /// </summary>
        /// <param name="resultCallback"></param>
        /// <param name="errorCallback"></param>
        /// <returns></returns>
        private static Mock<RequestContext<SaveResultRequestResult>> GetSaveResultsContextMock(
            Action<SaveResultRequestResult> resultCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<SaveResultRequestResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<SaveResultRequestResult> ()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }

            // Setup the mock for SendError
            var sendErrorFlow = requestContext
                .Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return requestContext;
        }

        /// <summary>
        /// Verify the call count for sendResult and error
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="sendResultCalls"></param>
        /// <param name="sendErrorCalls"></param>
        private static void VerifySaveResultsCallCount(Mock<RequestContext<SaveResultRequestResult>> mock,
            Times sendResultCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<SaveResultRequestResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        #endregion

    }
}
