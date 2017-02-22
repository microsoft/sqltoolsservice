//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    
    public class CommonTableExpressionFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void CTE()
        {
            LoadAndFormatAndCompare("CTE", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CTE_OneColumn()
        {
            LoadAndFormatAndCompare("CTE_OneColumn", GetInputFile("CTE_OneColumn.sql"), 
                GetBaselineFile("CTE_OneColumn.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CTE_MultipleExpressions()
        {
            LoadAndFormatAndCompare("CTE_MultipleExpressions", GetInputFile("CTE_MultipleExpressions.sql"), 
                GetBaselineFile("CTE_MultipleExpressions.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CTE_CommasBeforeDefinition()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceCommasBeforeNextStatement = true;

            // TODO:  fix verify to account for commma placement - this can 
            LoadAndFormatAndCompare("CTE_CommasBeforeDefinition", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE_CommasBeforeDefinition.sql"), options, false);
        }

        [Fact]
        public void CTE_EachReferenceOnNewLine()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;

            LoadAndFormatAndCompare("CTE_EachReferenceOnNewLine", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE_EachReferenceOnNewLine.sql"), options, true);
        }

        [Fact]
        public void CTE_EachReferenceOnNewLine_CommasBeforeDefinition()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceCommasBeforeNextStatement = true;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;

            // TODO:  fix verify to account for commma placement - this can 
            LoadAndFormatAndCompare("CTE_EachReferenceOnNewLine_CommasBeforeDefinition", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE_EachReferenceOnNewLine_CommasBeforeDefinition.sql"), options, false);
        }

        [Fact]
        public void CTE_UseTabs()
        {
            FormatOptions options = new FormatOptions();
            options.UseTabs = true;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("CTE_UseTabs", GetInputFile("CTE.sql"),
                GetBaselineFile("CTE_UseTabs.sql"), options, true);
        }

        [Fact]
        public void CTE_20Spaces()
        {
            FormatOptions options = new FormatOptions();
            options.SpacesPerIndent = 20;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("CTE_20Spaces", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE_20Spaces.sql"), options, true);
        }

        [Fact]
        public void CTE_UpperCaseKeywords()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Uppercase;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("CTE_UpperCaseKeywords", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE_UpperCaseKeywords.sql"), options, true);
        }

        [Fact]
        public void CTE_LowerCaseKeywords()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Lowercase;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("CTE_LowerCaseKeywords", GetInputFile("CTE.sql"), 
                GetBaselineFile("CTE_LowerCaseKeywords.sql"), options, true);
        }

    }
}
