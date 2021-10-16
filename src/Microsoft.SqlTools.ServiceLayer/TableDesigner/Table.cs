//------------------------------------------------------------------------------
// <copyright file="Table.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Relational.Design.Table;
using Microsoft.Data.Tools.Design.Core.Collections;
using Microsoft.Data.Tools.Design.Core.Context;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Relational.Design.VM
{
    /// <summary>
    /// This class provides VM for the table. It supports basic properties of a table and collection of columns
    /// </summary>
    internal class Table : Base, IDisposable
    {
        private EditingContext _editingContext;
        private ObservableCollection<Column> _columns = new ObservableCollection<Column>();
        private InstDataTypesContainer _instDataTypesContainer = null;
        private SortedObservableCollection<SqlTypePickerItem> _dataTypes = null;
        private SortedObservableCollection<SqlSequencePickerItem> _sequences = null;
        private bool _isHekatonTable;

        private Table(SqlTable sqlTable, EditingContext context)
            : base(sqlTable)
        {
            _editingContext = context;
            _isHekatonTable = sqlTable.IsMemoryOptimized;
            SqlSchemaModel sqlModel = sqlTable.Model as SqlSchemaModel;
            if (sqlModel != null)
            {
                sqlModel.ModelChanged += new EventHandler<ModelEventArgs>(OnSqlModelChanged);
            }
        }

        ~Table()
        {
            SqlTracer.DebugTraceEvent(TraceEventType.Error, SqlTraceId.TableDesigner, typeof(Table).Name + ".destructor(): Undisposed Table object");
            Dispose(false);
        }

        public SqlTable SqlTable
        {
            get { return this.SqlModelElement as SqlTable; }
        }

        public InstDataTypesContainer InstDataTypesContainer
        {
            get
            {
                //If IsMemoryOptimized is changed in the model, we will recreate the InstDataTypesContainer
                if (_instDataTypesContainer == null || _isHekatonTable^SqlTable.IsMemoryOptimized)
                {
                    _instDataTypesContainer = SqlTable.IsMemoryOptimized
                        ? new InstHekatonDataTypesContainer(SqlSchemaModel, true)
                        : new InstDataTypesContainer(SqlSchemaModel, true);
                    _isHekatonTable = SqlTable.IsMemoryOptimized;
                }

                return _instDataTypesContainer;
            }
        }

        public SortedObservableCollection<SqlTypePickerItem> DataTypes
        {
            get
            {
                if (_dataTypes == null)
                {
                    _dataTypes = new SortedObservableCollection<SqlTypePickerItem>(new SqlTypesComparer());
                }

                return _dataTypes;
            }
        }

        public SortedObservableCollection<SqlSequencePickerItem> Sequences
        {
            get
            {
                if (_sequences == null)
                {
                    _sequences = new SortedObservableCollection<SqlSequencePickerItem>(new SqlSequenceComparer());
                }

                return _sequences;
            }
        }

        public ObservableCollection<Column> Columns
        {
            get { return _columns; }
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
                return this.SqlModelElement.GetName();
            }
            set
            {
                if (this.Name != value && !string.IsNullOrEmpty(value))
                {
                    PerformEdit(svc => svc.RenameElement(this.SqlTable, value));
                }
            }
        }

        public static Table CreateTable(SqlTable sqlTable, EditingContext editingContext)
        {
            if (sqlTable != null)
            {
                Table vmTable = new Table(sqlTable, editingContext);

                foreach (SqlColumn sqlColumn in sqlTable.Columns)
                {
                    Column vmColumn = new Column(sqlColumn, vmTable);
                    vmTable.Columns.Add(vmColumn);
                }

                vmTable.SubscribeToColumnChanges();
                return vmTable;
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override EditingContext GetEditingContext()
        {
            return _editingContext;
        }

        internal void ReAttach(SqlTable table)
        {
            this.SqlModelElement = table;
            OnPropertyChanged(null);
            this.UnsubscribeToColumnChanges();
            SyncWithModel(table.Columns, Columns);
            this.SubscribeToColumnChanges();
        }

        internal void SetPrimaryKey(IEnumerable<Column> selectedColumns)
        {
            SqlTracer.AssertTraceEvent(selectedColumns != null, TraceEventType.Critical, SqlTraceId.TableDesigner, "Null argument: selectedColumns");

            IEnumerable<SqlColumn> cols =
                from col in selectedColumns where col.SqlColumn != null select col.SqlColumn;
            string constraintName = "PK_" + this.Name;   // TODO: naming convention for new constraints
            PerformEdit(svc => svc.SetPrimaryKey(this.SqlTable, constraintName, cols));
        }

        internal void RemovePrimaryKey(IEnumerable<Column> selectedColumns)
        {
            PerformEdit(svc => svc.RemovePrimaryKey(this.SqlTable));
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                SqlSchemaModel sqlModel = this.SqlSchemaModel;
                if (sqlModel != null && !sqlModel.IsDisposing)
                {
                    sqlModel.ModelChanged -= new EventHandler<ModelEventArgs>(OnSqlModelChanged);
                }
            }
        }

        private void SubscribeToColumnChanges()
        {
            _columns.CollectionChanged += Columns_CollectionChanged;
        }

        private void UnsubscribeToColumnChanges()
        {
            _columns.CollectionChanged -= Columns_CollectionChanged;
        }

        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (object o in e.NewItems)
                {
                    Column col = o as Column;
                    if ((col != null) && (col.SqlModelElement == null))
                    {
                        col.SetTableParentOnNewColumn(this);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (object o in e.OldItems)
                {
                    Column col = o as Column;
                    if (col != null)
                    {
                        col.Delete();
                    }
                }
            }
        }

        private void OnSqlModelChanged(object sender, ModelEventArgs e)
        {
            EditingContext editingContext = this.GetEditingContext();

            if (this.InstDataTypesContainer != null && this.InstDataTypesContainer.InstDataTypes != null && editingContext != null)
            {
                // Handle ModelChanged event to look for changes in the list of available types
                // IDesignerHostService svc = editingContext.Services.GetService<IDesignerHostService>();
                Action operation = () => VerifySqlTypeChanges(e);
                // if (svc != null)
                // {
                //     svc.EnsureInvokeInUIThread(operation);
                // }
                // else
                // {
                    operation();
                // }
            }
        }

        private void VerifySqlTypeChanges(ModelEventArgs e)
        {
            // Look for changes related to SqlType elements
            foreach (Tuple<IModelElement, ModelEventType> change in e.Changes)
            {
                SqlType type = change.Item1 as SqlType;

                if (InstDataTypesContainer.IsValidDataType(type))
                {
                    switch (change.Item2)
                    {
                        case ModelEventType.ElementDeleted:
                            {
                                this.InstDataTypesContainer.DeleteInstType(type);
                            }
                            break;

                        case ModelEventType.ElementAdded:
                            {
                                this.InstDataTypesContainer.AddInstType(type);
                            }
                            break;
                    }
                }
            }
        }

        private void SyncWithModel(IList<SqlColumn> mdlList, ObservableCollection<Column> vmList)
        {
            // reuse existing VM instances and attach new model columns
            // Note: important not to use mdlList[i] as the [] operator has very slow performance
            int initialVmListCount = vmList.Count;
            int i = 0;
            foreach (SqlColumn sqlColumn in mdlList)
            {
                if (i < initialVmListCount)
                {
                    vmList[i].ReAttach(sqlColumn);
                }
                else
                {
                    // create VM instances for any additional columns
                    Column column = new Column(sqlColumn, this);
                    vmList.Add(column);
                }

                i++;
            }

            // remove extra VM instances (i will only be < initialVmListCount
            // if the vmList originally had more members than mdlList)
            if (i < initialVmListCount)
            {
                // need to use vmList.Count rather than initialVmListCount to account for
                // changes to vmList.Count in the case of non-committed columns below
                for (; i < vmList.Count; )
                {
                    // this check needs to be applied to prevent removing not committed column
                    if (vmList[i].SqlModelElement != null)
                    {
                        vmList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
    }
}
