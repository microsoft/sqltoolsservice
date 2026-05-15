//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// SSDT counterpart: MetadataProvider/Schema.cs → TSqlModelSchema

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// Lazy <see cref="ISchema"/> wrapper backed by <see cref="TSqlModel"/>.
    /// Each collection (Tables, Views, etc.) is populated on first access only.
    /// <para>
    /// Object collections query both UserDefined and BuiltIn scopes separately, so each object
    /// wrapper (TSqlModelTable, etc.) knows its true scope regardless of which scope the schema
    /// was originally created in. This correctly handles schemas like 'dbo' that exist in both scopes.
    /// </para>
    /// </summary>
    internal sealed class TSqlModelSchema : ISchema
    {
        private readonly TSqlModelDatabase _database;
        private readonly TSqlModel _model;
        private readonly string _name;
        private readonly DacQueryScopes _scope;

        // ── Backing fields — each collection is created once in the ctor
        // (just stores the loader delegate; no TSqlModel scan yet).
        private readonly IncrementalTableCollection                          _tables;
        private readonly IncrementalObjectCollection<IView>                  _views;
        private readonly IncrementalObjectCollection<IStoredProcedure>       _storedProcedures;
        private readonly IncrementalObjectCollection<IScalarValuedFunction>  _scalarValuedFunctions;
        private readonly IncrementalObjectCollection<ITableValuedFunction>   _tableValuedFunctions;
        private readonly IncrementalObjectCollection<IUserDefinedDataType>   _userDefinedDataTypes;
        private readonly IncrementalObjectCollection<IUserDefinedTableType>  _userDefinedTableTypes;

        public TSqlModelSchema(TSqlModelDatabase database, TSqlModel model, string schemaName,
            DacQueryScopes scope = DacQueryScopes.UserDefined)
        {
            _database = database;
            _model    = model;
            _name     = schemaName;
            _scope    = scope;

            // Capture 'this' — safe because LazyCollection doesn't call back until enumerated.
            // Query UserDefined and BuiltIn separately so each object knows its true scope.
            _tables = new IncrementalTableCollection(
                this,
                model,
                schemaName,
                (schema, obj, isUserDefined) => new TSqlModelTable(schema, obj, isUserDefined));

            _views = new IncrementalObjectCollection<IView>(
                this, model, schemaName, ModelSchema.View,
                (schema, obj, isUserDefined) => new TSqlModelView(schema, obj, isUserDefined));

            _storedProcedures = new IncrementalObjectCollection<IStoredProcedure>(
                this, model, schemaName, ModelSchema.Procedure,
                (schema, obj, isUserDefined) => new TSqlModelStoredProcedure(schema, obj, isUserDefined));

            _scalarValuedFunctions = new IncrementalObjectCollection<IScalarValuedFunction>(
                this, model, schemaName, ModelSchema.ScalarFunction,
                (schema, obj, isUserDefined) => new TSqlModelScalarFunction(schema, obj, isUserDefined));

            _tableValuedFunctions = new IncrementalObjectCollection<ITableValuedFunction>(
                this, model, schemaName, ModelSchema.TableValuedFunction,
                (schema, obj, isUserDefined) => new TSqlModelTableValuedFunction(schema, obj, isUserDefined));

            _userDefinedDataTypes = new IncrementalObjectCollection<IUserDefinedDataType>(
                this, model, schemaName, ModelSchema.DataType,
                (schema, obj, isUserDefined) => new TSqlModelUserDefinedDataType(schema, ObjectName(obj), isUserDefined));

            _userDefinedTableTypes = new IncrementalObjectCollection<IUserDefinedTableType>(
                this, model, schemaName, ModelSchema.TableType,
                (schema, obj, isUserDefined) => new TSqlModelUserDefinedTableType(schema, ObjectName(obj), isUserDefined));
        }

        public string Name => _name;
        public bool IsSystemObject => _scope == DacQueryScopes.BuiltIn;
        public IDatabaseObject Parent => _database;
        public IDatabase Database => _database;

        // ── Lazy collections — return same cached instance every time ─────

        public IMetadataCollection<ITable>               Tables               => _tables;
        public IMetadataCollection<IView>                Views                => _views;
        public IMetadataCollection<IStoredProcedure>     StoredProcedures     => _storedProcedures;
        public IMetadataCollection<IScalarValuedFunction> ScalarValuedFunctions => _scalarValuedFunctions;
        public IMetadataCollection<ITableValuedFunction>  TableValuedFunctions  => _tableValuedFunctions;
        public IMetadataCollection<IUserDefinedDataType>  UserDefinedDataTypes  => _userDefinedDataTypes;
        public IMetadataCollection<IUserDefinedTableType> UserDefinedTableTypes => _userDefinedTableTypes;

        // ── Empty collections (not needed for IntelliSense completions) ───
        public IMetadataCollection<IUserDefinedAggregate> UserDefinedAggregates => LazyCollection<IUserDefinedAggregate>.Empty;
        public IMetadataCollection<IUserDefinedClrType> UserDefinedClrTypes => LazyCollection<IUserDefinedClrType>.Empty;
        public IMetadataCollection<IExtendedStoredProcedure> ExtendedStoredProcedures => LazyCollection<IExtendedStoredProcedure>.Empty;
        public IMetadataCollection<ISynonym> Synonyms => LazyCollection<ISynonym>.Empty;
        public IDatabasePrincipal? Owner => null;

        // ── Visitor pattern ──────────────────────────────────────────────
        public T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => default(T)!;
        public T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IDatabaseOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>Resets a single named object to re-fetch from the model on next access.</summary>
        internal void ResetObject(string name, SqlObjectType type)
        {
            switch (type)
            {
                case SqlObjectType.Table:                _tables.ResetTable(name);            break;
                case SqlObjectType.View:                 _views.Reset(name);                  break;
                case SqlObjectType.StoredProcedure:      _storedProcedures.Reset(name);       break;
                case SqlObjectType.ScalarFunction:       _scalarValuedFunctions.Reset(name);  break;
                case SqlObjectType.TableValuedFunction:  _tableValuedFunctions.Reset(name);   break;
                case SqlObjectType.UserDefinedDataType:  _userDefinedDataTypes.Reset(name);   break;
                case SqlObjectType.UserDefinedTableType: _userDefinedTableTypes.Reset(name);  break;
            }
        }

        /// <summary>Adds a new named object entry (after creation in the model).</summary>
        internal void AddObject(string name, SqlObjectType type)
        {
            switch (type)
            {
                case SqlObjectType.Table:                _tables.AddTable(name);              break;
                case SqlObjectType.View:                 _views.Add(name);                    break;
                case SqlObjectType.StoredProcedure:      _storedProcedures.Add(name);         break;
                case SqlObjectType.ScalarFunction:       _scalarValuedFunctions.Add(name);    break;
                case SqlObjectType.TableValuedFunction:  _tableValuedFunctions.Add(name);     break;
                case SqlObjectType.UserDefinedDataType:  _userDefinedDataTypes.Add(name);     break;
                case SqlObjectType.UserDefinedTableType: _userDefinedTableTypes.Add(name);    break;
            }
        }

        /// <summary>Removes a named object entry (after deletion from the model).</summary>
        internal void RemoveObject(string name, SqlObjectType type)
        {
            switch (type)
            {
                case SqlObjectType.Table:                _tables.RemoveTable(name);           break;
                case SqlObjectType.View:                 _views.Remove(name);                 break;
                case SqlObjectType.StoredProcedure:      _storedProcedures.Remove(name);      break;
                case SqlObjectType.ScalarFunction:       _scalarValuedFunctions.Remove(name); break;
                case SqlObjectType.TableValuedFunction:  _tableValuedFunctions.Remove(name);  break;
                case SqlObjectType.UserDefinedDataType:  _userDefinedDataTypes.Remove(name);  break;
                case SqlObjectType.UserDefinedTableType: _userDefinedTableTypes.Remove(name); break;
            }
        }

        private bool Match(TSqlObject obj) =>
            obj.Name.Parts.Count >= 2 &&
            string.Equals(obj.Name.Parts[0], _name, StringComparison.OrdinalIgnoreCase);

        private static string ObjectName(TSqlObject obj) =>
            obj.Name.Parts[obj.Name.Parts.Count - 1];

        /// <summary>
        /// Fetches the latest <see cref="TSqlModelTable"/> from the live model for a specific table.
        /// Used as the post-reset factory inside <see cref="IncrementalTableCollection"/>.
        /// </summary>
        internal TSqlModelTable? CreateTableFromModel(string tableName)
        {
            // Try UserDefined first, then BuiltIn
            foreach (var scope in new[] { DacQueryScopes.UserDefined, DacQueryScopes.BuiltIn })
            {
                foreach (var obj in _model.GetObjects(scope, ModelSchema.Table))
                {
                    if (obj.Name.Parts.Count >= 2 &&
                        string.Equals(obj.Name.Parts[0], _name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(obj.Name.Parts[obj.Name.Parts.Count - 1], tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        return new TSqlModelTable(this, obj, scope == DacQueryScopes.UserDefined);
                    }
                }
            }
            return null;
        }

        // ── Nested class: IncrementalTableCollection ─────────────────────────

        /// <summary>
        /// Per-table lazy wrapper collection that supports O(1) per-table replacement
        /// after an incremental DacFx model update. The backing map is null until first
        /// access so schemas that are never enumerated carry zero overhead.
        /// </summary>
        internal sealed class IncrementalTableCollection : IMetadataCollection<ITable>
        {
            private readonly TSqlModelSchema _schema;
            private readonly TSqlModel _model;
            private readonly string _schemaName;
            private readonly Func<TSqlModelSchema, TSqlObject, bool, TSqlModelTable> _tableFactory;

            // Null until first access — populated by EnsureInitialized().
            private volatile Dictionary<string, ResettableLazy<TSqlModelTable>>? _map;
            private readonly object _mapLock = new object();

            public IncrementalTableCollection(
                TSqlModelSchema schema,
                TSqlModel model,
                string schemaName,
                Func<TSqlModelSchema, TSqlObject, bool, TSqlModelTable> tableFactory)
            {
                _schema      = schema;
                _model       = model;
                _schemaName  = schemaName;
                _tableFactory = tableFactory;
            }

            private Dictionary<string, ResettableLazy<TSqlModelTable>> EnsureInitialized()
            {
                if (_map != null) return _map;
                lock (_mapLock)
                {
                    if (_map != null) return _map;

                    var map = new Dictionary<string, ResettableLazy<TSqlModelTable>>(
                        StringComparer.OrdinalIgnoreCase);

                    // Scan both scopes once; UserDefined first so it wins on name collision.
                    foreach (var (scope, isUserDefined) in new[]
                    {
                        (DacQueryScopes.UserDefined, true),
                        (DacQueryScopes.BuiltIn,     false)
                    })
                    {
                        foreach (var obj in _model.GetObjects(scope, ModelSchema.Table))
                        {
                            if (obj.Name.Parts.Count < 2) continue;
                            if (!string.Equals(obj.Name.Parts[0], _schemaName,
                                    StringComparison.OrdinalIgnoreCase)) continue;

                            string tableName = obj.Name.Parts[obj.Name.Parts.Count - 1];
                            if (map.ContainsKey(tableName)) continue; // UserDefined wins

                            // Seed the lazy with the already-fetched object; only after
                            // ResetTable will the factory switch to a fresh model query.
                            var captured = obj;
                            var capturedIsUserDefined = isUserDefined;
                            map[tableName] = new ResettableLazy<TSqlModelTable>(
                                () => _tableFactory(_schema, captured, capturedIsUserDefined));
                        }
                    }

                    _map = map;
                    return _map;
                }
            }

            // ── IMetadataCollection<ITable> ──────────────────────────────────

            public int Count => EnsureInitialized().Count;

#pragma warning disable CS8603
            public ITable this[string name]
            {
                get
                {
                    var map = EnsureInitialized();
                    return map.TryGetValue(name, out var lazy) ? lazy.Value : null;
                }
            }
#pragma warning restore CS8603

            public bool Contains(string name) => EnsureInitialized().ContainsKey(name);
            public bool Contains(ITable item) => EnsureInitialized().Values.Any(l => l.Value == item);

            public IEnumerable<ITable> FindAll(Predicate<ITable> predicate) =>
                EnsureInitialized().Values.Select(l => l.Value).Where(t => predicate(t));

            public IEnumerable<ITable> FindAll(string name) =>
                EnsureInitialized().TryGetValue(name, out var lazy)
                    ? new[] { (ITable)lazy.Value }
                    : Enumerable.Empty<ITable>();

            public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection =>
                new LazyCollection<IMetadataObject>(
                    () => EnsureInitialized().Values.Select(l => (IMetadataObject)l.Value));

            public IEnumerator<ITable> GetEnumerator() =>
                EnsureInitialized().Values.Select(l => (ITable)l.Value).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            // ── Per-table mutation methods ────────────────────────────────────

            /// <summary>
            /// Resets the wrapper for <paramref name="tableName"/> so the next access
            /// queries the live model via <see cref="TSqlModelSchema.CreateTableFromModel"/>.
            /// No-op if the map has not been initialized yet (lazy init will pick up the latest state).
            /// </summary>
            internal void ResetTable(string tableName)
            {
                var map = _map;
                if (map == null) return;
                if (map.TryGetValue(tableName, out var lazy))
                    lazy.Reset(() => _schema.CreateTableFromModel(tableName)!);
            }

            /// <summary>
            /// Adds a new wrapper entry for a table that was added to the model.
            /// No-op if map is not yet initialized (lazy init will include it).
            /// </summary>
            internal void AddTable(string tableName)
            {
                var map = _map;
                if (map == null) return;
                var captured = tableName;
                lock (_mapLock)
                {
                    // Re-read _map inside lock to avoid racing with EnsureInitialized
                    map = _map;
                    if (map == null) return;
                    var newMap = new Dictionary<string, ResettableLazy<TSqlModelTable>>(
                        map, StringComparer.OrdinalIgnoreCase);
                    newMap[captured] = new ResettableLazy<TSqlModelTable>(
                        () => _schema.CreateTableFromModel(captured)!);
                    _map = newMap;
                }
            }

            /// <summary>
            /// Removes the wrapper entry for a table that was deleted from the model.
            /// No-op if map is not yet initialized.
            /// </summary>
            internal void RemoveTable(string tableName)
            {
                var map = _map;
                if (map == null) return;
                lock (_mapLock)
                {
                    map = _map;
                    if (map == null) return;
                    var newMap = new Dictionary<string, ResettableLazy<TSqlModelTable>>(
                        map, StringComparer.OrdinalIgnoreCase);
                    newMap.Remove(tableName);
                    _map = newMap;
                }
            }
        }

        // ── Nested class: IncrementalObjectCollection<TInterface> ─────────────
        internal sealed class IncrementalObjectCollection<TInterface> : IMetadataCollection<TInterface>
            where TInterface : class, IMetadataObject
        {
            private readonly TSqlModelSchema _schema;
            private readonly TSqlModel _model;
            private readonly string _schemaName;
            private readonly ModelTypeClass _objectType;
            private readonly Func<TSqlModelSchema, TSqlObject, bool, TInterface> _objectFactory;

            private volatile Dictionary<string, ResettableLazy<TInterface>>? _map;
            private readonly object _mapLock = new object();

            public IncrementalObjectCollection(
                TSqlModelSchema schema,
                TSqlModel model,
                string schemaName,
                ModelTypeClass objectType,
                Func<TSqlModelSchema, TSqlObject, bool, TInterface> objectFactory)
            {
                _schema        = schema;
                _model         = model;
                _schemaName    = schemaName;
                _objectType    = objectType;
                _objectFactory = objectFactory;
            }

            private Dictionary<string, ResettableLazy<TInterface>> EnsureInitialized()
            {
                if (_map != null) return _map;
                lock (_mapLock)
                {
                    if (_map != null) return _map;

                    var map = new Dictionary<string, ResettableLazy<TInterface>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var (scope, isUserDefined) in new[]
                    {
                        (DacQueryScopes.UserDefined, true),
                        (DacQueryScopes.BuiltIn,     false)
                    })
                    {
                        foreach (var obj in _model.GetObjects(scope, _objectType))
                        {
                            if (obj.Name.Parts.Count < 2) continue;
                            if (!string.Equals(obj.Name.Parts[0], _schemaName,
                                    StringComparison.OrdinalIgnoreCase)) continue;

                            string objName = obj.Name.Parts[obj.Name.Parts.Count - 1];
                            if (map.ContainsKey(objName)) continue; // UserDefined wins

                            // Seed with already-fetched object; Reset switches to FetchFromModel.
                            var captured = obj;
                            var capturedIsUserDefined = isUserDefined;
                            map[objName] = new ResettableLazy<TInterface>(
                                () => _objectFactory(_schema, captured, capturedIsUserDefined));
                        }
                    }

                    _map = map;
                    return _map;
                }
            }

            private TInterface? FetchFromModel(string objectName)
            {
                foreach (var scope in new[] { DacQueryScopes.UserDefined, DacQueryScopes.BuiltIn })
                {
                    foreach (var obj in _model.GetObjects(scope, _objectType))
                    {
                        if (obj.Name.Parts.Count >= 2 &&
                            string.Equals(obj.Name.Parts[0], _schemaName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(obj.Name.Parts[obj.Name.Parts.Count - 1], objectName, StringComparison.OrdinalIgnoreCase))
                        {
                            return _objectFactory(_schema, obj, scope == DacQueryScopes.UserDefined);
                        }
                    }
                }
                return null;
            }

            public int Count => EnsureInitialized().Count;

#pragma warning disable CS8603
            public TInterface this[string name]
            {
                get
                {
                    var map = EnsureInitialized();
                    return map.TryGetValue(name, out var lazy) ? lazy.Value : null;
                }
            }
#pragma warning restore CS8603

            public bool Contains(string name) => EnsureInitialized().ContainsKey(name);
            public bool Contains(TInterface item) => EnsureInitialized().Values.Any(l => l.Value == item);

            public IEnumerable<TInterface> FindAll(Predicate<TInterface> predicate) =>
                EnsureInitialized().Values.Select(l => l.Value).Where(t => predicate(t));

            public IEnumerable<TInterface> FindAll(string name) =>
                EnsureInitialized().TryGetValue(name, out var lazy)
                    ? new[] { lazy.Value }
                    : Enumerable.Empty<TInterface>();

            public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection =>
                new LazyCollection<IMetadataObject>(
                    () => EnsureInitialized().Values.Select(l => (IMetadataObject)(object)l.Value));

            public IEnumerator<TInterface> GetEnumerator() =>
                EnsureInitialized().Values.Select(l => l.Value).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal void Reset(string objectName)
            {
                var map = _map;
                if (map == null) return;
                if (map.TryGetValue(objectName, out var lazy))
                    lazy.Reset(() => FetchFromModel(objectName)!);
            }

            internal void Add(string objectName)
            {
                var map = _map;
                if (map == null) return;
                var captured = objectName;
                lock (_mapLock)
                {
                    map = _map;
                    if (map == null) return;
                    var newMap = new Dictionary<string, ResettableLazy<TInterface>>(map, StringComparer.OrdinalIgnoreCase);
                    newMap[captured] = new ResettableLazy<TInterface>(() => FetchFromModel(captured)!);
                    _map = newMap;
                }
            }

            internal void Remove(string objectName)
            {
                var map = _map;
                if (map == null) return;
                lock (_mapLock)
                {
                    map = _map;
                    if (map == null) return;
                    var newMap = new Dictionary<string, ResettableLazy<TInterface>>(map, StringComparer.OrdinalIgnoreCase);
                    newMap.Remove(objectName);
                    _map = newMap;
                }
            }
        }
    }
}
