// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.SaveResults
{
    public class BatchTests
    {
        [Test]
        public void SaveAsFailsOutOfRangeResultSet([Values(-1,100)] int resultSetIndex)
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
