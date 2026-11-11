//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.References
{
    /// <summary>
    /// Kind of database reference in a SQL project.
    /// </summary>
    internal enum DatabaseReferenceKind
    {
        SystemDatabase,
        Dacpac,
        SqlProject,
        NuGetPackage,
    }

    /// <summary>
    /// Outcome of resolving a database reference to something DacFx can load.
    /// </summary>
    internal enum DatabaseReferenceResolution
    {
        Resolved,
        NotFound,
        NotRestored,
        Failed,
    }

    /// <summary>
    /// A database reference of a SQL project, resolved the way a project build resolves it.
    /// </summary>
    internal sealed class ResolvedDatabaseReference
    {
        /// <summary>Name of the reference in the project (its path, package id, or system database).</summary>
        public string Name { get; init; } = string.Empty;

        public DatabaseReferenceKind Kind { get; init; }

        public DatabaseReferenceResolution Resolution { get; init; }

        /// <summary>The dacpac to load, or the referenced .sqlproj for a project reference.</summary>
        public string? ResolvedPath { get; init; }

        /// <summary>Why the reference could not be resolved, or how it was resolved when that is noteworthy.</summary>
        public string? Message { get; init; }

        /// <summary>The reference to load into a model. Null when the reference could not be resolved.</summary>
        public TSqlModelReference? ModelReference { get; init; }

        /// <summary>
        /// Names the referenced database can be written as in the project's scripts: its literal name, or its
        /// SQLCMD variable as $(Variable) plus the variable's value. Empty for same-database references and for
        /// references to another server.
        /// </summary>
        public IReadOnlyList<string> DatabaseNames { get; init; } = Array.Empty<string>();

        /// <summary>
        /// True when the reference is part of the project's own database (a composite reference).
        /// </summary>
        public bool IsSameDatabase =>
            ModelReference != null &&
            string.IsNullOrEmpty(ModelReference.ServerSqlCmdVariable) &&
            string.IsNullOrEmpty(ModelReference.DatabaseSqlCmdVariable) &&
            string.IsNullOrEmpty(ModelReference.DatabaseVariableLiteralValue);
    }

    /// <summary>
    /// A SQL project's database references and SQLCMD variable values, resolved from its .sqlproj.
    /// </summary>
    internal sealed class ResolvedProjectReferences
    {
        public ResolvedProjectReferences(
            string projectFilePath,
            IReadOnlyList<ResolvedDatabaseReference> references,
            IReadOnlyDictionary<string, string> sqlCmdVariables,
            IReadOnlyDictionary<string, DateTime>? dependencies = null)
        {
            ProjectFilePath = projectFilePath;
            References = references;
            SqlCmdVariables = sqlCmdVariables;
            Dependencies = dependencies ?? new Dictionary<string, DateTime>();
        }

        public string ProjectFilePath { get; }

        public IReadOnlyList<ResolvedDatabaseReference> References { get; }

        /// <summary>
        /// The files and folders the resolution read or looked for, including missing ones and those of referenced
        /// projects, each with its <see cref="GetDependencyTimestamp"/> from before it was read. Creating, changing or
        /// deleting a file, or adding, deleting or renaming a script in a folder, can change the result.
        /// </summary>
        public IReadOnlyDictionary<string, DateTime> Dependencies { get; }

        /// <summary>
        /// Returns the last write time of a file or folder, or <see cref="DateTime.MinValue"/> when it doesn't exist.
        /// A folder's last write time changes when a file in it is added, deleted or renamed.
        /// </summary>
        internal static DateTime GetDependencyTimestamp(string path) =>
            File.Exists(path) ? File.GetLastWriteTimeUtc(path)
            : Directory.Exists(path) ? Directory.GetLastWriteTimeUtc(path)
            : DateTime.MinValue;

        /// <summary>
        /// SQLCMD variables that have a value, keyed by name without $(). Variables without a value are left out,
        /// because DacFx treats an empty value as missing and disables applying changes.
        /// </summary>
        public IReadOnlyDictionary<string, string> SqlCmdVariables { get; }

        /// <summary>The references that resolved, ready to load into a model.</summary>
        public IReadOnlyList<TSqlModelReference> ModelReferences =>
            References.Where(reference => reference.ModelReference != null).Select(reference => reference.ModelReference!).ToList();
    }
}
