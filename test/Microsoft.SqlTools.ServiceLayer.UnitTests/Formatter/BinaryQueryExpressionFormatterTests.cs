//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class BinaryQueryExpressionFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void BQE_IndentOperands()
        {
            FormatOptions options = new FormatOptions();
            //options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("BQE_IndentOperands", GetInputFile("BQE_IndentOperands.sql"), 
                GetBaselineFile("BQE_IndentOperands.sql"), options, true);
        }

        [Fact]
        public void BQE_KeywordCasing_UpperCase()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Uppercase;
            LoadAndFormatAndCompare("BQE_KeywordCasing_UpperCase", GetInputFile("BQE_KeywordCasing.sql"), 
                GetBaselineFile("BQE_KeywordCasing_UpperCase.sql"), options, true);
        }

        [Fact]
        public void BQE_KeywordCasing_LowerCase()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Lowercase;
            LoadAndFormatAndCompare("BQE_KeywordCasing_LowerCase", GetInputFile("BQE_KeywordCasing.sql"), 
                GetBaselineFile("BQE_KeywordCasing_LowerCase.sql"), options, true);
        }

        [Fact]
        public void BQE_KeywordCasing_NoFormat()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.None;
            LoadAndFormatAndCompare("BQE_KeywordCasing_NoFormat", GetInputFile("BQE_KeywordCasing.sql"), 
                GetBaselineFile("BQE_KeywordCasing_NoFormat.sql"), options, true);
        }

        [Fact]
        public void BQE_OperatorsOnNewLine()
        {
            FormatOptions options = new FormatOptions();
            LoadAndFormatAndCompare("BQE_OperatorsOnNewLine", GetInputFile("BQE_OperatorsOnNewLine.sql"),
                GetBaselineFile("BQE_OperatorsOnNewLine.sql"), options, true);
        }
    }
}
