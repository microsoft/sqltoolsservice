//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// SQL projects surface required by the language service for project-aware IntelliSense.
    /// The hosting service layer implements this so the language service can be decoupled from
    /// the concrete SQL projects service.
    /// </summary>
    public interface IProjectIntelliSenseService
    {
        /// <summary>
        /// Updates the cached IntelliSense model for a file that belongs to a project.
        /// </summary>
        Task UpdateProjectIntelliSenseAsync(string projectUri, string filePathOrUri, bool deleted, string sqlTextOverride = null);

        /// <summary>
        /// Determines whether the named object is defined in more than one file in the project.
        /// </summary>
        bool TryIsDuplicate(string projectUri, string name, out bool isDuplicate);

        /// <summary>
        /// Gets the URIs of the other files that belong to the same project, excluding <paramref name="excludeUri"/>.
        /// </summary>
        IReadOnlyList<string> GetSiblingProjectFileUris(string projectUri, string excludeUri);
    }
}
