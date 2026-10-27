//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.References
{
    /// <summary>
    /// Resolves the database references of a SQL project the way a project build does:
    /// system databases to master/msdb dacpacs for the project's platform, dacpac references to files,
    /// project references to the referenced project's scripts (built in memory, no build needed), and
    /// NuGet package references to the dacpac restored with the package.
    /// </summary>
    /// <remarks>
    /// Only the project's direct references are loaded, as in a build; a referenced project carries its own
    /// references, which are used only to resolve that project. Resolution never throws for a bad reference;
    /// the reference is reported with a <see cref="DatabaseReferenceResolution"/> other than Resolved instead.
    /// </remarks>
    internal sealed partial class ProjectReferenceResolver
    {
        private const string SystemDacpacsFolderName = "SystemDacpacs";
        private const string DacpacExtension = ".dacpac";
        private const string SystemDacpacPackagePrefix = "Microsoft.SqlServer.Dacpacs.";
        private const string DspPrefix = "Microsoft.Data.Tools.Schema.Sql.Sql";
        private const string DspSuffix = "DatabaseSchemaProvider";

        /// <summary>Referenced projects nested deeper than this are not loaded.</summary>
        internal const int MaxProjectReferenceDepth = 16;

        [GeneratedRegex(@"\$\((?<name>[A-Za-z_][A-Za-z0-9_.-]*)\)")]
        private static partial Regex MsBuildPropertyPattern();

        private readonly string systemDacpacsRoot;
        private readonly Func<string, SqlProject> openProject;

        /// <param name="systemDacpacsRoot">Folder that holds the bundled system dacpacs as &lt;platform&gt;/master.dacpac.</param>
        /// <param name="openProject">Opens a referenced project with its scripts. Defaults to <see cref="SqlProject.OpenProject(string, bool)"/>.</param>
        public ProjectReferenceResolver(string systemDacpacsRoot, Func<string, SqlProject>? openProject = null)
        {
            this.systemDacpacsRoot = systemDacpacsRoot ?? throw new ArgumentNullException(nameof(systemDacpacsRoot));
            this.openProject = openProject ?? (path => SqlProject.OpenProject(path, onlyLoadProperties: false));
        }

        /// <summary>
        /// The system dacpacs shipped with SQL Tools Service, laid out like the SQL projects SDK's
        /// $(SystemDacpacsLocation)\SystemDacpacs folder so builds can use the same copy.
        /// </summary>
        public static string DefaultSystemDacpacsRoot => Path.Combine(AppContext.BaseDirectory, SystemDacpacsFolderName);

        /// <summary>
        /// Resolves the references of <paramref name="project"/>, whose .sqlproj is at <paramref name="projectFilePath"/>.
        /// </summary>
        public ResolvedProjectReferences Resolve(SqlProject project, string projectFilePath)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(projectFilePath);

            string fullPath = Path.GetFullPath(projectFilePath);
            return Resolve(project, fullPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { fullPath }, depth: 0);
        }

        private ResolvedProjectReferences Resolve(SqlProject project, string projectFilePath, HashSet<string> projectsBeingResolved, int depth)
        {
            string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            IReadOnlyDictionary<string, string> sqlCmdVariables = GetSqlCmdVariableValues(project);

            // Every file the result depends on, recorded whether it exists or not, so creating a missing one is noticed too.
            // Each is recorded before it's read, so a change made while resolving is noticed next time. The caller read the
            // .sqlproj already: the cache, or the referencing project's resolution, records it before that.
            var dependencies = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            AddDependency(dependencies, projectFilePath);
            AddDependency(dependencies, ProjectAssets.GetAssetsFilePath(projectDirectory));
            ProjectAssets? assets = ProjectAssets.TryLoad(projectDirectory);

            var references = new List<ResolvedDatabaseReference>();
            foreach (DatabaseReference reference in project.DatabaseReferences)
            {
                try
                {
                    references.Add(reference switch
                    {
                        SystemDatabaseReference systemReference => ResolveSystemDatabase(systemReference, project.Properties.DatabaseSchemaProvider, assets, dependencies),
                        DacpacReference dacpacReference => ResolveDacpac(dacpacReference, project, projectDirectory, sqlCmdVariables, dependencies),
                        SqlProjectReference projectReference => ResolveProject(projectReference, projectDirectory, sqlCmdVariables, projectsBeingResolved, depth, dependencies),
                        NugetPackageReference packageReference => ResolvePackage(packageReference, assets, sqlCmdVariables, dependencies),
                        _ => Unresolved(reference.Name, DatabaseReferenceKind.Dacpac, DatabaseReferenceResolution.Failed, SR.DatabaseReferenceNotSupported(reference.Name)),
                    });
                }
                catch (Exception ex)
                {
                    references.Add(Unresolved(reference.Name, GetKind(reference), DatabaseReferenceResolution.Failed, ex.Message));
                }
            }

            return new ResolvedProjectReferences(projectFilePath, references, sqlCmdVariables, dependencies);
        }

        /// <summary>
        /// Returns the value of each SQLCMD variable: its value when the project sets a literal one, otherwise its default.
        /// A value such as $(SqlCmdVar__1) is filled in by the publish profile or command line, so the default is used.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> GetSqlCmdVariableValues(SqlProject project)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (SqlCmdVariable variable in project.SqlCmdVariables)
            {
                string? value = !string.IsNullOrWhiteSpace(variable.Value) && !variable.Value.Contains("$(", StringComparison.Ordinal)
                    ? variable.Value
                    : variable.DefaultValue;
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(variable.VarName))
                {
                    values[variable.VarName] = value;
                }
            }

            return values;
        }

        private ResolvedDatabaseReference ResolveSystemDatabase(
            SystemDatabaseReference reference,
            string databaseSchemaProvider,
            ProjectAssets? assets,
            Dictionary<string, DateTime> dependencies)
        {
            string databaseName = reference.SystemDb == SystemDatabase.MSDB ? "msdb" : "master";
            string? dacpacPath = null;
            string? message = null;

            if (reference.ReferenceType == ReferenceType.PackageReference)
            {
                dacpacPath = FindSystemDacpacInPackages(assets, databaseName, dependencies);
                if (dacpacPath == null)
                {
                    message = SR.SystemDatabasePackageNotRestored(databaseName);
                }
            }

            if (dacpacPath == null)
            {
                string? platformFolder = GetSystemDacpacFolderName(databaseSchemaProvider);
                string? bundledPath = platformFolder == null ? null : Path.Combine(systemDacpacsRoot, platformFolder, databaseName + DacpacExtension);
                if (bundledPath != null)
                {
                    AddDependency(dependencies, bundledPath);
                }

                if (bundledPath == null || !File.Exists(bundledPath))
                {
                    return Unresolved(reference.Name, DatabaseReferenceKind.SystemDatabase, DatabaseReferenceResolution.NotFound,
                        SR.SystemDatabaseDacpacNotFound(databaseName, platformFolder ?? databaseSchemaProvider));
                }

                dacpacPath = bundledPath;
            }

            // Package references are bound to their database by the SDK, and the SQL projects library reports "master"
            // for every package reference, so use the system database's own name for those.
            string? literal = reference.ReferenceType == ReferenceType.PackageReference
                ? databaseName
                : reference.DatabaseVariableLiteralName;

            return new ResolvedDatabaseReference
            {
                Name = reference.Name,
                Kind = DatabaseReferenceKind.SystemDatabase,
                Resolution = DatabaseReferenceResolution.Resolved,
                ResolvedPath = dacpacPath,
                Message = message,
                ModelReference = new TSqlModelReference(
                    dacpacPath,
                    databaseVariableLiteralValue: literal,
                    suppressMissingDependenciesErrors: reference.SuppressMissingDependencies),
                DatabaseNames = string.IsNullOrEmpty(literal) ? Array.Empty<string>() : new[] { literal },
            };
        }

        private ResolvedDatabaseReference ResolveDacpac(
            DacpacReference reference,
            SqlProject project,
            string projectDirectory,
            IReadOnlyDictionary<string, string> sqlCmdVariables,
            Dictionary<string, DateTime> dependencies)
        {
            string? expandedPath = ExpandMsBuildProperties(reference.DacpacPath, project, projectDirectory, out string? undefinedProperty);
            if (expandedPath == null)
            {
                return Unresolved(reference.Name, DatabaseReferenceKind.Dacpac, DatabaseReferenceResolution.NotFound,
                    SR.DatabaseReferenceUndefinedProperty(reference.DacpacPath, undefinedProperty ?? string.Empty));
            }

            string dacpacPath = ToFullPath(projectDirectory, expandedPath);
            AddDependency(dependencies, dacpacPath);
            if (!File.Exists(dacpacPath))
            {
                return Unresolved(reference.Name, DatabaseReferenceKind.Dacpac, DatabaseReferenceResolution.NotFound,
                    SR.DatabaseReferenceFileNotFound(dacpacPath));
            }

            (string? server, string? database, string? literal, IReadOnlyList<string> names) = GetBinding(reference, sqlCmdVariables);
            return new ResolvedDatabaseReference
            {
                Name = reference.Name,
                Kind = DatabaseReferenceKind.Dacpac,
                Resolution = DatabaseReferenceResolution.Resolved,
                ResolvedPath = dacpacPath,
                ModelReference = new TSqlModelReference(dacpacPath, server, database, literal, reference.SuppressMissingDependencies),
                DatabaseNames = names,
            };
        }

        private ResolvedDatabaseReference ResolveProject(
            SqlProjectReference reference,
            string projectDirectory,
            IReadOnlyDictionary<string, string> sqlCmdVariables,
            HashSet<string> projectsBeingResolved,
            int depth,
            Dictionary<string, DateTime> dependencies)
        {
            string referencedProjectPath = ToFullPath(projectDirectory, reference.ProjectPath);
            AddDependency(dependencies, referencedProjectPath);
            if (!File.Exists(referencedProjectPath))
            {
                return Unresolved(reference.Name, DatabaseReferenceKind.SqlProject, DatabaseReferenceResolution.NotFound,
                    SR.DatabaseReferenceFileNotFound(referencedProjectPath));
            }

            if (projectsBeingResolved.Contains(referencedProjectPath))
            {
                return Unresolved(reference.Name, DatabaseReferenceKind.SqlProject, DatabaseReferenceResolution.Failed,
                    SR.DatabaseReferenceCircular(referencedProjectPath));
            }

            if (depth >= MaxProjectReferenceDepth)
            {
                return Unresolved(reference.Name, DatabaseReferenceKind.SqlProject, DatabaseReferenceResolution.Failed,
                    SR.DatabaseReferenceTooDeep(referencedProjectPath));
            }

            // Record the folders before opening the project, which lists its scripts: an SDK-style project includes every
            // .sql file under its folder, so adding, deleting or renaming one changes the scripts without changing the .sqlproj
            string referencedDirectory = Path.GetDirectoryName(referencedProjectPath) ?? string.Empty;
            AddProjectFolderDependencies(dependencies, referencedDirectory);

            SqlProject referencedProject = openProject(referencedProjectPath);
            List<string> scripts = referencedProject.SqlObjectScripts
                .Select(script => ToFullPath(referencedDirectory, script.Path))
                .ToList();
            foreach (string script in scripts)
            {
                // Scripts the project includes from outside its folder
                AddDependency(dependencies, Path.GetDirectoryName(script) ?? referencedDirectory);
            }

            projectsBeingResolved.Add(referencedProjectPath);
            ResolvedProjectReferences nestedReferences;
            try
            {
                nestedReferences = Resolve(referencedProject, referencedProjectPath, projectsBeingResolved, depth + 1);
            }
            finally
            {
                projectsBeingResolved.Remove(referencedProjectPath);
            }

            foreach ((string path, DateTime timestamp) in nestedReferences.Dependencies)
            {
                // Keep the timestamps recorded here, before the referenced project was read
                dependencies.TryAdd(path, timestamp);
            }

            (string? server, string? database, string? literal, IReadOnlyList<string> names) = GetBinding(reference, sqlCmdVariables);
            return new ResolvedDatabaseReference
            {
                Name = reference.Name,
                Kind = DatabaseReferenceKind.SqlProject,
                Resolution = DatabaseReferenceResolution.Resolved,
                ResolvedPath = referencedProjectPath,
                ModelReference = TSqlModelReference.ForProject(
                    referencedProjectPath,
                    scripts,
                    referencedProject.Properties.DatabaseSchemaProvider,
                    nestedReferences.ModelReferences,
                    server,
                    database,
                    literal,
                    reference.SuppressMissingDependencies),
                DatabaseNames = names,
            };
        }

        private static ResolvedDatabaseReference ResolvePackage(
            NugetPackageReference reference,
            ProjectAssets? assets,
            IReadOnlyDictionary<string, string> sqlCmdVariables,
            Dictionary<string, DateTime> dependencies)
        {
            string? packageDirectory = assets?.GetPackageDirectory(reference.PackageName);
            if (packageDirectory == null)
            {
                return Unresolved(reference.Name, DatabaseReferenceKind.NuGetPackage, DatabaseReferenceResolution.NotRestored,
                    SR.DatabaseReferencePackageNotRestored(reference.PackageName));
            }

            // Like the SQL projects SDK, look for tools/<package id>.dacpac, falling back to the package's only dacpac.
            string toolsDirectory = Path.Combine(packageDirectory, "tools");
            string dacpacPath = Path.Combine(toolsDirectory, reference.PackageName + DacpacExtension);
            AddDependency(dependencies, dacpacPath);
            if (!File.Exists(dacpacPath))
            {
                string[] dacpacs = Directory.Exists(toolsDirectory)
                    ? Directory.GetFiles(toolsDirectory, "*" + DacpacExtension)
                    : Array.Empty<string>();
                if (dacpacs.Length != 1)
                {
                    return Unresolved(reference.Name, DatabaseReferenceKind.NuGetPackage, DatabaseReferenceResolution.NotFound,
                        SR.DatabaseReferenceFileNotFound(dacpacPath));
                }

                dacpacPath = dacpacs[0];
                AddDependency(dependencies, dacpacPath);
            }

            (string? server, string? database, string? literal, IReadOnlyList<string> names) = GetBinding(reference, sqlCmdVariables);
            return new ResolvedDatabaseReference
            {
                Name = reference.Name,
                Kind = DatabaseReferenceKind.NuGetPackage,
                Resolution = DatabaseReferenceResolution.Resolved,
                ResolvedPath = dacpacPath,
                ModelReference = new TSqlModelReference(dacpacPath, server, database, literal, reference.SuppressMissingDependencies),
                DatabaseNames = names,
            };
        }

        /// <summary>
        /// Returns a reference's server variable, database variable and literal, plus the names its database can be
        /// written as. A literal takes precedence over a database variable, as in a build.
        /// </summary>
        private static (string? Server, string? Database, string? Literal, IReadOnlyList<string> Names) GetBinding(
            UserDatabaseReference reference,
            IReadOnlyDictionary<string, string> sqlCmdVariables)
        {
            string? server = NullIfEmpty(reference.ServerVariable?.VarName);
            string? database = NullIfEmpty(reference.DatabaseVariable?.VarName);
            string? literal = NullIfEmpty(reference.DatabaseVariableLiteralName);

            var names = new List<string>();
            if (server == null)
            {
                if (literal != null)
                {
                    names.Add(literal);
                }
                else if (database != null)
                {
                    names.Add("$(" + database + ")");
                    if (sqlCmdVariables.TryGetValue(database, out string? value))
                    {
                        names.Add(value);
                    }
                }
            }

            return (server, database, literal, names);
        }

        /// <summary>
        /// Finds the restored Microsoft.SqlServer.Dacpacs.* package for a system database and returns its dacpac.
        /// Fabric packages (DbFabric, FabricDw) only provide master.
        /// </summary>
        private static string? FindSystemDacpacInPackages(ProjectAssets? assets, string databaseName, Dictionary<string, DateTime> dependencies)
        {
            if (assets == null)
            {
                return null;
            }

            foreach (string packageId in assets.PackageIds.Where(id => id.StartsWith(SystemDacpacPackagePrefix, StringComparison.OrdinalIgnoreCase)))
            {
                bool isMsdbPackage = packageId.EndsWith(".Msdb", StringComparison.OrdinalIgnoreCase);
                if (isMsdbPackage != (databaseName == "msdb"))
                {
                    continue;
                }

                string? packageDirectory = assets.GetPackageDirectory(packageId);
                if (packageDirectory == null)
                {
                    continue;
                }

                string dacpacPath = Path.Combine(packageDirectory, "tools", databaseName + DacpacExtension);
                AddDependency(dependencies, dacpacPath);
                if (File.Exists(dacpacPath))
                {
                    return dacpacPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the SQL projects SDK's system dacpac folder for a platform, for example "160" for
        /// Microsoft.Data.Tools.Schema.Sql.Sql160DatabaseSchemaProvider, "AzureV12", "AzureDw", "DbFabric".
        /// </summary>
        internal static string? GetSystemDacpacFolderName(string? databaseSchemaProvider)
        {
            if (string.IsNullOrEmpty(databaseSchemaProvider))
            {
                return null;
            }

            string name = databaseSchemaProvider;
            if (name.StartsWith(DspPrefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(DspPrefix.Length);
            }

            if (name.EndsWith(DspSuffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - DspSuffix.Length);
            }

            // Synapse dedicated pools use the AzureDw folder.
            return string.Equals(name, "Dw", StringComparison.OrdinalIgnoreCase) ? "AzureDw" : name;
        }

        /// <summary>
        /// Expands $(Property) in a reference path using well-known MSBuild properties and the project's own properties.
        /// Returns null when a property is not defined, since the path cannot be resolved without evaluating MSBuild.
        /// </summary>
        internal string? ExpandMsBuildProperties(string path, SqlProject project, string projectDirectory, out string? undefinedProperty)
        {
            string? missing = null;
            string projectDirectoryWithSeparator = projectDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? projectDirectory
                : projectDirectory + Path.DirectorySeparatorChar;

            string expanded = MsBuildPropertyPattern().Replace(path, match =>
            {
                string name = match.Groups["name"].Value;
                string? value = name switch
                {
                    "MSBuildProjectDirectory" => projectDirectory,
                    "MSBuildThisFileDirectory" or "ProjectDir" => projectDirectoryWithSeparator,
                    // The SDK's system dacpacs live under <location>\SystemDacpacs; STS ships the same layout.
                    "SystemDacpacsLocation" or "NETCoreTargetsPath" or "DacPacRootPath" => Path.GetDirectoryName(systemDacpacsRoot),
                    _ => project.Properties.GetProperty(name),
                };

                if (value == null)
                {
                    missing ??= name;
                    return match.Value;
                }

                return value;
            });

            undefinedProperty = missing;
            return missing == null ? expanded : null;
        }

        private static string ToFullPath(string baseDirectory, string path)
        {
            // Paths in a .sqlproj use Windows separators, which are not separators on macOS and Linux.
            string normalized = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.IsPathRooted(normalized) ? normalized : Path.Combine(baseDirectory, normalized));
        }

        /// <summary>Records a file or folder the result depends on, with its timestamp as of now, unless it's recorded already.</summary>
        private static void AddDependency(Dictionary<string, DateTime> dependencies, string path) =>
            dependencies.TryAdd(path, ResolvedProjectReferences.GetDependencyTimestamp(path));

        /// <summary>Folders below a project's folder, without following links, which could loop.</summary>
        private static readonly EnumerationOptions SubfolderOptions = new()
        {
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        /// <summary>
        /// Records a project's folder and the folders below it that an SDK-style project's default .sql glob searches:
        /// all except bin and obj at the top, and hidden folders such as .git.
        /// </summary>
        private static void AddProjectFolderDependencies(Dictionary<string, DateTime> dependencies, string projectDirectory)
        {
            var folders = new Stack<string>();
            folders.Push(projectDirectory);
            while (folders.Count > 0)
            {
                string folder = folders.Pop();
                AddDependency(dependencies, folder);

                List<string> subfolders;
                try
                {
                    subfolders = Directory.EnumerateDirectories(folder, "*", SubfolderOptions).ToList();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue; // deleted meanwhile, or not readable
                }

                foreach (string subfolder in subfolders)
                {
                    string name = Path.GetFileName(subfolder);
                    bool isBuildOutput = folder == projectDirectory &&
                        (name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase));
                    if (!isBuildOutput && !name.StartsWith('.'))
                    {
                        folders.Push(subfolder);
                    }
                }
            }
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static DatabaseReferenceKind GetKind(DatabaseReference reference) => reference switch
        {
            SystemDatabaseReference => DatabaseReferenceKind.SystemDatabase,
            SqlProjectReference => DatabaseReferenceKind.SqlProject,
            NugetPackageReference => DatabaseReferenceKind.NuGetPackage,
            _ => DatabaseReferenceKind.Dacpac,
        };

        private static ResolvedDatabaseReference Unresolved(string name, DatabaseReferenceKind kind, DatabaseReferenceResolution resolution, string message) =>
            new()
            {
                Name = name,
                Kind = kind,
                Resolution = resolution,
                Message = message,
            };
    }
}
