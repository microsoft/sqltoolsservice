//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// SSDT counterparts:
//   MetadataProvider/Table.cs                → TSqlModelTable
//   MetadataProvider/View.cs                 → TSqlModelView
//   MetadataProvider/StoredProcedure.cs      → TSqlModelStoredProcedure
//   MetadataProvider/ScalarValuedFunction.cs → TSqlModelScalarFunction
//   MetadataProvider/TableValuedFunction.cs  → TSqlModelTableValuedFunction
//   MetadataProvider/UserDefinedDataType.cs  → TSqlModelUserDefinedDataType
//   MetadataProvider/UserdefinedTableType.cs → TSqlModelUserDefinedTableType

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    // =========================================================================
    // Base for all schema-owned objects
    // =========================================================================
    internal abstract class TSqlModelSchemaObject : ISchemaOwnedObject
    {
        protected readonly TSqlModelSchema _schema;
        protected readonly string _name;
        private readonly bool _isUserDefined;

        protected TSqlModelSchemaObject(TSqlModelSchema schema, string name, bool isUserDefined)
        {
            _schema = schema;
            _name   = name;
            _isUserDefined = isUserDefined;
        }

        public string Name => _name;
        public bool IsSystemObject => !_isUserDefined;  // System objects are NOT user-defined
        public IDatabaseObject Parent => _schema;
        public ISchema Schema => _schema;

        public abstract T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor);
        public T Accept<T>(IDatabaseObjectVisitor<T> visitor) => Accept((ISchemaOwnedObjectVisitor<T>)visitor);
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => Accept((IDatabaseObjectVisitor<T>)visitor);
    }

    // =========================================================================
    // TSqlModelTable : ITable
    // =========================================================================
    internal sealed class TSqlModelTable : TSqlModelSchemaObject, ITable
    {
        private readonly TSqlObject _tableObj;
        private IMetadataOrderedCollection<IColumn>? _columns;
        private IMetadataCollection<IConstraint>? _constraints;
        private IMetadataCollection<IIndex>? _indexes;

        public TSqlModelTable(TSqlModelSchema schema, TSqlObject tableObj, bool isUserDefined)
            : base(schema, tableObj.Name.Parts[tableObj.Name.Parts.Count - 1], isUserDefined)
        {
            _tableObj = tableObj;
        }

        // Lazy columns — only loaded when accessed
        public IMetadataOrderedCollection<IColumn> Columns =>
            _columns ??= new LazyOrderedCollection<IColumn>(
                () => _tableObj.GetReferenced(Table.Columns)
                               .Select(c => new TSqlModelColumn(this, c))
                               .Cast<IColumn>());

        public TabularType TabularType => TabularType.Table;
        public ITabular Unaliased => this;

        // IDatabaseTable
        public CollationInfo CollationInfo => CollationInfo.Default;

        // Lazy constraints — PK and UK constraints loaded on first access
        public IMetadataCollection<IConstraint> Constraints =>
            _constraints ??= new LazyCollection<IConstraint>(
                () => _tableObj.GetReferencing(PrimaryKeyConstraint.Host, DacQueryScopes.UserDefined)
                               .Select(c => (IConstraint)new TSqlModelPrimaryKeyConstraint(this, c))
                               .Concat(
                                   _tableObj.GetReferencing(UniqueConstraint.Host, DacQueryScopes.UserDefined)
                                            .Select(c => (IConstraint)new TSqlModelUniqueConstraint(this, c))));

        // Lazy indexes — binder uses Indexes (not Constraints) for FK key validation
        public IMetadataCollection<IIndex> Indexes =>
            _indexes ??= new LazyCollection<IIndex>(
                () => _tableObj.GetReferencing(PrimaryKeyConstraint.Host, DacQueryScopes.UserDefined)
                               .Select(c => (IIndex)new TSqlModelRelationalIndex(
                                   this,
                                   () => c.GetReferenced(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined)
                                          .Select((col, i) => (IOrderedColumn)new TSqlModelOrderedColumn(new TSqlModelColumn(this, col), i)),
                                   () => c.GetReferenced(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined)
                                          .Select(col => (IIndexedColumn)new TSqlModelIndexedColumn(new TSqlModelColumn(this, col))),
                                   ConstraintType.PrimaryKey,
                                   c.GetProperty<bool>(PrimaryKeyConstraint.Clustered)))
                               .Concat(
                                   _tableObj.GetReferencing(UniqueConstraint.Host, DacQueryScopes.UserDefined)
                                            .Select(c => (IIndex)new TSqlModelRelationalIndex(
                                                this,
                                                () => c.GetReferenced(UniqueConstraint.Columns, DacQueryScopes.UserDefined)
                                                       .Select((col, i) => (IOrderedColumn)new TSqlModelOrderedColumn(new TSqlModelColumn(this, col), i)),
                                                () => c.GetReferenced(UniqueConstraint.Columns, DacQueryScopes.UserDefined)
                                                       .Select(col => (IIndexedColumn)new TSqlModelIndexedColumn(new TSqlModelColumn(this, col))),
                                                ConstraintType.Unique,
                                                c.GetProperty<bool>(UniqueConstraint.Clustered)))));

        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;

        // ITableViewBase
        public bool IsQuotedIdentifierOn => false;
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;

        // ITable (no additional members beyond ITableViewBase + IDatabaseTable + ITabular)

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelView : IView
    // =========================================================================
    internal sealed class TSqlModelView : TSqlModelSchemaObject, IView
    {
        private readonly TSqlObject _viewObj;
        private IMetadataOrderedCollection<IColumn>? _columns;

        public TSqlModelView(TSqlModelSchema schema, TSqlObject viewObj, bool isUserDefined)
            : base(schema, viewObj.Name.Parts[viewObj.Name.Parts.Count - 1], isUserDefined)
        {
            _viewObj = viewObj;
        }

        // Lazy columns — populated on first access
        public IMetadataOrderedCollection<IColumn> Columns =>
            _columns ??= new LazyOrderedCollection<IColumn>(
                () => _viewObj.GetReferenced(View.Columns)
                              .Select(c => new TSqlModelColumn(this, c))
                              .Cast<IColumn>());

        public TabularType TabularType => TabularType.View;
        public ITabular Unaliased => this;

        // IDatabaseTable
        public CollationInfo CollationInfo => CollationInfo.Default;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;

        // ITableViewBase — reads actual model values
        public bool IsQuotedIdentifierOn => _viewObj.GetProperty<bool>(View.QuotedIdentifierOn);
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;

        // IView
        public bool HasCheckOption      => _viewObj.GetProperty<bool>(View.WithCheckOption);
        public bool HasColumnSpecification => false; // no direct DacFx equivalent
        public bool IsEncrypted         => _viewObj.GetProperty<bool>(View.WithEncryption);
        public bool IsSchemaBound       => _viewObj.GetProperty<bool>(View.WithSchemaBinding);
        public string? QueryText        => null;     // not needed for completions
        public bool ReturnsViewMetadata => _viewObj.GetProperty<bool>(View.WithViewMetadata);

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelStoredProcedure : IStoredProcedure
    // =========================================================================
    internal sealed class TSqlModelStoredProcedure : TSqlModelSchemaObject, IStoredProcedure
    {
        private readonly TSqlObject _procObj;
        private IMetadataOrderedCollection<IParameter>? _parameters;

        public TSqlModelStoredProcedure(TSqlModelSchema schema, TSqlObject procObj, bool isUserDefined)
            : base(schema, procObj.Name.Parts[procObj.Name.Parts.Count - 1], isUserDefined)
        {
            _procObj = procObj;
        }

        // IFunctionModuleBase — lazy parameters, populated on first access
        public IMetadataOrderedCollection<IParameter> Parameters =>
            _parameters ??= new LazyOrderedCollection<IParameter>(() =>
                _procObj
                    .GetReferenced(Procedure.Parameters)
                    .Select(p => (IParameter)new TSqlModelParameter(
                        p.Name.Parts[p.Name.Parts.Count - 1],
                        p.GetProperty<bool>(Parameter.IsOutput))));

        // ICallableModule
        public IScalarDataType? ReturnType => null;
        public CallableModuleType ModuleType => CallableModuleType.StoredProcedure;

        // IUserDefinedFunctionModuleBase
        public IExecutionContext? ExecutionContext => null;
        public bool IsEncrypted => _procObj.GetProperty<bool>(Procedure.WithEncryption);

        // IStoredProcedure
        public string? BodyText        => null;
        public bool ForReplication      => _procObj.GetProperty<bool>(Procedure.ForReplication);
        public bool IsQuotedIdentifierOn => _procObj.GetProperty<bool>(Procedure.QuotedIdentifierOn);
        public bool IsRecompiled        => _procObj.GetProperty<bool>(Procedure.WithRecompile);
        public bool IsSqlClr            => false;
        public bool Startup             => false;

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelScalarFunction : IScalarValuedFunction
    // =========================================================================
    internal sealed class TSqlModelScalarFunction : TSqlModelSchemaObject, IScalarValuedFunction
    {
        private readonly TSqlObject _fnObj;
        private IMetadataOrderedCollection<IParameter>? _parameters;

        public TSqlModelScalarFunction(TSqlModelSchema schema, TSqlObject fnObj, bool isUserDefined)
            : base(schema, fnObj.Name.Parts[fnObj.Name.Parts.Count - 1], isUserDefined)
        {
            _fnObj = fnObj;
        }

        // IFunctionModuleBase — lazy parameters from model
        public IMetadataOrderedCollection<IParameter> Parameters =>
            _parameters ??= new LazyOrderedCollection<IParameter>(() =>
                _fnObj
                    .GetReferenced(ScalarFunction.Parameters)
                    .Select(p => (IParameter)new TSqlModelParameter(
                        p.Name.Parts[p.Name.Parts.Count - 1],
                        p.GetProperty<bool>(Parameter.IsOutput))));

        // ICallableModule
        public IScalarDataType? ReturnType => null; // type resolution complex — not needed for completions
        public CallableModuleType ModuleType => CallableModuleType.ScalarFunction;

        // IUserDefinedFunctionModuleBase
        public IExecutionContext? ExecutionContext => null;
        public bool IsEncrypted => _fnObj.GetProperty<bool>(ScalarFunction.WithEncryption);

        // IUserDefinedFunction
        public string? BodyText          => null;
        public bool IsSchemaBound        => _fnObj.GetProperty<bool>(ScalarFunction.WithSchemaBinding);
        public bool IsSqlClr             => false;
        public bool IsQuotedIdentifierOn => _fnObj.GetProperty<bool>(ScalarFunction.QuotedIdentifierOn);

        // IScalar
        public IScalarDataType? DataType => null; // type resolution complex
        public bool Nullable             => true;
        public ScalarType ScalarType     => ScalarType.ScalarFunction;

        // IScalarFunction
        public bool IsAggregateFunction => false;

        // IScalarValuedFunction
        public bool ReturnsNullOnNullInput => _fnObj.GetProperty<bool>(ScalarFunction.ReturnsNullOnNullInput);

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelTableValuedFunction : ITableValuedFunction
    // =========================================================================
    internal sealed class TSqlModelTableValuedFunction : TSqlModelSchemaObject, ITableValuedFunction
    {
        private readonly TSqlObject _fnObj;
        private IMetadataOrderedCollection<IColumn>? _columns;
        private IMetadataOrderedCollection<IParameter>? _parameters;

        public TSqlModelTableValuedFunction(TSqlModelSchema schema, TSqlObject fnObj, bool isUserDefined)
            : base(schema, fnObj.Name.Parts[fnObj.Name.Parts.Count - 1], isUserDefined)
        {
            _fnObj = fnObj;
        }

        // ITabular / IDatabaseTable — lazy columns, populated on first access
        public IMetadataOrderedCollection<IColumn> Columns =>
            _columns ??= new LazyOrderedCollection<IColumn>(
                () => _fnObj.GetReferenced(TableValuedFunction.Columns)
                            .Select(c => new TSqlModelColumn(this, c))
                            .Cast<IColumn>());

        public TabularType TabularType => TabularType.TableValuedFunction;
        public ITabular Unaliased => this;
        public CollationInfo CollationInfo => CollationInfo.Default;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;

        // ITableViewBase
        public bool IsQuotedIdentifierOn => _fnObj.GetProperty<bool>(TableValuedFunction.QuotedIdentifierOn);
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;

        // IFunctionModuleBase — lazy parameters, populated on first access
        public IMetadataOrderedCollection<IParameter> Parameters =>
            _parameters ??= new LazyOrderedCollection<IParameter>(() =>
                _fnObj
                    .GetReferenced(TableValuedFunction.Parameters)
                    .Select(p => (IParameter)new TSqlModelParameter(
                        p.Name.Parts[p.Name.Parts.Count - 1],
                        p.GetProperty<bool>(Parameter.IsOutput))));

        // IUserDefinedFunctionModuleBase
        public IExecutionContext? ExecutionContext => null;
        public bool IsEncrypted => _fnObj.GetProperty<bool>(TableValuedFunction.WithEncryption);

        // IUserDefinedFunction
        public string? BodyText   => null;
        public bool IsSchemaBound => _fnObj.GetProperty<bool>(TableValuedFunction.WithSchemaBinding);
        public bool IsSqlClr      => false;

        // ITableValuedFunction — true for inline TVFs (single SELECT); false for multi-statement TVFs
        // (multi-statement TVFs have a ReturnTableVariableName, inline ones do not)
        public bool IsInline =>
            string.IsNullOrEmpty(_fnObj.GetProperty<string>(TableValuedFunction.ReturnTableVariableName));

        public string? TableVariableName =>
            _fnObj.GetProperty<string>(TableValuedFunction.ReturnTableVariableName);

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelUserDefinedDataType : IUserDefinedDataType
    // =========================================================================
    internal sealed class TSqlModelUserDefinedDataType : TSqlModelSchemaObject, IUserDefinedDataType
    {
        public TSqlModelUserDefinedDataType(TSqlModelSchema schema, string name, bool isUserDefined)
            : base(schema, name, isUserDefined) { }

        // IDataType
        public bool IsCursor => false;
        public bool IsScalar => true;
        public bool IsTable => false;
        public bool IsUnknown => false;

        // IScalarDataType
        public ISystemDataType? BaseSystemDataType => null;
        public bool IsClr => false;
        public bool IsSystem => false;
        public bool IsVoid => false;
        public bool IsXml => false;

        // IUserDefinedDataType
        public bool Nullable => false;

        // IScalarDataType / IDataType casts
        public IScalarDataType? AsScalarDataType => this;
        public ITableDataType? AsTableDataType => null;
        public IUserDefinedType? AsUserDefinedType => this;

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelUserDefinedTableType : IUserDefinedTableType
    // =========================================================================
    internal sealed class TSqlModelUserDefinedTableType : TSqlModelSchemaObject, IUserDefinedTableType
    {
        public TSqlModelUserDefinedTableType(TSqlModelSchema schema, string name, bool isUserDefined)
            : base(schema, name, isUserDefined) { }

        // IDataType
        public bool IsCursor => false;
        public bool IsScalar => false;
        public bool IsTable => true;
        public bool IsUnknown => false;

        // IScalarDataType casts (not applicable)
        public IScalarDataType? AsScalarDataType => null;
        public ITableDataType? AsTableDataType => this;
        public IUserDefinedType? AsUserDefinedType => this;

        // ITabular / IDatabaseTable (table type has columns but we skip them for now)
        public IMetadataOrderedCollection<IColumn> Columns => LazyOrderedCollection<IColumn>.Empty;
        public TabularType TabularType => TabularType.TableDataType;
        public ITabular Unaliased => this;
        public CollationInfo CollationInfo => CollationInfo.Default;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;

        // ITableViewBase
        public bool IsQuotedIdentifierOn => false;
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelParameter : IScalarParameter
    // Implements IScalarParameter (not just IParameter) because IMetadataObjectVisitor<T>
    // dispatches on the concrete parameter kind. We always produce scalar parameters.
    // =========================================================================
    internal sealed class TSqlModelParameter : IScalarParameter
    {
        public TSqlModelParameter(string name, bool isOutput)
        {
            Name     = name;
            IsOutput = isOutput;
        }

        public string Name           { get; }
        public bool IsOutput         { get; }
        public bool IsReadOnly       => false;
        public string? DefaultValue  => null;
        public bool IsSystemObject   => false;

        // ILocalVariable
        public bool IsScalarVariable => true;
        public bool IsTableVariable  => false;
        public bool IsCursorVariable => false;
        public bool IsParameter      => true;

        // IScalar (via IScalarVariable) — IScalarDataType satisfies ILocalVariable.DataType via explicit impl
        public ScalarType ScalarType         => ScalarType.ScalarVariable;
        public IScalarDataType? DataType     => null;
        IDataType? ILocalVariable.DataType   => DataType;
        public bool Nullable                 => true;

        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((IScalarParameter)this);
    }

    // =========================================================================
    // TSqlModelColumn : IColumn
    // =========================================================================
    internal sealed class TSqlModelColumn : IColumn
    {
        private readonly ITabular _parent;
        private readonly string _name;
        private readonly TSqlObject _colObj;

        public TSqlModelColumn(ITabular parent, TSqlObject colObj)
        {
            _parent = parent;
            _colObj = colObj;
            _name   = colObj.Name.Parts[colObj.Name.Parts.Count - 1];
        }

        public string Name => _name;


        // IScalar
        public IScalarDataType? DataType
        {
            get
            {
                TSqlObject? typeObj = _colObj.GetReferenced(Column.DataType).FirstOrDefault();
                if (typeObj == null) return null;

                // Primary: use the last Name part (e.g. "nvarchar", "int")
                string typeName = string.Empty;
                if (typeObj.Name?.Parts?.Count > 0)
                    typeName = typeObj.Name.Parts[typeObj.Name.Parts.Count - 1];

                // Fallback: read the SqlDataType enum which is always populated for built-in types.
                // This handles cases where Name.Parts is empty or holds a non-useful value (e.g. for
                // view-derived columns whose DataType object doesn't carry a resolvable identifier).
                // Use fully-qualified names to avoid ambiguity with IScalarDataType.DataType (this
                // property's name) and with Microsoft.SqlServer.Management.SqlParser.Metadata.SqlDataType.
                if (string.IsNullOrEmpty(typeName))
                {
                    var sqlDt = typeObj.GetProperty<Microsoft.SqlServer.Dac.Model.SqlDataType>(
                        Microsoft.SqlServer.Dac.Model.DataType.SqlDataType);
                    if (sqlDt != Microsoft.SqlServer.Dac.Model.SqlDataType.Unknown)
                        typeName = sqlDt.ToString().ToLowerInvariant();
                }

                return string.IsNullOrEmpty(typeName) ? null : new TSqlModelNamedDataType(typeName);
            }
        }

        public bool Nullable => _colObj.GetProperty<bool>(Column.Nullable);
        public ScalarType ScalarType => ScalarType.Column;

        // IColumn properties
        public ICollation? Collation => null;
        // Expression is the DacFx model property that holds a computed column's expression text.
        // Non-computed columns return null/empty for Expression, so this is safe for all columns.
        public ComputedColumnInfo? ComputedColumnInfo
        {
            get
            {
                string? expression = _colObj.GetProperty<string>(Column.Expression);
                if (string.IsNullOrEmpty(expression)) return null;
                bool isPersisted = _colObj.GetProperty<bool>(Column.Persisted);
                return new ComputedColumnInfo(expression, isPersisted);
            }
        }
        public IDefaultConstraint? DefaultValue => null;
        // Return a default IDENTITY(1,1) spec when the column is an identity column.
        // Actual seed/increment values require navigating Column.IdentityColumnInfo sub-element
        // which is not yet implemented; (1,1) is correct for the vast majority of real-world tables.
        public IdentityColumnInfo? IdentityColumnInfo =>
            _colObj.GetProperty<bool>(Column.IsIdentity)
                ? new IdentityColumnInfo(1, 1)
                : null;
        // Walk the relationship backwards: find any PrimaryKeyConstraint that lists this column.
        public bool InPrimaryKey =>
            _colObj.GetReferencing(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined).Any();
        public bool IsColumnSet => false;
        public bool IsGeneratedAlwaysAsRowEnd => false;
        public bool IsGeneratedAlwaysAsRowStart => false;
        public bool IsGeneratedAlwaysAsSequenceNumberEnd => false;
        public bool IsGeneratedAlwaysAsSequenceNumberStart => false;
        public bool IsGeneratedAlwaysAsTransactionIdEnd => false;
        public bool IsGeneratedAlwaysAsTransactionIdStart => false;
        public bool IsSparse => _colObj.GetProperty<bool>(Column.Sparse);
        public ITabular Parent => _parent;
        public bool RowGuidCol => _colObj.GetProperty<bool>(Column.IsRowGuidCol);

        // IMetadataObject
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((IColumn)this);
    }

    // =========================================================================
    // TSqlModelNamedDataType : IScalarDataType
    // Minimal IScalarDataType wrapper around a raw type name string (e.g. "datetime", "int").
    // Used by TSqlModelColumn.DataType to feed the hover tooltip formatter.
    // =========================================================================
    internal sealed class TSqlModelNamedDataType : IScalarDataType
    {
        private readonly string _name;

        public TSqlModelNamedDataType(string name) { _name = name; }

        // IMetadataObject
        public string Name => _name;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);

        // IDataType
        public bool IsCursor  => false;
        public bool IsScalar  => true;
        public bool IsTable   => false;
        public bool IsUnknown => false;

        // IScalarDataType
        public ISystemDataType? BaseSystemDataType => null;
        public bool IsClr    => false;
        public bool IsSystem => true;
        public bool IsVoid   => false;
        public bool IsXml    => false;

        // IDataType casts
        public IScalarDataType? AsScalarDataType => this;
        public ITableDataType?  AsTableDataType  => null;
        public IUserDefinedType? AsUserDefinedType => null;
    }

    // =========================================================================
    // TSqlModelOrderedColumn : IOrderedColumn
    // Wraps a column reference with its ordinal position in a constraint.
    // =========================================================================
    internal sealed class TSqlModelOrderedColumn : IOrderedColumn
    {
        private readonly IColumn _column;
        private readonly int _ordinal;

        public TSqlModelOrderedColumn(IColumn column, int ordinal)
        {
            _column  = column;
            _ordinal = ordinal;
        }

        public string Name                 => _column.Name;
        public IColumn ReferencedColumn    => _column;
        public int OrderOrdinal            => _ordinal;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelRelationalIndex : IRelationalIndex
    // Index wrapper for PK/UK constraints. Populates IndexedColumns and IndexKey
    // so the binder can resolve FK references against the correct key.
    // =========================================================================
    internal sealed class TSqlModelRelationalIndex : IRelationalIndex
    {
        private readonly ITabular _parent;
        private readonly Func<IEnumerable<IOrderedColumn>> _columnFactory;
        private readonly Func<IEnumerable<IIndexedColumn>>? _indexedColumnFactory;
        private readonly IUniqueConstraintBase _indexKeyRef;
        private readonly bool _isClustered;
        private IMetadataOrderedCollection<IOrderedColumn>? _orderedColumns;
        private IMetadataOrderedCollection<IIndexedColumn>? _indexedColumns;

        public TSqlModelRelationalIndex(
            ITabular parent,
            Func<IEnumerable<IOrderedColumn>> columnFactory,
            Func<IEnumerable<IIndexedColumn>>? indexedColumnFactory,
            ConstraintType constraintType,
            bool isClustered)
        {
            _parent               = parent;
            _columnFactory        = columnFactory;
            _indexedColumnFactory = indexedColumnFactory;
            _indexKeyRef          = new MinimalConstraintKey(constraintType);
            _isClustered          = isClustered;
        }

        // IMetadataObject
        public string Name => string.Empty;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);

        // IIndex
        public ITabular Parent        => _parent;
        public IndexType Type         => IndexType.Relational;
        public bool DisallowPageLocks => false;
        public bool DisallowRowLocks  => false;
        public byte FillFactor        => 0;
        public bool IgnoreDuplicateKeys => false;
        public bool IsDisabled        => false;
        public bool PadIndex          => false;

        // IRelationalIndex
        public bool CompactLargeObjects       => false;
        public IFileGroup FileGroup           => null!;
        public IFileGroup FileStreamFileGroup => null!;
        public IPartitionScheme FileStreamPartitionScheme => null!;
        public string FilterDefinition        => string.Empty;

        public IMetadataOrderedCollection<IIndexedColumn> IndexedColumns =>
            _indexedColumns ??= _indexedColumnFactory != null
                ? new LazyOrderedCollection<IIndexedColumn>(_indexedColumnFactory)
                : LazyOrderedCollection<IIndexedColumn>.Empty;

        public IUniqueConstraintBase IndexKey => _indexKeyRef;
        public bool IsClustered    => _isClustered;
        public bool IsSystemNamed  => false;
        public bool IsUnique       => true;
        public bool NoAutomaticRecomputation => false;
        public IPartitionScheme PartitionScheme => null!;

        public IMetadataOrderedCollection<IOrderedColumn> OrderedColumns =>
            _orderedColumns ??= new LazyOrderedCollection<IOrderedColumn>(_columnFactory);

        private sealed class MinimalConstraintKey : IUniqueConstraintBase
        {
            private readonly ConstraintType _type;
            internal MinimalConstraintKey(ConstraintType type) { _type = type; }
            public string Name         => string.Empty;
            public ITabular Parent     => null!;
            public bool IsSystemNamed  => false;
            public ConstraintType Type => _type;
            public IRelationalIndex AssociatedIndex => null!;
            public T Accept<T>(IMetadataObjectVisitor<T> visitor) => throw new NotSupportedException();
        }
    }

    // =========================================================================
    // TSqlModelIndexedColumn : IIndexedColumn
    // Key column in an index; IsIncluded=false marks it as a key (not included) column.
    // =========================================================================
    internal sealed class TSqlModelIndexedColumn : IIndexedColumn
    {
        private readonly IColumn _column;
        internal TSqlModelIndexedColumn(IColumn column) { _column = column; }
        public string Name             => _column.Name;
        public IColumn ReferencedColumn => _column;
        public SortOrder SortOrder     => SortOrder.Ascending;
        public bool IsIncluded         => false;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // TSqlModelPrimaryKeyConstraint : IPrimaryKeyConstraint
    // =========================================================================
    internal sealed class TSqlModelPrimaryKeyConstraint : IPrimaryKeyConstraint
    {
        private readonly ITabular _parent;
        private readonly string _name;
        private readonly IRelationalIndex _associatedIndex;

        public TSqlModelPrimaryKeyConstraint(ITabular parent, TSqlObject constraintObj)
        {
            _parent = parent;
            _name   = constraintObj.Name.Parts[constraintObj.Name.Parts.Count - 1];
            bool isClustered = constraintObj.GetProperty<bool>(PrimaryKeyConstraint.Clustered);
            _associatedIndex = new TSqlModelRelationalIndex(
                parent,
                () => constraintObj
                    .GetReferenced(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined)
                    .Select((c, i) => (IOrderedColumn)new TSqlModelOrderedColumn(new TSqlModelColumn(parent, c), i)),
                () => constraintObj
                    .GetReferenced(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined)
                    .Select(c => (IIndexedColumn)new TSqlModelIndexedColumn(new TSqlModelColumn(parent, c))),
                ConstraintType.PrimaryKey,
                isClustered);
        }

        public string Name => _name;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((IPrimaryKeyConstraint)this);
        public ITabular Parent         => _parent;
        public bool IsSystemNamed      => false;
        public ConstraintType Type     => ConstraintType.PrimaryKey;
        public IRelationalIndex AssociatedIndex => _associatedIndex;
    }

    // =========================================================================
    // TSqlModelUniqueConstraint : IUniqueConstraint
    // =========================================================================
    internal sealed class TSqlModelUniqueConstraint : IUniqueConstraint
    {
        private readonly ITabular _parent;
        private readonly string _name;
        private readonly IRelationalIndex _associatedIndex;

        public TSqlModelUniqueConstraint(ITabular parent, TSqlObject constraintObj)
        {
            _parent = parent;
            _name   = constraintObj.Name.Parts[constraintObj.Name.Parts.Count - 1];
            bool isClustered = constraintObj.GetProperty<bool>(UniqueConstraint.Clustered);
            _associatedIndex = new TSqlModelRelationalIndex(
                parent,
                () => constraintObj
                    .GetReferenced(UniqueConstraint.Columns, DacQueryScopes.UserDefined)
                    .Select((c, i) => (IOrderedColumn)new TSqlModelOrderedColumn(new TSqlModelColumn(parent, c), i)),
                () => constraintObj
                    .GetReferenced(UniqueConstraint.Columns, DacQueryScopes.UserDefined)
                    .Select(c => (IIndexedColumn)new TSqlModelIndexedColumn(new TSqlModelColumn(parent, c))),
                ConstraintType.Unique,
                isClustered);
        }

        public string Name => _name;
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((IUniqueConstraint)this);
        public ITabular Parent         => _parent;
        public bool IsSystemNamed      => false;
        public ConstraintType Type     => ConstraintType.Unique;
        public IRelationalIndex AssociatedIndex => _associatedIndex;
    }
}
