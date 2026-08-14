//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.LanguageService.Formatter;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Exposes the subset of workspace settings that the language service needs in order to
    /// decide whether to provide diagnostics, suggestions and quick info, and to react to
    /// configuration changes.
    /// </summary>
    public interface ILanguageServiceSettings
    {
        /// <summary>Gets a flag determining if diagnostics (error checking) are enabled.</summary>
        bool IsDiagnosticsEnabled { get; }

        /// <summary>Gets a flag determining if completion suggestions are enabled.</summary>
        bool IsSuggestionsEnabled { get; }

        /// <summary>Gets a flag determining if quick info (hover) is enabled.</summary>
        bool IsQuickInfoEnabled { get; }

        /// <summary>Gets a flag determining if IntelliSense is enabled.</summary>
        bool IsIntelliSenseEnabled { get; }

        /// <summary>Gets a flag determining if error checking is enabled.</summary>
        bool IsErrorCheckingEnabled { get; }

        /// <summary>Gets a flag determining if Always Encrypted parameterization is enabled.</summary>
        bool IsAlwaysEncryptedParameterizationEnabled { get; }

        /// <summary>
        /// Gets a flag determining whether the large-script optimization is enabled. When enabled, completion does
        /// not parse very large scripts inline (it returns the default list and relies on the debounced diagnostics
        /// parse to refresh the tree), and diagnostics use a longer debounce for those scripts.
        /// </summary>
        bool IsLargeScriptOptimizationEnabled { get; }

        /// <summary>Gets the keyword casing used when generating completion suggestions.</summary>
        CasingOptions FormatKeywordCasing { get; }

        /// <summary>Gets the maximum time, in milliseconds, to wait for a completion operation.</summary>
        int CompletionTimeoutInMilliseconds { get; }

        /// <summary>
        /// Merges the values from <paramref name="newSettings"/> into this instance.
        /// </summary>
        /// <param name="newSettings">The settings to merge in.</param>
        void UpdateFrom(ILanguageServiceSettings newSettings);
    }
}
