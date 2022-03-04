//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.InsightsGenerator.UnitTests
{
    public class WorkFlowTests
    {
        [Fact]
        public async void mainWorkFlowTest()
        {
            Workflow instance = new Workflow();
            string insight = await instance.ProcessInputData(getSampleDataArray());
            Assert.NotNull(insight);
        }

        public DataArray getSampleDataArray()
        {
            string sampleTableString =
                @"Country Count Category
China	455	Category1
Turkey	254	Category1
United States	188	Category1
Japan	171	Category2
United States	106	Category3
Brazil	91	Category3
Thailand	67	Category1
Korea	61	Category3
Russia	61	Category1
China	60	Category3
Brazil	57	Category1
Germany	51	Category3
Turkey	49	Category3
Russia	45	Category3
Japan	44	Category3
United States	38	Category4
Thailand	37	Category3
India	36	Category3
Germany	35	Category1
France	33	Category1
India	31	Category1
Japan	28	Category1
Mexico	27	Category3
Canada	23	Category3
Mexico	22	Category1
Vietnam	22	Category3
Korea	21	Category1
Korea	21	Category5
United Kingdom	20	Category3
Vietnam	18	Category1
Canada	17	Category1
United Kingdom	17	Category1
China	12	Category4
France	12	Category3
China	10	Category2
Korea	8	Category4
Brazil	6	Category4
Russia	6	Category4
United States	6	Category5
France	5	Category4
Germany	5	Category4
United Kingdom	5	Category4
Thailand	4	Category4
Turkey	4	Category4
Canada	3	Category5
Mexico	3	Category4
United States	3	Category2
Canada	2	Category4
Germany	1	Category2
India	1	Category4
India	1	Category5
Japan	1	Category4
Japan	1	Category5
Korea	1	Category2";

            string[] sampleRows = sampleTableString.Split(Environment.NewLine);


            var columnNames = sampleRows[0].Split(" ");

            List<string[]> sampleRowList = new List<string[]>();

            for (int i = 1; i < sampleRows.Length; i++)
            {
                sampleRowList.Add(sampleRows[i].Split("	"));
            }

            DataArray result = new DataArray();
            result.ColumnNames = columnNames;
            result.Cells = sampleRowList.ToArray();
            return result;
        }
    }
}