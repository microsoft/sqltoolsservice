//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Formatter
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
            const string settingsJson = @"
{
    ""params"": {
        ""mssql"": {
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
            var sqlToolsSettings = new SqlToolsSettings();
            sqlToolsSettings.SqlTools.Format.AlignColumnDefinitionsInColumns = true;
            sqlToolsSettings.SqlTools.Format.DatatypeCasing = CasingOptions.Lowercase;
            sqlToolsSettings.SqlTools.Format.KeywordCasing = CasingOptions.Uppercase;
            sqlToolsSettings.SqlTools.Format.PlaceCommasBeforeNextStatement = true;
            sqlToolsSettings.SqlTools.Format.PlaceSelectStatementReferencesOnNewLine = true;
            sqlToolsSettings.SqlTools.Format.UseBracketForIdentifiers = true;

            FormatOptions options = new FormatOptions();
            TSqlFormatterService.UpdateFormatOptionsFromSettings(options, sqlToolsSettings.SqlTools.Format);

            Assert.True(options.AlignColumnDefinitionsInColumns);
            Assert.Equal(CasingOptions.Lowercase, options.DatatypeCasing);
            Assert.Equal(CasingOptions.Uppercase, options.KeywordCasing);
            Assert.True(options.PlaceCommasBeforeNextStatement);
            Assert.True(options.PlaceEachReferenceOnNewLineInQueryStatements);
            Assert.True(options.EncloseIdentifiersInSquareBrackets);
        }

    }
}
