//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// SSDT counterparts:
//   MetadataProvider/Table.cs                → LazyModelTable
//   MetadataProvider/View.cs                 → LazyModelView
//   MetadataProvider/StoredProcedure.cs      → LazyModelStoredProcedure
//   MetadataProvider/ScalarValuedFunction.cs → LazyModelScalarFunction
//   MetadataProvider/TableValuedFunction.cs  → LazyModelTableValuedFunction
//   MetadataProvider/UserDefinedDataType.cs  → LazyModelUserDefinedDataType
//   MetadataProvider/UserdefinedTableType.cs → LazyModelUserDefinedTableType

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    // =========================================================================
    // Base for all schema-owned objects
    // =========================================================================
    internal abstract class LazySchemaOwnedBase : ISchemaOwnedObject
    {
        protected readonly LazyModelSchema _schema;
        protected readonly string _name;
        private readonly bool _isUserDefined;

        protected LazySchemaOwnedBase(LazyModelSchema schema, string name, bool isUserDefined)
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
    // LazyModelTable : ITable
    // =========================================================================
    internal sealed class LazyModelTable : LazySchemaOwnedBase, ITable
    {
        private readonly TSqlObject _tableObj;
        private IMetadataOrderedCollection<IColumn>? _columns;

        public LazyModelTable(LazyModelSchema schema, TSqlObject tableObj, bool isUserDefined)
            : base(schema, tableObj.Name.Parts[tableObj.Name.Parts.Count - 1], isUserDefined)
        {
            _tableObj = tableObj;
        }

        // Lazy columns — only loaded when accessed
        public IMetadataOrderedCollection<IColumn> Columns =>
            _columns ??= new LazyOrderedCollection<IColumn>(
                () => _tableObj.GetReferenced(Table.Columns)
                               .Select(c => new LazyModelColumn(this, c.Name.Parts[c.Name.Parts.Count - 1]))
                               .Cast<IColumn>());

        public TabularType TabularType => TabularType.Table;
        public ITabular Unaliased => this;

        // IDatabaseTable
        public CollationInfo CollationInfo => CollationInfo.Default;
        public IMetadataCollection<IConstraint> Constraints => LazyCollection<IConstraint>.Empty;
        public IMetadataCollection<IIndex> Indexes => LazyCollection<IIndex>.Empty;
        public IMetadataCollection<IStatistics> Statistics => LazyCollection<IStatistics>.Empty;

        // ITableViewBase
        public bool IsQuotedIdentifierOn => false;
        public IMetadataCollection<IDmlTrigger> Triggers => LazyCollection<IDmlTrigger>.Empty;

        // ITable (no additional members beyond ITableViewBase + IDatabaseTable + ITabular)

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // LazyModelView : IView
    // =========================================================================
    internal sealed class LazyModelView : LazySchemaOwnedBase, IView
    {
        private readonly TSqlObject _viewObj;
        private IMetadataOrderedCollection<IColumn>? _columns;

        public LazyModelView(LazyModelSchema schema, TSqlObject viewObj, bool isUserDefined)
            : base(schema, viewObj.Name.Parts[viewObj.Name.Parts.Count - 1], isUserDefined)
        {
            _viewObj = viewObj;
        }

        // Lazy columns — populated on first access
        public IMetadataOrderedCollection<IColumn> Columns =>
            _columns ??= new LazyOrderedCollection<IColumn>(
                () => _viewObj.GetReferenced(View.Columns)
                              .Select(c => new LazyModelColumn(this, c.Name.Parts[c.Name.Parts.Count - 1]))
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
    // LazyModelStoredProcedure : IStoredProcedure
    // =========================================================================
    internal sealed class LazyModelStoredProcedure : LazySchemaOwnedBase, IStoredProcedure
    {
        private readonly TSqlObject _procObj;
        private IMetadataOrderedCollection<IParameter>? _parameters;

        public LazyModelStoredProcedure(LazyModelSchema schema, TSqlObject procObj, bool isUserDefined)
            : base(schema, procObj.Name.Parts[procObj.Name.Parts.Count - 1], isUserDefined)
        {
            _procObj = procObj;
        }

        // IFunctionModuleBase — lazy parameters, populated on first access
        public IMetadataOrderedCollection<IParameter> Parameters =>
            _parameters ??= new LazyOrderedCollection<IParameter>(() =>
                _procObj
                    .GetReferenced(Procedure.Parameters)
                    .Select(p => (IParameter)new LazyModelParameter(
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
    // LazyModelScalarFunction : IScalarValuedFunction
    // =========================================================================
    internal sealed class LazyModelScalarFunction : LazySchemaOwnedBase, IScalarValuedFunction
    {
        private readonly TSqlObject _fnObj;
        private IMetadataOrderedCollection<IParameter>? _parameters;

        public LazyModelScalarFunction(LazyModelSchema schema, TSqlObject fnObj, bool isUserDefined)
            : base(schema, fnObj.Name.Parts[fnObj.Name.Parts.Count - 1], isUserDefined)
        {
            _fnObj = fnObj;
        }

        // IFunctionModuleBase — lazy parameters from model
        public IMetadataOrderedCollection<IParameter> Parameters =>
            _parameters ??= new LazyOrderedCollection<IParameter>(() =>
                _fnObj
                    .GetReferenced(ScalarFunction.Parameters)
                    .Select(p => (IParameter)new LazyModelParameter(
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
    // LazyModelTableValuedFunction : ITableValuedFunction
    // =========================================================================
    internal sealed class LazyModelTableValuedFunction : LazySchemaOwnedBase, ITableValuedFunction
    {
        private readonly TSqlObject _fnObj;
        private IMetadataOrderedCollection<IColumn>? _columns;
        private IMetadataOrderedCollection<IParameter>? _parameters;

        public LazyModelTableValuedFunction(LazyModelSchema schema, TSqlObject fnObj, bool isUserDefined)
            : base(schema, fnObj.Name.Parts[fnObj.Name.Parts.Count - 1], isUserDefined)
        {
            _fnObj = fnObj;
        }

        // ITabular / IDatabaseTable — lazy columns, populated on first access
        public IMetadataOrderedCollection<IColumn> Columns =>
            _columns ??= new LazyOrderedCollection<IColumn>(
                () => _fnObj.GetReferenced(TableValuedFunction.Columns)
                            .Select(c => new LazyModelColumn(this, c.Name.Parts[c.Name.Parts.Count - 1]))
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
                    .Select(p => (IParameter)new LazyModelParameter(
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
    // LazyModelUserDefinedDataType : IUserDefinedDataType
    // =========================================================================
    internal sealed class LazyModelUserDefinedDataType : LazySchemaOwnedBase, IUserDefinedDataType
    {
        public LazyModelUserDefinedDataType(LazyModelSchema schema, string name, bool isUserDefined)
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
    // LazyModelUserDefinedTableType : IUserDefinedTableType
    // =========================================================================
    internal sealed class LazyModelUserDefinedTableType : LazySchemaOwnedBase, IUserDefinedTableType
    {
        public LazyModelUserDefinedTableType(LazyModelSchema schema, string name, bool isUserDefined)
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
    // LazyModelParameter : IScalarParameter
    // Implements IScalarParameter (not just IParameter) because IMetadataObjectVisitor<T>
    // dispatches on the concrete parameter kind. We always produce scalar parameters.
    // =========================================================================
    internal sealed class LazyModelParameter : IScalarParameter
    {
        public LazyModelParameter(string name, bool isOutput)
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
    // LazyModelColumn : IColumn
    // =========================================================================
    internal sealed class LazyModelColumn : IColumn
    {
        private readonly ITabular _parent;
        private readonly string _name;

        public LazyModelColumn(ITabular parent, string name)
        {
            _parent = parent;
            _name   = name;
        }

        public string Name => _name;


        // IScalar
        public IScalarDataType? DataType => null;
        public bool Nullable => true;
        public ScalarType ScalarType => ScalarType.Column;

        // IColumn properties
        public ICollation? Collation => null;
        public ComputedColumnInfo? ComputedColumnInfo => null;
        public IDefaultConstraint? DefaultValue => null;
        public IdentityColumnInfo? IdentityColumnInfo => null;
        public bool InPrimaryKey => false;
        public bool IsColumnSet => false;
        public bool IsGeneratedAlwaysAsRowEnd => false;
        public bool IsGeneratedAlwaysAsRowStart => false;
        public bool IsGeneratedAlwaysAsSequenceNumberEnd => false;
        public bool IsGeneratedAlwaysAsSequenceNumberStart => false;
        public bool IsGeneratedAlwaysAsTransactionIdEnd => false;
        public bool IsGeneratedAlwaysAsTransactionIdStart => false;
        public bool IsSparse => false;
        public ITabular Parent => _parent;
        public bool RowGuidCol => false;

        // IMetadataObject
        public T Accept<T>(IMetadataObjectVisitor<T> visitor) => visitor.Visit((IColumn)this);
    }
}
