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
    /// DataTransformation tests
    /// </summary>
    public class RulesEngineTests
    {
        [Fact]
        public void TemplateParserTest()
        {
            ColumnHeaders header = TemplateParser(@"TestTemplates/template_16.txt");
            var expectedSingleHashValues = new List<string>(new string[] {"slices", "time", "stHData", "Etime", "ESlices", "EstHData"  });
            var expectedDoubleHashValues = new List<string>(new string[] { "SlicePar_GG_1(s)", "OutPar_N_C_1", "SlicePar_GG_1" });

            Assert.True(Enumerable.SequenceEqual(expectedSingleHashValues, header.SingleHashValues));
            Assert.True(Enumerable.SequenceEqual(expectedDoubleHashValues, header.DoubleHashValues));
        }
    }
}

