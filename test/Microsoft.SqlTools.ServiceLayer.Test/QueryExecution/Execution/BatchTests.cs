//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory());

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
        }

        [Fact]
        public void BatchExecuteNoResultSets()
        {
            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            bool completionCallbackFired = false;
            Batch.BatchAsyncEventHandler callback = b =>
            {
                completionCallbackFired = true;
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            // If I execute a query that should get no result sets
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory());
            batch.BatchCompletion += callback;
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

            // ... The callback for batch completion should have been fired
            Assert.True(completionCallbackFired);
            Assert.NotNull(batchSummaryFromCallback);
        }

        [Fact]
        public void BatchExecuteOneResultSet()
        {
            const int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            bool completionCallbackFired = false;
            Batch.BatchAsyncEventHandler callback = b =>
            {
                completionCallbackFired = true;
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            // If I execute a query that should get one result set
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory());
            batch.BatchCompletion += callback;
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... There should be exactly one result set
            Assert.Equal(resultSets, batch.ResultSets.Count());
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
            Assert.True(completionCallbackFired);
            Assert.NotNull(batchSummaryFromCallback);
        }

        [Fact]
        public void BatchExecuteTwoResultSets()
        {
            var dataset = new[] { Common.StandardTestData, Common.StandardTestData };
            int resultSets = dataset.Length;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(dataset, false);

            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            bool completionCallbackFired = false;
            Batch.BatchAsyncEventHandler callback = b =>
            {
                completionCallbackFired = true;
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            // If I execute a query that should get two result sets
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory());
            batch.BatchCompletion += callback;
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
            Assert.True(completionCallbackFired);
            Assert.NotNull(batchSummaryFromCallback);
        }

        [Fact]
        public void BatchExecuteInvalidQuery()
        {
            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            bool completionCallbackFired = false;
            Batch.BatchAsyncEventHandler callback = b =>
            {
                completionCallbackFired = true;
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);

            // If I execute a batch that is invalid
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory());
            batch.BatchCompletion += callback;
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
            Assert.True(completionCallbackFired);
            Assert.NotNull(batchSummaryFromCallback);
        }

        [Fact]
        public async Task BatchExecuteExecuted()
        {
            // Setup: Create a callback for batch completion
            BatchSummary batchSummaryFromCallback = null;
            bool completionCallbackFired = false;
            Batch.BatchAsyncEventHandler callback = b =>
            {
                completionCallbackFired = true;
                batchSummaryFromCallback = b.Summary;
                return Task.FromResult(0);
            };

            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // If I execute a batch
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory());
            batch.BatchCompletion += callback;
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... The callback for batch completion should have been fired
            Assert.True(completionCallbackFired);
            Assert.NotNull(batchSummaryFromCallback);

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception
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
            Assert.Throws<ArgumentException>(() => new Batch(query, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory()));
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
            Assert.Throws<ArgumentOutOfRangeException>(() => new Batch("stuff", Common.SubsectionDocument, -1, Common.GetFileStreamFactory()));
        }

        private static DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
        }

    }
}
