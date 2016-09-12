// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SaveResultsTests
    {
        [Fact]
        public void SaveResultsAsCsvSuccessTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with correct parameters
            var saveParams = new SaveResultsRequestParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "testwrite.csv";
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect to see a file successfully created in filepath and a success message
            Assert.Equal("Success", result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

            // Delete temp file after test
            if(File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        [Fact]
        public void SaveResultsAsCsvExceptionTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with incorrect filepath
            var saveParams = new SaveResultsRequestParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "G:\\test.csv";
            // SaveResultRequestResult result = null;
            String errMessage = null;
            var saveRequest = GetSaveResultsContextMock( null, err => errMessage = (String) err);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect to see error message
            Assert.NotNull(errMessage);
            VerifySaveResultsCallCount(saveRequest, Times.Never(), Times.Once());
            Assert.False(File.Exists(saveParams.FilePath));
        }

        [Fact]
        public void SaveResultsAsCsvQueryNotFoundTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with query that is no longer active
            var saveParams = new SaveResultsRequestParams { OwnerUri = "falseuri", ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "testwrite.csv";
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            // queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect message that save failed
            Assert.Equal("Failed to save results, ID not found.", result.Messages);
            Assert.False(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());
        }

        #region Mocking

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

        private static void VerifySaveResultsCallCount(Mock<RequestContext<SaveResultRequestResult>> mock,
            Times sendResultCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<SaveResultRequestResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        public static Mock<RequestContext<QueryExecuteResult>> GetQueryExecuteResultContextMock(
            Action<QueryExecuteResult> resultCallback,
            Action<EventType<QueryExecuteCompleteParams>, QueryExecuteCompleteParams> eventCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<QueryExecuteResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }

            // Setup the mock for SendEvent
            var sendEventFlow = requestContext.Setup(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type),
                It.IsAny<QueryExecuteCompleteParams>()))
                .Returns(Task.FromResult(0));
            if (eventCallback != null)
            {
                sendEventFlow.Callback(eventCallback);
            }

            // Setup the mock for SendError
            var sendErrorFlow = requestContext.Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return requestContext;
        }


        #endregion

    }
}
