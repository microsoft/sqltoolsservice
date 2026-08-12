//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.SqlContext;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlContext
{
    /// <summary>
    /// Tests for the SqlContext settins
    /// </summary>
    public class SettingsTests
    {    
        /// <summary>
        /// Validate that the Language Service default settings are as expected
        /// </summary>
        [Test]
        public void ValidateLanguageServiceDefaults()
        {
            var sqlToolsSettings = new SqlToolsSettings();
            Assert.True(sqlToolsSettings.IsDiagnosticsEnabled);
            Assert.True(sqlToolsSettings.IsSuggestionsEnabled);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableSuggestions);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableQuickInfo);
            Assert.AreEqual(2_000, sqlToolsSettings.CompletionTimeoutInMilliseconds);
        }

        /// <summary>
        /// Validate that completion timeout settings are bounded before use by the language service.
        /// </summary>
        [TestCase(0, 500)]
        [TestCase(500, 500)]
        [TestCase(1_500, 1_500)]
        [TestCase(30_000, 30_000)]
        [TestCase(30_001, 30_000)]
        public void ValidateCompletionTimeout(int configuredTimeout, int expectedTimeout)
        {
            var sqlToolsSettings = new SqlToolsSettings();
            sqlToolsSettings.SqlTools.IntelliSense.CompletionTimeoutMilliseconds = configuredTimeout;

            Assert.AreEqual(expectedTimeout, sqlToolsSettings.CompletionTimeoutInMilliseconds);
        }

        /// <summary>
        /// Validate that a configuration update changes the completion timeout.
        /// </summary>
        [Test]
        public void ValidateCompletionTimeoutUpdate()
        {
            var sqlToolsSettings = new SqlToolsSettings();
            var updatedSettings = new SqlToolsSettings();
            updatedSettings.SqlTools.IntelliSense.CompletionTimeoutMilliseconds = 10_000;

            sqlToolsSettings.Update(updatedSettings);

            Assert.AreEqual(10_000, sqlToolsSettings.CompletionTimeoutInMilliseconds);
        }

        /// <summary>
        /// Validate that the IsDiagnosticsEnabled flag behavior
        /// </summary>
        [Test]
        public void ValidateIsDiagnosticsEnabled()
        {
            var sqlToolsSettings = new SqlToolsSettings();

            // diagnostics is enabled if IntelliSense and Diagnostics flags are set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking = true;
            Assert.True(sqlToolsSettings.IsDiagnosticsEnabled);

            // diagnostics is disabled if either IntelliSense and Diagnostics flags is not set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking = true;
            Assert.False(sqlToolsSettings.IsDiagnosticsEnabled);

            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking = false;
            Assert.False(sqlToolsSettings.IsDiagnosticsEnabled);          
        }

        /// <summary>
        /// Validate that the IsSuggestionsEnabled flag behavior
        /// </summary>
        [Test]
        public void ValidateIsSuggestionsEnabled()
        {
            var sqlToolsSettings = new SqlToolsSettings();

            // suggestions is enabled if IntelliSense and Suggestions flags are set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableSuggestions = true;
            Assert.True(sqlToolsSettings.IsSuggestionsEnabled);

            // suggestions is disabled if either IntelliSense and Suggestions flags is not set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            sqlToolsSettings.SqlTools.IntelliSense.EnableSuggestions = true;
            Assert.False(sqlToolsSettings.IsSuggestionsEnabled);

            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableSuggestions = false;
            Assert.False(sqlToolsSettings.IsSuggestionsEnabled);          
        }

        /// <summary>
        /// Validate that the IsQuickInfoEnabled flag behavior
        /// </summary>
        [Test]
        public void ValidateIsQuickInfoEnabled()
        {
            var sqlToolsSettings = new SqlToolsSettings();

            // quick info is enabled if IntelliSense and quick info flags are set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableQuickInfo = true;
            Assert.True(sqlToolsSettings.IsQuickInfoEnabled);

            // quick info is disabled if either IntelliSense and quick info flags is not set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            sqlToolsSettings.SqlTools.IntelliSense.EnableQuickInfo = true;
            Assert.False(sqlToolsSettings.IsQuickInfoEnabled);

            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableQuickInfo = false;
            Assert.False(sqlToolsSettings.IsQuickInfoEnabled);          
        }
    }
}
