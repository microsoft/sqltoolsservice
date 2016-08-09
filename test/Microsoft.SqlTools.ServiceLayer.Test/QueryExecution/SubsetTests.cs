using System;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SubsetTests
    {
        [Theory]
        [InlineData(2)]
        [InlineData(20)]
        public void SubsetValidTest(int rowCount)
        {
            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery();

            // ... And I ask for a subset with valid arguments
            ResultSetSubset subset = q.GetSubset(0, 0, rowCount);

            // Then:
            // I should get the requested number of rows
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.RowCount);
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.Rows.Length);
        }

        [Fact]
        public void SubsetUnexecutedQueryTest()
        {
            // If I have a query that has *not* been executed
            Query q = new Query("NO OP", Common.CreateTestConnectionInfo(null, false));

            // ... And I ask for a subset with valid arguments
            // Then:
            // ... It should throw an exception
            Assert.Throws<InvalidOperationException>(() => q.GetSubset(0, 0, 2));
        }

        [Theory]
        [InlineData(-1, 0, 2)]  // Invalid result set, too low
        [InlineData(2, 0, 2)]   // Invalid result set, too high
        [InlineData(0, -1, 2)]  // Invalid start index, too low
        [InlineData(0, 10, 2)]  // Invalid start index, too high
        [InlineData(0, 0, -1)]  // Invalid row count, too low
        [InlineData(0, 0, 0)]   // Invalid row count, zero
        public void SubsetInvalidParamsTest(int resultSetIndex, int rowStartInex, int rowCount)
        {
            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => q.GetSubset(resultSetIndex, rowStartInex, rowCount));
        }
    }
}
