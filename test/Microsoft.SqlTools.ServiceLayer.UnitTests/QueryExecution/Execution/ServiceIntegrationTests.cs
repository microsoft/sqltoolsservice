//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.Execution
{
    public class ServiceIntegrationTests
    {

        #region Get SQL Tests

         [Fact]
        public void GetSqlTextFromDocumentRequestFull()
        {
            // Setup:
            // ... Create a workspace service with a multi-line constructed query
            // ... Create a query execution service without a connection service (we won't be
            //     executing queries), and the previously created workspace service
            string query = string.Format("{0}{1}GO{1}{0}", Constants.StandardQuery, Environment.NewLine);
            var workspaceService = GetDefaultWorkspaceService(query);
            var queryService = new QueryExecutionService(null, workspaceService);

            // If: I attempt to get query text from execute document params (entire document)
            var queryParams = new ExecuteDocumentSelectionParams {OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};
            var queryText = queryService.GetSqlText(queryParams);

            // Then: The text should match the constructed query
            Assert.Equal(query, queryText);
        }

        [Fact]
        public void GetSqlTextFromDocumentRequestFull()
        {
            // Setup:
            // ... Create a workspace service with a multi-line constructed query
            // ... Create a query execution service without a connection service (we won't be
            //     executing queries), and the previously created workspace service
            string query = string.Format("{0}{1}GO{1}{0}", Constants.StandardQuery, Environment.NewLine);
            var workspaceService = GetDefaultWorkspaceService(query);
            var queryService = new QueryExecutionService(null, workspaceService);

            // If: I attempt to get query text from execute document params (entire document)
            var queryParams = new ExecuteDocumentSelectionParams {OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};
            var queryText = queryService.GetSqlText(queryParams);

            // Then: The text should match the constructed query
            Assert.Equal(query, queryText);
        }

        [Fact]
        public void GetSqlTextFromDocumentRequestPartial()
        {
            // Setup:
            // ... Create a workspace service with a multi-line constructed query
            string query = string.Format("{0}{1}GO{1}{0}", Constants.StandardQuery, Environment.NewLine);
            var workspaceService = GetDefaultWorkspaceService(query);
            var queryService = new QueryExecutionService(null, workspaceService);

            // If: I attempt to get query text from execute document params (partial document)
            var queryParams = new ExecuteDocumentSelectionParams {OwnerUri = Constants.OwnerUri, QuerySelection = Common.SubsectionDocument};
            var queryText = queryService.GetSqlText(queryParams);

            // Then: The text should be a subset of the constructed query
            Assert.Contains(queryText, query);
        }

        [Fact]
        public void GetSqlTextFromStringRequest()
        {
            // Setup: 
            // ... Create a query execution service without a connection service or workspace
            //     service (we won't execute code that uses either
            var queryService = new QueryExecutionService(null, null);

            // If: I attempt to get query text from execute string params
            var queryParams = new ExecuteStringParams {OwnerUri = Constants.OwnerUri, Query = Constants.StandardQuery};
            var queryText = queryService.GetSqlText(queryParams);

            // Then: The text should match the standard query
            Assert.Equal(Constants.StandardQuery, queryText);
        }

        [Fact]
        public void GetSqlTextFromInvalidType()
        {
            // Setup:
            // ... Mock up an implementation of ExecuteRequestParamsBase
            // ... Create a query execution service without a connection service or workspace
            //     service (we won't execute code that uses either
            var mockParams = new Mock<ExecuteRequestParamsBase>().Object;
            var queryService = new QueryExecutionService(null, null);

            // If: I attempt to get query text from the mock params
            // Then: It should throw an exception
            Assert.Throws<InvalidCastException>(() => queryService.GetSqlText(mockParams));
        }

        #endregion

        #region Inter-Service API Tests

        [Fact]
        public async Task InterServiceExecuteNullExecuteParams()
        {
            // Setup: Create a query service
            var qes = new QueryExecutionService(null, null);
            var eventSender = new EventFlowValidator<ExecuteRequestResult>().Complete().Object;


            // If: I call the inter-service API to execute with a null execute params
            // Then: It should throw
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => qes.InterServiceExecuteQuery(null, null, eventSender, null, null, null, null));
        }

        [Fact]
        public async Task InterServiceExecuteNullEventSender()
        {
            // Setup: Create a query service, and execute params
            var qes = new QueryExecutionService(null, null);
            var executeParams = new ExecuteStringParams();

            // If: I call the inter-service API to execute a query with a a null event sender
            // Then: It should throw
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => qes.InterServiceExecuteQuery(executeParams, null, null, null, null, null, null));
        }

        [Fact]
        public async Task InterServiceDisposeNullSuccessFunc()
        {
            // Setup: Create a query service and dispose params
            var qes = new QueryExecutionService(null, null);
            Func<string, Task> failureFunc = Task.FromResult;

            // If: I call the inter-service API to dispose a query with a null success function
            // Then: It should throw
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => qes.InterServiceDisposeQuery(Constants.OwnerUri, null, failureFunc));
        }

        [Fact]
        public async Task InterServiceDisposeNullFailureFunc()
        {
            // Setup: Create a query service and dispose params
            var qes = new QueryExecutionService(null, null);
            Func<Task> successFunc = () => Task.FromResult(0);

            // If: I call the inter-service API to dispose a query with a null success function
            // Then: It should throw
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => qes.InterServiceDisposeQuery(Constants.OwnerUri, successFunc, null));
        }

        #endregion

        #region Execution Tests
        // NOTE: In order to limit test duplication, we're running the ExecuteDocumentSelection
        // version of execute query. The code paths are almost identical.

        [Fact]
        private async Task QueryExecuteAllBatchesNoOp()
        {
            // If:
            // ... I request to execute a valid query with all batches as no op
            var workspaceService = GetDefaultWorkspaceService(string.Format("{0}\r\nGO\r\n{0}", Common.NoOpQuery));
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { QuerySelection = Common.WholeDocument, OwnerUri = Constants.OwnerUri };

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddEventValidation(QueryCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(2, p.BatchSummaries.Length);
                    Assert.All(p.BatchSummaries, bs => Assert.Equal(0, bs.ResultSetSummaries.Length));
                }).Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteSingleBatchNoResultsTest()
        {
            // If:
            // ... I request to execute a valid query with no results
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { QuerySelection = Common.WholeDocument, OwnerUri = Constants.OwnerUri};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();

            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteSingleBatchSingleResultTest()
        {
            // If:
            // ... I request to execute a valid query with results
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false,
                workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardResultSetValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteSingleBatchMultipleResultTest()
        {
            // If:
            // ... I request to execute a valid query with one batch and multiple result sets
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var dataset = new[] {Common.StandardTestResultSet, Common.StandardTestResultSet};
            var queryService = Common.GetPrimedExecutionService(dataset, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardResultSetValidator()
                .AddStandardResultSetValidator()
                .AddStandardMessageValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteMultipleBatchSingleResultTest()
        {
            // If:
            // ... I request a to execute a valid query with multiple batches
            var workspaceService = GetDefaultWorkspaceService(string.Format("{0}\r\nGO\r\n{0}", Constants.StandardQuery));
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardResultSetValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardResultSetValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(2)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteUnconnectedUriTest()
        {
            // Given:
            // If:
            // ... I request to execute a query using a file URI that isn't connected
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, false, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = "notConnected", QuerySelection = Common.WholeDocument };

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be no active queries
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async Task QueryExecuteInProgressTest()
        {
            // If:
            // ... I request to execute a query
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await Common.AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query without waiting for the first to complete
            queryService.ActiveQueries[Constants.OwnerUri].HasExecuted = false; // Simulate query hasn't finished
            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should only be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteCompletedTest()
        {
            // If:
            // ... I request to execute a query
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await Common.AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query after waiting for the first to complete
            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();

            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should only be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteMissingSelectionTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(string.Empty);

            // If:
            // ... I request to execute a query with a missing query string
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = null};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardErrorValidation()
                .Complete();
            await queryService.HandleExecuteRequest(queryParams, efv.Object);

            // Then:
            // ... Am error should have been sent
            efv.Validate();

            // ... There should not be an active query
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async Task QueryExecuteInvalidQueryTest()
        {
            // If:
            // ... I request to execute a query that is invalid
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, true, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams {OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... Am error should have been sent
            efv.Validate();

            // ... There should not be an active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task SimpleExecuteErrorWithNoResultsTest()
        {
            var queryService = Common.GetPrimedExecutionService(null, true, false, null);
            var queryParams = new SimpleExecuteParams { OwnerUri = Constants.OwnerUri, QueryString = Constants.StandardQuery };
            var efv = new EventFlowValidator<SimpleExecuteResult>()
                .AddSimpleExecuteErrorValidator(SR.QueryServiceResultSetHasNoResults)
                .Complete();
            await queryService.HandleSimpleExecuteRequest(queryParams, efv.Object);

            Query q;
            queryService.ActiveQueries.TryGetValue(Constants.OwnerUri, out q);

            // wait on the task to finish
            q.ExecutionTask.Wait();

            efv.Validate();

            Assert.Equal(0, queryService.ActiveQueries.Count);
        }
        
        [Fact]
        public async Task SimpleExecuteVerifyResultsTest()
        {
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, null);
            var queryParams = new SimpleExecuteParams { OwnerUri = Constants.OwnerUri, QueryString = Constants.StandardQuery };
            var efv = new EventFlowValidator<SimpleExecuteResult>()
                .AddSimpleExecuteQueryResultValidator(Common.StandardTestDataSet)
                .Complete();
            await queryService.HandleSimpleExecuteRequest(queryParams, efv.Object);

            Query q;
            queryService.ActiveQueries.TryGetValue(Constants.OwnerUri, out q);

            // wait on the task to finish
            q.ExecutionTask.Wait();
            
            efv.Validate();

            Assert.Equal(0, queryService.ActiveQueries.Count);
        }

        #endregion

        private static WorkspaceService<SqlToolsSettings> GetDefaultWorkspaceService(string query)
        {
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();
            var workspaceService = Common.GetPrimedWorkspaceService(query);
            return workspaceService;
        }
    }

    public static class QueryExecutionEventFlowValidatorExtensions
    {

        public static EventFlowValidator<SimpleExecuteResult> AddSimpleExecuteQueryResultValidator(
            this EventFlowValidator<SimpleExecuteResult> efv, TestResultSet[] testData)
        {
            return efv.AddResultValidation(p => 
            {
                Assert.Equal(p.RowCount, testData[0].Rows.Count);
            });
        }

        public static EventFlowValidator<SimpleExecuteResult> AddSimpleExecuteErrorValidator(
            this EventFlowValidator<SimpleExecuteResult> efv, string expectedMessage)
        {
            return efv.AddSimpleErrorValidation((m, e) => 
            {
                Assert.Equal(m, expectedMessage);
            });
        }

        public static EventFlowValidator<ExecuteRequestResult> AddStandardQueryResultValidator(
            this EventFlowValidator<ExecuteRequestResult> efv)
        {
            // We just need to makes sure we get a result back, there's no params to validate
            return efv.AddResultValidation(Assert.NotNull);
        }

        public static EventFlowValidator<TRequestContext> AddStandardBatchStartValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(BatchStartEvent.Type, p =>
            {
                // Validate OwnerURI and batch summary is returned
                Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardBatchCompleteValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(BatchCompleteEvent.Type, p =>
            {
                // Validate OwnerURI and result summary are returned
                Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardMessageValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(MessageEvent.Type, p =>
            {
                // Validate OwnerURI and message are returned
                Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.Message);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardResultSetValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(ResultSetCompleteEvent.Type, p =>
            {
                // Validate OwnerURI and summary are returned
                Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.ResultSetSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardQueryCompleteValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv, int expectedBatches)
        {
            return efv.AddEventValidation(QueryCompleteEvent.Type, p =>
            {
                Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummaries);
                Assert.Equal(expectedBatches, p.BatchSummaries.Length);
            });
        }
    }
}
