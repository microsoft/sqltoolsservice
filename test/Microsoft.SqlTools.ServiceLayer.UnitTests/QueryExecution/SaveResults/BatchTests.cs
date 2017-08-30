// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.SaveResults
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
