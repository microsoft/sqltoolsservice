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
            this.ServiceHost.SetRequestHandler(SaveTableChangesRequest.Type, HandleSaveTableChangesRequest);
            this.ServiceHost.SetRequestHandler(GenerateScriptRequest.Type, HandleGenerateScriptRequest);
            this.ServiceHost.SetRequestHandler(GenerateReportRequest.Type, HandleGenerateReportRequest);
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
                var connectinStringbuilder = new SqlConnectionStringBuilder(tableInfo.ConnectionString);
                connectinStringbuilder.InitialCatalog = tableInfo.Database;
                var connectionString = connectinStringbuilder.ToString();
                var table = new Dac.TableDesigner(connectionString, tableInfo.Schema, tableInfo.Name, tableInfo.IsNewTable);
                this.idTableMap.Add(tableInfo.Id, table);
                var viewModel = this.GetTableViewModel(tableInfo);
                var view = this.GetDesignerViewInfo(tableInfo);
                await requestContext.SendResult(new TableDesignerInfo()
                {
                    ViewModel = viewModel,
                    View = view,
                    ColumnTypes = table.DataTypes.ToList(),
                    Schemas = table.Schemas.ToList()
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

        private Task HandleSaveTableChangesRequest(TableInfo tableInfo, RequestContext<SaveTableChangesResponse> requestContext)
        {
            return this.HandleRequest<SaveTableChangesResponse>(requestContext, async () =>
            {
                var tableDesigner = this.GetTableDesigner(tableInfo);
                tableDesigner.CommitChanges();
                await requestContext.SendResult(new SaveTableChangesResponse());
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

        private Task HandleGenerateReportRequest(TableInfo tableInfo, RequestContext<string> requestContext)
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
                    default:
                        break;
                }
            }
            else
            {
                // TODO: Handle the add item request on second level properties, e.g. Adding a column to an index
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
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        table.Columns.RemoveAt(Convert.ToInt32(path[1]));
                        break;
                    default:
                        break;
                }
            }
            else
            {
                // TODO: Handle the add item request on second level properties, e.g. Adding a column to an index
            }
        }

        private void HandleUpdateItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var table = this.GetTableDesigner(requestParams.TableInfo).TableViewModel;
            var path = requestParams.TableChangeInfo.Path;

            if (path.Length == 3)
            {
                var propertyName = path[0] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        var colIndex = Convert.ToInt32(path[1]);
                        var colPropertyName = path[2] as string;
                        switch (colPropertyName)
                        {
                            case TableColumnPropertyNames.Name:
                                table.Columns.Items[colIndex].Delete();
                                // table.Columns.Items[colIndex].Name = requestParams.TableChangeInfo.Value as string;
                                break;
                            case TableColumnPropertyNames.Length:
                                table.Columns.Items[colIndex].Length = requestParams.TableChangeInfo.Value as string;
                                break;
                            case TableColumnPropertyNames.AllowNulls:
                                table.Columns.Items[colIndex].IsNullable = (bool)requestParams.TableChangeInfo.Value;
                                break;
                            case TableColumnPropertyNames.Precision:
                                table.Columns.Items[colIndex].Precision = Int32.Parse(requestParams.TableChangeInfo.Value as string);
                                break;
                            case TableColumnPropertyNames.Scale:
                                table.Columns.Items[colIndex].Scale = Int32.Parse(requestParams.TableChangeInfo.Value as string);
                                break;
                            case TableColumnPropertyNames.Type:
                                table.Columns.Items[colIndex].DataType = requestParams.TableChangeInfo.Value as string;
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

        private TableViewModel GetTableViewModel(TableInfo tableInfo)
        {
            var tableDesigner = this.GetTableDesigner(tableInfo);
            var table = tableDesigner.TableViewModel;
            var tableViewModel = new TableViewModel();
            tableViewModel.Name.Value = table.Name;
            tableViewModel.Schema.Value = table.Schema;
            tableViewModel.Description.Value = table.Description;

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
                foreignKeyViewModel.PrimaryKeyTable.Value = foreignKey.PrimaryKeyTable;
                foreignKeyViewModel.PrimaryKeyTable.Values = tableDesigner.AllTables.ToList();
                foreignKeyViewModel.IsNotForReplication.Checked = foreignKey.IsNotForReplication;
                for (int i = 0; i < foreignKey.ForeignKeyColumns.Count; i++)
                {
                    var foreignKeyColumn = foreignKey.ForeignKeyColumns[i];
                    var primaryKeyColumn = foreignKey.PrimaryKeyColumns[i];
                    var mapping = new ForeignKeyColumnMapping();
                    mapping.ForeignKeyColumn.Value = foreignKeyColumn;
                    mapping.ForeignKeyColumn.Values = table.Columns.Items.Select(c => c.Name).ToList();
                    mapping.PrimaryKeyColumn.Value = primaryKeyColumn;
                    mapping.PrimaryKeyColumn.Values = tableDesigner.GetColumnsForTable(foreignKey.PrimaryKeyTable).ToList();
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

            tableViewModel.Script.Enabled = false;
            tableViewModel.Script.Value = tableDesigner.Script;
            // TODO: set other properties of the table
            return tableViewModel;
        }

        private TableDesignerView GetDesignerViewInfo(TableInfo tableInfo)
        {
            var view = new TableDesignerView();
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
            }, new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.IdentitySeed,
                Description = SR.TableColumnIdentitySeedPropertyDescription,
                Group = SR.TableColumnIdentityGroupName,
                ComponentType = DesignerComponentType.Input,
                ComponentProperties = new InputBoxProperties()
                {
                    Title = SR.TableColumnIdentitySeedPropertyTitle
                }
            },new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.IdentityIncrement,
                Description = SR.TableColumnIdentityIncrementPropertyDescription,
                Group = SR.TableColumnIdentityGroupName,
                ComponentType = DesignerComponentType.Input,
                ComponentProperties = new InputBoxProperties()
                {
                    Title = SR.TableColumnIdentityIncrementPropertyTitle
                }
            }});
            view.ColumnTableOptions.canAddRows = true;
            view.ColumnTableOptions.canRemoveRows = true;

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
            }});
            view.ForeignKeyTableOptions.canAddRows = true;
            view.ForeignKeyTableOptions.canRemoveRows = true;
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
            view.CheckConstraintTableOptions.canAddRows = true;
            view.CheckConstraintTableOptions.canRemoveRows = true;
            return view;
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
