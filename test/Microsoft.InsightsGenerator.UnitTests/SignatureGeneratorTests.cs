using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Microsoft.InsightsGenerator.UnitTests
{
    public class SignatureGeneratorTests
    {

        [Fact]
        public void TopTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallTopInsights(3, 0, 1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void TopSliceTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedTopInsights(3, 0, 2, 1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void BottomTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallBottomInsights(3, 0, 1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void BottomSliceTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedBottomInsights(3, 0, 2, 1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void AverageTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallAverageInsights(1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void SumTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.OverallSumInsights(1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void SlicedSumTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedSumInsights(2, 1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void SlicedAverageTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedAverageInsights(2, 1);

            foreach (List<string> list in sigGen.Result.Insights)
            {
                foreach (string str in list)
                {
                    Console.WriteLine(str);
                }
            }
        }

        [Fact]
        public void SlicedPercentageTest()
        {
            SignatureGenerator sigGen = new SignatureGenerator(sampleDataArray(false));
            sigGen.SlicedPercentageInsights(2, 1);

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

            string[] sampleRows = sampleTableString.Split("\n");
            List<string[]> sampleRowList = new List<string[]>();
            foreach (var row in sampleRows)
            {
                sampleRowList.Add(row.Split("	"));
            }

            sample.Cells = sampleRowList.ToArray();
            return sample;
        }
    }
}
