//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.Formatter;
using Microsoft.Kusto.ServiceLayer.Formatter.Contracts;
using Microsoft.Kusto.ServiceLayer.SqlContext;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Formatter
{
    public class FormatterSettingsTests
    {
        [Fact]
        public void ValidateFormatterServiceDefaults()
        {
            var sqlToolsSettings = new SqlToolsSettings();
            Assert.Null(sqlToolsSettings.SqlTools.Format.AlignColumnDefinitionsInColumns);
            Assert.Equal(CasingOptions.None, sqlToolsSettings.SqlTools.Format.DatatypeCasing);
            Assert.Equal(CasingOptions.None, sqlToolsSettings.SqlTools.Format.KeywordCasing);
            Assert.Null(sqlToolsSettings.SqlTools.Format.PlaceCommasBeforeNextStatement);
            Assert.Null(sqlToolsSettings.SqlTools.Format.PlaceSelectStatementReferencesOnNewLine);
            Assert.Null(sqlToolsSettings.SqlTools.Format.UseBracketForIdentifiers);
        }
        
        [Fact]
        public void ValidateFormatSettingsParsedFromJson()
        {
            ValidateFormatSettings("mssql");
            ValidateFormatSettings("sql");
        }

        private static void ValidateFormatSettings(string settingsPropertyName)
        {
            string settingsJson = @"
{
    ""params"": {
        """ + settingsPropertyName + @""": {
            ""format"": {
                useBracketForIdentifiers: true,
                placeCommasBeforeNextStatement: true,
                placeSelectStatementReferencesOnNewLine: true,
                keywordCasing: ""uppercase"",
                datatypeCasing: ""lowercase"",
                alignColumnDefinitionsInColumns: true
            }
        }
    }
}";

            JObject message = JObject.Parse(settingsJson);
            JToken messageParams = null;
            message.TryGetValue("params", out messageParams);
            SqlToolsSettings sqlToolsSettings = messageParams.ToObject<SqlToolsSettings>();

            Assert.True(sqlToolsSettings.SqlTools.Format.AlignColumnDefinitionsInColumns);
            Assert.Equal(CasingOptions.Lowercase, sqlToolsSettings.SqlTools.Format.DatatypeCasing);
            Assert.Equal(CasingOptions.Uppercase, sqlToolsSettings.SqlTools.Format.KeywordCasing);
            Assert.True(sqlToolsSettings.SqlTools.Format.PlaceCommasBeforeNextStatement);
            Assert.True(sqlToolsSettings.SqlTools.Format.PlaceSelectStatementReferencesOnNewLine);
            Assert.True(sqlToolsSettings.SqlTools.Format.UseBracketForIdentifiers);
        }

        [Fact]
        public void FormatOptionsMatchDefaultSettings()
        {
            var options = new FormatOptions();
            AssertOptionsHaveDefaultValues(options);
        }

        private static void AssertOptionsHaveDefaultValues(FormatOptions options)
        {
            Assert.False(options.AlignColumnDefinitionsInColumns);
            Assert.Equal(CasingOptions.None, options.DatatypeCasing);
            Assert.Equal(CasingOptions.None, options.KeywordCasing);
            Assert.False(options.PlaceCommasBeforeNextStatement);
            Assert.False(options.PlaceEachReferenceOnNewLineInQueryStatements);
            Assert.False(options.EncloseIdentifiersInSquareBrackets);
        }

        [Fact]
        public void CanCopyDefaultFormatSettingsToOptions()
        {
            var sqlToolsSettings = new SqlToolsSettings();            
            FormatOptions options = new FormatOptions();
            TSqlFormatterService.UpdateFormatOptionsFromSettings(options, sqlToolsSettings.SqlTools.Format);
            AssertOptionsHaveDefaultValues(options);
        }

        [Fact]
        public void CanCopyAlteredFormatSettingsToOptions()
        {
            SqlToolsSettings sqlToolsSettings = CreateNonDefaultFormatSettings();

            FormatOptions options = new FormatOptions();
            TSqlFormatterService.UpdateFormatOptionsFromSettings(options, sqlToolsSettings.SqlTools.Format);

            AssertOptionsHaveExpectedNonDefaultValues(options);
        }

        private static SqlToolsSettings CreateNonDefaultFormatSettings()
        {
            var sqlToolsSettings = new SqlToolsSettings();
            sqlToolsSettings.SqlTools.Format.AlignColumnDefinitionsInColumns = true;
            sqlToolsSettings.SqlTools.Format.DatatypeCasing = CasingOptions.Lowercase;
            sqlToolsSettings.SqlTools.Format.KeywordCasing = CasingOptions.Uppercase;
            sqlToolsSettings.SqlTools.Format.PlaceCommasBeforeNextStatement = true;
            sqlToolsSettings.SqlTools.Format.PlaceSelectStatementReferencesOnNewLine = true;
            sqlToolsSettings.SqlTools.Format.UseBracketForIdentifiers = true;
            return sqlToolsSettings;
        }

        private static void AssertOptionsHaveExpectedNonDefaultValues(FormatOptions options)
        {
            Assert.True(options.AlignColumnDefinitionsInColumns);
            Assert.Equal(CasingOptions.Lowercase, options.DatatypeCasing);
            Assert.Equal(CasingOptions.Uppercase, options.KeywordCasing);
            Assert.True(options.PlaceCommasBeforeNextStatement);
            Assert.True(options.PlaceEachReferenceOnNewLineInQueryStatements);
            Assert.True(options.EncloseIdentifiersInSquareBrackets);

            Assert.False(options.UppercaseDataTypes);
            Assert.True(options.UppercaseKeywords);
            Assert.True(options.LowercaseDataTypes);
            Assert.False(options.LowercaseKeywords);
            Assert.False(options.DoNotFormatDataTypes);
            Assert.False(options.DoNotFormatKeywords);
        }

        [Fact]
        public void CanMergeRequestOptionsAndSettings()
        {
            var sqlToolsSettings = CreateNonDefaultFormatSettings();

            FormatOptions options = TSqlFormatterService.MergeFormatOptions(
                new FormattingOptions { InsertSpaces = true, TabSize = 2 }, 
                sqlToolsSettings.SqlTools.Format);

            AssertOptionsHaveExpectedNonDefaultValues(options);
            Assert.False(options.UseTabs);
            Assert.True(options.UseSpaces);
            Assert.Equal(2, options.SpacesPerIndent);
        }
    }
}
