//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    
    public class SqlSelectStatementFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void SimpleQuery()
        {
            LoadAndFormatAndCompare("SimpleQuery", GetInputFile("SimpleQuery.sql"), 
                GetBaselineFile("SimpleQuery.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void SimpleQuery_CommasBeforeDefinition()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceCommasBeforeNextStatement = true;

            // TODO:  fix verify to account for commma placement - this can 
            LoadAndFormatAndCompare("SimpleQuery_CommasBeforeDefinition", GetInputFile("SimpleQuery.sql"), 
                GetBaselineFile("SimpleQuery_CommasBeforeDefinition.sql"), options, false);
        }

        [Fact]
        public void SimpleQuery_EachReferenceOnNewLine()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;

            LoadAndFormatAndCompare("SimpleQuery_EachReferenceOnNewLine", GetInputFile("SimpleQuery.sql"), 
                GetBaselineFile("SimpleQuery_EachReferenceOnNewLine.sql"), options, true);
        }

        [Fact]
        public void SimpleQuery_EachReferenceOnNewLine_CommasBeforeDefinition()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceCommasBeforeNextStatement = true;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;

            // TODO:  fix verify to account for commma placement - this can 
            LoadAndFormatAndCompare("SimpleQuery_EachReferenceOnNewLine_CommasBeforeDefinition", 
                GetInputFile("SimpleQuery.sql"), GetBaselineFile("SimpleQuery_EachReferenceOnNewLine_CommasBeforeDefinition.sql"), options, false);
        }

        [Fact]
        public void SimpleQuery_UseTabs()
        {
            FormatOptions options = new FormatOptions();
            options.UseTabs = true;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("SimpleQuery_UseTabs", GetInputFile("SimpleQuery.sql"), 
                GetBaselineFile("SimpleQuery_UseTabs.sql"), options, true);
        }

        [Fact]
        public void SimpleQuery_20Spaces()
        {
            FormatOptions options = new FormatOptions();
            options.SpacesPerIndent = 20;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("SimpleQuery_20Spaces", GetInputFile("SimpleQuery.sql"),
                GetBaselineFile("SimpleQuery_20Spaces.sql"), options, true);
        }

        [Fact]
        public void SimpleQuery_UpperCaseKeywords()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Uppercase;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("SimpleQuery_UpperCaseKeywords", GetInputFile("SimpleQuery.sql"),
                GetBaselineFile("SimpleQuery_UpperCaseKeywords.sql"), options, true);
        }

        [Fact]
        public void SimpleQuery_LowerCaseKeywords()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Lowercase;
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("SimpleQuery_LowerCaseKeywords", GetInputFile("SimpleQuery.sql"), 
                GetBaselineFile("SimpleQuery_LowerCaseKeywords.sql"), options, true);
        }

        [Fact]
        public void SimpleQuery_ForBrowseClause()
        {
            LoadAndFormatAndCompare("SimpleQuery_ForBrowseClause", GetInputFile("SimpleQuery_ForBrowseClause.sql"), 
                GetBaselineFile("SimpleQuery_ForBrowseClause.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void SimpleQuery_ForXmlClause()
        {
            LoadAndFormatAndCompare("SimpleQuery_ForXmlClause", GetInputFile("SimpleQuery_ForXmlClause.sql"), 
                GetBaselineFile("SimpleQuery_ForXmlClause.sql"), new FormatOptions(), true);
        }
    }
}
