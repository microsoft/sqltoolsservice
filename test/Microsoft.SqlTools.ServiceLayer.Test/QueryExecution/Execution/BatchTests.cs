//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class BatchTests
    {
        [Fact]
        public void BatchCreationTest()
        {
            // If I create a new batch...
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory(null));

            // Then: 
            // ... The text of the batch should be stored
            Assert.NotEmpty(batch.BatchText);

            // ... It should not have executed and no error
            Assert.False(batch.HasExecuted, "The query should not have executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... The results should be empty
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);
            Assert.Empty(batch.ResultMessages);

            // ... The start line of the batch should be 0
            Assert.Equal(0, batch.Selection.StartLine);

            // ... It's ordinal ID should be what I set it to
            Assert.Equal(Common.Ordinal, batch.Id);

            // ... The summary should have the same info
            Assert.False(batch.Summary.HasError);
            Assert.Equal(Common.Ordinal, batch.Summary.Id);
            Assert.Null(batch.Summary.ResultSetSummaries);
            Assert.Null(batch.Summary.Messages);
            Assert.Equal(0, batch.Summary.Selection.StartLine);
            Assert.NotEqual(default(DateTime).ToString("o"), batch.Summary.ExecutionStart); // Should have been set at construction
            Assert.Null(batch.Summary.ExecutionEnd);
            Assert.Null(batch.Summary.ExecutionElapsed);
        }

        /// <summary>
        /// Note: This test also tests the start notification feature
        /// </summary>
        [Fact]
        public void BatchExecuteNoResultSets()
        {
            // Setup: 
            // ... Create a callback for batch start
            BatchSummary batchSummaryFromStart = null;
            Batch.BatchAsyncEventHandler batchStartCallback = b =>
            {
                batchSummaryFromStart = b.Summary;
                return Task.FromResult(0);
            };

            // ... Create a callback for batch completion
            BatchSummary batchSummaryFromCompletion = null;
            Batch.BatchAsyncEventHandler batchCompleteCallback = b =>
            {
                batchSummaryFromCompletion = b.Summary;
                return Task.FromResult(0);
            };

            // ... Create a callback for result completion
            bool resultCallbackFired = false;
            ResultSet.ResultSetAsyncEventHandler resultSetCallback = r =>
            {
                resultCallbackFired = true;
                return Task.FromResult(0);
            };

            // If I execute a query that should get no result sets
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            batch.BatchStart += batchStartCallback;
            batch.BatchCompletion += batchCompleteCallback;
            batch.ResultSetCompletion += resultSetCallback;
            batch.Execute(GetConnection(Common.CreateTestConnectionInfo(null, false)), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The query should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... The results should be empty
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);

            // ... The results should not be null
            Assert.NotNull(batch.ResultSets);
            Assert.NotNull(batch.ResultSummaries);

            // ... There should be a message for how many rows were affected
            Assert.Equal(1, batch.ResultMessages.Count());

            // ... The callback for batch start should have been called
            // ... The info from it should have been basic
            Assert.NotNull(batchSummaryFromStart);
            Assert.False(batchSummaryFromStart.HasError);
            Assert.Equal(Common.Ordinal, batchSummaryFromStart.Id);
            Assert.Equal(Common.SubsectionDocument, batchSummaryFromStart.Selection);
            Assert.True(DateTime.Parse(batchSummaryFromStart.ExecutionStart) > default(DateTime));
            Assert.Null(batchSummaryFromStart.ResultSetSummaries);
            Assert.Null(batchSummaryFromStart.Messages);
            Assert.Null(batchSummaryFromStart.ExecutionElapsed);
            Assert.Null(batchSummaryFromStart.ExecutionEnd);

            // ... The callback for batch completion should have been fired
            // ... The summary should match the expected info
            Assert.NotNull(batchSummaryFromCompletion);
            Assert.False(batchSummaryFromCompletion.HasError);
            Assert.Equal(Common.Ordinal, batchSummaryFromCompletion.Id);
            Assert.Equal(0, batchSummaryFromCompletion.ResultSetSummaries.Length);
            Assert.Equal(1, batchSummaryFromCompletion.Messages.Length);
            Assert.Equal(Common.SubsectionDocument, batchSummaryFromCompletion.Selection);
            Assert.True(DateTime.Parse(batchSummaryFromCompletion.ExecutionStart) > default(DateTime));
            Assert.True(DateTime.Parse(batchSummaryFromCompletion.ExecutionEnd) > default(DateTime));
            Assert.NotNull(batchSummaryFromCompletion.ExecutionElapsed);

            // ... The callback for the result set should NOT have been fired
            Assert.False(resultCallbackFired);
        }

        [Fact]
        public void BatchExecuteOneResultSet()
        {
            const int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            Batch.BatchAsyncEventHandler batchCallback = b =>
            {
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            // ... Create a callback for result set completion
            bool resultCallbackFired = false;
            ResultSet.ResultSetAsyncEventHandler resultSetCallback = r =>
            {
                resultCallbackFired = true;
                return Task.FromResult(0);
            };

            // If I execute a query that should get one result set
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            batch.BatchCompletion += batchCallback;
            batch.ResultSetCompletion += resultSetCallback;
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... There should be exactly one result set
            Assert.Equal(resultSets, batch.ResultSets.Count);
            Assert.Equal(resultSets, batch.ResultSummaries.Length);

            // ... Inside the result set should be with 5 rows
            Assert.Equal(Common.StandardRows, batch.ResultSets.First().RowCount);
            Assert.Equal(Common.StandardRows, batch.ResultSummaries[0].RowCount);

            // ... Inside the result set should have 5 columns
            Assert.Equal(Common.StandardColumns, batch.ResultSets.First().Columns.Length);
            Assert.Equal(Common.StandardColumns, batch.ResultSummaries[0].ColumnInfo.Length);

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, batch.ResultMessages.Count());

            // ... The callback for batch completion should have been fired
            Assert.NotNull(batchSummaryFromCallback);

            // ... The callback for resultset completion should have been fired
            Assert.True(resultCallbackFired);  // We only want to validate that it happened, validation of the 
                                               // summary is done in result set tests
        }

        [Fact]
        public void BatchExecuteTwoResultSets()
        {
            var dataset = new[] { Common.StandardTestData, Common.StandardTestData };
            int resultSets = dataset.Length;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(dataset, false);

            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            Batch.BatchAsyncEventHandler batchCallback = b =>
            {
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            // ... Create a callback for resultset completion
            int resultSummaryCount  = 0;
            ResultSet.ResultSetAsyncEventHandler resultSetCallback = r =>
            {
                resultSummaryCount++;
                return Task.FromResult(0);
            };

            // If I execute a query that should get two result sets
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            batch.BatchCompletion += batchCallback;
            batch.ResultSetCompletion += resultSetCallback;
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... There should be exactly two result sets
            Assert.Equal(resultSets, batch.ResultSets.Count());

            foreach (ResultSet rs in batch.ResultSets)
            {
                // ... Each result set should have 5 rows
                Assert.Equal(Common.StandardRows, rs.RowCount);

                // ... Inside each result set should be 5 columns
                Assert.Equal(Common.StandardColumns, rs.Columns.Length);
            }

            // ... There should be exactly two result set summaries
            Assert.Equal(resultSets, batch.ResultSummaries.Length);

            foreach (ResultSetSummary rs in batch.ResultSummaries)
            {
                // ... Inside each result summary, there should be 5 rows
                Assert.Equal(Common.StandardRows, rs.RowCount);

                // ... Inside each result summary, there should be 5 column definitions
                Assert.Equal(Common.StandardColumns, rs.ColumnInfo.Length);
            }

            // ... The callback for batch completion should have been fired
            Assert.NotNull(batchSummaryFromCallback);

            // ... The callback for result set completion should have been fired
            Assert.Equal(2, resultSummaryCount);
        }

        [Fact]
        public void BatchExecuteInvalidQuery()
        {
            // Setup:
            // ... Create a callback for batch start
            bool batchStartCalled = false;
            Batch.BatchAsyncEventHandler batchStartCallback = b =>
            {
                batchStartCalled = true;
                return Task.FromResult(0);
            };
             
            // ... Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            Batch.BatchAsyncEventHandler batchCompleteCallback = b =>
            {
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            // ... Create a callback that will fail the test if it's called
            ResultSet.ResultSetAsyncEventHandler resultSetCallback = r =>
            {
                throw new Exception("ResultSet callback was called when it should not have been.");
            };

            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);

            // If I execute a batch that is invalid
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            batch.BatchStart += batchStartCallback;
            batch.BatchCompletion += batchCompleteCallback;
            batch.ResultSetCompletion += resultSetCallback;
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed with error
            Assert.True(batch.HasExecuted);
            Assert.True(batch.HasError);

            // ... There should be no result sets
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);

            // ... There should be plenty of messages for the error
            Assert.NotEmpty(batch.ResultMessages);

            // ... The callback for batch completion should have been fired
            Assert.NotNull(batchSummaryFromCallback);

            // ... The callback for batch start should have been fired
            Assert.True(batchStartCalled);
        }

        [Fact]
        public async Task BatchExecuteExecuted()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // If I execute a batch
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // Setup for part 2: 
            // ... Create a callback for batch completion
            Batch.BatchAsyncEventHandler completeCallback = b =>
            {
                throw new Exception("Batch completion callback should not have been called");
            };

            // ... Create a callback for batch start
            Batch.BatchAsyncEventHandler startCallback = b =>
            {
                throw new Exception("Batch start callback should not have been called");
            };

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception
            batch.BatchStart += startCallback;
            batch.BatchCompletion += completeCallback;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                batch.Execute(GetConnection(ci), CancellationToken.None));

            // ... The data should still be available without error
            Assert.False(batch.HasError, "The batch should not be in an error condition");
            Assert.True(batch.HasExecuted, "The batch should still be marked executed.");
            Assert.NotEmpty(batch.ResultSets);
            Assert.NotEmpty(batch.ResultSummaries);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void BatchExecuteNoSql(string query)
        {
            // If:
            // ... I create a batch that has an empty query
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentException>(() => new Batch(query, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory(null)));
        }

        [Fact]
        public void BatchNoBufferFactory()
        {
            // If:
            // ... I create a batch that has no file stream factory
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Batch("stuff", Common.SubsectionDocument, Common.Ordinal, null));
        }

        [Fact]
        public void BatchInvalidOrdinal()
        {
            // If:
            // ... I create a batch has has an ordinal less than 0
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => new Batch("stuff", Common.SubsectionDocument, -1, Common.GetFileStreamFactory(null)));
        }

        private static DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
        }

    }
}
