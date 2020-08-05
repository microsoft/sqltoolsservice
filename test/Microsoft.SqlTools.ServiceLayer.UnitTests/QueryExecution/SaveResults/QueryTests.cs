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
    public class QueryTests
    {
        [Test]
        public void SaveAsFailsOutOfRangeBatch([Values(-1,100)] int batchIndex)
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
