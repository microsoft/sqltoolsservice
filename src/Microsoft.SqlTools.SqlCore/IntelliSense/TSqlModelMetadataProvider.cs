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
            // ── Step 1: Compute old and new object sets ────────────────────────────────
            HashSet<QualifiedSqlObject> oldSet =
                _fileToObjects.TryGetValue(sourceName, out HashSet<QualifiedSqlObject>? existing)
                    ? existing
                    : new HashSet<QualifiedSqlObject>();

            HashSet<QualifiedSqlObject> newSet;
            if (deleted)
            {
                newSet = new HashSet<QualifiedSqlObject>();
            }
            else
            {
                newSet = new HashSet<QualifiedSqlObject>();
                foreach (TSqlObject obj in _model.GetObjects(DacQueryScopes.UserDefined))
                {
                    if (obj.Name?.Parts == null || obj.Name.Parts.Count < 2) continue;
                    if (!TryGetSqlObjectType(obj.ObjectType, out SqlObjectType sqlType)) continue;
                    SourceInformation? si = obj.GetSourceInformation();
                    if (si?.SourceName != null &&
                        string.Equals(si.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        newSet.Add(new QualifiedSqlObject(obj.Name.Parts[0], obj.Name.Parts[1], sqlType));
                    }
                }
            }

            // ── Step 2: Apply per-object wrapper updates (NO collection rebuild) ──────
            TSqlModelDatabase db = _server.Database;

            foreach (QualifiedSqlObject q in oldSet)
                if (!newSet.Contains(q))
                    ((TSqlModelSchema?)db.Schemas[q.SchemaName])?.RemoveObject(q.ObjectName, q.ObjectType);

            foreach (QualifiedSqlObject q in newSet)
                if (!oldSet.Contains(q))
                    ((TSqlModelSchema?)db.Schemas[q.SchemaName])?.AddObject(q.ObjectName, q.ObjectType);

            foreach (QualifiedSqlObject q in newSet)
                if (oldSet.Contains(q))
                    ((TSqlModelSchema?)db.Schemas[q.SchemaName])?.ResetObject(q.ObjectName, q.ObjectType);

            // ── Step 3: Patch source location index (F12) ─────────────────────────
            lock (_sourceLock)
            {
                var staleKeys = _sourceLocations
                    .Where(kv => kv.Value.SourceName != null &&
                                 string.Equals(kv.Value.SourceName, sourceName,
                                     StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (string key in staleKeys)
                    _sourceLocations.Remove(key);

                if (!deleted)
                {
                    foreach (TSqlObject obj in _model.GetObjects(DacQueryScopes.UserDefined))
                    {
                        if (obj.Name?.Parts == null) continue;
                        SourceInformation? si = obj.GetSourceInformation();
                        if (si?.SourceName != null &&
                            string.Equals(si.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            _sourceLocations[string.Join(".", obj.Name.Parts)] = si;
                        }
                    }
                }
            }

            // ── Step 4: Update file-to-objects mapping ───────────────────────────────
            _fileToObjects[sourceName] = newSet;
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
