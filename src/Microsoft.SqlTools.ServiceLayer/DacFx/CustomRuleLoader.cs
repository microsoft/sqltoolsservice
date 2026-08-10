//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Loads custom SQL code analysis rules from NuGet analyzer packages referenced by a .sqlproj.
    /// Reads obj/project.assets.json to locate the analyzer assemblies, loads them, and reflects over
    /// <see cref="ExportCodeAnalysisRuleAttribute"/> to enumerate the rules they contribute.
    /// </summary>
    internal static class CustomRuleLoader
    {
        private const string AssetsFileName = "project.assets.json";
        private const string IntermediateFolderName = "obj";
        private const string AnalyzerPathPrefix = "analyzers/dotnet/cs/";
        private const string AssemblyExtension = ".dll";
        private const string PackageLibraryType = "package";
        private const string PackageReferenceElementName = "PackageReference";
        private const string IncludeAttributeName = "Include";
        private const string VersionMetadataName = "Version";

        /// <summary>
        /// Characters that mark a version as a range or floating version rather than a single pinned
        /// value. Resolving those requires NuGet's range semantics, so they are left uncompared.
        /// </summary>
        private static readonly char[] VersionRangeCharacters = { '*', '[', ']', '(', ')', ',' };

        /// <summary>
        /// Severity reported for custom rules. Analyzer packages don't declare one, and any severity
        /// saved in the .sqlproj is applied on top of this by the caller.
        /// </summary>
        private static readonly string DefaultCustomRuleSeverity = SqlRuleProblemSeverity.Warning.ToString();

        /// <summary>
        /// A package as recorded by the last restore.
        /// </summary>
        /// <param name="Version">Version that was restored.</param>
        /// <param name="ContributesRules">Whether the package ships analyzer assemblies.</param>
        private readonly record struct RestoredPackage(string Version, bool ContributesRules);

        /// <summary>
        /// Rules and non-fatal warnings produced by a load operation.
        /// </summary>
        internal sealed class LoadResult
        {
            private readonly HashSet<string> contributedRuleIds = new(StringComparer.OrdinalIgnoreCase);

            public List<CodeAnalysisRuleInfo> Rules { get; } = new();

            public List<string> Warnings { get; } = new();

            /// <summary>
            /// True when restoring the project would resolve a reported warning. Lets a client offer
            /// a restore action without having to parse the localized warning text.
            /// </summary>
            public bool RestoreRequired { get; set; }

            /// <summary>
            /// All warnings as a single message, or <c>null</c> when there are none.
            /// </summary>
            public string? Warning => Warnings.Count == 0 ? null : string.Join(Environment.NewLine, Warnings);

            /// <summary>
            /// Records a rule, keeping the first definition of any given ID and warning about the rest.
            /// Two analyzer packages can declare the same rule ID; DacFx keeps only one of them at
            /// build time without reporting which, so the conflict has to be surfaced here.
            /// </summary>
            internal void AddRule(CodeAnalysisRuleInfo rule)
            {
                if (contributedRuleIds.Add(rule.RuleId))
                {
                    Rules.Add(rule);
                }
                else
                {
                    Warnings.Add(SR.CustomRuleDuplicateId(rule.RuleId));
                }
            }
        }

        /// <summary>
        /// Loads custom rules from the NuGet analyzer packages referenced by the project.
        /// Never throws; failures are reported through <see cref="LoadResult.Warnings"/>.
        /// </summary>
        /// <param name="projectFileUriOrPath">
        /// Location of the .sqlproj file, either a <c>file://</c> URI as LSP clients send, or a
        /// plain OS path.
        /// </param>
        public static LoadResult LoadFromProject(string projectFileUriOrPath)
        {
            LoadResult result = new();

            try
            {
                string projectFilePath = ToLocalPath(projectFileUriOrPath);
                string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
                string assetsFilePath = Path.Combine(projectDirectory, IntermediateFolderName, AssetsFileName);

                if (!File.Exists(assetsFilePath))
                {
                    result.RestoreRequired = true;
                    result.Warnings.Add(SR.CustomRulesRestoreRequired);
                    return result;
                }

                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsFilePath));
                JsonElement root = document.RootElement;

                // The assets file only reflects the last successful restore, so it stays internally
                // consistent after a package upgrade that has not been restored. Comparing it against
                // the project is the only way to notice that the loaded rules are stale.
                ReportPackagesNeedingRestore(projectFilePath, root, result);

                // A package can ship the same assembly under several target frameworks, so each
                // distinct assembly identity is reflected over only once.
                HashSet<string> processedAssemblies = new(StringComparer.OrdinalIgnoreCase);

                foreach (string assemblyPath in ResolveAnalyzerAssemblyPaths(root, result))
                {
                    Assembly? assembly = TryLoadAssembly(assemblyPath, result);

                    if (assembly != null && processedAssemblies.Add(assembly.FullName ?? assemblyPath))
                    {
                        AddRulesFromAssembly(assembly, result);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add(SR.CustomRulesLoadFailed(ex.Message));
            }

            return result;
        }

        /// <summary>
        /// Reports packages whose referenced version does not match what was restored, so stale
        /// rules are not presented as though they matched the project.
        /// Only exact versions are compared; a floating version or range cannot be matched against a
        /// single restored version by string comparison, so those are left alone.
        /// </summary>
        private static void ReportPackagesNeedingRestore(string projectFilePath, JsonElement root, LoadResult result)
        {
            IReadOnlyDictionary<string, RestoredPackage> restoredPackages = ReadRestoredPackages(root);

            foreach ((string packageName, string referencedVersion) in ReadReferencedPackageVersions(projectFilePath))
            {
                // A package absent from the assets file is left alone: without a restore there is no
                // file list to tell whether it contributes rules, and warning about every unrestored
                // package would report unrelated ones as code analysis problems.
                if (referencedVersion.IndexOfAny(VersionRangeCharacters) < 0
                    && restoredPackages.TryGetValue(packageName, out RestoredPackage restored)
                    && restored.ContributesRules
                    && !string.Equals(referencedVersion, restored.Version, StringComparison.OrdinalIgnoreCase))
                {
                    result.RestoreRequired = true;
                    result.Warnings.Add(SR.CustomRulesPackageOutOfDate(packageName, referencedVersion, restored.Version));
                }
            }
        }

        /// <summary>
        /// Reads the packages the last restore resolved, keyed by package name.
        /// </summary>
        private static IReadOnlyDictionary<string, RestoredPackage> ReadRestoredPackages(JsonElement root)
        {
            Dictionary<string, RestoredPackage> restoredPackages = new(StringComparer.OrdinalIgnoreCase);

            if (!TryGetObject(root, "libraries", out JsonElement libraries))
            {
                return restoredPackages;
            }

            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                // Entry names use the "PackageId/Version" format.
                string[] nameParts = library.Name.Split('/');

                if (nameParts.Length == 2)
                {
                    restoredPackages[nameParts[0]] = new RestoredPackage(nameParts[1], ContributesRules(library));
                }
            }

            return restoredPackages;
        }

        /// <summary>
        /// Whether a restored package ships analyzer assemblies, which is what makes it a source of
        /// custom rules.
        /// </summary>
        private static bool ContributesRules(JsonProperty library) =>
            library.Value.TryGetProperty("files", out JsonElement files)
            && files.ValueKind == JsonValueKind.Array
            && files.EnumerateArray().Any(file =>
                file.ValueKind == JsonValueKind.String && IsCandidateAssembly(file.GetString()!));

        /// <summary>
        /// Reads the package references declared by the project. The version can be either an
        /// attribute or a child element, and the element is namespace-qualified in original-style
        /// projects, so both forms are handled.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> ReadReferencedPackageVersions(string projectFilePath)
        {
            XDocument project = XDocument.Load(projectFilePath);

            foreach (XElement element in project.Descendants().Where(x => x.Name.LocalName == PackageReferenceElementName))
            {
                string? packageName = element.Attribute(IncludeAttributeName)?.Value;
                string? version = element.Attribute(VersionMetadataName)?.Value
                    ?? element.Elements().FirstOrDefault(x => x.Name.LocalName == VersionMetadataName)?.Value;

                if (!string.IsNullOrWhiteSpace(packageName) && !string.IsNullOrWhiteSpace(version))
                {
                    yield return new KeyValuePair<string, string>(packageName.Trim(), version.Trim());
                }
            }
        }

        /// <summary>
        /// Resolves the on-disk paths of the analyzer assemblies contributed by the packages the
        /// project references directly.
        /// </summary>
        internal static List<string> ResolveAnalyzerAssemblyPaths(JsonElement root, LoadResult result)
        {
            IReadOnlyList<string> packageFolders = ReadPackageFolders(root);
            HashSet<string> directDependencies = ReadDirectDependencies(root);
            List<string> assemblyPaths = new();

            if (!TryGetObject(root, "libraries", out JsonElement libraries))
            {
                return assemblyPaths;
            }

            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                if (!TryGetDirectlyReferencedPackage(library, directDependencies, out string packageName, out string version) ||
                    !library.Value.TryGetProperty("files", out JsonElement files) ||
                    files.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement file in files.EnumerateArray())
                {
                    string packageRelativePath = file.ValueKind == JsonValueKind.String ? file.GetString()! : string.Empty;

                    if (!IsCandidateAssembly(packageRelativePath))
                    {
                        continue;
                    }

                    // Packages are laid out as <packageFolder>/<lowercase id>/<version>/<file>.
                    string folderRelativePath = Path.Combine(
                        packageName.ToLowerInvariant(),
                        version,
                        packageRelativePath.Replace('/', Path.DirectorySeparatorChar));

                    string? resolvedPath = packageFolders
                        .Select(packageFolder => Path.Combine(packageFolder, folderRelativePath))
                        .FirstOrDefault(File.Exists);

                    if (resolvedPath != null)
                    {
                        assemblyPaths.Add(resolvedPath);
                    }
                    else
                    {
                        result.Warnings.Add(SR.CustomRulesAssemblyNotFound(folderRelativePath));
                    }
                }
            }

            return assemblyPaths;
        }

        /// <summary>
        /// Reads the NuGet package folders recorded in the assets file. Taking them from the file
        /// covers custom NUGET_PACKAGES locations and shared caches without probing the environment.
        /// </summary>
        private static IReadOnlyList<string> ReadPackageFolders(JsonElement root)
        {
            List<string> packageFolders = new();

            if (TryGetObject(root, "packageFolders", out JsonElement packageFoldersElement))
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

        /// <summary>
        /// Reads the packages the project references directly, so transitive dependencies are not
        /// loaded and reflected over.
        /// </summary>
        private static HashSet<string> ReadDirectDependencies(JsonElement root)
        {
            HashSet<string> directDependencies = new(StringComparer.OrdinalIgnoreCase);

            if (!root.TryGetProperty("project", out JsonElement project) ||
                !TryGetObject(project, "frameworks", out JsonElement frameworks))
            {
                return directDependencies;
            }

            foreach (JsonProperty framework in frameworks.EnumerateObject())
            {
                if (TryGetObject(framework.Value, "dependencies", out JsonElement dependencies))
                {
                    foreach (JsonProperty dependency in dependencies.EnumerateObject())
                    {
                        directDependencies.Add(dependency.Name);
                    }
                }
            }

            return directDependencies;
        }

        /// <summary>
        /// Matches a "libraries" entry that is a NuGet package the project references directly.
        /// Entry names use the "PackageId/Version" format.
        /// </summary>
        private static bool TryGetDirectlyReferencedPackage(
            JsonProperty library,
            HashSet<string> directDependencies,
            out string packageName,
            out string version)
        {
            packageName = string.Empty;
            version = string.Empty;

            string[] nameParts = library.Name.Split('/');

            if (nameParts.Length != 2 ||
                !directDependencies.Contains(nameParts[0]) ||
                !library.Value.TryGetProperty("type", out JsonElement libraryType) ||
                libraryType.ValueKind != JsonValueKind.String ||
                !string.Equals(libraryType.GetString(), PackageLibraryType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            packageName = nameParts[0];
            version = nameParts[1];
            return true;
        }

        /// <summary>
        /// Matches the assemblies the build itself would load. The SQL projects SDK sets
        /// <c>$(Language)</c> to C# so that NuGet resolves <c>analyzers/dotnet/cs/</c> into the
        /// <c>@(Analyzer)</c> item group, and that item group is the only analyzer input handed to
        /// the code analysis build task. Rules shipped anywhere else in a package, <c>lib/</c>
        /// included, never run during a build, so listing them here would be misleading.
        /// </summary>
        private static bool IsCandidateAssembly(string packageRelativePath) =>
            packageRelativePath.EndsWith(AssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
            packageRelativePath.StartsWith(AnalyzerPathPrefix, StringComparison.OrdinalIgnoreCase);

        private static bool TryGetObject(JsonElement parent, string propertyName, out JsonElement value) =>
            parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object;

        /// <summary>
        /// Returns the OS-native path from either a <c>file://</c> URI string or a plain OS path,
        /// matching how the other SQL project endpoints accept both forms.
        /// </summary>
        private static string ToLocalPath(string uriOrPath) =>
            Uri.TryCreate(uriOrPath, UriKind.Absolute, out Uri? uri) && uri.IsFile
                ? FileUtilities.UriToLocalPath(uri)
                : uriOrPath;

        /// <summary>
        /// Loads the analyzer into the default load context so that rule metadata is read exactly as
        /// DacFx reads it at build time. A reflection-only context would avoid running package code,
        /// but the documented rule pattern computes <c>DisplayName</c> and <c>Description</c> in an
        /// attribute override that reads a resource file, so those values are only correct when the
        /// attribute actually executes.
        /// </summary>
        private static Assembly? TryLoadAssembly(string assemblyPath, LoadResult result)
        {
            try
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                result.Warnings.Add(SR.CustomRulesAssemblyLoadFailed(Path.GetFileName(assemblyPath), ex.Message));
                return null;
            }
        }

        /// <summary>
        /// Adds a <see cref="CodeAnalysisRuleInfo"/> for every exported code analysis rule in the assembly.
        /// </summary>
        internal static void AddRulesFromAssembly(Assembly assembly, LoadResult result)
        {
            try
            {
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.IsAbstract || !typeof(SqlCodeAnalysisRule).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    ExportCodeAnalysisRuleAttribute? attribute = type.GetCustomAttribute<ExportCodeAnalysisRuleAttribute>();

                    if (attribute != null)
                    {
                        result.AddRule(CreateRuleInfo(attribute));
                    }
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add(SR.CustomRulesAssemblyLoadFailed(assembly.GetName().Name ?? string.Empty, ex.Message));
            }
        }

        private static CodeAnalysisRuleInfo CreateRuleInfo(ExportCodeAnalysisRuleAttribute attribute)
        {
            string ruleId = attribute.Id;

            return new CodeAnalysisRuleInfo
            {
                RuleId = ruleId,
                ShortRuleId = ruleId[(ruleId.LastIndexOf('.') + 1)..],
                DisplayName = attribute.DisplayName ?? ruleId,
                Description = attribute.Description ?? string.Empty,
                Category = attribute.Category ?? string.Empty,
                Severity = DefaultCustomRuleSeverity,
                RuleScope = attribute.RuleScope.ToString(),
                IsBuiltIn = false
            };
        }
    }
}
