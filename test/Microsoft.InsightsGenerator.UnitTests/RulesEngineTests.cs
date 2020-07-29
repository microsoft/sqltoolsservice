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
    }
}
