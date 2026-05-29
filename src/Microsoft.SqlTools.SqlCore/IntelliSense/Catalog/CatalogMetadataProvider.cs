//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// Connected SQL Parser metadata provider backed by compact sys-catalog queries.
    /// This is intentionally limited to the IntelliSense hot path: database names,
    /// schemas, schema-owned object names, columns, module parameters, scalar alias
    /// types, table types, and synonyms.
    /// </summary>
    public sealed class CatalogMetadataProvider : MetadataProviderBase
    {
        internal const string EnableEnvironmentVariable = "SQLTOOLS_ENABLE_CATALOG_METADATA_PROVIDER";

        private readonly CatalogMetadataStore _store;
        private readonly CatalogServer _server;

        private CatalogMetadataProvider(ServerConnection connection)
        {
            _store = new CatalogMetadataStore(connection);
            _server = new CatalogServer(_store);
        }

        public static bool IsEnabled =>
            string.Equals(Environment.GetEnvironmentVariable(EnableEnvironmentVariable), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable(EnableEnvironmentVariable), "1", StringComparison.OrdinalIgnoreCase);

        public static bool IsEnabledForSetting(bool? enableCatalogMetadataProvider)
        {
            return enableCatalogMetadataProvider ?? IsEnabled;
        }

        public static CatalogMetadataProvider CreateConnectedProvider(ServerConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var provider = new CatalogMetadataProvider(connection);
            provider._server.EnsureCurrentDatabase();
            return provider;
        }

        public void PreloadCurrentDatabaseMetadata()
        {
            _server.CurrentDatabase.Preload();
        }

        public override IServer Server => _server;
        public override MetadataProviderEventHandler? AfterBindHandler => null;
        public override MetadataProviderEventHandler? BeforeBindHandler => null;
    }

    internal sealed class CatalogMetadataStore
    {
        private readonly ServerConnection _connection;
        private readonly object _connectionLock = new object();

        public CatalogMetadataStore(ServerConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public string ServerName => _connection.ServerInstance ?? string.Empty;

        public string CurrentDatabaseName
        {
            get
            {
                string databaseName = _connection.DatabaseName;
                if (string.IsNullOrEmpty(databaseName))
                {
                    databaseName = _connection.CurrentDatabase;
                }
                if (string.IsNullOrEmpty(databaseName))
                {
                    databaseName = _connection.SqlConnectionObject.Database;
                }
                return string.IsNullOrEmpty(databaseName) ? "master" : databaseName;
            }
        }

        public CatalogDatabaseInfo GetDatabaseInfo(string databaseName)
        {
            string sql = @"
select
    d.name,
    convert(nvarchar(256), d.collation_name) as collation_name,
    convert(int, d.compatibility_level) as compatibility_level,
    convert(bit, case when d.name = db_name() then 1 else 0 end) as is_current
from sys.databases as d
where d.name = @databaseName and has_dbaccess(d.name) = 1;";

            CatalogDatabaseInfo? info = null;
            bool isCurrentDatabase = false;
            try
            {
                ExecuteReader(sql, command => AddParameter(command, "@databaseName", databaseName), reader =>
                {
                    if (reader.Read())
                    {
                        isCurrentDatabase = GetBoolean(reader, "is_current") == true;
                        info = new CatalogDatabaseInfo(
                            GetString(reader, "name") ?? databaseName,
                            GetString(reader, "collation_name"),
                            MapCompatibilityLevel(GetInt32(reader, "compatibility_level") ?? 0),
                            "dbo");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load database metadata for '{databaseName}'. Error: {ex.Message}");
            }

            if (info != null && isCurrentDatabase)
            {
                info = new CatalogDatabaseInfo(info.Name, info.CollationName, info.CompatibilityLevel, LoadDefaultSchemaName(databaseName));
            }

            return info ?? new CatalogDatabaseInfo(databaseName, null, DatabaseCompatibilityLevel.Current, "dbo");
        }

        public IReadOnlyList<string> LoadAccessibleDatabaseNames()
        {
            string sql = @"
select d.name
from sys.databases as d
where d.state = 0 and has_dbaccess(d.name) = 1
order by d.name;";

            var databaseNames = new List<string>();
            try
            {
                ExecuteReader(sql, null, reader =>
                {
                    while (reader.Read())
                    {
                        string? databaseName = GetString(reader, "name");
                        if (!string.IsNullOrEmpty(databaseName))
                        {
                            databaseNames.Add(databaseName);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load database names for catalog metadata provider. Error: {ex.Message}");
            }

            if (!databaseNames.Contains(CurrentDatabaseName, StringComparer.OrdinalIgnoreCase))
            {
                databaseNames.Add(CurrentDatabaseName);
            }

            return databaseNames;
        }

        public CatalogDatabaseSnapshot LoadDatabaseSnapshot(string databaseName)
        {
            string sys = QuoteIdentifier(databaseName) + ".sys";
            string sql = $@"
select s.name as schema_name
from {sys}.schemas as s
order by s.name;

select
    s.name as schema_name,
    o.name as object_name,
    convert(int, o.object_id) as object_id,
    convert(nvarchar(2), o.type) as object_type,
    convert(bit, o.is_ms_shipped) as is_system_object,
    convert(bit, isnull(m.uses_quoted_identifier, 1)) as uses_quoted_identifier,
    convert(bit, isnull(m.is_schema_bound, 0)) as is_schema_bound,
    convert(nvarchar(max), syn.base_object_name) as base_object_name
from {sys}.all_objects as o
inner join {sys}.schemas as s on s.schema_id = o.schema_id
left join {sys}.sql_modules as m on m.object_id = o.object_id
left join {sys}.synonyms as syn on syn.object_id = o.object_id
where o.type in ('U', 'V', 'P', 'PC', 'RF', 'X', 'FN', 'FS', 'IF', 'TF', 'FT', 'SN')
order by s.name, o.name;

select
    s.name as schema_name,
    t.name as type_name,
    convert(int, t.user_type_id) as user_type_id,
    convert(int, t.system_type_id) as system_type_id,
    convert(bit, t.is_nullable) as is_nullable,
    convert(smallint, t.max_length) as max_length,
    convert(tinyint, t.precision) as numeric_precision,
    convert(tinyint, t.scale) as numeric_scale,
    convert(bit, t.is_table_type) as is_table_type,
    convert(int, isnull(tt.type_table_object_id, 0)) as type_table_object_id,
    convert(nvarchar(128), base_type.name) as base_type_name
from {sys}.types as t
inner join {sys}.schemas as s on s.schema_id = t.schema_id
left join {sys}.table_types as tt on tt.user_type_id = t.user_type_id
left join {sys}.types as base_type
    on base_type.user_type_id = base_type.system_type_id
    and base_type.system_type_id = t.system_type_id
where t.is_user_defined = 1
order by s.name, t.name;";

            var schemas = new List<string>();
            var objects = new List<CatalogObjectInfo>();
            var types = new List<CatalogTypeInfo>();

            try
            {
                ExecuteReader(sql, null, reader =>
                {
                    while (reader.Read())
                    {
                        string? schemaName = GetString(reader, "schema_name");
                        if (!string.IsNullOrEmpty(schemaName))
                        {
                            schemas.Add(schemaName);
                        }
                    }

                    if (!reader.NextResult())
                    {
                        return;
                    }

                    while (reader.Read())
                    {
                        string? schemaName = GetString(reader, "schema_name");
                        string? objectName = GetString(reader, "object_name");
                        string? objectType = GetString(reader, "object_type");
                        if (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(objectType))
                        {
                            continue;
                        }
                        objectType = objectType.Trim();

                        objects.Add(new CatalogObjectInfo(
                            schemaName,
                            objectName,
                            GetInt32(reader, "object_id") ?? 0,
                            objectType,
                            MapObjectKind(objectType),
                            GetBoolean(reader, "is_system_object") ?? false,
                            GetBoolean(reader, "uses_quoted_identifier") ?? true,
                            GetBoolean(reader, "is_schema_bound") ?? false,
                            GetString(reader, "base_object_name")));
                    }

                    if (!reader.NextResult())
                    {
                        return;
                    }

                    while (reader.Read())
                    {
                        string? schemaName = GetString(reader, "schema_name");
                        string? typeName = GetString(reader, "type_name");
                        if (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(typeName))
                        {
                            continue;
                        }

                        var dataType = new CatalogDataTypeInfo(
                            GetString(reader, "base_type_name") ?? typeName,
                            schemaName,
                            GetBoolean(reader, "is_table_type") ?? false,
                            true,
                            GetInt16(reader, "max_length") ?? 0,
                            GetByte(reader, "numeric_precision") ?? 0,
                            GetByte(reader, "numeric_scale") ?? 0);

                        types.Add(new CatalogTypeInfo(
                            schemaName,
                            typeName,
                            GetInt32(reader, "user_type_id") ?? 0,
                            GetInt32(reader, "type_table_object_id") ?? 0,
                            dataType,
                            GetBoolean(reader, "is_nullable") ?? true,
                            GetBoolean(reader, "is_table_type") ?? false));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load catalog metadata snapshot for database '{databaseName}'. Error: {ex.Message}");
            }

            return new CatalogDatabaseSnapshot(schemas, objects, types);
        }

        public IReadOnlyList<CatalogColumnInfo> LoadColumns(string databaseName, int objectId)
        {
            if (objectId == 0)
            {
                return Array.Empty<CatalogColumnInfo>();
            }

            string sys = QuoteIdentifier(databaseName) + ".sys";
            string sql = $@"
select
    c.name as column_name,
    convert(int, c.column_id) as column_id,
    convert(bit, c.is_nullable) as is_nullable,
    convert(bit, c.is_identity) as is_identity,
    convert(bit, c.is_computed) as is_computed,
    convert(bit, c.is_sparse) as is_sparse,
    convert(bit, c.is_column_set) as is_column_set,
    convert(bit, c.is_rowguidcol) as is_rowguidcol,
    convert(smallint, c.max_length) as max_length,
    convert(tinyint, c.precision) as numeric_precision,
    convert(tinyint, c.scale) as numeric_scale,
    type_schema.name as type_schema_name,
    t.name as type_name,
    convert(bit, t.is_user_defined) as is_user_defined_type,
    convert(bit, t.is_table_type) as is_table_type
from {sys}.all_columns as c
inner join {sys}.types as t on t.user_type_id = c.user_type_id
inner join {sys}.schemas as type_schema on type_schema.schema_id = t.schema_id
where c.object_id = @objectId
order by c.column_id;";

            var columns = new List<CatalogColumnInfo>();
            try
            {
                ExecuteReader(sql, command => AddParameter(command, "@objectId", objectId), reader =>
                {
                    while (reader.Read())
                    {
                        string? columnName = GetString(reader, "column_name");
                        string? typeName = GetString(reader, "type_name");
                        if (string.IsNullOrEmpty(columnName))
                        {
                            continue;
                        }

                        columns.Add(new CatalogColumnInfo(
                            columnName,
                            GetInt32(reader, "column_id") ?? 0,
                            new CatalogDataTypeInfo(
                                typeName ?? "sql_variant",
                                GetString(reader, "type_schema_name"),
                                GetBoolean(reader, "is_table_type") ?? false,
                                GetBoolean(reader, "is_user_defined_type") ?? false,
                                GetInt16(reader, "max_length") ?? 0,
                                GetByte(reader, "numeric_precision") ?? 0,
                                GetByte(reader, "numeric_scale") ?? 0),
                            GetBoolean(reader, "is_nullable") ?? true,
                            GetBoolean(reader, "is_identity") ?? false,
                            GetBoolean(reader, "is_computed") ?? false,
                            GetBoolean(reader, "is_sparse") ?? false,
                            GetBoolean(reader, "is_column_set") ?? false,
                            GetBoolean(reader, "is_rowguidcol") ?? false));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load columns for object '{databaseName}.{objectId}'. Error: {ex.Message}");
            }

            return columns;
        }

        public IReadOnlyList<CatalogParameterInfo> LoadParameters(string databaseName, int objectId)
        {
            if (objectId == 0)
            {
                return Array.Empty<CatalogParameterInfo>();
            }

            string sys = QuoteIdentifier(databaseName) + ".sys";
            string sql = $@"
select
    p.name as parameter_name,
    convert(int, p.parameter_id) as parameter_id,
    convert(bit, p.is_output) as is_output,
    convert(bit, p.is_readonly) as is_readonly,
    convert(nvarchar(max), p.default_value) as default_value,
    type_schema.name as type_schema_name,
    t.name as type_name,
    convert(bit, t.is_user_defined) as is_user_defined_type,
    convert(bit, t.is_table_type) as is_table_type,
    convert(smallint, p.max_length) as max_length,
    convert(tinyint, p.precision) as numeric_precision,
    convert(tinyint, p.scale) as numeric_scale
from {sys}.parameters as p
inner join {sys}.types as t on t.user_type_id = p.user_type_id
inner join {sys}.schemas as type_schema on type_schema.schema_id = t.schema_id
where p.object_id = @objectId and p.parameter_id > 0
order by p.parameter_id;";

            var parameters = new List<CatalogParameterInfo>();
            try
            {
                ExecuteReader(sql, command => AddParameter(command, "@objectId", objectId), reader =>
                {
                    while (reader.Read())
                    {
                        string? parameterName = GetString(reader, "parameter_name");
                        string? typeName = GetString(reader, "type_name");
                        if (string.IsNullOrEmpty(parameterName))
                        {
                            continue;
                        }

                        parameters.Add(new CatalogParameterInfo(
                            parameterName,
                            new CatalogDataTypeInfo(
                                typeName ?? "sql_variant",
                                GetString(reader, "type_schema_name"),
                                GetBoolean(reader, "is_table_type") ?? false,
                                GetBoolean(reader, "is_user_defined_type") ?? false,
                                GetInt16(reader, "max_length") ?? 0,
                                GetByte(reader, "numeric_precision") ?? 0,
                                GetByte(reader, "numeric_scale") ?? 0),
                            GetBoolean(reader, "is_output") ?? false,
                            GetBoolean(reader, "is_readonly") ?? false,
                            GetString(reader, "default_value")));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load parameters for object '{databaseName}.{objectId}'. Error: {ex.Message}");
            }

            return parameters;
        }

        public CatalogDataTypeInfo? LoadScalarReturnType(string databaseName, int objectId)
        {
            if (objectId == 0)
            {
                return null;
            }

            string sys = QuoteIdentifier(databaseName) + ".sys";
            string sql = $@"
select
    type_schema.name as type_schema_name,
    t.name as type_name,
    convert(bit, t.is_user_defined) as is_user_defined_type,
    convert(bit, t.is_table_type) as is_table_type,
    convert(smallint, p.max_length) as max_length,
    convert(tinyint, p.precision) as numeric_precision,
    convert(tinyint, p.scale) as numeric_scale
from {sys}.parameters as p
inner join {sys}.types as t on t.user_type_id = p.user_type_id
inner join {sys}.schemas as type_schema on type_schema.schema_id = t.schema_id
where p.object_id = @objectId and p.parameter_id = 0;";

            CatalogDataTypeInfo? returnType = null;
            try
            {
                ExecuteReader(sql, command => AddParameter(command, "@objectId", objectId), reader =>
                {
                    if (reader.Read())
                    {
                        string? typeName = GetString(reader, "type_name");
                        returnType = new CatalogDataTypeInfo(
                            typeName ?? "sql_variant",
                            GetString(reader, "type_schema_name"),
                            GetBoolean(reader, "is_table_type") ?? false,
                            GetBoolean(reader, "is_user_defined_type") ?? false,
                            GetInt16(reader, "max_length") ?? 0,
                            GetByte(reader, "numeric_precision") ?? 0,
                            GetByte(reader, "numeric_scale") ?? 0);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load scalar return type for object '{databaseName}.{objectId}'. Error: {ex.Message}");
            }

            return returnType;
        }

        private string LoadDefaultSchemaName(string databaseName)
        {
            string sys = QuoteIdentifier(databaseName) + ".sys";
            string sql = $@"
select isnull(
    (select default_schema_name from {sys}.database_principals where name = user_name()),
    N'dbo') as default_schema_name;";

            string? defaultSchemaName = null;
            try
            {
                ExecuteReader(sql, null, reader =>
                {
                    if (reader.Read())
                    {
                        defaultSchemaName = GetString(reader, "default_schema_name");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load default schema for database '{databaseName}'. Error: {ex.Message}");
            }

            return string.IsNullOrEmpty(defaultSchemaName) ? "dbo" : defaultSchemaName;
        }

        private void ExecuteReader(string sql, Action<DbCommand>? configure, Action<DbDataReader> read)
        {
            lock (_connectionLock)
            {
                if (!_connection.IsOpen)
                {
                    _connection.Connect();
                }

                using (DbCommand command = _connection.SqlConnectionObject.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 15;
                    configure?.Invoke(command);

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        read(reader);
                    }
                }
            }
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        internal static string QuoteIdentifier(string value)
        {
            return "[" + value.Replace("]", "]]") + "]";
        }

        private static string? GetString(DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
        }

        private static bool? GetBoolean(DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToBoolean(reader.GetValue(ordinal));
        }

        private static int? GetInt32(DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static short? GetInt16(DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToInt16(reader.GetValue(ordinal));
        }

        private static byte? GetByte(DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToByte(reader.GetValue(ordinal));
        }

        private static CatalogObjectKind MapObjectKind(string objectType)
        {
            switch (objectType)
            {
                case "U":
                    return CatalogObjectKind.Table;
                case "V":
                    return CatalogObjectKind.View;
                case "P":
                case "PC":
                case "RF":
                case "X":
                    return CatalogObjectKind.StoredProcedure;
                case "FN":
                case "FS":
                    return CatalogObjectKind.ScalarFunction;
                case "IF":
                case "TF":
                case "FT":
                    return CatalogObjectKind.TableValuedFunction;
                case "SN":
                    return CatalogObjectKind.Synonym;
                default:
                    return CatalogObjectKind.Unknown;
            }
        }

        private static DatabaseCompatibilityLevel MapCompatibilityLevel(int compatibilityLevel)
        {
            switch (compatibilityLevel)
            {
                case 80:
                    return DatabaseCompatibilityLevel.Version80;
                case 90:
                    return DatabaseCompatibilityLevel.Version90;
                case 100:
                    return DatabaseCompatibilityLevel.Version100;
                case 110:
                    return DatabaseCompatibilityLevel.Version110;
                case 120:
                    return DatabaseCompatibilityLevel.Version120;
                case 130:
                    return DatabaseCompatibilityLevel.Version130;
                case 140:
                    return DatabaseCompatibilityLevel.Version140;
                case 150:
                    return DatabaseCompatibilityLevel.Version150;
                case 160:
                    return DatabaseCompatibilityLevel.Version160;
                case 170:
                    return DatabaseCompatibilityLevel.Version170;
                default:
                    return DatabaseCompatibilityLevel.Current;
            }
        }
    }

    internal sealed class CatalogServer : IServer
    {
        private readonly CatalogMetadataStore _store;
        private readonly CatalogDatabaseCollection _databases;
        private CatalogDatabase? _currentDatabase;

        public CatalogServer(CatalogMetadataStore store)
        {
            _store = store;
            _databases = new CatalogDatabaseCollection(this, store);
        }

        public string Name => _store.ServerName;
        public bool IsSystemObject => false;
        public IDatabaseObject Parent => null!;
        public CollationInfo CollationInfo => CollationInfo.Default;
        public IMetadataCollection<IDatabase> Databases => _databases;
        public IMetadataCollection<ICredential> Credentials => LazyCollection<ICredential>.Empty;
        public IMetadataCollection<ILogin> Logins => LazyCollection<ILogin>.Empty;
        public IMetadataCollection<IServerDdlTrigger> Triggers => LazyCollection<IServerDdlTrigger>.Empty;

        internal CatalogDatabase CurrentDatabase => _currentDatabase ??= GetOrCreateDatabase(_store.CurrentDatabaseName, verifyAccess: false);

        public void EnsureCurrentDatabase()
        {
            _ = CurrentDatabase;
        }

        internal CatalogDatabase GetOrCreateDatabase(string databaseName, bool verifyAccess)
        {
            return _databases.GetOrCreate(databaseName, verifyAccess);
        }

        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogDatabase : IDatabase
    {
        private readonly CatalogServer _server;
        private readonly CatalogMetadataStore _store;
        private readonly Lazy<CatalogDatabaseInfo> _info;
        private readonly Lazy<CatalogDatabaseSnapshot> _snapshot;
        private readonly Lazy<IMetadataCollection<ISchema>> _schemas;

        public CatalogDatabase(CatalogServer server, CatalogMetadataStore store, string name)
        {
            _server = server;
            _store = store;
            Name = name;
            _info = new Lazy<CatalogDatabaseInfo>(() => _store.GetDatabaseInfo(name));
            _snapshot = new Lazy<CatalogDatabaseSnapshot>(() => _store.LoadDatabaseSnapshot(name));
            _schemas = new Lazy<IMetadataCollection<ISchema>>(() =>
                new LookupCollection<ISchema>(() => _snapshot.Value.Schemas.Select(schemaName => (ISchema)new CatalogSchema(this, schemaName))));
        }

        public string Name { get; }
        public bool IsSystemObject => string.Equals(Name, "master", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(Name, "msdb", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(Name, "model", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(Name, "tempdb", StringComparison.OrdinalIgnoreCase);
        public IDatabaseObject Parent => _server;
        public IServer Server => _server;
        public CollationInfo CollationInfo => !string.IsNullOrEmpty(_info.Value.CollationName) ? CollationInfo.GetCollationInfo(_info.Value.CollationName) : CollationInfo.Default;
        public DatabaseCompatibilityLevel CompatibilityLevel => _info.Value.CompatibilityLevel;
        public string DefaultSchemaName => _info.Value.DefaultSchemaName;
        public IUser? Owner => null;
        public IMetadataCollection<ISchema> Schemas => _schemas.Value;
        public IMetadataCollection<IApplicationRole> ApplicationRoles => LazyCollection<IApplicationRole>.Empty;
        public IMetadataCollection<IAsymmetricKey> AsymmetricKeys => LazyCollection<IAsymmetricKey>.Empty;
        public IMetadataCollection<ICertificate> Certificates => LazyCollection<ICertificate>.Empty;
        public IMetadataCollection<IDatabaseRole> Roles => LazyCollection<IDatabaseRole>.Empty;
        public IMetadataCollection<IDatabaseDdlTrigger> Triggers => LazyCollection<IDatabaseDdlTrigger>.Empty;
        public IMetadataCollection<IUser> Users => LazyCollection<IUser>.Empty;

        internal void Preload()
        {
            _ = _snapshot.Value;
        }

        internal IEnumerable<CatalogObjectInfo> GetObjects(string schemaName, CatalogObjectKind kind) =>
            _snapshot.Value.Objects.Where(o => o.Kind == kind && string.Equals(o.SchemaName, schemaName, StringComparison.OrdinalIgnoreCase));

        internal IEnumerable<CatalogTypeInfo> GetTypes(string schemaName, bool tableTypes) =>
            _snapshot.Value.Types.Where(t => t.IsTableType == tableTypes && string.Equals(t.SchemaName, schemaName, StringComparison.OrdinalIgnoreCase));

        internal IReadOnlyList<CatalogColumnInfo> LoadColumns(int objectId) => _store.LoadColumns(Name, objectId);
        internal IReadOnlyList<CatalogParameterInfo> LoadParameters(int objectId) => _store.LoadParameters(Name, objectId);
        internal CatalogDataTypeInfo? LoadScalarReturnType(int objectId) => _store.LoadScalarReturnType(Name, objectId);

        public T Accept<T>(IServerOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IServerOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogSchema : ISchema
    {
        private readonly CatalogDatabase _database;
        private readonly Lazy<IMetadataCollection<ITable>> _tables;
        private readonly Lazy<IMetadataCollection<IView>> _views;
        private readonly Lazy<IMetadataCollection<IStoredProcedure>> _storedProcedures;
        private readonly Lazy<IMetadataCollection<IScalarValuedFunction>> _scalarValuedFunctions;
        private readonly Lazy<IMetadataCollection<ITableValuedFunction>> _tableValuedFunctions;
        private readonly Lazy<IMetadataCollection<ISynonym>> _synonyms;
        private readonly Lazy<IMetadataCollection<IUserDefinedDataType>> _userDefinedDataTypes;
        private readonly Lazy<IMetadataCollection<IUserDefinedTableType>> _userDefinedTableTypes;

        public CatalogSchema(CatalogDatabase database, string name)
        {
            _database = database;
            Name = name;
            _tables = new Lazy<IMetadataCollection<ITable>>(() => new LookupCollection<ITable>(() => _database.GetObjects(Name, CatalogObjectKind.Table).Select(o => (ITable)new CatalogTable(this, o))));
            _views = new Lazy<IMetadataCollection<IView>>(() => new LookupCollection<IView>(() => _database.GetObjects(Name, CatalogObjectKind.View).Select(o => (IView)new CatalogView(this, o))));
            _storedProcedures = new Lazy<IMetadataCollection<IStoredProcedure>>(() => new LookupCollection<IStoredProcedure>(() => _database.GetObjects(Name, CatalogObjectKind.StoredProcedure).Select(o => (IStoredProcedure)new CatalogStoredProcedure(this, o))));
            _scalarValuedFunctions = new Lazy<IMetadataCollection<IScalarValuedFunction>>(() => new LookupCollection<IScalarValuedFunction>(() => _database.GetObjects(Name, CatalogObjectKind.ScalarFunction).Select(o => (IScalarValuedFunction)new CatalogScalarFunction(this, o))));
            _tableValuedFunctions = new Lazy<IMetadataCollection<ITableValuedFunction>>(() => new LookupCollection<ITableValuedFunction>(() => _database.GetObjects(Name, CatalogObjectKind.TableValuedFunction).Select(o => (ITableValuedFunction)new CatalogTableValuedFunction(this, o))));
            _synonyms = new Lazy<IMetadataCollection<ISynonym>>(() => new LookupCollection<ISynonym>(() => _database.GetObjects(Name, CatalogObjectKind.Synonym).Select(o => (ISynonym)new CatalogSynonym(this, o))));
            _userDefinedDataTypes = new Lazy<IMetadataCollection<IUserDefinedDataType>>(() => new LookupCollection<IUserDefinedDataType>(() => _database.GetTypes(Name, tableTypes: false).Select(t => (IUserDefinedDataType)new CatalogUserDefinedDataType(this, t))));
            _userDefinedTableTypes = new Lazy<IMetadataCollection<IUserDefinedTableType>>(() => new LookupCollection<IUserDefinedTableType>(() => _database.GetTypes(Name, tableTypes: true).Select(t => (IUserDefinedTableType)new CatalogUserDefinedTableType(this, t))));
        }

        public string Name { get; }
        public bool IsSystemObject => string.Equals(Name, "sys", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(Name, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);
        public IDatabaseObject Parent => _database;
        public IDatabase Database => _database;
        public IDatabasePrincipal? Owner => null;
        public IMetadataCollection<ITable> Tables => _tables.Value;
        public IMetadataCollection<IView> Views => _views.Value;
        public IMetadataCollection<IStoredProcedure> StoredProcedures => _storedProcedures.Value;
        public IMetadataCollection<IScalarValuedFunction> ScalarValuedFunctions => _scalarValuedFunctions.Value;
        public IMetadataCollection<ITableValuedFunction> TableValuedFunctions => _tableValuedFunctions.Value;
        public IMetadataCollection<ISynonym> Synonyms => _synonyms.Value;
        public IMetadataCollection<IUserDefinedDataType> UserDefinedDataTypes => _userDefinedDataTypes.Value;
        public IMetadataCollection<IUserDefinedTableType> UserDefinedTableTypes => _userDefinedTableTypes.Value;
        public IMetadataCollection<IUserDefinedAggregate> UserDefinedAggregates => LazyCollection<IUserDefinedAggregate>.Empty;
        public IMetadataCollection<IUserDefinedClrType> UserDefinedClrTypes => LazyCollection<IUserDefinedClrType>.Empty;
        public IMetadataCollection<IExtendedStoredProcedure> ExtendedStoredProcedures => LazyCollection<IExtendedStoredProcedure>.Empty;

        internal CatalogDatabase CatalogDatabase => _database;

        public T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => default(T)!;
        public T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IDatabaseOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal abstract class CatalogSchemaObject : ISchemaOwnedObject
    {
        protected readonly CatalogSchema SchemaObject;
        protected readonly CatalogObjectInfo ObjectInfo;

        protected CatalogSchemaObject(CatalogSchema schema, CatalogObjectInfo objectInfo)
        {
            SchemaObject = schema;
            ObjectInfo = objectInfo;
        }

        public string Name => ObjectInfo.Name;
        public bool IsSystemObject => ObjectInfo.IsSystemObject;
        public IDatabaseObject Parent => SchemaObject;
        public ISchema Schema => SchemaObject;
        public abstract T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((ISchemaOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => Accept((IDatabaseObjectVisitor<T>)visitor);
    }

    internal abstract class CatalogTabularObject : CatalogSchemaObject, ITableViewBase
    {
        private readonly Lazy<IMetadataOrderedCollection<IColumn>> _columns;

        protected CatalogTabularObject(CatalogSchema schema, CatalogObjectInfo objectInfo)
            : base(schema, objectInfo)
        {
            _columns = new Lazy<IMetadataOrderedCollection<IColumn>>(() =>
                new LookupOrderedCollection<IColumn>(
                    SchemaObject.CatalogDatabase.LoadColumns(ObjectInfo.ObjectId)
                        .Select(c => (IColumn)new CatalogColumn(this, c))));
        }

        public abstract TabularType TabularType { get; }
        public IMetadataOrderedCollection<IColumn> Columns => _columns.Value;
        public ITabular Unaliased => this;
        public CollationInfo CollationInfo => SchemaObject.CatalogDatabase.CollationInfo;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;
        public bool IsQuotedIdentifierOn => ObjectInfo.UsesQuotedIdentifier;
    }

    internal sealed class CatalogTable : CatalogTabularObject, ITable
    {
        public CatalogTable(CatalogSchema schema, CatalogObjectInfo objectInfo) : base(schema, objectInfo)
        {
        }

        public override TabularType TabularType => TabularType.Table;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogView : CatalogTabularObject, IView
    {
        public CatalogView(CatalogSchema schema, CatalogObjectInfo objectInfo) : base(schema, objectInfo)
        {
        }

        public override TabularType TabularType => TabularType.View;
        public bool HasCheckOption => false;
        public bool HasColumnSpecification => false;
        public bool IsEncrypted => false;
        public bool IsSchemaBound => ObjectInfo.IsSchemaBound;
        public string? QueryText => null;
        public bool ReturnsViewMetadata => false;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal abstract class CatalogModuleObject : CatalogSchemaObject, IUserDefinedFunctionModuleBase
    {
        private readonly Lazy<IMetadataOrderedCollection<IParameter>> _parameters;

        protected CatalogModuleObject(CatalogSchema schema, CatalogObjectInfo objectInfo)
            : base(schema, objectInfo)
        {
            _parameters = new Lazy<IMetadataOrderedCollection<IParameter>>(() =>
                new LookupOrderedCollection<IParameter>(
                    SchemaObject.CatalogDatabase.LoadParameters(ObjectInfo.ObjectId)
                        .Select(p => (IParameter)new CatalogParameter(p))));
        }

        public IMetadataOrderedCollection<IParameter> Parameters => _parameters.Value;
        public IExecutionContext? ExecutionContext => null;
        public bool IsEncrypted => false;
        public string? BodyText => null;
        public bool IsSchemaBound => ObjectInfo.IsSchemaBound;
        public bool IsSqlClr => false;
    }

    internal sealed class CatalogStoredProcedure : CatalogModuleObject, IStoredProcedure
    {
        public CatalogStoredProcedure(CatalogSchema schema, CatalogObjectInfo objectInfo) : base(schema, objectInfo)
        {
        }

        public IScalarDataType? ReturnType => null;
        public CallableModuleType ModuleType => CallableModuleType.StoredProcedure;
        public bool ForReplication => false;
        public bool IsQuotedIdentifierOn => ObjectInfo.UsesQuotedIdentifier;
        public bool IsRecompiled => false;
        public bool Startup => false;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogScalarFunction : CatalogModuleObject, IScalarValuedFunction
    {
        private readonly Lazy<IScalarDataType?> _returnType;

        public CatalogScalarFunction(CatalogSchema schema, CatalogObjectInfo objectInfo) : base(schema, objectInfo)
        {
            _returnType = new Lazy<IScalarDataType?>(() => SchemaObject.CatalogDatabase.LoadScalarReturnType(ObjectInfo.ObjectId)?.ToScalarDataType());
        }

        public IScalarDataType? ReturnType => _returnType.Value;
        public CallableModuleType ModuleType => CallableModuleType.ScalarFunction;
        public bool IsQuotedIdentifierOn => ObjectInfo.UsesQuotedIdentifier;
        public IScalarDataType? DataType => ReturnType;
        public bool Nullable => true;
        public ScalarType ScalarType => ScalarType.ScalarFunction;
        public bool IsAggregateFunction => false;
        public bool ReturnsNullOnNullInput => false;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogTableValuedFunction : CatalogModuleObject, ITableValuedFunction
    {
        private readonly Lazy<IMetadataOrderedCollection<IColumn>> _columns;

        public CatalogTableValuedFunction(CatalogSchema schema, CatalogObjectInfo objectInfo) : base(schema, objectInfo)
        {
            _columns = new Lazy<IMetadataOrderedCollection<IColumn>>(() =>
                new LookupOrderedCollection<IColumn>(
                    SchemaObject.CatalogDatabase.LoadColumns(ObjectInfo.ObjectId)
                        .Select(c => (IColumn)new CatalogColumn(this, c))));
        }

        public IMetadataOrderedCollection<IColumn> Columns => _columns.Value;
        public TabularType TabularType => TabularType.TableValuedFunction;
        public ITabular Unaliased => this;
        public CollationInfo CollationInfo => SchemaObject.CatalogDatabase.CollationInfo;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;
        public bool IsQuotedIdentifierOn => ObjectInfo.UsesQuotedIdentifier;
        public bool IsInline => string.Equals(ObjectInfo.ObjectType, "IF", StringComparison.OrdinalIgnoreCase);
        public string? TableVariableName => null;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogSynonym : CatalogSchemaObject, ISynonym
    {
        public CatalogSynonym(CatalogSchema schema, CatalogObjectInfo objectInfo) : base(schema, objectInfo)
        {
        }

        public string BaseObjectName => ObjectInfo.BaseObjectName ?? string.Empty;
        public SynonymBaseType BaseType => SynonymBaseType.None;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogUserDefinedDataType : CatalogSchemaObject, IUserDefinedDataType
    {
        private readonly CatalogTypeInfo _typeInfo;

        public CatalogUserDefinedDataType(CatalogSchema schema, CatalogTypeInfo typeInfo)
            : base(schema, new CatalogObjectInfo(typeInfo.SchemaName, typeInfo.Name, 0, null, CatalogObjectKind.UserDefinedDataType, false, true, false, null))
        {
            _typeInfo = typeInfo;
        }

        public bool Nullable => _typeInfo.Nullable;
        public bool IsCursor => false;
        public bool IsScalar => true;
        public bool IsTable => false;
        public bool IsUnknown => false;
        public ISystemDataType BaseSystemDataType => _typeInfo.DataType.ToSystemDataType();
        public bool IsClr => false;
        public bool IsSystem => false;
        public bool IsVoid => false;
        public bool IsXml => string.Equals(_typeInfo.DataType.Name, "xml", StringComparison.OrdinalIgnoreCase);
        public IScalarDataType AsScalarDataType => this;
        public ITableDataType? AsTableDataType => null;
        public IUserDefinedType AsUserDefinedType => this;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogUserDefinedTableType : CatalogSchemaObject, IUserDefinedTableType
    {
        private readonly CatalogTypeInfo _typeInfo;
        private readonly Lazy<IMetadataOrderedCollection<IColumn>> _columns;

        public CatalogUserDefinedTableType(CatalogSchema schema, CatalogTypeInfo typeInfo)
            : base(schema, new CatalogObjectInfo(typeInfo.SchemaName, typeInfo.Name, typeInfo.TypeTableObjectId, null, CatalogObjectKind.UserDefinedTableType, false, true, false, null))
        {
            _typeInfo = typeInfo;
            _columns = new Lazy<IMetadataOrderedCollection<IColumn>>(() =>
                new LookupOrderedCollection<IColumn>(
                    SchemaObject.CatalogDatabase.LoadColumns(_typeInfo.TypeTableObjectId)
                        .Select(c => (IColumn)new CatalogColumn(this, c))));
        }

        public bool IsCursor => false;
        public bool IsScalar => false;
        public bool IsTable => true;
        public bool IsUnknown => false;
        public IScalarDataType? AsScalarDataType => null;
        public ITableDataType AsTableDataType => this;
        public IUserDefinedType AsUserDefinedType => this;
        public IMetadataOrderedCollection<IColumn> Columns => _columns.Value;
        public TabularType TabularType => TabularType.TableDataType;
        public ITabular Unaliased => this;
        public CollationInfo CollationInfo => SchemaObject.CatalogDatabase.CollationInfo;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;
        public bool IsQuotedIdentifierOn => true;
        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogColumn : IColumn
    {
        private readonly ITabular _parent;
        private readonly CatalogColumnInfo _columnInfo;

        public CatalogColumn(ITabular parent, CatalogColumnInfo columnInfo)
        {
            _parent = parent;
            _columnInfo = columnInfo;
        }

        public string Name => _columnInfo.Name;
        public IScalarDataType DataType => _columnInfo.DataType.ToScalarDataType();
        public bool Nullable => _columnInfo.Nullable;
        public ScalarType ScalarType => ScalarType.Column;
        public ICollation? Collation => null;
        public ComputedColumnInfo? ComputedColumnInfo => _columnInfo.IsComputed ? new ComputedColumnInfo(null, false) : null;
        public IDefaultConstraint? DefaultValue => null;
        public IdentityColumnInfo? IdentityColumnInfo => _columnInfo.IsIdentity ? new IdentityColumnInfo(1, 1, false) : null;
        public bool InPrimaryKey => false;
        public bool IsColumnSet => _columnInfo.IsColumnSet;
        public bool IsGeneratedAlwaysAsRowEnd => false;
        public bool IsGeneratedAlwaysAsRowStart => false;
        public bool IsGeneratedAlwaysAsSequenceNumberEnd => false;
        public bool IsGeneratedAlwaysAsSequenceNumberStart => false;
        public bool IsGeneratedAlwaysAsTransactionIdEnd => false;
        public bool IsGeneratedAlwaysAsTransactionIdStart => false;
        public bool IsSparse => _columnInfo.IsSparse;
        public ITabular Parent => _parent;
        public bool RowGuidCol => _columnInfo.IsRowGuidCol;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    internal sealed class CatalogParameter : IScalarParameter
    {
        private readonly CatalogParameterInfo _parameterInfo;

        public CatalogParameter(CatalogParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo;
        }

        public string Name => _parameterInfo.Name;
        public bool IsOutput => _parameterInfo.IsOutput;
        public bool IsReadOnly => _parameterInfo.IsReadOnly;
        public string? DefaultValue => _parameterInfo.DefaultValue;
        public bool IsSystemObject => false;
        public bool IsScalarVariable => true;
        public bool IsTableVariable => false;
        public bool IsCursorVariable => false;
        public bool IsParameter => true;
        public ScalarType ScalarType => ScalarType.ScalarVariable;
        public IScalarDataType DataType => _parameterInfo.DataType.ToScalarDataType();
        IDataType ILocalVariable.DataType => DataType;
        public bool Nullable => true;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((IScalarParameter)this);
    }

    internal sealed class CatalogScalarDataType : ISystemDataType
    {
        private readonly CatalogDataTypeInfo _dataTypeInfo;

        public CatalogScalarDataType(CatalogDataTypeInfo dataTypeInfo)
        {
            _dataTypeInfo = dataTypeInfo;
        }

        public string Name => _dataTypeInfo.Name;
        public bool IsCursor => false;
        public bool IsScalar => true;
        public bool IsTable => false;
        public bool IsUnknown => false;
        public bool IsSystem => !_dataTypeInfo.IsUserDefined;
        public bool IsClr => false;
        public bool IsXml => string.Equals(_dataTypeInfo.Name, "xml", StringComparison.OrdinalIgnoreCase);
        public bool IsVoid => false;
        public ISystemDataType BaseSystemDataType => this;
        public IScalarDataType AsScalarDataType => this;
        public ITableDataType? AsTableDataType => null;
        public IUserDefinedType? AsUserDefinedType => null;
        public DataTypeSpec TypeSpec => DataTypeSpec.GetDataTypeSpec(_dataTypeInfo.Name) ?? DataTypeSpec.Variant;
        public int Length => _dataTypeInfo.Length;
        public int NumericPrecision => _dataTypeInfo.NumericPrecision;
        public int NumericScale => _dataTypeInfo.NumericScale;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((ISystemDataType)this);
    }

    internal sealed class CatalogDatabaseCollection : IMetadataCollection<IDatabase>
    {
        private readonly CatalogServer _server;
        private readonly CatalogMetadataStore _store;
        private readonly Lazy<Dictionary<string, CatalogDatabase>> _databases;
        private readonly object _syncRoot = new object();

        public CatalogDatabaseCollection(CatalogServer server, CatalogMetadataStore store)
        {
            _server = server;
            _store = store;
            _databases = new Lazy<Dictionary<string, CatalogDatabase>>(() =>
            {
                var map = new Dictionary<string, CatalogDatabase>(StringComparer.OrdinalIgnoreCase);
                foreach (string databaseName in _store.LoadAccessibleDatabaseNames())
                {
                    if (!map.ContainsKey(databaseName))
                    {
                        map[databaseName] = new CatalogDatabase(_server, _store, databaseName);
                    }
                }
                return map;
            });
        }

        public int Count => _databases.Value.Count;

        public IDatabase this[string name] => GetOrCreate(name, verifyAccess: true);

        internal CatalogDatabase GetOrCreate(string name, bool verifyAccess)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = _store.CurrentDatabaseName;
            }

            Dictionary<string, CatalogDatabase> map = _databases.Value;
            lock (_syncRoot)
            {
                if (!map.TryGetValue(name, out CatalogDatabase? database))
                {
                    if (verifyAccess)
                    {
                        _ = _store.GetDatabaseInfo(name);
                    }
                    database = new CatalogDatabase(_server, _store, name);
                    map[name] = database;
                }
                return database;
            }
        }

        public bool Contains(string name) => _databases.Value.ContainsKey(name);
        public bool Contains(IDatabase item) => _databases.Value.Values.Contains(item);
        public IEnumerable<IDatabase> FindAll(Predicate<IDatabase> predicate) => _databases.Value.Values.Where(database => predicate(database));
        public IEnumerable<IDatabase> FindAll(string name) => _databases.Value.TryGetValue(name, out CatalogDatabase? database) ? new[] { (IDatabase)database } : Enumerable.Empty<IDatabase>();
        public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection => new LazyCollection<IMetadataObject>(() => _databases.Value.Values.Cast<IMetadataObject>());
        public IEnumerator<IDatabase> GetEnumerator() => _databases.Value.Values.Cast<IDatabase>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class LookupCollection<T> : IMetadataCollection<T>
        where T : class, IMetadataObject
    {
        private readonly Lazy<Dictionary<string, T>> _items;

        public LookupCollection(Func<IEnumerable<T>> loader)
        {
            _items = new Lazy<Dictionary<string, T>>(() =>
            {
                var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
                foreach (T item in loader())
                {
                    if (!map.ContainsKey(item.Name))
                    {
                        map[item.Name] = item;
                    }
                }
                return map;
            });
        }

        public int Count => _items.Value.Count;
        public T this[string name] => _items.Value.TryGetValue(name, out T? value) ? value : null!;
        public bool Contains(string name) => _items.Value.ContainsKey(name);
        public bool Contains(T item) => _items.Value.Values.Contains(item);
        public IEnumerable<T> FindAll(Predicate<T> predicate) => _items.Value.Values.Where(item => predicate(item));
        public IEnumerable<T> FindAll(string name) => _items.Value.TryGetValue(name, out T? value) ? new[] { value } : Enumerable.Empty<T>();
        public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection => new LazyCollection<IMetadataObject>(() => _items.Value.Values.Cast<IMetadataObject>());
        public IEnumerator<T> GetEnumerator() => _items.Value.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class LookupOrderedCollection<T> : IMetadataOrderedCollection<T>
        where T : class, IMetadataObject
    {
        private readonly T[] _items;
        private readonly Dictionary<string, T> _lookup;

        public LookupOrderedCollection(IEnumerable<T> items)
        {
            _items = items.ToArray();
            _lookup = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (T item in _items)
            {
                if (!_lookup.ContainsKey(item.Name))
                {
                    _lookup[item.Name] = item;
                }
            }
        }

        public int Count => _items.Length;
        public T this[int index] => _items[index];
        public T this[string name] => _lookup.TryGetValue(name, out T? value) ? value : null!;
        public bool Contains(string name) => _lookup.ContainsKey(name);
        public bool Contains(T item) => _items.Contains(item);
        public IEnumerable<T> FindAll(Predicate<T> predicate) => _items.Where(item => predicate(item));
        public IEnumerable<T> FindAll(string name) => _lookup.TryGetValue(name, out T? value) ? new[] { value } : Enumerable.Empty<T>();
        public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection => new LazyCollection<IMetadataObject>(() => _items.Cast<IMetadataObject>());
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class CatalogDatabaseInfo
    {
        public CatalogDatabaseInfo(string name, string? collationName, DatabaseCompatibilityLevel compatibilityLevel, string defaultSchemaName)
        {
            Name = name;
            CollationName = collationName;
            CompatibilityLevel = compatibilityLevel;
            DefaultSchemaName = defaultSchemaName;
        }

        public string Name { get; }
        public string? CollationName { get; }
        public DatabaseCompatibilityLevel CompatibilityLevel { get; }
        public string DefaultSchemaName { get; }
    }

    internal sealed class CatalogDatabaseSnapshot
    {
        public CatalogDatabaseSnapshot(IReadOnlyList<string> schemas, IReadOnlyList<CatalogObjectInfo> objects, IReadOnlyList<CatalogTypeInfo> types)
        {
            Schemas = schemas;
            Objects = objects;
            Types = types;
        }

        public IReadOnlyList<string> Schemas { get; }
        public IReadOnlyList<CatalogObjectInfo> Objects { get; }
        public IReadOnlyList<CatalogTypeInfo> Types { get; }
    }

    internal sealed class CatalogObjectInfo
    {
        public CatalogObjectInfo(string schemaName, string name, int objectId, string? objectType, CatalogObjectKind kind, bool isSystemObject, bool usesQuotedIdentifier, bool isSchemaBound, string? baseObjectName)
        {
            SchemaName = schemaName;
            Name = name;
            ObjectId = objectId;
            ObjectType = objectType;
            Kind = kind;
            IsSystemObject = isSystemObject;
            UsesQuotedIdentifier = usesQuotedIdentifier;
            IsSchemaBound = isSchemaBound;
            BaseObjectName = baseObjectName;
        }

        public string SchemaName { get; }
        public string Name { get; }
        public int ObjectId { get; }
        public string? ObjectType { get; }
        public CatalogObjectKind Kind { get; }
        public bool IsSystemObject { get; }
        public bool UsesQuotedIdentifier { get; }
        public bool IsSchemaBound { get; }
        public string? BaseObjectName { get; }
    }

    internal sealed class CatalogTypeInfo
    {
        public CatalogTypeInfo(string schemaName, string name, int userTypeId, int typeTableObjectId, CatalogDataTypeInfo dataType, bool nullable, bool isTableType)
        {
            SchemaName = schemaName;
            Name = name;
            UserTypeId = userTypeId;
            TypeTableObjectId = typeTableObjectId;
            DataType = dataType;
            Nullable = nullable;
            IsTableType = isTableType;
        }

        public string SchemaName { get; }
        public string Name { get; }
        public int UserTypeId { get; }
        public int TypeTableObjectId { get; }
        public CatalogDataTypeInfo DataType { get; }
        public bool Nullable { get; }
        public bool IsTableType { get; }
    }

    internal sealed class CatalogColumnInfo
    {
        public CatalogColumnInfo(string name, int ordinal, CatalogDataTypeInfo dataType, bool nullable, bool isIdentity, bool isComputed, bool isSparse, bool isColumnSet, bool isRowGuidCol)
        {
            Name = name;
            Ordinal = ordinal;
            DataType = dataType;
            Nullable = nullable;
            IsIdentity = isIdentity;
            IsComputed = isComputed;
            IsSparse = isSparse;
            IsColumnSet = isColumnSet;
            IsRowGuidCol = isRowGuidCol;
        }

        public string Name { get; }
        public int Ordinal { get; }
        public CatalogDataTypeInfo DataType { get; }
        public bool Nullable { get; }
        public bool IsIdentity { get; }
        public bool IsComputed { get; }
        public bool IsSparse { get; }
        public bool IsColumnSet { get; }
        public bool IsRowGuidCol { get; }
    }

    internal sealed class CatalogParameterInfo
    {
        public CatalogParameterInfo(string name, CatalogDataTypeInfo dataType, bool isOutput, bool isReadOnly, string? defaultValue)
        {
            Name = name;
            DataType = dataType;
            IsOutput = isOutput;
            IsReadOnly = isReadOnly;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public CatalogDataTypeInfo DataType { get; }
        public bool IsOutput { get; }
        public bool IsReadOnly { get; }
        public string? DefaultValue { get; }
    }

    internal sealed class CatalogDataTypeInfo
    {
        public CatalogDataTypeInfo(string name, string? schemaName, bool isTableType, bool isUserDefined, int length, int numericPrecision, int numericScale)
        {
            Name = name;
            SchemaName = schemaName;
            IsTableType = isTableType;
            IsUserDefined = isUserDefined;
            Length = length;
            NumericPrecision = numericPrecision;
            NumericScale = numericScale;
        }

        public string Name { get; }
        public string? SchemaName { get; }
        public bool IsTableType { get; }
        public bool IsUserDefined { get; }
        public int Length { get; }
        public int NumericPrecision { get; }
        public int NumericScale { get; }

        public IScalarDataType ToScalarDataType() => new CatalogScalarDataType(this);
        public ISystemDataType ToSystemDataType() => new CatalogScalarDataType(new CatalogDataTypeInfo(Name, SchemaName, false, false, Length, NumericPrecision, NumericScale));
    }

    internal enum CatalogObjectKind
    {
        Unknown,
        Table,
        View,
        StoredProcedure,
        ScalarFunction,
        TableValuedFunction,
        Synonym,
        UserDefinedDataType,
        UserDefinedTableType
    }
}
