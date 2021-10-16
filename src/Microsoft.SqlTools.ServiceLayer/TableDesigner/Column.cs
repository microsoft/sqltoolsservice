//------------------------------------------------------------------------------
// <copyright file="Column.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Relational.Design.Table;
using Microsoft.Data.Tools.Design.Core.Context;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.DesignServices;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Relational.Design.VM
{
    /// <summary>
    /// This class provides VM for a column. It supports basic properties of a column and interelationship between them.
    /// If a property implements a rule, it is made virtual so, any future specialization can override the rule.
    /// </summary>
    internal class Column : Base
    {
        private Table _vmTable;
        private bool _isHighlighted;

        public Column()
            : base(null)
        {
        }

        public Column(SqlColumn sqlCol, Table vmTable)
            : base(sqlCol)
        {
            _vmTable = vmTable;
        }

        public SqlColumn SqlColumn
        {
            get { return this.SqlModelElement as SqlColumn; }
        }

        public SqlSimpleColumn SqlSimpleColumn
        {
            get { return this.SqlModelElement as SqlSimpleColumn; }
        }

        public bool IsHighlighted
        {
            get
            {
                return _isHighlighted;
            }
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnPropertyChanged("IsHighlighted");
                }
            }
        }

        public Table Table
        {
            get
            {
                return _vmTable;
            }
            private set
            {
                _vmTable = value;
            }
        }

        public bool TemporalGeneratedAlwaysColumn
        {
            get
            {
                SqlSimpleColumn col = this.SqlSimpleColumn;

                return col != null && col.GeneratedAlwaysType != SqlColumnGeneratedAlwaysType.None;
            }
        }
        /// <summary>
        /// BrowsableAttribute specifies whether a property or event should be displayed in a Properties window, 
        /// and in the view model we use it to specify whether this property is exposed to unit test
        /// </summary>
        [Browsable(true)]
        public string Name
        {
            get
            {
                return SqlColumn.GetName();
            }
            set
            {
                if (this.Name != value)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (this.SqlModelElement == null)
                        {
                            try
                            {
                                // SqlEtwProvider.EventWriteTableDesignerAddColumns(true);

                                // setting name, but column doesn't exist yet: create new column
                                // make sure it has name
                                EnsureSqlColumn(value);
                            }
                            finally
                            {
                                // SqlEtwProvider.EventWriteTableDesignerAddColumns(false);
                            }
                        }
                        else
                        {
                            try
                            {
                                // SqlEtwProvider.EventWriteTableDesignerRefactorRename(true);
                                PerformEdit(svc => svc.RenameElement(this.SqlColumn, value), refreshItem: true);
                            }
                            finally
                            {
                                // SqlEtwProvider.EventWriteTableDesignerRefactorRename(false);
                            }
                        }
                    }

                    OnPropertyChanged(null);
                }
            }
        }

        [Browsable(true)]
        public bool CanEditDataType
        {
            get
            {
                SqlSimpleColumn simpleColumn = this.SqlSimpleColumn;
                return simpleColumn != null && !simpleColumn.IsFileStream && !this.TemporalGeneratedAlwaysColumn && simpleColumn.GraphType == SqlColumnGraphType.None;
            }
        }

        [Browsable(true)]
        public string DataType
        {
            get
            {
                SqlTypeDisplayBase designTimeSqlTypeDisplay = this.SqlSimpleColumn == null ? null :
                    SqlTypeDisplayFactory.GetDesignTimeSqlTypeDisplay(this.SqlSimpleColumn);
                return designTimeSqlTypeDisplay != null ? designTimeSqlTypeDisplay.DisplayName : null;
            }

            set
            {
                if (DataType != value)
                {
                    ISqlModelUpdatingService svcUpdate = GetPersistDesignerChangeService();
                    SqlTracer.AssertTraceEvent(svcUpdate != null, TraceEventType.Error, SqlTraceId.TableDesigner, "could not get ISqlModelUpdatingService");
                    if (svcUpdate != null)
                    {
                        SqlType sqlType = this.Table.InstDataTypesContainer.GetRepresentedDataType(value);

                        // Only if data type is in the data type list or a valid condensed data type,
                        // should we call model updater API to update the data type for the column
                        if (sqlType != null || svcUpdate.IsTypeSpecValid(SqlSimpleColumn, value))
                        {
                            PerformEdit(
                                svc => VMUtils.SetDataType(svc, this.SqlSimpleColumn, sqlType, value),
                                refreshItem: true, refreshDesignerState: false);
                        }
                        else
                        {
                            // refresh to original value
                            OnPropertyChanged("DataType");
                        }
                    }
                }
            }
        }

        public IEnumerable<SqlTypePickerItem> DataTypes
        {
            get
            {
                return this.Table.DataTypes;
            }
        }

        [Browsable(true)]
        public bool CanEditDescription
        {
            get
            {
                return this.SqlColumn != null;
            }
        }

        [Browsable(true)]
        public string Description
        {
            get
            {
                if (this.SqlColumn != null)
                {
                    return this.SqlColumn.GetDescription();
                }

                return null;
            }
            set
            {
                if (this.Description != value)
                {
                    PerformEdit(svc => this.SqlColumn.SetDescription(svc, value));
                }
            }
        }

        [Browsable(true)]
        public bool CanEditSequence
        {
            get
            {
                return this.SqlSimpleColumn != null;
            }
        }

        [Browsable(true)]
        public string Sequence
        {
            get
            {
                SqlSequence sequence = VMUtils.GetSequenceInDefaultConstraintForDisplay(this.SqlSimpleColumn);
                if (sequence != null)
                {
                    return sequence.GetName(Tools.Schema.ElementNameStyle.FullyQualifiedName);
                }

                return null;
            }

            set
            {
                // TODO (yicecen): Consider Refactoring this logic to get rid off the null object pattern and use directly value variable instead.
                if (Sequence != value)
                {
                    SqlSequencePickerItem sequenceComboItem = this.Table.Sequences.FirstOrDefault<SqlSequencePickerItem>(item => { return (item.Name == value) || (item.FullyQualifiedName == value); });
                    AddNewSqlSequencePickerItem addNewItem;
                    if (sequenceComboItem is NullSqlSequencePickerItem && !String.IsNullOrEmpty(this.Sequence))
                    {
                        PerformEdit(svc => svc.SetSequenceToDefaultValue(this.SqlSimpleColumn, null));
                    }
                    else if ((addNewItem = sequenceComboItem as AddNewSqlSequencePickerItem) != null)
                    {
                        string suggestedName = VMUtils.GetDefaultNameForNewSequence(this);
                        string newName;

                        if (AddNewSqlSequencePickerItem.GetNewSequenceName != null && 
                            AddNewSqlSequencePickerItem.GetNewSequenceName(this.GetEditingContext(), suggestedName, out newName) == true)
                        {
                            newName = VMUtils.GetNonConflictingNameForModelItem<SqlSequence>(this.Table.SqlSchemaModel.GetElements<SqlSequence>(ModelElementQueryFilter.Internal), this.Table.SqlSchemaModel, newName);
                            PerformEdit(svc => svc.SetNewSequenceAndAssociateToDefaultConstraint(this.Table.SqlTable, this.SqlSimpleColumn, newName));
                        }
                        else
                        {
                            OnPropertyChanged("Sequence");
                        }
                        
                    }
                    else if (sequenceComboItem != null && !(sequenceComboItem is NullSqlSequencePickerItem))
                    {
                        SqlTracer.AssertTraceEvent(sequenceComboItem.SqlSequence != null, TraceEventType.Error, SqlTraceId.TableDesigner, "Why sequenceComboItem.SqlSequence is null?");
                        if (sequenceComboItem.SqlSequence != null)
                        {
                            PerformEdit(svc => svc.SetSequenceToDefaultValue(this.SqlSimpleColumn, sequenceComboItem.SqlSequence));
                        }
                    }
                    else
                    {
                        OnPropertyChanged("Sequence");
                    }
                }
            }
        }

        public IEnumerable<SqlSequencePickerItem> Sequences
        {
            get
            {
                return this.Table.Sequences;
            }
        }

        [Browsable(true)]
        public bool CanEditName
        {
            get
            {
                if (this.SqlSimpleColumn != null)
                {
                    return this.SqlSimpleColumn.GraphType == SqlColumnGraphType.None; // graph columns cannot be edited
                }

                // all other columns can be edited
                return true;
            }
        }

        [Browsable(true)]
        public bool CanEditLength
        {
            get
            {
                SqlSimpleColumn simpleColumn = this.SqlSimpleColumn;
                if (simpleColumn != null)
                {
                    return VMUtils.CanDataTypeHaveLength(simpleColumn) &&
                           !this.TemporalGeneratedAlwaysColumn &&
                           !simpleColumn.IsFileStream &&                      // if filestream column, type is fixed to varbinary(MAX)
                           simpleColumn.GraphType == SqlColumnGraphType.None; // graph columns cannot be edited
                }
                return false;
            }
        }

        /// <summary>
        /// Length has to be typeof(string) in order to handle the "MAX" value
        /// </summary>
        [Browsable(true)]
        public string Length
        {
            get { return VMUtils.GetDataTypeLength(this.SqlSimpleColumn); }

            set
            {
                if (this.Length != value)
                {
                    PerformEdit(svc => VMUtils.SetDataTypeLength(svc, this.SqlSimpleColumn, value), refreshItem: true, refreshDesignerState: false);
                }
            }
        }

        [Browsable(true)]
        public bool CanEditPrecision
        {
            get { return VMUtils.CanDataTypeHavePrecision(this.SqlSimpleColumn); }
        }

        [Browsable(true)]
        public int? Precision
        {
            get { return VMUtils.GetDataTypePrecision(this.SqlSimpleColumn); }

            set
            {
                if (this.Precision != value)
                {
                    PerformEdit(svc => VMUtils.SetDataTypePrecision(svc, this.SqlSimpleColumn, value), refreshItem: true, refreshDesignerState: false);
                }
            }
        }

        [Browsable(true)]
        public bool CanEditScale
        {
            get { return VMUtils.CanDataTypeHaveScale(this.SqlSimpleColumn); }
        }

        [Browsable(true)]
        public int? Scale
        {
            get { return VMUtils.GetDataTypeScale(this.SqlSimpleColumn); }

            set
            {
                if (this.Scale != value)
                {
                    PerformEdit(svc => VMUtils.SetDataTypeScale(svc, this.SqlSimpleColumn, value), refreshItem: true, refreshDesignerState: false);
                }
            }
        }

        [Browsable(true)]
        public string DefaultValue
        {
            get
            {
                return VMUtils.GetDefaultValue(this.SqlSimpleColumn);
            }
            set
            {
                if (this.DefaultValue != value)
                {
                    PerformEdit(svc => svc.SetColumnDefaultValue(this.SqlSimpleColumn, value));
                }
            }
        }

        [Browsable(true)]
        public bool CanEditDefaultValue
        {
            get
            {
                return (this.SqlColumn is SqlSimpleColumn && this.SqlSimpleColumn.GraphType == SqlColumnGraphType.None);
            }
        }

        [Browsable(true)]
        public bool CanEditIdentityValues
        {
            get
            {
                return IsIdentity;
            }
        }

        [Browsable(true)]
        public decimal? IdentitySeed
        {
            get
            {
                return VMUtils.GetIdentitySeed(this.SqlSimpleColumn);
            }

            set
            {
                if (IdentitySeed != value)
                {
                    PerformEdit(svc => VMUtils.SetIdentitySeed(svc, this.SqlSimpleColumn, value));
                }
            }
        }

        [Browsable(true)]
        public decimal? IdentityIncrement
        {
            get
            {
                return VMUtils.GetIdentityIncrement(this.SqlSimpleColumn);
            }
            set
            {
                if (IdentityIncrement != value)
                {
                    PerformEdit(svc => VMUtils.SetIdentityIncrement(svc, this.SqlSimpleColumn, value));
                }
            }
        }

        [Browsable(true)]
        public ColumnType ColType
        {
            get
            {
                if (IsPrimary)
                {
                    return ColumnType.PrimaryKey;
                }

                return ColumnType.None;
            }
        }

        [Browsable(true)]
        public bool CanEditRequired
        {
            get
            {
                return (this.SqlColumn is SqlSimpleColumn && this.SqlSimpleColumn.GraphType == SqlColumnGraphType.None);
            }
        }

        [Browsable(true)]
        public bool CanEditIsNullable
        {
            get
            {
                return !(this.SqlColumn is SqlComputedColumn) && !this.TemporalGeneratedAlwaysColumn
                    && !(this.SqlSimpleColumn != null && this.SqlSimpleColumn.GraphType != SqlColumnGraphType.None);
            }
        }

        [Browsable(true)]
        public bool IsNullable
        {
            get
            {
                if (this.SqlColumn is SqlComputedColumn)
                {
                    return false;
                }

                if (this.SqlSimpleColumn == null)
                {
                    return true;
                }

                return SqlSimpleColumn.IsNullable;
            }
            set
            {
                if (this.IsNullable != value)
                {
                    PerformEdit(svc => svc.SetColumnNullable(this.SqlSimpleColumn, value), refreshItem: true, refreshDesignerState: false);
                }
            }
        }

        [Browsable(true)]
        public bool IsIdentity
        {
            get
            {
                if (SqlSimpleColumn != null)
                {
                    return this.SqlSimpleColumn.IsIdentity;
                }

                return false;
            }
            set
            {
                if (IsIdentity != value)
                {
                    PerformEdit(svc => svc.SetColumnIsIdentity(this.SqlSimpleColumn, value));
                }
            }
        }

        [Browsable(true)]
        public bool CanEditIsIdentity
        {
            get
            {
                return (this.SqlColumn is SqlSimpleColumn && this.SqlSimpleColumn.GraphType == SqlColumnGraphType.None);
            }
        }

        [Browsable(true)]
        public bool CanBePrimary
        {
            get
            {
                // later this can be passed to model
                return VMUtils.CanDataTypeBeID(SqlColumn);
            }
        }

        [Browsable(true)]
        public bool IsPrimary
        {
            get
            {
                return VMUtils.GetIsColumnPrimary(this.SqlColumn);
            }
        }

        /// <summary>
        /// Override ToString() in order to return a readable value for screen reader
        /// </summary>
        public override string ToString()
        {
            // Accessibility BugFix: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/395818
            // In Table Designer when navigating to Primary Key icon, the Narrator doesn't inform it's Primary Key.
            // This is because the Value is not set for the Primary Key to be read by Screen Reader.
            // Previously this method is used to inform the Row Display Index to the Screen Reader.
            // Now this method will check for Primary Key and inform Primary Key with Row Display Index.
            return String.Format(CultureInfo.CurrentCulture, "{0} {1}{2}", IsPrimary ? ViewModelResources.PrimaryKey : string.Empty, ViewModelResources.DataGrid_RowDisplayIndex, this.Table.Columns.IndexOf(this));
        }

        public override EditingContext GetEditingContext()
        {
            return Table != null ? Table.GetEditingContext() : null;
        }

        internal void SetTableParentOnNewColumn(Table table)
        {
            SqlTracer.AssertTraceEvent(table != null, TraceEventType.Error, SqlTraceId.TableDesigner, "Table should not be null");
            SqlTracer.AssertTraceEvent(this.SqlModelElement == null, TraceEventType.Error, SqlTraceId.TableDesigner, "column has already been initialized");
            this.Table = table;
        }

        internal bool Delete()
        {
            if (this.SqlColumn != null && (this.SqlSimpleColumn == null || this.SqlSimpleColumn.GraphType == SqlColumnGraphType.None))
            {
                return PerformEdit(svc => svc.DeleteColumn(this.SqlColumn), refreshItem: false, refreshDesignerState: false);
            }
            return false;
        }

        internal void ReAttach(SqlColumn column)
        {
            if (this.SqlModelElement != column)
            {
                this.SqlModelElement = column;
            }

            // Raise event to force UI refresh
            OnPropertyChanged(null);
        }

        private void EnsureSqlColumn(string columnName)
        {
            if (this.SqlModelElement == null)
            {
                // if the view model doesn't yet have a corresponding column in the model,
                // insert a new column in table at the same position as the current view model column
                int index = this.Table.Columns.IndexOf(this);

                PerformEdit(svc =>
                {
                    svc.InsertSimpleColumnAt(
                        this.Table.SqlTable, columnName ?? ViewModelResources.NewColumnName, index);

                    IList<SqlColumn> columns = this.Table.SqlTable.Columns;
                    SqlTracer.AssertTraceEvent(index < columns.Count, TraceEventType.Error, SqlTraceId.TableDesigner, "invalid column index");
                    if (index < columns.Count)
                    {
                        // attach view model to new SqlColumn
                        this.SqlModelElement = columns[index];
                    }
                }, refreshItem: true, refreshDesignerState: false);

                if (this.SqlModelElement == null)
                {
                    // failed to insert column
                    this.Table.Columns.Remove(this);
                }
            }
        }
    }

    /// <summary>
    /// Column type which is used in the XAML
    /// </summary>
    public enum ColumnType
    {
        None,
        PrimaryKey
    }
}
