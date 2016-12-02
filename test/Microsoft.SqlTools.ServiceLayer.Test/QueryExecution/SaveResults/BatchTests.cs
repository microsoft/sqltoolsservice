using System;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.SaveResults
{
    public class BatchTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void SaveAsFailsOutOfRangeResultSet(int resultSetIndex)
        {
            // If: I attempt to save results for an invalid result set index
            // Then: I should get an ArgumentOutOfRange exception
            Batch batch = Common.GetBasicExecutedBatch();
            SaveResultsRequestParams saveParams = new SaveResultsRequestParams {ResultSetIndex = resultSetIndex};
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                batch.SaveAs(saveParams, null, null, null));
        }
    }
}
