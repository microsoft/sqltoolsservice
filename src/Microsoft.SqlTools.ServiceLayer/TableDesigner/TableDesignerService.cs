//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;
using Dac = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using STSHost = Microsoft.SqlTools.ServiceLayer.Hosting.ServiceHost;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    /// <summary>
    /// Class that handles the Table Designer related requests
    /// </summary>
    public sealed class TableDesignerService : IDisposable
    {
        private Dictionary<string, Dac.TableDesigner> idTableMap = new Dictionary<string, Dac.TableDesigner>();
        private bool disposed = false;
        private static readonly Lazy<TableDesignerService> instance = new Lazy<TableDesignerService>(() => new TableDesignerService());

        public TableDesignerService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static TableDesignerService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the table designer service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(InitializeTableDesignerRequest.Type, HandleInitializeTableDesignerRequest);
            this.ServiceHost.SetRequestHandler(ProcessTableDesignerEditRequest.Type, HandleProcessTableDesignerEditRequest);
            this.ServiceHost.SetRequestHandler(PublishTableChangesRequest.Type, HandlePublishTableChangesRequest);
            this.ServiceHost.SetRequestHandler(GenerateScriptRequest.Type, HandleGenerateScriptRequest);
            this.ServiceHost.SetRequestHandler(GeneratePreviewReportRequest.Type, HandleGeneratePreviewReportRequest);
            this.ServiceHost.SetRequestHandler(DisposeTableDesignerRequest.Type, HandleDisposeTableDesignerRequest);
        }
        private Task HandleRequest<T>(RequestContext<T> requestContext, Func<Task> action)
        {
            // The request handling will take some time to return, we need to use a separate task to run the request handler so that it won't block the main thread.
            // For any specific table designer instance, ADS UI can make sure there are at most one request being processed at any given time, so we don't have to worry about race conditions.
            Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
            return Task.CompletedTask;
        }

        private Task HandleInitializeTableDesignerRequest(TableInfo tableInfo, RequestContext<TableDesignerInfo> requestContext)
        {
            return this.HandleRequest<TableDesignerInfo>(requestContext, async () =>
            {
                var tableDesigner = this.CreateTableDesigner(tableInfo);
                var viewModel = this.GetTableViewModel(tableInfo);
                var view = this.GetDesignerViewInfo(tableInfo);
                await requestContext.SendResult(new TableDesignerInfo()
                {
                    ViewModel = viewModel,
                    View = view,
                    ColumnTypes = tableDesigner.DataTypes.ToList(),
                    Schemas = tableDesigner.Schemas.ToList()
                });
            });
        }

        private Task HandleProcessTableDesignerEditRequest(ProcessTableDesignerEditRequestParams requestParams, RequestContext<ProcessTableDesignerEditResponse> requestContext)
        {
            return this.HandleRequest<ProcessTableDesignerEditResponse>(requestContext, async () =>
            {
                DesignerPathUtils.Validate(requestParams.TableChangeInfo.Path, requestParams.TableChangeInfo.Type);
                switch (requestParams.TableChangeInfo.Type)
                {
                    case DesignerEditType.Add:
                        this.HandleAddItemRequest(requestParams);
                        break;
                    case DesignerEditType.Remove:
                        this.HandleRemoveItemRequest(requestParams);
                        break;
                    case DesignerEditType.Update:
                        this.HandleUpdateItemRequest(requestParams);
                        break;
                    default:
                        break;
                }
                await requestContext.SendResult(new ProcessTableDesignerEditResponse()
                {
                    ViewModel = this.GetTableViewModel(requestParams.TableInfo),
                    IsValid = true
                });
            });
        }

        private Task HandlePublishTableChangesRequest(TableInfo tableInfo, RequestContext<PublishTableChangesResponse> requestContext)
        {
            return this.HandleRequest<PublishTableChangesResponse>(requestContext, async () =>
            {
                var tableDesigner = this.GetTableDesigner(tableInfo);
                tableDesigner.CommitChanges();
                string newId = string.Format("{0}|{1}|{2}|{3}|{4}", STSHost.ProviderName, tableInfo.Server, tableInfo.Database, tableDesigner.TableViewModel.Schema, tableDesigner.TableViewModel.Name);
                string oldId = tableInfo.Id;
                this.idTableMap.Remove(oldId);
                if (newId != oldId)
                {
                    tableInfo.Name = tableDesigner.TableViewModel.Name;
                    tableInfo.Schema = tableDesigner.TableViewModel.Schema;
                    tableInfo.IsNewTable = false;
                    tableInfo.Id = newId;
                }
                // Recreate the table designer after the changes are published to make sure the table information is up to date.
                // Todo: improve the dacfx table designer feature, so that we don't have to recreate it.
                this.CreateTableDesigner(tableInfo);
                await requestContext.SendResult(new PublishTableChangesResponse()
                {
                    NewTableInfo = tableInfo,
                    ViewModel = this.GetTableViewModel(tableInfo)
                });
            });
        }

        private Task HandleGenerateScriptRequest(TableInfo tableInfo, RequestContext<string> requestContext)
        {
            return this.HandleRequest<string>(requestContext, async () =>
            {
                var table = this.GetTableDesigner(tableInfo);
                var script = table.GenerateScript();
                await requestContext.SendResult(script);
            });
        }

        private Task HandleGeneratePreviewReportRequest(TableInfo tableInfo, RequestContext<string> requestContext)
        {
            return this.HandleRequest<string>(requestContext, async () =>
            {
                var table = this.GetTableDesigner(tableInfo);
                var report = table.GenerateReport();
                await requestContext.SendResult(report);
            });
        }

        private Task HandleDisposeTableDesignerRequest(TableInfo tableInfo, RequestContext<DisposeTableDesignerResponse> requestContext)
        {
            return this.HandleRequest<DisposeTableDesignerResponse>(requestContext, async () =>
            {
                this.idTableMap.Remove(tableInfo.Id);
                await requestContext.SendResult(new DisposeTableDesignerResponse());
            });
        }

        private void HandleAddItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var table = this.GetTableDesigner(requestParams.TableInfo).TableViewModel;
            var path = requestParams.TableChangeInfo.Path;
            // Handle the add item request on top level table properties, e.g. Columns, Indexes.
            if (path.Length == 1)
            {
                var propertyName = path[0] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        table.Columns.AddNew();
                        break;
                    case TablePropertyNames.CheckConstraints:
                        table.CheckConstraints.AddNew();
                        break;
                    case TablePropertyNames.ForeignKeys:
                        table.ForeignKeys.AddNew();
                        break;
                    case TablePropertyNames.Indexes:
                        table.Indexes.AddNew();
                        break;
                    default:
                        break;
                }
            }
            else if (path.Length == 3)
            {
                var propertyNameL1 = path[0] as string;
                var indexL1 = Convert.ToInt32(path[1]);
                var propertyNameL2 = path[2] as string;
                switch (propertyNameL1)
                {
                    case TablePropertyNames.ForeignKeys:
                        switch (propertyNameL2)
                        {
                            case ForeignKeyPropertyNames.ColumnMapping:
                                table.ForeignKeys.Items[indexL1].AddNewColumnMapping();
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.Indexes:
                        switch (propertyNameL2)
                        {
                            case IndexPropertyNames.Columns:
                                table.Indexes.Items[indexL1].AddNewColumnSpecification();
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void HandleRemoveItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var table = this.GetTableDesigner(requestParams.TableInfo).TableViewModel;
            var path = requestParams.TableChangeInfo.Path;
            // Handle the add item request on top level table properties, e.g. Columns, Indexes.
            if (path.Length == 2)
            {
                var propertyName = path[0] as string;
                var objIndex = Convert.ToInt32(path[1]);
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        table.Columns.RemoveAt(objIndex);
                        break;
                    case TablePropertyNames.CheckConstraints:
                        table.CheckConstraints.RemoveAt(objIndex);
                        break;
                    case TablePropertyNames.ForeignKeys:
                        table.ForeignKeys.RemoveAt(objIndex);
                        break;
                    case TablePropertyNames.Indexes:
                        table.Indexes.RemoveAt(objIndex);
                        break;
                    default:
                        break;
                }
            }
            else if (path.Length == 4)
            {
                var propertyNameL1 = path[0] as string;
                var indexL1 = Convert.ToInt32(path[1]);
                var propertyNameL2 = path[2] as string;
                var indexL2 = Convert.ToInt32(path[3]);
                switch (propertyNameL1)
                {
                    case TablePropertyNames.ForeignKeys:
                        switch (propertyNameL2)
                        {
                            case ForeignKeyPropertyNames.ColumnMapping:
                                table.ForeignKeys.Items[indexL1].RemoveColumnMapping(indexL2);
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.Indexes:
                        switch (propertyNameL2)
                        {
                            case IndexPropertyNames.Columns:
                                table.Indexes.Items[indexL1].RemoveColumnSpecification(indexL2);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void HandleUpdateItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var table = this.GetTableDesigner(requestParams.TableInfo).TableViewModel;
            var path = requestParams.TableChangeInfo.Path;
            var newValue = requestParams.TableChangeInfo.Value;
            if (path.Length == 1)
            {
                var propertyName = path[0] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Description:
                        table.Description = GetStringValue(newValue);
                        break;
                    case TablePropertyNames.Name:
                        table.Name = GetStringValue(newValue);
                        break;
                    case TablePropertyNames.Schema:
                        table.Schema = GetStringValue(newValue);
                        break;
                    default:
                        break;
                }
            }
            else if (path.Length == 3)
            {
                var propertyNameL1 = path[0] as string;
                var indexL1 = Convert.ToInt32(path[1]);
                var propertyNameL2 = path[2] as string;
                switch (propertyNameL1)
                {
                    case TablePropertyNames.Columns:
                        var column = table.Columns.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case TableColumnPropertyNames.AllowNulls:
                                column.IsNullable = GetBooleanValue(newValue);
                                break;
                            case TableColumnPropertyNames.DefaultValue:
                                column.DefaultValue = GetStringValue(newValue);
                                break;
                            case TableColumnPropertyNames.IdentityIncrement:
                                column.IdentityIncrement = GetInt32Value(newValue);
                                break;
                            case TableColumnPropertyNames.IdentitySeed:
                                column.IdentitySeed = GetInt32Value(newValue);
                                break;
                            case TableColumnPropertyNames.IsIdentity:
                                column.IsIdentity = GetBooleanValue(newValue);
                                break;
                            case TableColumnPropertyNames.IsPrimaryKey:
                                column.IsPrimaryKey = GetBooleanValue(newValue);
                                break;
                            case TableColumnPropertyNames.Length:
                                column.Length = GetStringValue(newValue);
                                break;
                            case TableColumnPropertyNames.Name:
                                column.Name = GetStringValue(newValue);
                                break;
                            case TableColumnPropertyNames.Precision:
                                column.Precision = GetInt32Value(newValue);
                                break;
                            case TableColumnPropertyNames.Scale:
                                column.Scale = GetInt32Value(newValue);
                                break;
                            case TableColumnPropertyNames.Type:
                                column.DataType = GetStringValue(newValue);
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.CheckConstraints:
                        var checkConstraint = table.CheckConstraints.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case CheckConstraintPropertyNames.Name:
                                checkConstraint.Name = GetStringValue(newValue);
                                break;
                            case CheckConstraintPropertyNames.Enabled:
                                checkConstraint.Enabled = GetBooleanValue(newValue);
                                break;
                            case CheckConstraintPropertyNames.Expression:
                                checkConstraint.Expression = GetStringValue(newValue);
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.ForeignKeys:
                        var foreignKey = table.ForeignKeys.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case ForeignKeyPropertyNames.Enabled:
                                foreignKey.Enabled = GetBooleanValue(newValue);
                                break;
                            case ForeignKeyPropertyNames.IsNotForReplication:
                                foreignKey.IsNotForReplication = GetBooleanValue(newValue);
                                break;
                            case ForeignKeyPropertyNames.Name:
                                foreignKey.Name = GetStringValue(newValue);
                                break;
                            case ForeignKeyPropertyNames.OnDeleteAction:
                                foreignKey.OnDeleteAction = SqlForeignKeyActionUtil.GetValue(GetStringValue(newValue));
                                break;
                            case ForeignKeyPropertyNames.OnUpdateAction:
                                foreignKey.OnUpdateAction = SqlForeignKeyActionUtil.GetValue(GetStringValue(newValue));
                                break;
                            case ForeignKeyPropertyNames.ForeignTable:
                                foreignKey.ForeignTable = GetStringValue(newValue);
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.Indexes:
                        var sqlIndex = table.Indexes.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case IndexPropertyNames.Enabled:
                                sqlIndex.Enabled = GetBooleanValue(newValue);
                                break;
                            case IndexPropertyNames.IsClustered:
                                sqlIndex.IsClustered = GetBooleanValue(newValue);
                                break;
                            case IndexPropertyNames.IsUnique:
                                sqlIndex.IsUnique = GetBooleanValue(newValue);
                                break;
                            case IndexPropertyNames.Name:
                                sqlIndex.Name = GetStringValue(newValue);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (path.Length == 5)
            {
                var propertyNameL1 = path[0] as string;
                var indexL1 = Convert.ToInt32(path[1]);
                var propertyNameL2 = path[2] as string;
                var indexL2 = Convert.ToInt32(path[3]);
                var propertyNameL3 = path[4] as string;
                switch (propertyNameL1)
                {
                    case TablePropertyNames.ForeignKeys:
                        switch (propertyNameL2)
                        {
                            case ForeignKeyPropertyNames.ColumnMapping:
                                var foreignKey = table.ForeignKeys.Items[indexL1];
                                switch (propertyNameL3)
                                {
                                    case ForeignKeyColumnMappingPropertyNames.ForeignColumn:
                                        foreignKey.UpdateForeignColumn(indexL2, GetStringValue(newValue));
                                        break;
                                    case ForeignKeyColumnMappingPropertyNames.Column:
                                        foreignKey.UpdateColumn(indexL2, GetStringValue(newValue));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.Indexes:
                        var sqlIndex = table.Indexes.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case IndexPropertyNames.Columns:
                                switch (propertyNameL3)
                                {
                                    case IndexColumnSpecificationPropertyNames.Column:
                                        sqlIndex.UpdateColumnName(indexL2, GetStringValue(newValue));
                                        break;
                                    case IndexColumnSpecificationPropertyNames.Ascending:
                                        sqlIndex.UpdateIsAscending(indexL2, GetBooleanValue(newValue));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private int GetInt32Value(object value)
        {
            return Int32.Parse(value as string);
        }

        private string GetStringValue(object value)
        {
            return value as string;
        }

        private bool GetBooleanValue(object value)
        {
            return (bool)value;
        }

        private TableViewModel GetTableViewModel(TableInfo tableInfo)
        {
            var tableDesigner = this.GetTableDesigner(tableInfo);
            var table = tableDesigner.TableViewModel;
            var tableViewModel = new TableViewModel();
            tableViewModel.Name.Value = table.Name;
            tableViewModel.Schema.Value = table.Schema;
            tableViewModel.Description.Value = table.Description;
            tableViewModel.Description.Enabled = false; // TODO: https://github.com/microsoft/azuredatastudio/issues/18247

            foreach (var column in table.Columns.Items)
            {
                var columnViewModel = new TableColumnViewModel();
                columnViewModel.Name.Value = column.Name;
                columnViewModel.Name.Enabled = column.CanEditName;
                columnViewModel.Length.Value = column.Length;
                columnViewModel.Length.Enabled = column.CanEditLength;
                columnViewModel.Scale.Value = column.Scale?.ToString();
                columnViewModel.Scale.Enabled = column.CanEditScale;
                columnViewModel.Precision.Value = column.Precision?.ToString();
                columnViewModel.Precision.Enabled = column.CanEditPrecision;
                columnViewModel.AllowNulls.Checked = column.IsNullable;
                columnViewModel.AllowNulls.Enabled = column.CanEditIsNullable;
                columnViewModel.DefaultValue.Value = column.DefaultValue;
                columnViewModel.DefaultValue.Enabled = column.CanEditDefaultValue;
                columnViewModel.IsPrimaryKey.Checked = column.IsPrimaryKey;
                columnViewModel.IsPrimaryKey.Enabled = column.CanEditIsPrimaryKey;
                columnViewModel.Type.Value = column.DataType;
                columnViewModel.Type.Enabled = column.CanEditDataType;
                columnViewModel.IsIdentity.Enabled = column.CanEditIsIdentity;
                columnViewModel.IsIdentity.Checked = column.IsIdentity;
                columnViewModel.IdentitySeed.Enabled = column.CanEditIdentityValues;
                columnViewModel.IdentitySeed.Value = column.IdentitySeed?.ToString();
                columnViewModel.IdentityIncrement.Enabled = column.CanEditIdentityValues;
                columnViewModel.IdentityIncrement.Value = column.IdentityIncrement?.ToString();
                tableViewModel.Columns.Data.Add(columnViewModel);
            }

            foreach (var foreignKey in table.ForeignKeys.Items)
            {
                var foreignKeyViewModel = new ForeignKeyViewModel();
                foreignKeyViewModel.Name.Value = foreignKey.Name;
                foreignKeyViewModel.Enabled.Checked = foreignKey.Enabled;
                foreignKeyViewModel.OnDeleteAction.Value = SqlForeignKeyActionUtil.GetName(foreignKey.OnDeleteAction);
                foreignKeyViewModel.OnDeleteAction.Values = SqlForeignKeyActionUtil.ActionNames;
                foreignKeyViewModel.OnUpdateAction.Value = SqlForeignKeyActionUtil.GetName(foreignKey.OnUpdateAction);
                foreignKeyViewModel.OnUpdateAction.Values = SqlForeignKeyActionUtil.ActionNames;
                foreignKeyViewModel.ForeignTable.Value = foreignKey.ForeignTable;
                foreignKeyViewModel.ForeignTable.Values = tableDesigner.AllTables.ToList();
                foreignKeyViewModel.IsNotForReplication.Checked = foreignKey.IsNotForReplication;
                for (int i = 0; i < foreignKey.ForeignColumns.Count; i++)
                {
                    var foreignColumn = foreignKey.ForeignColumns[i];
                    var column = foreignKey.Columns[i];
                    var mapping = new ForeignKeyColumnMapping();
                    mapping.ForeignColumn.Value = foreignColumn;
                    mapping.ForeignColumn.Values = tableDesigner.GetColumnsForTable(foreignKey.ForeignTable).ToList();
                    mapping.Column.Value = column;
                    mapping.Column.Values = tableDesigner.GetColumnsForTable(table.FullName).ToList();
                    foreignKeyViewModel.Columns.Data.Add(mapping);
                }
                tableViewModel.ForeignKeys.Data.Add(foreignKeyViewModel);
            }

            foreach (var checkConstraint in table.CheckConstraints.Items)
            {
                var constraint = new CheckConstraintViewModel();
                constraint.Name.Value = checkConstraint.Name;
                constraint.Expression.Value = checkConstraint.Expression;
                constraint.Enabled.Checked = checkConstraint.Enabled;
                tableViewModel.CheckConstraints.Data.Add(constraint);
            }

            foreach (var index in table.Indexes.Items)
            {
                var indexVM = new IndexViewModel();
                indexVM.Name.Value = index.Name;
                indexVM.IsClustered.Checked = index.IsClustered;
                indexVM.Enabled.Checked = index.Enabled;
                indexVM.IsUnique.Checked = index.IsUnique;
                foreach (var columnSpec in index.Columns)
                {
                    var columnSpecVM = new IndexedColumnSpecification();
                    columnSpecVM.Ascending.Checked = columnSpec.isAscending;
                    columnSpecVM.Column.Value = columnSpec.Column;
                    columnSpecVM.Column.Values = tableDesigner.GetColumnsForTable(table.FullName).ToList();
                    indexVM.Columns.Data.Add(columnSpecVM);
                }
                indexVM.ColumnsDisplayValue.Value = index.ColumnsDisplayValue;
                indexVM.ColumnsDisplayValue.Enabled = false;
                tableViewModel.Indexes.Data.Add(indexVM);
            }
            tableViewModel.Script.Enabled = false;
            tableViewModel.Script.Value = tableDesigner.Script;
            return tableViewModel;
        }

        private TableDesignerView GetDesignerViewInfo(TableInfo tableInfo)
        {
            var view = new TableDesignerView();
            this.SetColumnsViewInfo(view);
            this.SetForeignKeysViewInfo(view);
            this.SetCheckConstraintsViewInfo(view);
            this.SetIndexesViewInfo(view);
            return view;
        }

        private void SetColumnsViewInfo(TableDesignerView view)
        {
            view.ColumnTableOptions.AdditionalProperties.AddRange(new DesignerDataPropertyInfo[] {
                new DesignerDataPropertyInfo()
                {
                    PropertyName = TableColumnPropertyNames.IsIdentity,
                    Description = SR.TableColumnIsIdentityPropertyDescription,
                    Group = SR.TableColumnIdentityGroupName,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableColumnIsIdentityPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = TableColumnPropertyNames.IdentitySeed,
                    Description = SR.TableColumnIdentitySeedPropertyDescription,
                    Group = SR.TableColumnIdentityGroupName,
                    ComponentType = DesignerComponentType.Input,
                    ComponentProperties = new InputBoxProperties()
                    {
                        Title = SR.TableColumnIdentitySeedPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = TableColumnPropertyNames.IdentityIncrement,
                    Description = SR.TableColumnIdentityIncrementPropertyDescription,
                    Group = SR.TableColumnIdentityGroupName,
                    ComponentType = DesignerComponentType.Input,
                    ComponentProperties = new InputBoxProperties()
                    {
                        Title = SR.TableColumnIdentityIncrementPropertyTitle
                    }
                }
            });
            view.ColumnTableOptions.CanAddRows = true;
            view.ColumnTableOptions.CanRemoveRows = true;
            view.ColumnTableOptions.RemoveRowConfirmationMessage = SR.TableDesignerDeleteColumnConfirmationMessage;
            view.ColumnTableOptions.ShowRemoveRowConfirmation = true;
        }

        private void SetForeignKeysViewInfo(TableDesignerView view)
        {
            view.ForeignKeyTableOptions.AdditionalProperties.AddRange(new DesignerDataPropertyInfo[] {
                new DesignerDataPropertyInfo()
                {
                    PropertyName = ForeignKeyPropertyNames.Enabled,
                    Description = SR.ForeignKeyIsEnabledDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerIsEnabledPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = ForeignKeyPropertyNames.IsNotForReplication,
                    Description = SR.ForeignKeyIsNotForReplicationDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.ForeignKeyIsNotForReplicationTitle
                    }
                }
            });
            view.ForeignKeyTableOptions.CanAddRows = true;
            view.ForeignKeyTableOptions.CanRemoveRows = true;
        }

        private void SetCheckConstraintsViewInfo(TableDesignerView view)
        {
            view.CheckConstraintTableOptions.AdditionalProperties.Add(
                new DesignerDataPropertyInfo()
                {
                    PropertyName = CheckConstraintPropertyNames.Enabled,
                    Description = SR.CheckConstraintIsEnabledDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerIsEnabledPropertyTitle
                    }
                });
            view.CheckConstraintTableOptions.CanAddRows = true;
            view.CheckConstraintTableOptions.CanRemoveRows = true;
        }

        private void SetIndexesViewInfo(TableDesignerView view)
        {
            view.IndexTableOptions.AdditionalProperties.AddRange(new DesignerDataPropertyInfo[] {
                new DesignerDataPropertyInfo()
                {
                    PropertyName = IndexPropertyNames.ColumnsDisplayValue,
                    ShowInPropertiesView = false,
                    ComponentType = DesignerComponentType.Input,
                    ComponentProperties = new InputBoxProperties()
                    {
                        Title = SR.TableDesignerColumnsDisplayValueTitle,
                        Width = 200
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = IndexPropertyNames.Enabled,
                    Description = SR.IndexIsEnabledPropertyDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerIsEnabledPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = IndexPropertyNames.IsClustered,
                    Description = SR.IndexIsClusteredPropertyDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerIndexIsClusteredPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = IndexPropertyNames.IsUnique,
                    Description = SR.IndexIsUniquePropertyDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerIsUniquePropertyTitle
                    }
                }
            });
            view.IndexTableOptions.PropertiesToDisplay = new List<string>() { IndexPropertyNames.Name, IndexPropertyNames.ColumnsDisplayValue, IndexPropertyNames.IsClustered, IndexPropertyNames.IsUnique };
            view.IndexTableOptions.CanAddRows = true;
            view.IndexTableOptions.CanRemoveRows = true;

            view.IndexColumnSpecificationTableOptions.AdditionalProperties.Add(
                new DesignerDataPropertyInfo()
                {
                    PropertyName = IndexColumnSpecificationPropertyNames.Ascending,
                    Description = SR.IndexColumnIsAscendingPropertyDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.IndexColumnIsAscendingPropertyTitle
                    }
                });
            view.IndexColumnSpecificationTableOptions.PropertiesToDisplay.AddRange(new string[] { IndexColumnSpecificationPropertyNames.Column, IndexColumnSpecificationPropertyNames.Ascending });
            view.IndexColumnSpecificationTableOptions.CanAddRows = true;
            view.IndexColumnSpecificationTableOptions.CanRemoveRows = true;
        }

        private Dac.TableDesigner CreateTableDesigner(TableInfo tableInfo)
        {
            var connectinStringbuilder = new SqlConnectionStringBuilder(tableInfo.ConnectionString);
            connectinStringbuilder.InitialCatalog = tableInfo.Database;
            var connectionString = connectinStringbuilder.ToString();
            var tableDesigner = new Dac.TableDesigner(connectionString, tableInfo.AccessToken, tableInfo.Schema, tableInfo.Name, tableInfo.IsNewTable);
            this.idTableMap[tableInfo.Id] = tableDesigner;
            return tableDesigner;
        }

        private Dac.TableDesigner GetTableDesigner(TableInfo tableInfo)
        {
            Dac.TableDesigner tableDesigner;
            if (this.idTableMap.TryGetValue(tableInfo.Id, out tableDesigner))
            {
                return tableDesigner;
            }
            else
            {
                throw new KeyNotFoundException(SR.TableNotInitializedException(tableInfo.Id));
            }
        }

        /// <summary>
        /// Disposes the table designer Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
