using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.InsightsGenerator.UnitTests
{
    public class WorkFlowTests
    {
        [Fact]
        public void ProcessInputDataNotNullResultTest()
        {
            Workflow workflow = new Workflow();
            string insights = workflow.ProcessInputData(getSampleDataArray()).GetAwaiter().GetResult();
            Assert.NotNull(insights);
            Assert.NotEmpty(insights);
        }

        [Fact]
        public void ProcessInputExactMatchResultTest()
        {
            Workflow workflow = new Workflow();
            string insights = workflow.ProcessInputData(getSampleDataArray()).GetAwaiter().GetResult();
            Assert.NotNull(insights);
            Assert.NotEmpty(insights);
            var expectedTopSliceInsight = @"topPerSlice
5
Category1
3
China (455) 34.89%
Turkey (254) 19.48%
United States (188) 14.42%
Category2
3
Japan (171) 91.94%
China (10) 5.38%
United States (3) 1.61%
Category3
3
United States (106) 15.5%
Brazil (91) 13.3%
Korea (61) 8.92%
Category4
3
United States (38) 38%
China (12) 12%
Korea (8) 8%
Category5
3
Korea (21) 65.62%
United States (6) 18.75%
Canada (3) 9.38%";
            Assert.True(expectedTopSliceInsight.Equals(insights));
        }

        private DataArray getSampleDataArray()
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