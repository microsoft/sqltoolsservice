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
        private readonly TSqlModel _model;
        private readonly TSqlModelDatabase _database;

        // The project database followed by one database per other-database reference.
        // Replaced as a whole when references change so readers never observe a partial update.
        private volatile IDatabase[] _databases;

        public TSqlModelServer(TSqlModel model, string databaseName)
        {
            _model = model;
            _database = new TSqlModelDatabase(this, model, databaseName);
            _databases = new IDatabase[] { _database };
        }

        internal TSqlModelDatabase Database => _database;

        public string Name => string.Empty;
        public bool IsSystemObject => false;
        public IDatabaseObject Parent => null!;
        public CollationInfo CollationInfo => CollationInfo.Default;

        public IMetadataCollection<IDatabase> Databases
        {
            get
            {
                IDatabase[] snapshot = _databases;
                return new LazyCollection<IDatabase>(() => snapshot);
            }
        }

        /// <summary>
        /// Replaces the databases exposed for the project's other-database references. Every name of a
        /// referenced database becomes a database, so both [$(OtherDb)] and the variable's value resolve.
        /// Names that match the project database are ignored, and references that share a name are merged.
        /// </summary>
        internal void SetReferencedDatabases(IEnumerable<ProjectReferencedDatabase> referencedDatabases)
        {
            var objectsByName = new Dictionary<string, List<TSqlObject>>(StringComparer.OrdinalIgnoreCase);
            foreach (ProjectReferencedDatabase referencedDatabase in referencedDatabases)
            {
                foreach (string name in referencedDatabase.Names)
                {
                    if (string.IsNullOrEmpty(name) || string.Equals(name, _database.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!objectsByName.TryGetValue(name, out List<TSqlObject>? objects))
                    {
                        objects = new List<TSqlObject>();
                        objectsByName[name] = objects;
                    }

                    objects.AddRange(referencedDatabase.Objects);
                }
            }

            var databases = new List<IDatabase>(objectsByName.Count + 1) { _database };
            foreach (KeyValuePair<string, List<TSqlObject>> entry in objectsByName)
            {
                databases.Add(new TSqlModelDatabase(this, _model, entry.Key, entry.Value));
            }

            _databases = databases.ToArray();
        }

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
        /// <summary>
        /// Scopes the project database draws objects from, in precedence order: the project's own objects win
        /// over objects from same-database references, which win over master's system objects and built-ins.
        /// Objects from references are not system objects unless they come from master.
        /// </summary>
        private static readonly (DacQueryScopes Scope, bool IsUserDefined)[] ProjectObjectScopes =
        {
            (DacQueryScopes.UserDefined, true),
            (DacQueryScopes.SameDatabase, true),
            (DacQueryScopes.System, false),
            (DacQueryScopes.BuiltIn, false),
        };

        private readonly TSqlModelServer _server;
        private readonly TSqlModel _model;
        private readonly string _name;

        // Objects of an other-database reference; null for the project database.
        private readonly IReadOnlyList<TSqlObject>? _referencedObjects;

        // Mutable schema map — supports EnsureSchema for incremental adds without rebuilding.
        private Dictionary<string, TSqlModelSchema>? _schemaMap;
        private readonly object _schemaLock = new object();

        private readonly DatabaseCompatibilityLevel _compatLevel;

        public TSqlModelDatabase(TSqlModelServer server, TSqlModel model, string name)
            : this(server, model, name, referencedObjects: null)
        {
        }

        /// <summary>
        /// Creates a database for an other-database reference, backed by the objects loaded from that reference.
        /// </summary>
        internal TSqlModelDatabase(TSqlModelServer server, TSqlModel model, string name, IReadOnlyList<TSqlObject>? referencedObjects)
        {
            _server = server;
            _model = model;
            _name = name;
            _referencedObjects = referencedObjects;
            _compatLevel = MapCompatibilityLevel(model.Version);
        }

        /// <summary>
        /// Returns the objects of <paramref name="type"/> in this database, in precedence order,
        /// with whether each one is user defined (as opposed to a system object).
        /// </summary>
        internal IEnumerable<(TSqlObject Obj, bool IsUserDefined)> GetObjects(ModelTypeClass type)
        {
            if (_referencedObjects != null)
            {
                foreach (TSqlObject obj in _referencedObjects)
                {
                    if (obj.ObjectType == type)
                    {
                        yield return (obj, true);
                    }
                }

                yield break;
            }

            foreach ((DacQueryScopes scope, bool isUserDefined) in ProjectObjectScopes)
            {
                foreach (TSqlObject obj in _model.GetObjects(scope, type))
                {
                    yield return (obj, isUserDefined);
                }
            }
        }

        /// <summary>
        /// Drops the schema map so it is rebuilt from the model on next access,
        /// for example after the model's database references changed.
        /// </summary>
        internal void ResetSchemaMap()
        {
            lock (_schemaLock)
            {
                _schemaMap = null;
            }
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
                lock (_schemaLock)
                {
                    // Snapshot the current values; the binder is recreated after every incremental
                    // update so a per-call snapshot is always fresh enough.
                    var snapshot = EnsureSchemaMap().Values.ToArray<ISchema>();
                    return new LazyCollection<ISchema>(() => snapshot);
                }
            }
        }

        /// <summary>
        /// Returns the schema map, building it on first use. Callers must hold <see cref="_schemaLock"/>,
        /// because <see cref="ResetSchemaMap"/> can drop the map at any time.
        /// </summary>
        private Dictionary<string, TSqlModelSchema> EnsureSchemaMap()
        {
            return _schemaMap ??= BuildSchemaMap();
        }

        private Dictionary<string, TSqlModelSchema> BuildSchemaMap()
        {
            var map = new Dictionary<string, TSqlModelSchema>(StringComparer.OrdinalIgnoreCase);

            if (_referencedObjects != null)
            {
                foreach (TSqlObject obj in _referencedObjects)
                {
                    // Schema objects are named by their schema; everything else is [schema].[name].
                    string? schemaName = obj.ObjectType == ModelSchema.Schema
                        ? obj.Name.Parts.FirstOrDefault()
                        : obj.Name.Parts.Count >= 2 ? obj.Name.Parts[0] : null;
                    if (schemaName != null && !map.ContainsKey(schemaName))
                        map[schemaName] = new TSqlModelSchema(this, schemaName, DacQueryScopes.UserDefined);
                }

                return map;
            }

            // 1) Source of truth: user-defined objects
            foreach (var obj in _model.GetObjects(DacQueryScopes.UserDefined))
            {
                if (obj.Name.Parts.Count >= 2)
                {
                    var schemaName = obj.Name.Parts[0];
                    if (!map.ContainsKey(schemaName))
                        map[schemaName] = new TSqlModelSchema(this, schemaName, DacQueryScopes.UserDefined);
                }
            }

            // 2) Explicit CREATE SCHEMA (edge case: empty schema)
            foreach (var s in _model.GetObjects(DacQueryScopes.UserDefined, ModelSchema.Schema))
            {
                var schemaName = s.Name.Parts[0];
                if (!map.ContainsKey(schemaName))
                    map[schemaName] = new TSqlModelSchema(this, schemaName, DacQueryScopes.UserDefined);
            }

            // 3) Schemas of objects from same-database references and from master (sys, INFORMATION_SCHEMA, ...)
            foreach (var (scope, schemaScope) in new[]
            {
                (DacQueryScopes.SameDatabase, DacQueryScopes.UserDefined),
                (DacQueryScopes.System, DacQueryScopes.BuiltIn),
            })
            {
                foreach (var obj in _model.GetObjects(scope))
                {
                    string? schemaName = obj.ObjectType == ModelSchema.Schema
                        ? obj.Name.Parts.FirstOrDefault()
                        : obj.Name.Parts.Count >= 2 ? obj.Name.Parts[0] : null;
                    if (schemaName != null && !map.ContainsKey(schemaName))
                        map[schemaName] = new TSqlModelSchema(this, schemaName, schemaScope);
                }
            }

            // 4) Built-in schemas only if not already claimed by user objects above
            foreach (var s in _model.GetObjects(DacQueryScopes.BuiltIn, ModelSchema.Schema))
            {
                var schemaName = s.Name.Parts[0];
                if (!map.ContainsKey(schemaName))
                    map[schemaName] = new TSqlModelSchema(this, schemaName, DacQueryScopes.BuiltIn);
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
            lock (_schemaLock)
            {
                Dictionary<string, TSqlModelSchema> map = EnsureSchemaMap();
                if (!map.TryGetValue(schemaName, out var schema))
                {
                    schema = new TSqlModelSchema(this, schemaName, DacQueryScopes.UserDefined);
                    map[schemaName] = schema;
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
            lock (_schemaLock)
            {
                TSqlModelSchema? schema = null;
                _schemaMap?.TryGetValue(schemaName, out schema);
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