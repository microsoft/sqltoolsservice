using System;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecuteTests
    {
        [Fact]
        public void QueryCreationTest()
        {
            // If I create a new query...
            Query query = new Query("NO OP", Common.CreateTestConnectionInfo(null, false));

            // Then: 
            // ... It should not have executed
            Assert.False(query.HasExecuted, "The query should not have executed.");

            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);
        }

        [Fact]
        public void QueryExecuteNoResultSets()
        {
            // If I execute a query that should get no result sets
            Query query = new Query("Query with no result sets", Common.CreateTestConnectionInfo(null, false));
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");
            
            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);

            // ... The results should not be null
            Assert.NotNull(query.ResultSets);
            Assert.NotNull(query.ResultSummary);
        }

        [Fact]
        public void QueryExecuteQueryOneResultSet()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] {Common.StandardTestData}, false);

            // If I execute a query that should get one result set
            int resultSets = 1;
            int rows = 5;
            int columns = 4;
            Query query = new Query("Query with one result sets", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");

            // ... There should be exactly one result set
            Assert.Equal(resultSets, query.ResultSets.Count);

            // ... Inside the result set should be with 5 rows
            Assert.Equal(rows, query.ResultSets[0].Rows.Count);

            // ... Inside the result set should have 5 columns and 5 column definitions
            Assert.Equal(columns, query.ResultSets[0].Rows[0].Length);
            Assert.Equal(columns, query.ResultSets[0].Columns.Length);

            // ... There should be exactly one result set summary
            Assert.Equal(resultSets, query.ResultSummary.Length);

            // ... Inside the result summary, there should be 5 column definitions
            Assert.Equal(columns, query.ResultSummary[0].ColumnInfo.Length);

            // ... Inside the result summary, there should be 5 rows
            Assert.Equal(rows, query.ResultSummary[0].RowCount);
        }

        [Fact]
        public void QueryExecuteQueryTwoResultSets()
        {
            var dataset = new[] {Common.StandardTestData, Common.StandardTestData};
            int resultSets = dataset.Length;
            int rows = Common.StandardTestData.Length;
            int columns = Common.StandardTestData[0].Count;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(dataset, false);

            // If I execute a query that should get two result sets
            Query query = new Query("Query with two result sets", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");

            // ... There should be exactly two result sets
            Assert.Equal(resultSets, query.ResultSets.Count);

            foreach (ResultSet rs in query.ResultSets)
            {
                // ... Each result set should have 5 rows
                Assert.Equal(rows, rs.Rows.Count);

                // ... Inside each result set should be 5 columns and 5 column definitions
                Assert.Equal(columns, rs.Rows[0].Length);
                Assert.Equal(columns, rs.Columns.Length);
            }

            // ... There should be exactly two result set summaries
            Assert.Equal(resultSets, query.ResultSummary.Length);

            foreach (ResultSetSummary rs in query.ResultSummary)
            {
                // ... Inside each result summary, there should be 5 column definitions
                Assert.Equal(columns, rs.ColumnInfo.Length);

                // ... Inside each result summary, there should be 5 rows
                Assert.Equal(rows, rs.RowCount);
            }
        }

        [Fact]
        public void QueryExecuteInvalidQuery()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);

            // If I execute a query that is invalid
            Query query = new Query("Invalid query", ci);

            // Then:
            // ... It should throw an exception
            Exception e = Assert.Throws<AggregateException>(() => query.Execute().Wait());
        }

        [Fact]
        public void QueryExecuteExecutedQuery()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] {Common.StandardTestData}, false);

            // If I execute a query
            Query query = new Query("Any query", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception wrapped in an aggregate exception
            AggregateException ae = Assert.Throws<AggregateException>(() => query.Execute().Wait());
            Assert.Equal(1, ae.InnerExceptions.Count);
            Assert.IsType<InvalidOperationException>(ae.InnerExceptions[0]);

            // ... The data should still be available
            Assert.True(query.HasExecuted, "The query should still be marked executed.");
            Assert.NotEmpty(query.ResultSets);
            Assert.NotEmpty(query.ResultSummary);
        }
    }
}
