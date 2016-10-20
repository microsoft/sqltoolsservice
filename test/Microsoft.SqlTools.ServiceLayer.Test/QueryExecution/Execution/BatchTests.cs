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
            Batch batch = new Batch(Common.StandardQuery, 0, 0, 2, 2, Common.GetFileStreamFactory());

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
        }

        [Fact]
        public void BatchExecuteNoResultSets()
        {
            // If I execute a query that should get no result sets
            Batch batch = new Batch(Common.StandardQuery, 0, 0, 2, 2, Common.GetFileStreamFactory());
            batch.Execute(GetConnection(Common.CreateTestConnectionInfo(null, false)), null, CancellationToken.None).Wait();

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
            Assert.Contains("1 ", batch.ResultMessages.First().Message);
            // NOTE: 1 is expected because this test simulates a 'update' statement where 1 row was affected.
            // The 1 in quotes is to make sure the 1 isn't part of a larger number
        }

        [Fact]
        public void BatchExecuteOneResultSet()
        {
            int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // If I execute a query that should get one result set
            Batch batch = new Batch(Common.StandardQuery, 0, 0, 2, 2, Common.GetFileStreamFactory());
            batch.Execute(GetConnection(ci), null, CancellationToken.None).Wait();

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
            Assert.Contains(Common.StandardRows.ToString(), batch.ResultMessages.First().Message);
        }

        [Fact]
        public void BatchExecuteTwoResultSets()
        {
            var dataset = new[] { Common.StandardTestData, Common.StandardTestData };
            int resultSets = dataset.Length;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(dataset, false);

            // If I execute a query that should get two result sets
            Batch batch = new Batch(Common.StandardQuery, 0, 0, 1, 1, Common.GetFileStreamFactory());
            batch.Execute(GetConnection(ci), null, CancellationToken.None).Wait();

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

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, batch.ResultMessages.Count());
            foreach (var rsm in batch.ResultMessages)
            {
                Assert.Contains(Common.StandardRows.ToString(), rsm.Message);
            }
        }

        [Fact]
        public void BatchExecuteInvalidQuery()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);

            // If I execute a batch that is invalid
            Batch batch = new Batch(Common.StandardQuery, 0, 0, 2, 2, Common.GetFileStreamFactory());
            batch.Execute(GetConnection(ci), null, CancellationToken.None).Wait();

            // Then:
            // ... It should have executed with error
            Assert.True(batch.HasExecuted);
            Assert.True(batch.HasError);

            // ... There should be no result sets
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);

            // ... There should be plenty of messages for the error
            Assert.NotEmpty(batch.ResultMessages);
        }

        [Fact]
        public async Task BatchExecuteExecuted()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // If I execute a batch
            Batch batch = new Batch(Common.StandardQuery, 0, 0, 2, 2, Common.GetFileStreamFactory());
            batch.Execute(GetConnection(ci), null, CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                batch.Execute(GetConnection(ci), null, CancellationToken.None));

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
            Assert.Throws<ArgumentException>(() => new Batch(query, 0, 0, 2, 2, Common.GetFileStreamFactory()));
        }

        [Fact]
        public void BatchNoBufferFactory()
        {
            // If:
            // ... I create a batch that has no file stream factory
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Batch("stuff", 0, 0, 2, 2, null));
        }

        private static DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
        }

    }
}
