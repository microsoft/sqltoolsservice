//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// SSDT counterparts:
//   MetadataProvider/Server.cs   → LazyModelServer
//   MetadataProvider/Database.cs → LazyModelDatabase

namespace Microsoft.SqlTools.ServiceLayer.IntelliSense
{
    // -------------------------------------------------------------------------
    // LazyModelServer : IServer
    // -------------------------------------------------------------------------
    internal sealed class LazyModelServer : IServer
    {
        private readonly LazyModelDatabase _database;

        public LazyModelServer(TSqlModel model, string databaseName)
        {
            _database = new LazyModelDatabase(this, model, databaseName);
        }

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
    // LazyModelDatabase : IDatabase
    // NOTE: CollationInfo is hardcoded to Default — known gap for case-sensitive databases.
    // -------------------------------------------------------------------------
    internal sealed class LazyModelDatabase : IDatabase
    {
        private readonly LazyModelServer _server;
        private readonly TSqlModel _model;
        private readonly string _name;
        private IMetadataCollection<ISchema>? _schemas;

        private readonly DatabaseCompatibilityLevel _compatLevel;

        public LazyModelDatabase(LazyModelServer server, TSqlModel model, string name)
        {
            _server      = server;
            _model       = model;
            _name        = name;
            _compatLevel = MapCompatibilityLevel(model.Version);
        }

        private static DatabaseCompatibilityLevel MapCompatibilityLevel(SqlServerVersion v) =>
            v switch
            {
                SqlServerVersion.Sql90  => DatabaseCompatibilityLevel.Version90,
                SqlServerVersion.Sql100 => DatabaseCompatibilityLevel.Version100,
                SqlServerVersion.Sql110 => DatabaseCompatibilityLevel.Version110,
                SqlServerVersion.Sql120 => DatabaseCompatibilityLevel.Version120,
                SqlServerVersion.Sql130 => DatabaseCompatibilityLevel.Version130,
                SqlServerVersion.Sql140 => DatabaseCompatibilityLevel.Version140,
                SqlServerVersion.Sql150 => DatabaseCompatibilityLevel.Version150,
                SqlServerVersion.Sql160 => DatabaseCompatibilityLevel.Version160,
                _                       => DatabaseCompatibilityLevel.Version170,  // Sql170 + future
            };

        public string Name => _name;
        public bool IsSystemObject => false;
        public IDatabaseObject Parent => _server;
        public CollationInfo CollationInfo => CollationInfo.Default;
        public DatabaseCompatibilityLevel CompatibilityLevel => _compatLevel;
        public string DefaultSchemaName => "dbo";
        public IUser? Owner => null;

        public IMetadataCollection<ISchema> Schemas => _schemas ??= BuildSchemas();

        private IMetadataCollection<ISchema> BuildSchemas()
        {
            // Query UserDefined and BuiltIn scopes separately so each LazyModelSchema receives
            // the correct scope for its child-object queries.
            // BuiltIn covers sys, INFORMATION_SCHEMA, dbo, guest, etc. — embedded in DacFx,
            // no server connection needed.
            var schemas = new List<ISchema>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (TSqlObject s in _model.GetObjects(DacQueryScopes.UserDefined, ModelSchema.Schema))
            {
                string schemaName = s.Name.Parts[0];
                if (seen.Add(schemaName))
                    schemas.Add(new LazyModelSchema(this, _model, schemaName, DacQueryScopes.UserDefined));
            }

            foreach (TSqlObject s in _model.GetObjects(DacQueryScopes.BuiltIn, ModelSchema.Schema))
            {
                string schemaName = s.Name.Parts[0];
                if (seen.Add(schemaName))
                    schemas.Add(new LazyModelSchema(this, _model, schemaName, DacQueryScopes.BuiltIn));
            }

            return new LazyCollection<ISchema>(schemas.ToArray);
        }

        // Empty collections — not needed for basic IntelliSense
        public IMetadataCollection<IApplicationRole> ApplicationRoles => LazyCollection<IApplicationRole>.Empty;
        public IMetadataCollection<IAsymmetricKey> AsymmetricKeys => LazyCollection<IAsymmetricKey>.Empty;
        public IMetadataCollection<ICertificate> Certificates => LazyCollection<ICertificate>.Empty;
        public IMetadataCollection<IDatabaseRole> Roles => LazyCollection<IDatabaseRole>.Empty;
        public IMetadataCollection<IDatabaseDdlTrigger> Triggers => LazyCollection<IDatabaseDdlTrigger>.Empty;
        public IMetadataCollection<IUser> Users => LazyCollection<IUser>.Empty;

        // IServerOwnedObject
        public IServer Server => _server;
        public T Accept<T>(IServerOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IServerOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }
}
