//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.LanguageService.Formatter;
using Microsoft.SqlTools.LanguageService.Formatter.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class FormatterSettingsTests
    {
        [Test]
        public void ValidateFormatterServiceDefaults()
        {
            var sqlToolsSettings = new SqlToolsSettings();
            Assert.Null(sqlToolsSettings.SqlTools.Format.AlignColumnDefinitionsInColumns);
            Assert.AreEqual(CasingOptions.None, sqlToolsSettings.SqlTools.Format.DatatypeCasing);
            Assert.AreEqual(CasingOptions.None, sqlToolsSettings.SqlTools.Format.KeywordCasing);
            Assert.Null(sqlToolsSettings.SqlTools.Format.EnablePreviewFormatter);
            Assert.Null(sqlToolsSettings.SqlTools.Format.Options);
            Assert.Null(sqlToolsSettings.SqlTools.Format.PlaceCommasBeforeNextStatement);
            Assert.Null(sqlToolsSettings.SqlTools.Format.PlaceSelectStatementReferencesOnNewLine);
            Assert.Null(sqlToolsSettings.SqlTools.Format.UseBracketForIdentifiers);
        }
        
        [Test]
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
                enablePreviewFormatter: true,
                keywordCasing: ""uppercase"",
                datatypeCasing: ""lowercase"",
                alignColumnDefinitionsInColumns: true,
                options: {
                    sqlVersion: ""sql160"",
                    sqlEngineType: ""standalone"",
                    alignClauseBodies: false,
                    keywordCasing: ""lowercase"",
                    numNewlinesAfterStatement: 3
                }
            }
        }
    }
}";

            JObject message = JObject.Parse(settingsJson);
            JToken messageParams = null;
            message.TryGetValue("params", out messageParams);
            SqlToolsSettings sqlToolsSettings = messageParams.ToObject<SqlToolsSettings>();

            Assert.True(sqlToolsSettings.SqlTools.Format.AlignColumnDefinitionsInColumns);
            Assert.AreEqual(CasingOptions.Lowercase, sqlToolsSettings.SqlTools.Format.DatatypeCasing);
            Assert.True(sqlToolsSettings.SqlTools.Format.EnablePreviewFormatter);
            Assert.AreEqual(CasingOptions.Uppercase, sqlToolsSettings.SqlTools.Format.KeywordCasing);
            Assert.True(sqlToolsSettings.SqlTools.Format.PlaceCommasBeforeNextStatement);
            Assert.True(sqlToolsSettings.SqlTools.Format.PlaceSelectStatementReferencesOnNewLine);
            Assert.True(sqlToolsSettings.SqlTools.Format.UseBracketForIdentifiers);
            Assert.NotNull(sqlToolsSettings.SqlTools.Format.Options);
            Assert.AreEqual(SqlFormatterVersion.Sql160, sqlToolsSettings.SqlTools.Format.Options.SqlVersion);
            Assert.AreEqual(SqlFormatterEngineType.Standalone, sqlToolsSettings.SqlTools.Format.Options.SqlEngineType);
            Assert.False(sqlToolsSettings.SqlTools.Format.Options.AlignClauseBodies);
            Assert.AreEqual(SqlFormatterKeywordCasing.Lowercase, sqlToolsSettings.SqlTools.Format.Options.KeywordCasing);
            Assert.AreEqual(3, sqlToolsSettings.SqlTools.Format.Options.NumNewlinesAfterStatement);
            Assert.True(sqlToolsSettings.SqlTools.Format.Options.PreserveComments);
        }

        [Test]
        public void SqlFormatterOptionsShouldUseScriptDomDefaults()
        {
            SqlFormatterOptions options = new SqlFormatterOptions();

            Assert.AreEqual(SqlFormatterVersion.Sql170, options.SqlVersion);
            Assert.AreEqual(SqlFormatterEngineType.All, options.SqlEngineType);
            Assert.AreEqual(SqlFormatterKeywordCasing.Uppercase, options.KeywordCasing);
            Assert.True(options.AlignClauseBodies);
            Assert.True(options.PreserveComments);
            Assert.AreEqual(1, options.NumNewlinesAfterStatement);
        }

        [Test]
        public void IntelliSenseKeywordCasingShouldFollowActiveFormatterProfile()
        {
            SqlToolsSettings settings = new SqlToolsSettings();
            settings.SqlTools.Format.KeywordCasing = CasingOptions.Lowercase;
            settings.SqlTools.Format.Options = new SqlFormatterOptions
            {
                KeywordCasing = SqlFormatterKeywordCasing.Uppercase
            };

            Assert.AreEqual(CasingOptions.Lowercase, settings.FormatKeywordCasing);

            settings.SqlTools.Format.EnablePreviewFormatter = true;

            Assert.AreEqual(CasingOptions.Uppercase, settings.FormatKeywordCasing);

            settings.SqlTools.Format.Options = null;

            Assert.AreEqual(CasingOptions.Uppercase, settings.FormatKeywordCasing);

            settings.SqlTools.Format.Options = new SqlFormatterOptions();
            settings.SqlTools.Format.Options.KeywordCasing = SqlFormatterKeywordCasing.PascalCase;

            Assert.AreEqual(CasingOptions.Uppercase, settings.FormatKeywordCasing);
        }

        [Test]
        public void FormatOptionsMatchDefaultSettings()
        {
            var options = new FormatOptions();
            AssertOptionsHaveDefaultValues(options);
        }

        private static void AssertOptionsHaveDefaultValues(FormatOptions options)
        {
            Assert.False(options.AlignColumnDefinitionsInColumns);
            Assert.AreEqual(CasingOptions.None, options.DatatypeCasing);
            Assert.AreEqual(CasingOptions.None, options.KeywordCasing);
            Assert.False(options.PlaceCommasBeforeNextStatement);
            Assert.False(options.PlaceEachReferenceOnNewLineInQueryStatements);
            Assert.False(options.EncloseIdentifiersInSquareBrackets);
        }

        [Test]
        public void CanCopyDefaultFormatSettingsToOptions()
        {
            var sqlToolsSettings = new SqlToolsSettings();            
            FormatOptions options = new FormatOptions();
            TSqlFormatterService.UpdateFormatOptionsFromSettings(options, sqlToolsSettings.SqlTools.Format);
            AssertOptionsHaveDefaultValues(options);
        }

        [Test]
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
            Assert.AreEqual(CasingOptions.Lowercase, options.DatatypeCasing);
            Assert.AreEqual(CasingOptions.Uppercase, options.KeywordCasing);
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

        [Test]
        public void CanMergeRequestOptionsAndSettings()
        {
            var sqlToolsSettings = CreateNonDefaultFormatSettings();

            FormatOptions options = TSqlFormatterService.MergeFormatOptions(
                new FormattingOptions { InsertSpaces = true, TabSize = 2 }, 
                sqlToolsSettings.SqlTools.Format);

            AssertOptionsHaveExpectedNonDefaultValues(options);
            Assert.False(options.UseTabs);
            Assert.True(options.UseSpaces);
            Assert.AreEqual(2, options.SpacesPerIndent);
        }
    }
}
