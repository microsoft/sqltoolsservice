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
using Microsoft.SqlTools.Utility;

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
        /// Reads the project's assets file. Returns null when the project has not been restored, or when the assets file
        /// can't be read, which is logged: references that don't need restored packages still resolve.
        /// </summary>
        internal static ProjectAssets? TryLoad(string projectDirectory)
        {
            string assetsFilePath = GetAssetsFilePath(projectDirectory);
            if (!File.Exists(assetsFilePath))
            {
                return null;
            }

            try
            {
                return Load(assetsFilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                // InvalidOperationException: an element has an unexpected JSON type
                Logger.Warning($"Could not read the restore output {assetsFilePath}, so the project's packages are treated as not restored: {ex.Message}");
                return null;
            }
        }

        private static ProjectAssets Load(string assetsFilePath)
        {
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

            return new ProjectAssets(ReadPackageFolders(root), packageVersions);
        }

        /// <summary>
        /// Reads the NuGet package folders recorded in the assets file. Taking them from the file
        /// covers custom NUGET_PACKAGES locations and shared caches without probing the environment.
        /// Only dacpacs are read from these folders. Code analysis assemblies, which run, are loaded
        /// from the configured package folder instead (see <see cref="CustomRuleLoader"/>).
        /// </summary>
        private static IReadOnlyList<string> ReadPackageFolders(JsonElement root)
        {
            List<string> packageFolders = new();

            if (root.TryGetProperty("packageFolders", out JsonElement packageFoldersElement) &&
                packageFoldersElement.ValueKind == JsonValueKind.Object)
            {
                packageFolders.AddRange(packageFoldersElement
                    .EnumerateObject()
                    .Select(packageFolder => packageFolder.Name.TrimEnd('\\', '/'))
                    .Where(packageFolder => !string.IsNullOrEmpty(packageFolder)));
            }

            if (packageFolders.Count == 0)
            {
                packageFolders.Add(
                    Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
            }

            return packageFolders;
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
