//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.Execution
{
    public class ServiceIntegrationTests
    {

        #region Get SQL Tests

        [Fact]
        public void ExecuteDocumentStatementTest()
        {            
            string query = string.Format("{0}{1}GO{1}{0}", Constants.StandardQuery, Environment.NewLine);
            var workspaceService = GetDefaultWorkspaceService(query);
            var queryService = new QueryExecutionService(null, workspaceService);

            var queryParams = new ExecuteDocumentStatementParams { OwnerUri = Constants.OwnerUri, Line = 0, Column = 0 };
            var queryText = queryService.GetSqlText(queryParams);

            // The text should match the standard query
            Assert.Equal(queryText, Constants.StandardQuery);
        }

        [Fact]
        public void ExecuteDocumentStatementSameLine()
        {
            var statement1 = Constants.StandardQuery;
            var statement2 = "SELECT * FROM sys.databases";
            // Test putting the cursor at the start of the line
            ExecuteDocumentStatementSameLineHelper(statement1, statement2, 0, statement1);
            // Test putting the cursor at the end of statement 1
            ExecuteDocumentStatementSameLineHelper(statement1, statement2, statement1.Length, statement1);
            // Test putting the cursor at the start of statement 2
            ExecuteDocumentStatementSameLineHelper(statement1, statement2, statement1.Length + 1, statement2);
            // Test putting the cursor at the end of the line
            ExecuteDocumentStatementSameLineHelper(statement1, statement2, statement1.Length + 1 + statement2.Length, statement2);
            // Test putting the cursor after a semicolon when only one statement is on the line
            ExecuteDocumentStatementSameLineHelper(statement1, "", statement1.Length + 1, statement1);
        }

        private void ExecuteDocumentStatementSameLineHelper(string statement1, string statement2, int cursorColumn, string expectedQueryText)
        {
            string query = string.Format("{0};{1}", statement1, statement2);
            var workspaceService = GetDefaultWorkspaceService(query);
            var queryService = new QueryExecutionService(null, workspaceService);

            // If a line has multiple statements and the cursor is somewhere in the line
            var queryParams = new ExecuteDocumentStatementParams { OwnerUri = Constants.OwnerUri, Line = 0, Column = cursorColumn };
            var queryText = queryService.GetSqlText(queryParams);

            // The query text should match the expected statement at the cursor
            Assert.Equal(expectedQueryText, queryText);
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
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
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
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
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

        public static IEnumerable<object[]> TestResultSetsData(int numTests) => Common.TestResultSetsEnumeration.Select(r => new object[] { r }).Take(numTests);

        [Xunit.Theory]
        [MemberData(nameof(TestResultSetsData), parameters: 5)]
        public async Task QueryExecuteSingleBatchSingleResultTest(TestResultSet testResultSet)
        {
            // If:
            // ... I request to execute a valid query with results
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var testDataSet = new[] { testResultSet };
            var queryService = Common.GetPrimedExecutionService(testDataSet, true, false, false, workspaceService, sizeFactor: testResultSet.Rows.Count/Common.StandardRows + 1);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            List<ResultSetEventParams> collectedResultSetEventParams = new List<ResultSetEventParams>();
            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddResultSetValidator(ResultSetAvailableEvent.Type, collectedResultSetEventParams)
                .AddResultSetValidator(ResultSetUpdatedEvent.Type, collectedResultSetEventParams)
                .AddResultSetValidator(ResultSetCompleteEvent.Type, collectedResultSetEventParams)
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.ValidateResultSetSummaries(collectedResultSetEventParams).Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Xunit.Theory]
        [MemberData(nameof(TestResultSetsData), parameters: 4)]
        public async Task QueryExecuteSingleBatchMultipleResultTest(TestResultSet testResultSet)
        {
            // If:
            // ... I request to execute a valid query with one batch and multiple result sets
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var testDataSet = new[] {testResultSet, testResultSet};
            var queryService = Common.GetPrimedExecutionService(testDataSet, true, false, false, workspaceService, sizeFactor: testResultSet.Rows.Count / Common.StandardRows + 1);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            List<ResultSetEventParams> collectedResultSetEventParams = new List<ResultSetEventParams>();
            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddResultSetValidator(ResultSetAvailableEvent.Type, collectedResultSetEventParams)
                .AddResultSetValidator(ResultSetUpdatedEvent.Type, collectedResultSetEventParams)
                .AddResultSetValidator(ResultSetCompleteEvent.Type, collectedResultSetEventParams)
                .AddStandardMessageValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.ValidateResultSetSummaries(collectedResultSetEventParams).Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteMultipleBatchSingleResultTest()
        {
            // If:
            // ... I request a to execute a valid query with multiple batches
            var workspaceService = GetDefaultWorkspaceService(string.Format("{0}\r\nGO\r\n{0}", Constants.StandardQuery));
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams { OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            List<ResultSetEventParams> collectedResultSetEventParams = new List<ResultSetEventParams>();
            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddResultSetValidator(ResultSetAvailableEvent.Type, collectedResultSetEventParams)
                .AddResultSetValidator(ResultSetUpdatedEvent.Type, collectedResultSetEventParams)
                .AddResultSetValidator(ResultSetCompleteEvent.Type, collectedResultSetEventParams)
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(2)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.ValidateResultSetSummaries(collectedResultSetEventParams).Validate();

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
            var queryService = Common.GetPrimedExecutionService(null, false, false, false, workspaceService);
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
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
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
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
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
        public async Task QueryExecuteInvalidQueryTest()
        {
            // If:
            // ... I request to execute a query that is invalid
            var workspaceService = GetDefaultWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, true, false, workspaceService);
            var queryParams = new ExecuteDocumentSelectionParams {OwnerUri = Constants.OwnerUri, QuerySelection = Common.WholeDocument};

            var efv = new EventFlowValidator<ExecuteRequestResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .AddStandardQueryCompleteValidator(1)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... An error should have been sent
            efv.Validate();

            // ... There should not be an active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        // TODO https://github.com/Microsoft/vscode-mssql/issues/1003 reenable and make non-flaky
        // [Fact]
        public async Task SimpleExecuteErrorWithNoResultsTest()
        {
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, null);
            var queryParams = new SimpleExecuteParams { OwnerUri = Constants.OwnerUri, QueryString = Constants.StandardQuery };
            var efv = new EventFlowValidator<SimpleExecuteResult>()
                .AddSimpleExecuteErrorValidator(SR.QueryServiceResultSetHasNoResults)
                .Complete();
            await queryService.HandleSimpleExecuteRequest(queryParams, efv.Object);

            await Task.WhenAll(queryService.ActiveSimpleExecuteRequests.Values);

            Query q = queryService.ActiveQueries.Values.First();
            Assert.NotNull(q);
            q.ExecutionTask.Wait();

            efv.Validate();

            Assert.Equal(0, queryService.ActiveQueries.Count);
            
        }
        
        // TODO reenable and make non-flaky
        // [Fact]
        public async Task SimpleExecuteVerifyResultsTest()
        {
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, false, null);
            var queryParams = new SimpleExecuteParams { OwnerUri = Constants.OwnerUri, QueryString = Constants.StandardQuery };
            var efv = new EventFlowValidator<SimpleExecuteResult>()
                .AddSimpleExecuteQueryResultValidator(Common.StandardTestDataSet)
                .Complete();
            await queryService.HandleSimpleExecuteRequest(queryParams, efv.Object);

            await Task.WhenAll(queryService.ActiveSimpleExecuteRequests.Values);

            Query q = queryService.ActiveQueries.Values.First();

            Assert.NotNull(q);

            // wait on the task to finish
            q.ExecutionTask.Wait();
            
            efv.Validate();

            Assert.Equal(0, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task SimpleExecuteMultipleQueriesTest()
        {
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, false, null);
            var queryParams = new SimpleExecuteParams { OwnerUri = Constants.OwnerUri, QueryString = Constants.StandardQuery };
            var efv1 = new EventFlowValidator<SimpleExecuteResult>()
                .AddSimpleExecuteQueryResultValidator(Common.StandardTestDataSet)
                .Complete();
            var efv2 = new EventFlowValidator<SimpleExecuteResult>()
                .AddSimpleExecuteQueryResultValidator(Common.StandardTestDataSet)
                .Complete();
            Task qT1 = queryService.HandleSimpleExecuteRequest(queryParams, efv1.Object);
            Task qT2 = queryService.HandleSimpleExecuteRequest(queryParams, efv2.Object);

            await Task.WhenAll(qT1, qT2);

            await Task.WhenAll(queryService.ActiveSimpleExecuteRequests.Values);

            var queries = queryService.ActiveQueries.Values.ToArray();
            var queryTasks = queries.Select(query => query.ExecutionTask);
            
            await Task.WhenAll(queryTasks);
            
            efv1.Validate();
            efv2.Validate();

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

        public static EventFlowValidator<TRequestContext> AddResultSetValidator<TRequestContext, T>(
            this EventFlowValidator<TRequestContext> efv, EventType<T> expectedEvent, List<ResultSetEventParams> resultSetEventParamList = null) where T : ResultSetEventParams
        {
            return efv.SetupCallbackOnMethodSendEvent(expectedEvent, (p) =>
            {
                // Validate OwnerURI and summary are returned
                Assert.Equal(Constants.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.ResultSetSummary);
                resultSetEventParamList?.Add(p);
            });
        }

        public static EventFlowValidator<TRequestContext> ValidateResultSetSummaries<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv, List<ResultSetEventParams> resultSetEventParamList)
        {
            string GetResultSetKey(ResultSetSummary summary) => $"BatchId:{summary.BatchId}, ResultId:{summary.Id}";

            // Separate the result set resultSetEventParamsList by batchid, resultsetid and by resultseteventtype.
            ConcurrentDictionary<string, List<ResultSetEventParams>> resultSetDictionary =
                new ConcurrentDictionary<string, List<ResultSetEventParams>>();

            foreach (var resultSetEventParam in resultSetEventParamList)
            {
                resultSetDictionary
                    .GetOrAdd(GetResultSetKey(resultSetEventParam.ResultSetSummary), (key) => new List<ResultSetEventParams>())
                    .Add(resultSetEventParam);
            }

            foreach (var (key, list) in resultSetDictionary)
            {
                ResultSetSummary  completeSummary = null, lastResultSetSummary = null;
                for (int i = 0; i < list.Count; i++)
                {
                    VerifyResultSummary(key, i, list, ref completeSummary, ref lastResultSetSummary);
                }

                // Verify that the completeEvent and lastResultSetSummary has same number of rows 
                //
                if (lastResultSetSummary != null && completeSummary != null)
                {
                    Assert.True(lastResultSetSummary.RowCount == completeSummary.RowCount, "CompleteSummary and last Update Summary should have same number of rows");
                }
            }


            return efv;
        }

        /// <summary>
        /// Verifies that a ResultSummary at a given position as expected within the list of ResultSummary items
        /// </summary>
        /// <param name="batchIdResultSetId">The batchId and ResultSetId for this list of events</param>
        /// <param name="position">The position with resultSetEventParamsList that we are verifying in this call<</param>
        /// <param name="resultSetEventParamsList">The list of resultSetParams that we are verifying</param>
        /// <param name="completeSummary"> This should be null when we start validating the list of ResultSetEventParams</param>
        /// <param name="lastResultSetSummary"> This should be null when we start validating the list of ResultSetEventParams</param>
        private static void VerifyResultSummary(string batchIdResultSetId, int position, List<ResultSetEventParams> resultSetEventParamsList, ref ResultSetSummary completeSummary, ref ResultSetSummary lastResultSetSummary)
        {
            ResultSetEventParams resultSetEventParams = resultSetEventParamsList[position];
            switch (resultSetEventParams.GetType().Name)
            {
                case nameof(ResultSetAvailableEventParams):
                    // Verify that the availableEvent is the first and only one of this type in the sequence. Since we set lastResultSetSummary on each available or updatedEvent, we check that there has been no lastResultSetSummary previously set yet.
                    //
                    Assert.True(null == lastResultSetSummary,
                        $"AvailableResultSet was not found to be the first message received for {batchIdResultSetId}"
                        + $"\r\nresultSetEventParamsList is:{string.Join("\r\n\t\t", resultSetEventParamsList.ConvertAll((p) => p.GetType() + ":" + p.ResultSetSummary))}"
                    );

                    // Save the lastResultSetSummary for this event for other verifications.
                    //
                    lastResultSetSummary = resultSetEventParams.ResultSetSummary;
                    break;
                case nameof(ResultSetUpdatedEventParams):
                    // Verify that the updateEvent is not the first in the sequence. Since we set lastResultSetSummary on each available or updatedEvent, we check that there has been no lastResultSetSummary previously set yet.
                    //
                    Assert.True(null != lastResultSetSummary,
                        $"UpdateResultSet was found to be the first message received for {batchIdResultSetId}"
                        + $"\r\nresultSetEventParamsList is:{string.Join("\r\n\t\t", resultSetEventParamsList.ConvertAll((p) => p.GetType() + ":" + p.ResultSetSummary))}"
                    );

                    // Verify that the number of rows in the current updatedSummary is >= those in the lastResultSetSummary
                    //
                    Assert.True(resultSetEventParams.ResultSetSummary.RowCount >= lastResultSetSummary.RowCount,
                        $"UpdatedResultSetSummary at position: {position} has less rows than LastUpdatedSummary (or AvailableSummary) received for {batchIdResultSetId}"
                        + $"\r\nresultSetEventParamsList is:{string.Join("\r\n\t\t", resultSetEventParamsList.ConvertAll((p) => p.GetType() + ":" + p.ResultSetSummary))}"
                        + $"\r\n\t\t LastUpdatedSummary (or Available):{lastResultSetSummary}"
                        + $"\r\n\t\t UpdatedResultSetSummary:{resultSetEventParams.ResultSetSummary}");

                    // Save the lastResultSetSummary for this event for other verifications.
                    //
                    lastResultSetSummary = resultSetEventParams.ResultSetSummary;
                    break;
                case nameof(ResultSetCompleteEventParams):
                    // Verify that there is only one completeEvent
                    //
                    Assert.True(null == completeSummary,
                          $"CompleteResultSet was received multiple times for {batchIdResultSetId}"
                        + $"\r\nresultSetEventParamsList is:{string.Join("\r\n\t\t", resultSetEventParamsList.ConvertAll((p) => p.GetType() + ":" + p.ResultSetSummary))}"
                        );

                    // Save the completeSummary for this event for other verifications.
                    //
                    completeSummary = resultSetEventParams.ResultSetSummary;
                    
                    // Verify that the complete flag is set
                    //
                    Assert.True(completeSummary.Complete,
                          $"completeSummary.Complete is not true"
                        + $"\r\nresultSetEventParamsList is:{string.Join("\r\n\t\t", resultSetEventParamsList.ConvertAll((p) => p.GetType() + ":" + p.ResultSetSummary))}"
                     );
                    break;
                default:
                    throw new AssertionException(
                        $"Unknown type of ResultSetEventParams, actual type received is: {resultSetEventParams.GetType().Name}");
            }
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
