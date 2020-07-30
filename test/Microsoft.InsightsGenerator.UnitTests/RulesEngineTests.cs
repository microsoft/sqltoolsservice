//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using static Microsoft.InsightsGenerator.RulesEngine;

namespace Microsoft.InsightsGenerator.UnitTests
{
    /// <summary>
    /// Rules Engine tests
    /// </summary>
    public class RulesEngineTests
    {
        [Fact]
        public void TemplateParserTest()
        {
            ColumnHeaders headerForTemp8 = TemplateParser(@"#inp had a total of #OutPar_N_C_1 ##OutPar_N_C_1 that constitues #Per% \n \n");
            ColumnHeaders headerForTemp16 = TemplateParser(@"For the #slices ##SlicePar_GG_1(s), the percentage of ##OutPar_N_C_1 on #time were \n #stHData\n this was compared with #Etime where #ESlices ##SlicePar_GG_1\n #EstHData  \n.");

            var expectedSingleHashValuesForTemp16 = new List<string>(new string[] { "#slices", "#time", "#stHData", "#Etime", "#ESlices", "#EstHData" });
            var expectedDoubleHashValuesForTemp16 = new List<string>(new string[] { "##SlicePar_GG_1(s)", "##OutPar_N_C_1", "##SlicePar_GG_1" });

            var expectedSingleHashValuesForTemp8 = new List<string>(new string[] { "#inp", "#OutPar_N_C_1", "#Per%" });
            var expectedDoubleHashValuesForTemp8 = new List<string>(new string[] { "##OutPar_N_C_1" });

            Assert.True(Enumerable.SequenceEqual(expectedSingleHashValuesForTemp8, headerForTemp8.SingleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedDoubleHashValuesForTemp8, headerForTemp8.DoubleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedSingleHashValuesForTemp16, headerForTemp16.SingleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedDoubleHashValuesForTemp16, headerForTemp16.DoubleHashValues));
        }
        [Fact]
        public void RulesEngineEndToEndTest()
        {
            // Create test input objects for the first test
            var singleHashList1 = new List<List<string>>();
            var list1_1 = new List<string>() { "uniqueinputs", "15" };
            var list1_2 = new List<string>() { "top", "3", "China: 55%", "United States: 49%", "Japan: 37%" };
            singleHashList1.Add(list1_1);
            singleHashList1.Add(list1_2);

            DataArray testArray1 = new DataArray();
            testArray1.ColumnNames = new string[] { "Country", "Area" };
            testArray1.TransformedColumnNames = new string[] { "input_g_0", "Output_0" };

            // Create test input objects for the second test
            var singleHashList2 = new List<List<string>>();
            var list2_1 = new List<string>() { "bottom", "5", "Apple: 30%", "Oragne: 28%", "Strawberry: 17%", "Pear: 13%", "Peach: 8%" };
            singleHashList2.Add(list2_1);

            DataArray testArray2 = new DataArray();
            testArray2.ColumnNames = new string[] { "fruits" };
            testArray2.TransformedColumnNames = new string[] { "Output_0" };

            var returnedStr1 = $@"{RulesEngine.FindMatchedTemplate(singleHashList1, testArray1)}";
            var returnedStr2 = $@"{RulesEngine.FindMatchedTemplate(singleHashList2, testArray2)}";

            string expectedOutput1 = "There were 15 Country (s),  the top 3 highest total Area were as follows:\\n China: 55%\r\nUnited States: 49%\r\nJapan: 37%\r\n\n\r\n";
            string expectedOutput2 = "The top 5 lowest total fruits were as follows:\\n Apple: 30%\r\nOragne: 28%\r\nStrawberry: 17%\r\nPear: 13%\r\nPeach: 8%\r\n\n\r\n";

            Assert.True(string.Equals(returnedStr1, expectedOutput1));
            Assert.True(string.Equals(returnedStr2, expectedOutput2));

            //The top #bottom lowest total ##Output_0 were as follows:\n #placeHolder

        }

    }
}
