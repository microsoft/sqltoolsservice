//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// SSDT counterparts:
//   MetadataProvider/Server.cs   → TSqlModelServer
//   MetadataProvider/Database.cs → TSqlModelDatabase

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    // -------------------------------------------------------------------------
    // TSqlModelServer : IServer
    // -------------------------------------------------------------------------
    internal sealed class TSqlModelServer : IServer
    {
        private readonly TSqlModelDatabase _database;

        public TSqlModelServer(TSqlModel model, string databaseName)
        {
            _database = new TSqlModelDatabase(this, model, databaseName);
        }

        internal TSqlModelDatabase Database => _database;

        public string Name => string.Empty;
        public bool IsSystemObject => false;
        public IDatabaseObject Parent => null!;
        public CollationInfo CollationInfo => CollationInfo.Default;

        public IMetadataCollection<IDatabase> Databases =>
            new LazyCollection<IDatabase>(() => new IDatabase[] { _database });

        public IMetadataCollection<ICredential> Credentials => LazyCollection<ICredential>.Empty;
        public IMetadataCollection<ILogin> Logins => LazyCollection<ILogin>.Empty;
        public IMetadataCollection<IServerDdlTrigger> Triggers => LazyCollection<IServerDdlTrigger>.Empty;

        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // -------------------------------------------------------------------------
    // TSqlModelDatabase : IDatabase
    // NOTE: CollationInfo is hardcoded to Default — known gap for case-sensitive databases.
    // -------------------------------------------------------------------------
    internal sealed class TSqlModelDatabase : IDatabase
    {
        private readonly TSqlModelServer _server;
        private readonly TSqlModel _model;
        private readonly string _name;

        // Mutable schema map — supports EnsureSchema for incremental adds without rebuilding.
        private Dictionary<string, TSqlModelSchema>? _schemaMap;
        private readonly object _schemaLock = new object();

        private readonly DatabaseCompatibilityLevel _compatLevel;

        public TSqlModelDatabase(TSqlModelServer server, TSqlModel model, string name)
        {
            _server = server;
            _model = model;
            _name = name;
            _compatLevel = MapCompatibilityLevel(model.Version);
        }

        private static DatabaseCompatibilityLevel MapCompatibilityLevel(SqlServerVersion v) =>
            v switch
            {
                SqlServerVersion.Sql90 => DatabaseCompatibilityLevel.Version90,
                SqlServerVersion.Sql100 => DatabaseCompatibilityLevel.Version100,
                SqlServerVersion.Sql110 => DatabaseCompatibilityLevel.Version110,
                SqlServerVersion.Sql120 => DatabaseCompatibilityLevel.Version120,
                SqlServerVersion.Sql130 => DatabaseCompatibilityLevel.Version130,
                SqlServerVersion.Sql140 => DatabaseCompatibilityLevel.Version140,
                SqlServerVersion.Sql150 => DatabaseCompatibilityLevel.Version150,
                SqlServerVersion.Sql160 => DatabaseCompatibilityLevel.Version160,
                _ => DatabaseCompatibilityLevel.Version170,
            };

        public string Name => _name;
        public bool IsSystemObject => false;
        public IDatabaseObject Parent => _server;
        public CollationInfo CollationInfo => CollationInfo.Default;
        public DatabaseCompatibilityLevel CompatibilityLevel => _compatLevel;
        // Read by the SqlParser binder (DatabaseEx.DefaultSchema) to resolve unqualified names
        // e.g. "SELECT * FROM Orders" → looks up "Orders" in "dbo". If null, the binder falls
        // back to an empty schema and bare-name completions/hover silently return nothing.
        public string DefaultSchemaName => "dbo";
        public IUser? Owner => null;

        /// <summary>
        /// Returns a live view of all schemas. Each call reflects the current state of the
        /// schema map, including schemas added by <see cref="EnsureSchema"/> after first access.
        /// </summary>
        public IMetadataCollection<ISchema> Schemas
        {
            get
            {
                EnsureSchemaMap();
                lock (_schemaLock)
                {
                    // Snapshot the current values; the binder is recreated after every incremental
                    // update so a per-call snapshot is always fresh enough.
                    var snapshot = _schemaMap!.Values.ToArray<ISchema>();
                    return new LazyCollection<ISchema>(() => snapshot);
                }
            }
        }

        private void EnsureSchemaMap()
        {
            if (_schemaMap != null) return;
            lock (_schemaLock)
            {
                if (_schemaMap != null) return;
                _schemaMap = BuildSchemaMap();
            }
        }

        private Dictionary<string, TSqlModelSchema> BuildSchemaMap()
        {
            var map = new Dictionary<string, TSqlModelSchema>(StringComparer.OrdinalIgnoreCase);

            // 1) Source of truth: user-defined objects
            foreach (var obj in _model.GetObjects(DacQueryScopes.UserDefined))
            {
                if (obj.Name.Parts.Count >= 2)
                {
                    var schemaName = obj.Name.Parts[0];
                    if (!map.ContainsKey(schemaName))
                        map[schemaName] = new TSqlModelSchema(this, _model, schemaName, DacQueryScopes.UserDefined);
                }
            }

            // 2) Explicit CREATE SCHEMA (edge case: empty schema)
            foreach (var s in _model.GetObjects(DacQueryScopes.UserDefined, ModelSchema.Schema))
            {
                var schemaName = s.Name.Parts[0];
                if (!map.ContainsKey(schemaName))
                    map[schemaName] = new TSqlModelSchema(this, _model, schemaName, DacQueryScopes.UserDefined);
            }

            // 3) Built-in schemas only if not already claimed by user objects above
            foreach (var s in _model.GetObjects(DacQueryScopes.BuiltIn, ModelSchema.Schema))
            {
                var schemaName = s.Name.Parts[0];
                if (!map.ContainsKey(schemaName))
                    map[schemaName] = new TSqlModelSchema(this, _model, schemaName, DacQueryScopes.BuiltIn);
            }

            return map;
        }

        /// <summary>
        /// Returns the <see cref="TSqlModelSchema"/> for <paramref name="schemaName"/>,
        /// creating a new user-defined schema wrapper if one does not yet exist.
        /// Used by <see cref="TSqlModelMetadataProvider.UpdateForFileChange"/> to handle
        /// files that introduce objects in a schema not present at project-open time.
        /// </summary>
        internal TSqlModelSchema EnsureSchema(string schemaName)
        {
            EnsureSchemaMap();
            lock (_schemaLock)
            {
                if (!_schemaMap!.TryGetValue(schemaName, out var schema))
                {
                    schema = new TSqlModelSchema(this, _model, schemaName, DacQueryScopes.UserDefined);
                    _schemaMap[schemaName] = schema;
                }
                return schema;
            }
        }

        /// <summary>
        /// Returns the <see cref="TSqlModelSchema"/> for <paramref name="schemaName"/>,
        /// or null if the schema is not in the map. Used for remove/reset operations
        /// where creating a phantom schema would be incorrect.
        /// </summary>
        internal TSqlModelSchema? GetSchema(string schemaName)
        {
            if (_schemaMap == null) return null;
            lock (_schemaLock)
            {
                _schemaMap.TryGetValue(schemaName, out var schema);
                return schema;
            }
        }

        public IMetadataCollection<IApplicationRole> ApplicationRoles => LazyCollection<IApplicationRole>.Empty;
        public IMetadataCollection<IAsymmetricKey> AsymmetricKeys => LazyCollection<IAsymmetricKey>.Empty;
        public IMetadataCollection<ICertificate> Certificates => LazyCollection<ICertificate>.Empty;
        public IMetadataCollection<IDatabaseRole> Roles => LazyCollection<IDatabaseRole>.Empty;
        public IMetadataCollection<IDatabaseDdlTrigger> Triggers => LazyCollection<IDatabaseDdlTrigger>.Empty;
        public IMetadataCollection<IUser> Users => LazyCollection<IUser>.Empty;

        public IServer Server => _server;

        public T Accept<T>(IServerOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IServerOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }
}