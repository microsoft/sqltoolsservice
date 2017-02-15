//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
{
    /// <summary>
    /// Tests for the SqlContext settins
    /// </summary>
    public class SettingsTests
    {    
        /// <summary>
        /// Validate that the Language Service default settings are as expected
        /// </summary>
        [Fact]
        public void ValidateLanguageServiceDefaults()
        {
            var sqlToolsSettings = new SqlToolsSettings();
            Assert.True(sqlToolsSettings.IsDiagnositicsEnabled);
            Assert.True(sqlToolsSettings.IsSuggestionsEnabled);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableSuggestions);
            Assert.True(sqlToolsSettings.SqlTools.IntelliSense.EnableQuickInfo);
            Assert.False(sqlToolsSettings.SqlTools.IntelliSense.LowerCaseSuggestions);            
        }

        /// <summary>
        /// Validate that the IsDiagnositicsEnabled flag behavior
        /// </summary>
        [Fact]
        public void ValidateIsDiagnosticsEnabled()
        {
            var sqlToolsSettings = new SqlToolsSettings();

            // diagnostics is enabled if IntelliSense and Diagnostics flags are set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking = true;
            Assert.True(sqlToolsSettings.IsDiagnositicsEnabled);

            // diagnostics is disabled if either IntelliSense and Diagnostics flags is not set
            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking = true;
            Assert.False(sqlToolsSettings.IsDiagnositicsEnabled);

            sqlToolsSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            sqlToolsSettings.SqlTools.IntelliSense.EnableErrorChecking = false;
            Assert.False(sqlToolsSettings.IsDiagnositicsEnabled);          
        }

        /// <summary>
        /// Validate that the IsSuggestionsEnabled flag behavior
        /// </summary>
        [Fact]
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
        [Fact]
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
