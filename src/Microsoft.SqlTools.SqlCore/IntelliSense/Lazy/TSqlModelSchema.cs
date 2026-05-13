//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

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

        public TSqlModelSchema(TSqlModelDatabase database, TSqlModel model, string schemaName,
            DacQueryScopes scope = DacQueryScopes.UserDefined)
        {
            _database = database;
            _model    = model;
            _name     = schemaName;
            _scope    = scope;

            // Capture 'this' — safe because LazyCollection doesn't call back until enumerated.
            // Query UserDefined and BuiltIn separately so each object knows its true scope.
            _tables = new LazyCollection<ITable>(
                () => GetObjectsWithScope(model, ModelSchema.Table)
                           .Select(pair => new TSqlModelTable(this, pair.Object, pair.IsUserDefined))
                           .Cast<ITable>());

            _views = new LazyCollection<IView>(
                () => GetObjectsWithScope(model, ModelSchema.View)
                           .Select(pair => new TSqlModelView(this, pair.Object, pair.IsUserDefined))
                           .Cast<IView>());

            _storedProcedures = new LazyCollection<IStoredProcedure>(
                () => GetObjectsWithScope(model, ModelSchema.Procedure)
                           .Select(pair => new TSqlModelStoredProcedure(this, pair.Object, pair.IsUserDefined))
                           .Cast<IStoredProcedure>());

            _scalarValuedFunctions = new LazyCollection<IScalarValuedFunction>(
                () => GetObjectsWithScope(model, ModelSchema.ScalarFunction)
                           .Select(pair => new TSqlModelScalarFunction(this, pair.Object, pair.IsUserDefined))
                           .Cast<IScalarValuedFunction>());

            _tableValuedFunctions = new LazyCollection<ITableValuedFunction>(
                () => GetObjectsWithScope(model, ModelSchema.TableValuedFunction)
                           .Select(pair => new TSqlModelTableValuedFunction(this, pair.Object, pair.IsUserDefined))
                           .Cast<ITableValuedFunction>());

            _userDefinedDataTypes = new LazyCollection<IUserDefinedDataType>(
                () => GetObjectsWithScope(model, ModelSchema.DataType)
                           .Select(pair => new TSqlModelUserDefinedDataType(this, ObjectName(pair.Object), pair.IsUserDefined))
                           .Cast<IUserDefinedDataType>());

            _userDefinedTableTypes = new LazyCollection<IUserDefinedTableType>(
                () => GetObjectsWithScope(model, ModelSchema.TableType)
                           .Select(pair => new TSqlModelUserDefinedTableType(this, ObjectName(pair.Object), pair.IsUserDefined))
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
        public T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => default(T)!;
        public T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((IDatabaseOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);

        // ── Helpers ──────────────────────────────────────────────────────
        
        /// <summary>
        /// Helper struct to track an object and whether it's user-defined or built-in.
        /// </summary>
        private readonly struct ObjectWithScope
        {
            public readonly TSqlObject Object;
            public readonly bool IsUserDefined;

            public ObjectWithScope(TSqlObject obj, bool isUserDefined)
            {
                Object = obj;
                IsUserDefined = isUserDefined;
            }
        }

        /// <summary>
        /// Gets objects from both UserDefined and BuiltIn scopes, tagging each with its source scope.
        /// This allows objects to have the correct IsSystemObject value regardless of which scope
        /// their parent schema was created in.
        /// </summary>
        private System.Collections.Generic.IEnumerable<ObjectWithScope> GetObjectsWithScope(
            TSqlModel model, ModelTypeClass objectType)
        {
            // Query UserDefined first (user tables, procedures, etc.)
            var userDefined = model.GetObjects(DacQueryScopes.UserDefined, objectType)
                .Where(Match)
                .Select(o => new ObjectWithScope(o, isUserDefined: true));

            // Query BuiltIn (system catalog objects)
            var builtIn = model.GetObjects(DacQueryScopes.BuiltIn, objectType)
                .Where(Match)
                .Select(o => new ObjectWithScope(o, isUserDefined: false));

            // Combine both: UserDefined objects first, then BuiltIn objects
            // If the same object exists in both scopes (unlikely for tables/views, but possible),
            // UserDefined wins (appears first in enumeration).
            return userDefined.Concat(builtIn);
        }

        private bool Match(TSqlObject obj) =>
            obj.Name.Parts.Count >= 2 &&
            string.Equals(obj.Name.Parts[0], _name, System.StringComparison.OrdinalIgnoreCase);

        private static string ObjectName(TSqlObject obj) =>
            obj.Name.Parts[obj.Name.Parts.Count - 1];
    }
}
