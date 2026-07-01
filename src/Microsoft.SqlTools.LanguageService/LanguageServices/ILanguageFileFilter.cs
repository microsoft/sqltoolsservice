//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Provides file filtering logic used by language service features (e.g. formatting)
    /// to determine whether a file should be treated as a non-MSSQL file and skipped.
    /// </summary>
    /// <remarks>
    /// TODO: This interface is a temporary seam introduced so that the moved
    /// <c>TSqlFormatterService</c> can call <c>LanguageService.ShouldSkipNonMssqlFile</c>
    /// without creating a circular dependency on the ServiceLayer project. Once the
    /// <c>LanguageService</c> class itself is moved into this library, the formatter can call
    /// it directly and this interface (plus its registration in HostLoader and the
    /// FileFilter getter in TSqlFormatterService) should be removed.
    /// </remarks>
    public interface ILanguageFileFilter
    {
        /// <summary>
        /// Checks if a given URI should be skipped because it is not an MSSQL file.
        /// </summary>
        /// <param name="uri">The document URI</param>
        /// <returns>True if the file should be skipped; false otherwise</returns>
        bool ShouldSkipNonMssqlFile(string uri);
    }
}
