//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// SSDT counterpart: MetadataProvider/Schema.cs

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// Lazy <see cref="ISchema"/> wrapper backed by <see cref="TSqlModel"/>.
    /// Each collection (Tables, Views, etc.) is populated on first access only.
    /// <para>
    /// The scope field is <c>DacQueryScopes.UserDefined</c> for project-defined schemas
    /// and <c>DacQueryScopes.BuiltIn</c> for system schemas (sys, INFORMATION_SCHEMA, dbo, guest, …).
    /// </para>
    /// </summary>
    internal sealed class LazyModelSchema : ISchema
    {
        private readonly LazyModelDatabase _database;
        private readonly TSqlModel _model;
        private readonly string _name;
        private readonly DacQueryScopes _scope;

        // ── Backing fields — each LazyCollection is created once in the ctor
        // (just stores the loader delegate; no TSqlModel scan yet).
        // The Lazy<T[]> inside fires exactly once on first enumeration.
        private readonly IMetadataCollection<ITable>               _tables;
        private readonly IMetadataCollection<IView>                _views;
        private readonly IMetadataCollection<IStoredProcedure>     _storedProcedures;
        private readonly IMetadataCollection<IScalarValuedFunction> _scalarValuedFunctions;
        private readonly IMetadataCollection<ITableValuedFunction>  _tableValuedFunctions;
        private readonly IMetadataCollection<IUserDefinedDataType>  _userDefinedDataTypes;
        private readonly IMetadataCollection<IUserDefinedTableType> _userDefinedTableTypes;

        public LazyModelSchema(LazyModelDatabase database, TSqlModel model, string schemaName,
            DacQueryScopes scope = DacQueryScopes.UserDefined)
        {
            _database = database;
            _model    = model;
            _name     = schemaName;
            _scope    = scope;

            // Capture 'this' — safe because LazyCollection doesn't call back until enumerated.
            _tables = new LazyCollection<ITable>(
                () => model.GetObjects(_scope, ModelSchema.Table)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelTable(this, o))
                           .Cast<ITable>());

            _views = new LazyCollection<IView>(
                () => model.GetObjects(_scope, ModelSchema.View)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelView(this, o))
                           .Cast<IView>());

            _storedProcedures = new LazyCollection<IStoredProcedure>(
                () => model.GetObjects(_scope, ModelSchema.Procedure)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelStoredProcedure(this, o))
                           .Cast<IStoredProcedure>());

            _scalarValuedFunctions = new LazyCollection<IScalarValuedFunction>(
                () => model.GetObjects(_scope, ModelSchema.ScalarFunction)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelScalarFunction(this, o))
                           .Cast<IScalarValuedFunction>());

            _tableValuedFunctions = new LazyCollection<ITableValuedFunction>(
                () => model.GetObjects(_scope, ModelSchema.TableValuedFunction)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelTableValuedFunction(this, o))
                           .Cast<ITableValuedFunction>());

            _userDefinedDataTypes = new LazyCollection<IUserDefinedDataType>(
                () => model.GetObjects(_scope, ModelSchema.DataType)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelUserDefinedDataType(this, ObjectName(o)))
                           .Cast<IUserDefinedDataType>());

            _userDefinedTableTypes = new LazyCollection<IUserDefinedTableType>(
                () => model.GetObjects(_scope, ModelSchema.TableType)
                           .Where(o => Match(o))
                           .Select(o => new LazyModelUserDefinedTableType(this, ObjectName(o)))
                           .Cast<IUserDefinedTableType>());
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
        public T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => throw new System.NotImplementedException("Schema is not schema-owned.");
        public T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IDatabaseOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);

        // ── Helpers ──────────────────────────────────────────────────────
        private bool Match(TSqlObject obj) =>
            obj.Name.Parts.Count >= 2 &&
            string.Equals(obj.Name.Parts[0], _name, System.StringComparison.OrdinalIgnoreCase);

        private static string ObjectName(TSqlObject obj) =>
            obj.Name.Parts[obj.Name.Parts.Count - 1];
    }
}
