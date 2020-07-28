//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
            var i = header.singleHashValues;
            Assert.NotNull(null);
        }
    }
}

