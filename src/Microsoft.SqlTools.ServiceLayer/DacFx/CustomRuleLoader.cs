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

        /// <summary>
        /// Severity reported for custom rules. Analyzer packages don't declare one, and any severity
        /// saved in the .sqlproj is applied on top of this by the caller.
        /// </summary>
        private static readonly string DefaultCustomRuleSeverity = SqlRuleProblemSeverity.Warning.ToString();

        /// <summary>
        /// Rules and non-fatal warnings produced by a load operation.
        /// </summary>
        internal sealed class LoadResult
        {
            private readonly HashSet<string> contributedRuleIds = new(StringComparer.OrdinalIgnoreCase);

            public List<CodeAnalysisRuleInfo> Rules { get; } = new();

            public List<string> Warnings { get; } = new();

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
                string projectDirectory = Path.GetDirectoryName(ToLocalPath(projectFileUriOrPath)) ?? string.Empty;
                string assetsFilePath = Path.Combine(projectDirectory, IntermediateFolderName, AssetsFileName);

                if (!File.Exists(assetsFilePath))
                {
                    result.Warnings.Add(SR.CustomRulesRestoreRequired);
                    return result;
                }

                // A package can ship the same assembly under several target frameworks, so each
                // distinct assembly identity is reflected over only once.
                HashSet<string> processedAssemblies = new(StringComparer.OrdinalIgnoreCase);

                foreach (string assemblyPath in ResolveAnalyzerAssemblyPaths(assetsFilePath, result))
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
        /// Resolves the on-disk paths of the analyzer assemblies contributed by the packages the
        /// project references directly.
        /// </summary>
        internal static List<string> ResolveAnalyzerAssemblyPaths(string assetsFilePath, LoadResult result)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsFilePath));
            JsonElement root = document.RootElement;

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
