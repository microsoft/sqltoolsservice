//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

            var expectedSingleHashValuesForTemp16 = new List<string>(new string[] {"slices", "time", "stHData", "Etime", "ESlices", "EstHData"  });
            var expectedDoubleHashValuesForTemp16 = new List<string>(new string[] { "SlicePar_GG_1(s)", "OutPar_N_C_1", "SlicePar_GG_1" });

            var expectedSingleHashValuesForTemp8 = new List<string>(new string[] { "inp", "OutPar_N_C_1", "Per%" });
            var expectedDoubleHashValuesForTemp8 = new List<string>(new string[] { "OutPar_N_C_1" });
      
            Assert.True(Enumerable.SequenceEqual(expectedSingleHashValuesForTemp8, headerForTemp8.SingleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedDoubleHashValuesForTemp8, headerForTemp8.DoubleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedSingleHashValuesForTemp16, headerForTemp16.SingleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedDoubleHashValuesForTemp16, headerForTemp16.DoubleHashValues));
        }
        [Fact]
        public void RulesEngineEndToEndTest()
        {
            var singleHashList = new List<List<string>>();
            var list1 = new List<string>() { "groups","15"};
            var list2 = new List<string>() { "top", "3", "China: 55%", "United States: 49%", "Japan: 37%" };
            singleHashList.Add(list1);
            singleHashList.Add(list2);

            DataArray test = new DataArray();
            test.ColumnNames = new string[]{ "Country", "Area" };
            test.TransformedColumnNames = new string []{ "input_g_0", "Output_0" };

            var str = RulesEngine.FindMatchedTemplate(singleHashList, test);
            //The ##InPar_GG_1 that had the largest of each ##SlicePar_GG_1 are \n

            //There were #groups ##input_g_0 (s),  the top #top highest total ##Output_0 were as follows:\n #
        }



    }
}
