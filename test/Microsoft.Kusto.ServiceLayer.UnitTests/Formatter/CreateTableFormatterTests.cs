//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Formatter
{
    
    public class CreateTableFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void CreateTable()
        {
            LoadAndFormatAndCompare("CreateTable", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable.sql"), new FormatOptions(), true);
        }

        /**
         * The test contains a timestamp column, which is the shortest (1 token) possible length for a column item.
         */
        [Fact]
        public void CreateTable_Timestamp()
        {
            FormatOptions options = new FormatOptions();
            options.AlignColumnDefinitionsInColumns = true;
            LoadAndFormatAndCompare("CreateTable_Timestamp", GetInputFile("CreateTable_Timestamp.sql"), GetBaselineFile("CreateTable_Timestamp.sql"), options, true);
        }

        [Fact]
        public void CreateTable_CommasBeforeDefinition()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceCommasBeforeNextStatement = true;

            // TODO:  fix verify to account for commma placement - this can 
            LoadAndFormatAndCompare("CreateTable_CommasBeforeDefinition", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_CommasBeforeDefinition.sql"), options, false);
        }

        [Fact]
        public void CreateTable_UseTabs()
        {
            FormatOptions options = new FormatOptions();
            options.UseTabs = true;
            LoadAndFormatAndCompare("CreateTable_UseTabs", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_UseTabs.sql"), options, true);
        }

        [Fact]
        public void CreateTable_20Spaces()
        {
            FormatOptions options = new FormatOptions();
            options.SpacesPerIndent = 20;
            LoadAndFormatAndCompare("CreateTable_20Spaces", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_20Spaces.sql"), options, true);
        }

        [Fact]
        public void CreateTable_UpperCaseKeywords()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Uppercase;
            LoadAndFormatAndCompare("CreateTable_UpperCaseKeywords", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_UpperCaseKeywords.sql"), options, true);
        }

        [Fact]
        public void CreateTable_LowerCaseKeywords()
        {
            FormatOptions options = new FormatOptions();
            options.KeywordCasing = CasingOptions.Lowercase;
            LoadAndFormatAndCompare("CreateTable_LowerCaseKeywords", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_LowerCaseKeywords.sql"), options, true);
        }

        [Fact]
        public void CreateTable_UpperCaseDataTypes()
        {
            FormatOptions options = new FormatOptions();
            options.DatatypeCasing = CasingOptions.Uppercase;
            LoadAndFormatAndCompare("CreateTable_UpperCaseDataTypes", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_UpperCaseDataTypes.sql"), options, true);
        }

        [Fact]
        public void CreateTable_LowerCaseDataTypes()
        {
            FormatOptions options = new FormatOptions();
            options.DatatypeCasing = CasingOptions.Lowercase;
            LoadAndFormatAndCompare("CreateTable_LowerCaseDataTypes", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_LowerCaseDataTypes.sql"), options, true);
        }

        [Fact]
        public void CreateTable_AlignInColumns()
        {
            FormatOptions options = new FormatOptions() { AlignColumnDefinitionsInColumns = true };
            LoadAndFormatAndCompare("CreateTable_AlignInColumns", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_AlignInColumns.sql"), options, true);
        }

        [Fact]
        public void CreateTable_AlignInColumnsUseTabs()
        {
            FormatOptions options = new FormatOptions();
            options.UseTabs = true;
            options.AlignColumnDefinitionsInColumns = true;
            LoadAndFormatAndCompare("CreateTable_AlignInColumnsUseTabs", GetInputFile("CreateTable.sql"), GetBaselineFile("CreateTable_AlignInColumnsUseTabs.sql"), options, true);
        }

        [Fact]
        public void CreateTable_On()
        {
            LoadAndFormatAndCompare("CreateTableOn", GetInputFile("CreateTableFull.sql"), GetBaselineFile("CreateTableOn.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateTable_Formatted()
        {
            LoadAndFormatAndCompare("CreateTable_Formatted", GetInputFile("CreateTable_Formatted.sql"), GetBaselineFile("CreateTable_Formatted.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateTable_CommentsBeforeComma()
        {
            FormatOptions options = new FormatOptions();
            options.UseTabs = false;
            options.AlignColumnDefinitionsInColumns = true;
            options.PlaceCommasBeforeNextStatement = true;
            LoadAndFormatAndCompare("CreateTable_CommentsBeforeComma", GetInputFile("CreateTable_CommentBeforeComma.sql"), GetBaselineFile("CreateTable_CommentBeforeComma.sql"), options, true);
        }

        [Fact]
        public void CreateTableAddress_AlignInColumns()
        {
            FormatOptions options = new FormatOptions();
            options.AlignColumnDefinitionsInColumns = true;
            LoadAndFormatAndCompare("CreateTableAddress_AlignInColumns", GetInputFile("Address.sql"), GetBaselineFile("CreateTableAddress_AlignInColumns.sql"), options, true);
        }

        [Fact]
        public void CreateTableAddress_AlignInColumnsUseTabs()
        {
            FormatOptions options = new FormatOptions();
            options.UseTabs = true;
            options.AlignColumnDefinitionsInColumns = true;
            LoadAndFormatAndCompare("CreateTableAddress_AlignInColumnsUseTabs", GetInputFile("Address.sql"), GetBaselineFile("CreateTableAddress_AlignInColumnsUseTabs.sql"), options, true);
        }


    }
}
