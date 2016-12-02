using System;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.SaveResults
{
    public class QueryTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void SaveAsFailsOutOfRangeBatch(int batchIndex)
        {
            // If: I save a basic query's results with out of range batch index
            // Then: I should get an out of range exception
            Query query = Common.GetBasicExecutedQuery();
            SaveResultsRequestParams saveParams = new SaveResultsRequestParams {BatchIndex = batchIndex};
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                query.SaveAs(saveParams, null, null, null));
        }
    }
}
