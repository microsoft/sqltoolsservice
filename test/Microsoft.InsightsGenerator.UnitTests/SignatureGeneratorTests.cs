//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Xunit;


namespace Microsoft.InsightsGenerator.UnitTests
{
    public class SignatureGeneratorTests
    {

        [Fact]
        public void TopTest()
        {
            var expectedTopInsight = @"top
3
China (455) 19.73%
Turkey (254) 11.01%
United States (188) 8.15%";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallTopInsights(3, 0, 1);

            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedTopInsight);
        }

        [Fact]
        public void TopSliceTest()
        {
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
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedTopInsights(3, 0, 2, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedTopSliceInsight);
        }

        [Fact]
        public void BottomTest()
        {
            var expectedBottomInsight = @"bottom
3
Korea (1) 0.04%
Germany (1) 0.04%
India (1) 0.04%";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallBottomInsights(3, 0, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedBottomInsight);
        }

        [Fact]
        public void BottomSliceTest()
        {
            var expectedBottomSliceInsight = @"bottomPerSlice
5
Category1
3
Canada (17) 1.3%
United Kingdom (17) 1.3%
Vietnam (18) 1.38%
Category2
3
Germany (1) 0.54%
Korea (1) 0.54%
United States (3) 1.61%
Category3
3
France (12) 1.75%
United Kingdom (20) 2.92%
Vietnam (22) 3.22%
Category4
3
India (1) 1%
Japan (1) 1%
Canada (2) 2%
Category5
3
India (1) 3.12%
Japan (1) 3.12%
Canada (3) 9.38%";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedBottomInsights(3, 0, 2, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedBottomSliceInsight);
        }

        [Fact]
        public void AverageTest()
        {
            var expectedAverageInsight = @"average
42.7";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallAverageInsights(1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedAverageInsight);
        }

        [Fact]
        public void SumTest()
        {
            var expectedSumInsight = @"sum
2306";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallSumInsights(1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedSumInsight);
        }

        [Fact]
        public void SlicedSumTest()
        {
            var expectedSlicedSumInsight = @"sumPerSlice
5
Category1
1304
Category2
186
Category3
684
Category4
100
Category5
32";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedSumInsights(2, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedSlicedSumInsight);
        }

        [Fact]
        public void SlicedAverageTest()
        {
            var expectedSlicedAverageInsight = @"sumPerSlice
5
Category1
86.93
Category2
37.2
Category3
45.6
Category4
7.14
Category5
6.4";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedAverageInsights(2, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedSlicedAverageInsight);
        }

        [Fact]
        public void SlicedPercentageTest()
        {
            var expectedSlicedPercentageInsight = @"percentagePerSlice
5
Category1
56.55
Category2
8.07
Category3
29.66
Category4
4.34
Category5
1.39";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedPercentageInsights(2, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedSlicedPercentageInsight);
        }

        [Fact]
        public void MaxAndMinInsightsTest()
        {
            var expectedMaxAndMinInsight = @"max
455
min
1";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallMaxInsights(1);
            sigGen.OverallMinInsights(1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedMaxAndMinInsight);
        }

        [Fact]
        public void MaxAndMinSlicedInsightsTest()
        {
            string expectedMaxAndMinSlicedInsight = @"maxPerSlice
5
Category1
455
Category2
171
Category3
106
Category4
38
Category5
21
minPerSlice
5
Category1
17
Category2
1
Category3
12
Category4
1
Category5
1";
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedMaxInsights(2, 1);
            sigGen.SlicedMinInsights(2, 1);
            CompareInsightWithExpectedOutcome(sigGen.Result.Insights, expectedMaxAndMinSlicedInsight);
        }


        public void CompareInsightWithExpectedOutcome(List<List<string>> insights, string expectedOutcome)
        {
            List<string> stringedInsights = new List<string>();
            foreach (List<string> insight in insights)
            {
                stringedInsights.Add(string.Join(Environment.NewLine, insight));
            }
            Assert.Equal(expectedOutcome, string.Join(Environment.NewLine, stringedInsights));
        }

        [Fact]
        public void LearnTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.Learn();
            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }
        public DataArray sampleDataArray(bool timeinput)
        {
            DataArray sample = new DataArray();

            var inputDataType = DataArray.DataType.String;
            if (timeinput)
            {
                inputDataType = DataArray.DataType.DateTime;
            }

            sample.ColumnNames = new string[] { "input_g_0", "output_0", "slicer_0" };
            sample.ColumnDataType = new DataArray.DataType[] { inputDataType, DataArray.DataType.Number, DataArray.DataType.String };
            string sampleTableString =
                @"China	455	Category1
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
            List<string[]> sampleRowList = new List<string[]>();
            foreach (var row in sampleRows)
            {
                sampleRowList.Add(row.Split("	"));
            }

            var columnTypes = new string[] { "input_g_1", "output_1", "slicer_1" };
            sample.Cells = sampleRowList.ToArray();
            sample.TransformedColumnNames = columnTypes;
            return sample;
        }
    }
}