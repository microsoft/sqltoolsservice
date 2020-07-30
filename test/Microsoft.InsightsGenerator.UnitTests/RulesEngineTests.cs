//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
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
            // Create test input objects for test #1
            var singleHashList1 = new List<List<string>>();
            var list1_1 = new List<string>() { "uniqueinputs", "15" };
            var list1_2 = new List<string>() { "top", "3", "China: 55%", "United States: 49%", "Japan: 37%" };
            singleHashList1.Add(list1_1);
            singleHashList1.Add(list1_2);

            DataArray testArray1 = new DataArray();
            testArray1.ColumnNames = new string[] { "Country", "Area" };
            testArray1.TransformedColumnNames = new string[] { "input_g_0", "output_0" };

            // Create test input objects for test #2
            var singleHashList2 = new List<List<string>>();
            var list2_1 = new List<string>() { "bottom", "5", "Apple: 30%", "Oragne: 28%", "Strawberry: 17%", "Pear: 13%", "Peach: 8%" };
            singleHashList2.Add(list2_1);

            DataArray testArray2 = new DataArray();
            testArray2.ColumnNames = new string[] { "fruits" };
            testArray2.TransformedColumnNames = new string[] { "output_0" };

            // Create test input objects for test#3
            var singleHashList3 = new List<List<string>>();
            var list3_1 = new List<string>() { "averageSlice", "4", "Cow: 60%", "Dog: 28%", "Cat: 17%", "Mouse: 8%"};
            singleHashList3.Add(list3_1);

            DataArray testArray3 = new DataArray();
            testArray3.ColumnNames = new string[] { "animals" };
            testArray3.TransformedColumnNames = new string[] { "slicer_0" };

            var returnedStr1 = $@"{RulesEngine.FindMatchedTemplate(singleHashList1, testArray1)}";
            var returnedStr2 = $@"{RulesEngine.FindMatchedTemplate(singleHashList2, testArray2)}";
            var returnedStr3 = $@"{RulesEngine.FindMatchedTemplate(singleHashList3, testArray3)}";


            string expectedOutput1 = "There were 15 Country (s),  the top 3 highest total Area were as follows:\\n China: 55%" + Environment.NewLine + "United States: 49%" + Environment.NewLine + "Japan: 37%" + Environment.NewLine + Environment.NewLine + Environment.NewLine;
            string expectedOutput2 = "The top 5 lowest total fruits were as follows:\\n Apple: 30%" + Environment.NewLine + "Oragne: 28%" + Environment.NewLine + "Strawberry: 17%" + Environment.NewLine + "Pear: 13%" + Environment.NewLine + "Peach: 8%" + Environment.NewLine + Environment.NewLine + Environment.NewLine;
            string expectedOutput3 = "For the 4 animals, the volume of each is: Cow: 60%" + Environment.NewLine + "Dog: 28%" + Environment.NewLine + "Cat: 17%" + Environment.NewLine + "Mouse: 8%" + Environment.NewLine + Environment.NewLine + Environment.NewLine;

            Assert.True(string.Equals(returnedStr1, returnedStr1));
            Assert.True(string.Equals(returnedStr2, expectedOutput2));
            Assert.True(string.Equals(returnedStr3, expectedOutput3));

        }
    }
}
