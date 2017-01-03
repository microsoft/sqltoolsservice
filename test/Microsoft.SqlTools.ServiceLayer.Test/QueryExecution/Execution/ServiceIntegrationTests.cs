//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class ServiceIntegrationTests
    {
        [Fact]
        public async Task QueryExecuteAllBatchesNoOp()
        {
            // If:
            // ... I request to execute a valid query with all batches as no op
            var workspaceService = GetDefaultWorkspaceService(string.Format("{0}\r\nGO\r\n{0}", Common.NoOpQuery));
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardMessageValidator()
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(0, p.BatchSummaries.Length);
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
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
<<<<<<< HEAD
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(1, p.BatchSummaries.Length);
                }).Complete();
=======
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
>>>>>>> dev

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
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new[] { Common.StandardTestData }, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardResultSetValidator()
<<<<<<< HEAD
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(1, p.BatchSummaries.Length);
                }).Complete();
=======
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(1)
                .Complete();
>>>>>>> dev
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
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var dataset = new[] { Common.StandardTestData, Common.StandardTestData };
            var queryService = Common.GetPrimedExecutionService(dataset, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardResultSetValidator()
                .AddStandardResultSetValidator()
<<<<<<< HEAD
                .AddStandardMessageValidator()
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(1, p.BatchSummaries.Length);
                }).Complete();
=======
                .AddStandardQueryCompleteValidator(1)
                .Complete();
>>>>>>> dev
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
            var workspaceService = GetDefaultWorkspaceService(string.Format("{0}\r\nGO\r\n{0}", Common.StandardQuery));
            var dataSet = new[] { Common.StandardTestData };
            var queryService = Common.GetPrimedExecutionService(dataSet, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardResultSetValidator()
<<<<<<< HEAD
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardResultSetValidator()
                .AddStandardMessageValidator()
                .AddStandardBatchCompleteValidator()
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(2, p.BatchSummaries.Length);
                }).Complete();
=======
                .AddStandardBatchCompleteValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardResultSetValidator()
                .AddStandardBatchCompleteValidator()
                .AddStandardQueryCompleteValidator(2)
                .Complete();
>>>>>>> dev
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async void QueryExecuteUnconnectedUriTest()
        {
            // Given:
            // If:
            // ... I request to execute a query using a file URI that isn't connected
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, false, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = "notConnected", QuerySelection = Common.WholeDocument };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddErrorValidation<string>(Assert.NotEmpty)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should be no active queries
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryExecuteInProgressTest()
        {
            // If:
            // ... I request to execute a query
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query without waiting for the first to complete
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;   // Simulate query hasn't finished
            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddErrorValidation<string>(Assert.NotEmpty)
                .Complete();
            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... All events should have been called as per their flow validator
            efv.Validate();

            // ... There should only be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async void QueryExecuteCompletedTest()
        {
            // If:
            // ... I request to execute a query
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query after waiting for the first to complete
            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardBatchCompleteValidator()
<<<<<<< HEAD
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(1, p.BatchSummaries.Length);
                }).Complete();
=======
                .AddStandardQueryCompleteValidator(1)
                .Complete();
>>>>>>> dev

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
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = null };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddErrorValidation<string>(Assert.NotEmpty)
                .Complete();
            await queryService.HandleExecuteRequest(queryParams, efv.Object);

            // Then:
            // ... Am error should have been sent
            efv.Validate();

            // ... There should not be an active query
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryExecuteInvalidQueryTest()
        {
            // If:
            // ... I request to execute a query that is invalid
            var workspaceService = GetDefaultWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, true, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            var efv = new EventFlowValidator<QueryExecuteResult>()
                .AddStandardQueryResultValidator()
                .AddStandardBatchStartValidator()
                .AddStandardBatchCompleteValidator()
<<<<<<< HEAD
                .AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
                {
                    // Validate OwnerURI matches
                    Assert.Equal(Common.OwnerUri, p.OwnerUri);
                    Assert.NotNull(p.BatchSummaries);
                    Assert.Equal(1, p.BatchSummaries.Length);
                }).Complete();
=======
                .AddStandardQueryCompleteValidator(1)
                .Complete();
>>>>>>> dev

            await Common.AwaitExecution(queryService, queryParams, efv.Object);

            // Then:
            // ... Am error should have been sent
            efv.Validate();

            // ... There should not be an active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
<<<<<<< HEAD
        }

        private static WorkspaceService<SqlToolsSettings> GetDefaultWorkspaceService(string query)
        {
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();
            var workspaceService = Common.GetPrimedWorkspaceService(query);
            return workspaceService;
        }
    }

    public static class EventFlowValidatorExtensions
    {
        public static EventFlowValidator<TRequestContext> AddStandardQueryResultValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            // We just need to makes sure we get a result back, there's no params to validate
            return efv.AddResultValidation<QueryExecuteResult>(null);
        }

        public static EventFlowValidator<TRequestContext> AddStandardBatchStartValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(QueryExecuteBatchStartEvent.Type, p =>
            {
                // Validate OwnerURI and batch summary is returned
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardBatchCompleteValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(QueryExecuteBatchCompleteEvent.Type, p =>
            {
                // Validate OwnerURI and batch summary are returned
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardResultSetValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
=======
        }

        private static WorkspaceService<SqlToolsSettings> GetDefaultWorkspaceService(string query)
        {
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();
            var workspaceService = Common.GetPrimedWorkspaceService(query);
            return workspaceService;
        }
    }

    public static class EventFlowValidatorExtensions
    {
        public static EventFlowValidator<QueryExecuteResult> AddStandardQueryResultValidator(
            this EventFlowValidator<QueryExecuteResult> efv)
        {
            // We just need to makes sure we get a result back, there's no params to validate
            return efv.AddResultValidation(r =>
            {
                Assert.Null(r.Messages);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardBatchStartValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(QueryExecuteBatchStartEvent.Type, p =>
            {
                // Validate OwnerURI and batch summary is returned
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardBatchCompleteValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(QueryExecuteBatchCompleteEvent.Type, p =>
            {
                // Validate OwnerURI and batch summary are returned
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummary);
            });
        }

        public static EventFlowValidator<TRequestContext> AddStandardResultSetValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
>>>>>>> dev
            return efv.AddEventValidation(QueryExecuteResultSetCompleteEvent.Type, p =>
            {
                // Validate OwnerURI and result summary are returned
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.ResultSetSummary);
            });
        }

<<<<<<< HEAD
        public static EventFlowValidator<TRequestContext> AddStandardMessageValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv)
        {
            return efv.AddEventValidation(QueryExecuteMessageEvent.Type, p =>
            {
                // Validate OwnerURI and message are returned
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.Message);
=======
        public static EventFlowValidator<TRequestContext> AddStandardQueryCompleteValidator<TRequestContext>(
            this EventFlowValidator<TRequestContext> efv, int expectedBatches)
        {
            return efv.AddEventValidation(QueryExecuteCompleteEvent.Type, p =>
            {
                Assert.True(string.IsNullOrWhiteSpace(p.Message));
                Assert.Equal(Common.OwnerUri, p.OwnerUri);
                Assert.NotNull(p.BatchSummaries);
                Assert.Equal(expectedBatches, p.BatchSummaries.Length);
>>>>>>> dev
            });
        }
    }
}
