//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.SqlTools.ServiceLayer.DacFx;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.References
{
    /// <summary>
    /// The NuGet packages a SQL project restored, read from obj/project.assets.json.
    /// </summary>
    internal sealed class ProjectAssets
    {
        private readonly IReadOnlyList<string> packageFolders;

        // Package id (case-insensitive) to the restored version.
        private readonly Dictionary<string, string> packageVersions;

        private ProjectAssets(IReadOnlyList<string> packageFolders, Dictionary<string, string> packageVersions)
        {
            this.packageFolders = packageFolders;
            this.packageVersions = packageVersions;
        }

        /// <summary>
        /// Returns the path of the assets file for the project in <paramref name="projectDirectory"/>.
        /// </summary>
        internal static string GetAssetsFilePath(string projectDirectory) =>
            Path.Combine(projectDirectory, "obj", "project.assets.json");

        /// <summary>
        /// Reads the project's assets file. Returns null when the project has not been restored.
        /// </summary>
        internal static ProjectAssets? TryLoad(string projectDirectory)
        {
            string assetsFilePath = GetAssetsFilePath(projectDirectory);
            if (!File.Exists(assetsFilePath))
            {
                return null;
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsFilePath));
            JsonElement root = document.RootElement;

            var packageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("libraries", out JsonElement libraries) && libraries.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty library in libraries.EnumerateObject())
                {
                    // Entries are named "PackageId/Version".
                    string[] nameParts = library.Name.Split('/');
                    if (nameParts.Length == 2 &&
                        library.Value.TryGetProperty("type", out JsonElement libraryType) &&
                        string.Equals(libraryType.GetString(), "package", StringComparison.OrdinalIgnoreCase))
                    {
                        packageVersions[nameParts[0]] = nameParts[1];
                    }
                }
            }

            return new ProjectAssets(CustomRuleLoader.ReadPackageFolders(root), packageVersions);
        }

        /// <summary>Ids of the restored packages.</summary>
        internal IEnumerable<string> PackageIds => packageVersions.Keys;

        /// <summary>
        /// Returns the folder a restored package was extracted to, or null if the package was not restored.
        /// Packages are laid out as &lt;packageFolder&gt;/&lt;lowercase id&gt;/&lt;lowercase version&gt;.
        /// </summary>
        internal string? GetPackageDirectory(string packageId)
        {
            if (!packageVersions.TryGetValue(packageId, out string? version))
            {
                return null;
            }

            return packageFolders
                .Select(folder => Path.Combine(folder, packageId.ToLowerInvariant(), version.ToLowerInvariant()))
                .FirstOrDefault(Directory.Exists);
        }
    }
}
