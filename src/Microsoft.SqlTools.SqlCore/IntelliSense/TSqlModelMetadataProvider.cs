//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

// SSDT counterpart: MetadataProvider/SchemaModelMetadataProvider.cs → TSqlModelMetadataProvider

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    internal enum SqlObjectType { Table, View, StoredProcedure, ScalarFunction, TableValuedFunction, UserDefinedDataType, UserDefinedTableType }

    internal readonly struct QualifiedSqlObject : IEquatable<QualifiedSqlObject>
    {
        public readonly string SchemaName;
        public readonly string ObjectName;
        public readonly SqlObjectType ObjectType;

        public QualifiedSqlObject(string schemaName, string objectName, SqlObjectType objectType)
        {
            SchemaName = schemaName;
            ObjectName = objectName;
            ObjectType = objectType;
        }

        public bool Equals(QualifiedSqlObject other) =>
            ObjectType == other.ObjectType &&
            string.Equals(SchemaName, other.SchemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ObjectName,  other.ObjectName,  StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => obj is QualifiedSqlObject q && Equals(q);

        public override int GetHashCode() =>
            ((int)ObjectType * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(SchemaName)) * 31 +
            StringComparer.OrdinalIgnoreCase.GetHashCode(ObjectName);
    }

    /// <summary>
    /// <see cref="MetadataProviderBase"/> backed by a <see cref="TSqlModel"/>. Fully offline — no server connection.
    /// <para>
    /// The <see cref="TSqlModel"/> exposes two object scopes:<br/>
    /// - <c>DacQueryScopes.UserDefined</c>: objects from the project's .sql files.<br/>
    /// - <c>DacQueryScopes.BuiltIn</c>: <c>sys.*</c>, <c>INFORMATION_SCHEMA.*</c> and all other
    ///   system catalog objects embedded in the DacFx assembly.
    /// </para>
    /// <para>
    /// Metadata collections are lazy-loaded: schema collections are built on first access;
    /// object and column collections within each schema are built on first access.
    /// The source location index is built eagerly at construction to enable instant
    /// Go to Definition lookups without delays.
    /// </para>
    /// </summary>
    public sealed class TSqlModelMetadataProvider : MetadataProviderBase
    {
        private readonly TSqlModel _model;
        private readonly TSqlModelServer _server;
        private readonly Dictionary<string, SourceInformation> _sourceLocations;

        // Maps sourceName (file path) → set of tracked objects (Table/View/Proc/Func) in that file.
        // Used by UpdateForFileChange to compute per-object reset/add/remove operations.
        private readonly Dictionary<string, HashSet<QualifiedSqlObject>> _fileToObjects;

        // Key = schema-qualified object name (e.g. "dbo.Foo").
        // Value = map of source file path → definition count in that file.
        // Total count > 1 means genuinely duplicated — either the same file defines it twice,
        // or two or more distinct files each define it at least once.
        // Built in BuildSourceLocationIndex; patched incrementally in UpdateForFileChange.
        private readonly Dictionary<string, Dictionary<string, int>> _duplicates;

        // Serializes reads of _sourceLocations against concurrent UpdateForFileChange writes.
        private readonly object _sourceLock = new object();

        /// <summary>
        /// Initializes a new lazy provider from an already-loaded <paramref name="model"/>.
        /// </summary>
        public TSqlModelMetadataProvider(TSqlModel model, string databaseName)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            _model = model;
            _server = new TSqlModelServer(model, databaseName);
            _fileToObjects = new Dictionary<string, HashSet<QualifiedSqlObject>>(StringComparer.OrdinalIgnoreCase);
            _duplicates = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            _sourceLocations = BuildSourceLocationIndex();
        }

        /// <summary>
        /// Builds the source location index by scanning all user-defined objects in the model.
        /// Called eagerly during construction to enable instant Go to Definition responses.
        /// </summary>
        private Dictionary<string, SourceInformation> BuildSourceLocationIndex()
        {
            var index = new Dictionary<string, SourceInformation>(StringComparer.OrdinalIgnoreCase);

            foreach (TSqlObject obj in _model.GetObjects(DacQueryScopes.UserDefined))
            {
                if (obj.Name?.Parts == null)
                    continue;

                string qualifiedName = string.Join(".", obj.Name.Parts);
                SourceInformation? sourceInfo = obj.GetSourceInformation();

                if (sourceInfo?.SourceName != null)
                {
                    index[qualifiedName] = sourceInfo;

                    // Count occurrences per file: same file defining same object twice → count = 2 → duplicate.
                    if (!_duplicates.TryGetValue(qualifiedName, out Dictionary<string, int>? fileCounts))
                        _duplicates[qualifiedName] = fileCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    fileCounts.TryGetValue(sourceInfo.SourceName, out int existing2);
                    fileCounts[sourceInfo.SourceName] = existing2 + 1;

                    // Track all incrementally-managed object types per source file.
                    if (obj.Name.Parts.Count >= 2 && TryGetSqlObjectType(obj.ObjectType, out SqlObjectType sqlType))
                    {
                        string sourceName = sourceInfo.SourceName;
                        if (!_fileToObjects.TryGetValue(sourceName, out HashSet<QualifiedSqlObject>? set))
                        {
                            set = new HashSet<QualifiedSqlObject>();
                            _fileToObjects[sourceName] = set;
                        }
                        set.Add(new QualifiedSqlObject(obj.Name.Parts[0], obj.Name.Parts[1], sqlType));
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// Retrieves source file information for a schema-qualified object name.
        /// </summary>
        /// <param name="qualifiedName">Schema-qualified name (e.g., "dbo.Orders").</param>
        /// <param name="sourceInfo">Source information if found; otherwise null.</param>
        /// <returns>True if source information was found; otherwise false.</returns>
        public bool TryGetSourceInformation(string qualifiedName, out SourceInformation? sourceInfo)
        {
            lock (_sourceLock)
            {
                return _sourceLocations.TryGetValue(qualifiedName, out sourceInfo);
            }
        }

        /// <summary>
        /// Incrementally updates the provider after a SQL file in a project is saved or deleted.
        /// Must be called AFTER <c>model.AddOrUpdateObjects</c> or <c>model.DeleteObjects</c>.
        /// </summary>
        /// <param name="sourceName">Canonical file path passed to DacFx (must match the path used during initial load).</param>
        /// <param name="deleted">True when the file was deleted (<c>DeleteObjects</c> was called).</param>
        public void UpdateForFileChange(string sourceName, bool deleted)
        {
            // ── Step 1: Compute old and new object sets; collect source info and counts in one scan ─
            HashSet<QualifiedSqlObject> oldSet =
                _fileToObjects.TryGetValue(sourceName, out HashSet<QualifiedSqlObject>? existing)
                    ? existing
                    : new HashSet<QualifiedSqlObject>();

            HashSet<QualifiedSqlObject> newSet;
            // qualName → SourceInformation for this file (last-write-wins, used for Go-to-Def)
            var newSourceLocations = new Dictionary<string, SourceInformation>(StringComparer.OrdinalIgnoreCase);
            // qualName → occurrence count in this file (for duplicate detection)
            var newCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Seeds for the dependency-invalidation BFS in Step 2b (populated only for non-deleted updates).
            var bfsSeeds = new List<TSqlObject>();

            if (deleted)
            {
                newSet = new HashSet<QualifiedSqlObject>();
            }
            else
            {
                newSet = new HashSet<QualifiedSqlObject>();
                foreach (TSqlObject obj in _model.GetObjects(DacQueryScopes.UserDefined))
                {
                    if (obj.Name?.Parts == null) continue;
                    SourceInformation? si = obj.GetSourceInformation();
                    if (si?.SourceName == null ||
                        !string.Equals(si.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string qualName = string.Join(".", obj.Name.Parts);
                    newSourceLocations[qualName] = si;
                    newCounts.TryGetValue(qualName, out int c);
                    newCounts[qualName] = c + 1;

                    if (obj.Name.Parts.Count >= 2 && TryGetSqlObjectType(obj.ObjectType, out SqlObjectType sqlType))
                    {
                        newSet.Add(new QualifiedSqlObject(obj.Name.Parts[0], obj.Name.Parts[1], sqlType));
                        bfsSeeds.Add(obj);
                    }
                }
            }

            // ── Step 2: Apply per-object wrapper updates (NO collection rebuild) ──────
            TSqlModelDatabase db = _server.Database;

            foreach (QualifiedSqlObject q in oldSet)
                if (!newSet.Contains(q))
                    db.GetSchema(q.SchemaName)?.RemoveObject(q.ObjectName, q.ObjectType);

            foreach (QualifiedSqlObject q in newSet)
                if (!oldSet.Contains(q))
                    db.EnsureSchema(q.SchemaName).AddObject(q.ObjectName, q.ObjectType);

            foreach (QualifiedSqlObject q in newSet)
                if (oldSet.Contains(q))
                    db.GetSchema(q.SchemaName)?.ResetObject(q.ObjectName, q.ObjectType);

            // ── Step 2b: BFS-invalidate transitive dependents of updated objects ───────
            // Walk GetReferencing() from every updated object to find all dependent views,
            // functions, and procedures (potentially chained across multiple levels) and
            // reset their lazy wrappers so cached metadata is re-fetched on next access.
            // All state is method-local; nothing is stored globally.
            if (bfsSeeds.Count > 0)
            {
                // Pre-seed visited with the changed file's own objects so we never re-reset
                // them here (they were already handled in Step 2 above).
                var visitedQualNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (QualifiedSqlObject q in newSet)
                    visitedQualNames.Add(q.SchemaName + "." + q.ObjectName);

                var bfsQueue = new Queue<TSqlObject>(bfsSeeds);
                while (bfsQueue.Count > 0)
                {
                    TSqlObject current = bfsQueue.Dequeue();
                    foreach (TSqlObject dep in current.GetReferencing(DacQueryScopes.UserDefined))
                    {
                        if (dep.Name?.Parts == null || dep.Name.Parts.Count < 2) continue;
                        string depQn = dep.Name.Parts[0] + "." + dep.Name.Parts[1];
                        if (!visitedQualNames.Add(depQn)) continue;  // already processed → skip

                        // Clear cached columns/params so the next IntelliSense request re-fetches.
                        if (TryGetSqlObjectType(dep.ObjectType, out SqlObjectType depType))
                            db.GetSchema(dep.Name.Parts[0])?.ResetObject(dep.Name.Parts[1], depType);

                        bfsQueue.Enqueue(dep);
                    }
                }
            }

            // ── Step 3: Patch _sourceLocations and _duplicates under one lock ──────────
            lock (_sourceLock)
            {
                // Clean up _sourceLocations by scanning for entries pointing to this file.
                var staleKeys = _sourceLocations
                    .Where(kv => kv.Value.SourceName != null &&
                                 string.Equals(kv.Value.SourceName, sourceName,
                                     StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (string key in staleKeys)
                    _sourceLocations.Remove(key);

                // Remove this file's previous counts from every duplicate bucket before applying
                // newCounts. This is keyed by exact qualified-name entries currently tracked in
                // _duplicates, so it cannot miss objects that weren't represented in oldSet.
                var duplicateKeysToRemove = new List<string>();
                foreach (var kv in _duplicates)
                {
                    Dictionary<string, int> fileCounts = kv.Value;
                    fileCounts.Remove(sourceName);
                    if (fileCounts.Count == 0)
                    {
                        duplicateKeysToRemove.Add(kv.Key);
                    }
                }
                foreach (string key in duplicateKeysToRemove)
                {
                    _duplicates.Remove(key);
                }

                // Apply new source locations and occurrence counts gathered in Step 1 — no second model scan.
                foreach (var kv in newSourceLocations)
                    _sourceLocations[kv.Key] = kv.Value;

                foreach (var kv in newCounts)
                {
                    if (!_duplicates.TryGetValue(kv.Key, out Dictionary<string, int>? fileCounts))
                        _duplicates[kv.Key] = fileCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    fileCounts[sourceName] = kv.Value;
                }
            }

            // ── Step 4: Update file-to-objects mapping ───────────────────────────────
            _fileToObjects[sourceName] = newSet;
        }

        /// <summary>
        /// Returns true if <paramref name="name"/> has a total definition count greater than 1 —
        /// either the same file defines it twice, or two or more files each define it at least once.
        /// Built once at project open; patched incrementally on each save.
        /// Per call cost is O(f) where f is the number of source files that define the requested object.
        /// </summary>
        public bool IsDuplicate(string name)
        {
            lock (_sourceLock)
            {
                // Direct lookup for schema-qualified names (e.g. "dbo.Foo").
                if (_duplicates.TryGetValue(name, out Dictionary<string, int>? fileCounts) && HasMultipleDefinitions(fileCounts))
                    return true;
                // Bare name (e.g. "Foo") from binder — DacFx always stores as "dbo.Foo".
                if (!name.Contains('.'))
                    return _duplicates.TryGetValue("dbo." + name, out fileCounts) && HasMultipleDefinitions(fileCounts);
                return false;
            }
        }

        private static bool HasMultipleDefinitions(Dictionary<string, int>? fileCounts)
        {
            if (fileCounts == null)
            {
                return false;
            }

            int total = 0;
            foreach (int count in fileCounts.Values)
            {
                total += count;
                if (total > 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the <see cref="TSqlObject"/> whose schema-qualified name matches
        /// <paramref name="qualifiedName"/> (case-insensitive), or <c>null</c> if not found.
        /// </summary>
        public TSqlObject? FindObject(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
                return null;
            return _model.GetObjects(DacQueryScopes.UserDefined)
                .FirstOrDefault(o =>
                    o.Name?.Parts != null &&
                    string.Join(".", o.Name.Parts).Equals(qualifiedName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the distinct source file paths of all objects that directly reference
        /// <paramref name="qualifiedName"/> according to the DacFx dependency graph.
        /// Returns an empty sequence when the object cannot be resolved in the model.
        /// <para>
        /// When <paramref name="qualifiedName"/> is a top-level object (table/view/procedure), the
        /// result includes references to the object itself AND references to each of its columns,
        /// because column-level references (e.g. an extended property on a column) attach to the
        /// column object, not to the parent. When <paramref name="qualifiedName"/> is a column
        /// (e.g. <c>dbo.Table.Column</c>), the column's own referencing files are returned.
        /// </para>
        /// <para>
        /// Uses <see cref="DacQueryScopes.All"/> (not <see cref="DacQueryScopes.UserDefined"/>) so
        /// that extended properties — emitted as <c>sp_addextendedproperty</c> in their own file —
        /// are included. DacFx does not classify extended properties as user-defined, so the narrower
        /// scope would drop a separate extended-property file from the rename candidate set. System
        /// objects surfaced by the wider scope have no source file and are filtered out below.
        /// </para>
        /// </summary>
        public IEnumerable<string> GetReferencingFilePaths(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
                return Enumerable.Empty<string>();

            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            TSqlObject? topLevel = FindObject(qualifiedName);
            if (topLevel != null)
            {
                // Table/view/proc: its own references plus every column's references.
                CollectReferencingSourceFiles(topLevel, found);
                foreach (TSqlObject column in GetColumnsOf(qualifiedName))
                    CollectReferencingSourceFiles(column, found);
            }
            else
            {
                // Not a top-level object — resolve it as a column. Match the fully qualified
                // name first (e.g. "dbo.Table.Column"); if the name is unqualified (e.g. just
                // "Column"), fall back to every column that shares that final name part so the
                // referencing files of the intended column are not missed.
                foreach (TSqlObject column in FindColumns(qualifiedName))
                    CollectReferencingSourceFiles(column, found);
            }

            return found;
        }

        /// <summary>
        /// Adds the source file of every object that directly references <paramref name="obj"/>
        /// (using <see cref="DacQueryScopes.All"/>) into <paramref name="found"/>. System objects
        /// with no source file are skipped.
        /// </summary>
        private static void CollectReferencingSourceFiles(TSqlObject obj, HashSet<string> found)
        {
            foreach (TSqlObject dep in obj.GetReferencing(DacQueryScopes.All))
            {
                string? path = dep.GetSourceInformation()?.SourceName;
                if (path != null)
                    found.Add(path);
            }
        }

        /// <summary>
        /// Returns the column objects belonging to the table whose schema-qualified name is
        /// <paramref name="tableQualifiedName"/> (e.g. all columns of "dbo.Orders"). Columns are
        /// not top-level types in DacFx and cannot be queried via <c>GetObjects</c>; they are
        /// reached as composing children of the table via <see cref="TSqlObject.GetChildren()"/>.
        /// </summary>
        private IEnumerable<TSqlObject> GetColumnsOf(string tableQualifiedName)
        {
            TSqlObject? table = FindObject(tableQualifiedName);
            return table == null ? Enumerable.Empty<TSqlObject>() : GetColumnChildren(table);
        }

        /// <summary>
        /// Returns the child objects of <paramref name="table"/> that are columns.
        /// </summary>
        private static IEnumerable<TSqlObject> GetColumnChildren(TSqlObject table)
        {
            foreach (TSqlObject child in table.GetChildren(DacQueryScopes.All))
            {
                if (child.ObjectType == ModelSchema.Column)
                    yield return child;
            }
        }

        /// <summary>
        /// Resolves a column reference to one or more <see cref="TSqlObject"/> columns. A fully
        /// qualified name (e.g. "dbo.Table.Column") resolves to the matching column of that table;
        /// an unqualified name (e.g. "Column") resolves to every column across all user-defined
        /// objects whose final name part matches. Columns are reached via the parent's
        /// <see cref="TSqlObject.GetChildren()"/> because they are not top-level DacFx types.
        /// </summary>
        private IEnumerable<TSqlObject> FindColumns(string columnName)
        {
            int lastDot = columnName.LastIndexOf('.');
            if (lastDot > 0)
            {
                // Qualified: resolve the parent table, then match the trailing column name part.
                string parentName = columnName.Substring(0, lastDot);
                string lastPart = columnName.Substring(lastDot + 1);
                TSqlObject? table = FindObject(parentName);
                if (table == null)
                    yield break;
                foreach (TSqlObject column in GetColumnChildren(table))
                {
                    if (column.Name?.Parts != null && column.Name.Parts.Count > 0 &&
                        string.Equals(column.Name.Parts[column.Name.Parts.Count - 1], lastPart, StringComparison.OrdinalIgnoreCase))
                        yield return column;
                }
                yield break;
            }

            // Unqualified: scan every user-defined object's columns for a matching final name part.
            foreach (TSqlObject obj in _model.GetObjects(DacQueryScopes.UserDefined))
            {
                foreach (TSqlObject column in GetColumnChildren(obj))
                {
                    if (column.Name?.Parts != null && column.Name.Parts.Count > 0 &&
                        string.Equals(column.Name.Parts[column.Name.Parts.Count - 1], columnName, StringComparison.OrdinalIgnoreCase))
                        yield return column;
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="qualifiedName"/> resolves to a top-level object
        /// (table/view/procedure/function) or to a column. Used to normalize names that may carry a
        /// database prefix or be column references.
        /// </summary>
        public bool CanResolveName(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
                return false;
            lock (_sourceLock)
            {
                if (_sourceLocations.ContainsKey(qualifiedName))
                    return true;
            }
            return FindColumns(qualifiedName).Any();
        }

        /// <summary>
        /// Returns the source file that defines <paramref name="qualifiedName"/>, resolving both
        /// top-level objects and columns. A column resolves to its own source file, or, if DacFx
        /// reports none, to the parent table's file. Returns <c>null</c> when unresolved.
        /// </summary>
        public string? GetDefiningFilePath(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
                return null;

            lock (_sourceLock)
            {
                if (_sourceLocations.TryGetValue(qualifiedName, out SourceInformation? si) && si?.SourceName != null)
                    return si.SourceName;
            }

            // Column (qualified or unqualified): use the column's own source file, falling back to
            // the parent table's file when DacFx reports no source for the column itself.
            TSqlObject? column = FindColumns(qualifiedName).FirstOrDefault();
            string? columnSource = column?.GetSourceInformation()?.SourceName;
            if (columnSource != null)
                return columnSource;

            if (column?.Name?.Parts != null && column.Name.Parts.Count >= 2)
            {
                string parentName = string.Join(".", column.Name.Parts.Take(column.Name.Parts.Count - 1));
                lock (_sourceLock)
                {
                    if (_sourceLocations.TryGetValue(parentName, out SourceInformation? parentSi))
                        return parentSi?.SourceName;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves an unqualified name to its full schema-qualified name by finding the first
        /// user-defined object whose last name part matches <paramref name="lastPart"/>
        /// (case-insensitive). Returns the joined full name (e.g. "dbo.Customers") or
        /// <c>null</c> if no match is found.
        /// </summary>
        public string? FindQualifiedNameByLastPart(string lastPart)
        {
            if (string.IsNullOrEmpty(lastPart))
                return null;
            var obj = _model.GetObjects(DacQueryScopes.UserDefined)
                .FirstOrDefault(o =>
                    o.Name?.Parts != null && o.Name.Parts.Count > 0 &&
                    string.Equals(o.Name.Parts[o.Name.Parts.Count - 1], lastPart, StringComparison.OrdinalIgnoreCase));
            return obj?.Name?.Parts != null ? string.Join(".", obj.Name.Parts) : null;
        }

        private static bool TryGetSqlObjectType(ModelTypeClass modelType, out SqlObjectType sqlType)
        {
            if (modelType == ModelSchema.Table)               { sqlType = SqlObjectType.Table;                return true; }
            if (modelType == ModelSchema.View)                { sqlType = SqlObjectType.View;                 return true; }
            if (modelType == ModelSchema.Procedure)           { sqlType = SqlObjectType.StoredProcedure;      return true; }
            if (modelType == ModelSchema.ScalarFunction)      { sqlType = SqlObjectType.ScalarFunction;       return true; }
            if (modelType == ModelSchema.TableValuedFunction) { sqlType = SqlObjectType.TableValuedFunction;  return true; }
            if (modelType == ModelSchema.DataType)            { sqlType = SqlObjectType.UserDefinedDataType;  return true; }
            if (modelType == ModelSchema.TableType)           { sqlType = SqlObjectType.UserDefinedTableType; return true; }
            sqlType = default;
            return false;
        }

        /// <inheritdoc/>
        public override IServer Server => _server;

        /// <inheritdoc/>
        public override MetadataProviderEventHandler? AfterBindHandler => null;

        /// <inheritdoc/>
        public override MetadataProviderEventHandler? BeforeBindHandler => null;
    }
}
