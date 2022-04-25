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
using Microsoft.Data.Tools.Sql.DesignServices;
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
                    View = view
                });
            });
        }

        private Task HandleProcessTableDesignerEditRequest(ProcessTableDesignerEditRequestParams requestParams, RequestContext<ProcessTableDesignerEditResponse> requestContext)
        {
            return this.HandleRequest<ProcessTableDesignerEditResponse>(requestContext, async () =>
            {
                var refreshViewRequired = false;
                string inputValidationError = null;
                DesignerPathUtils.Validate(requestParams.TableChangeInfo.Path, requestParams.TableChangeInfo.Type);
                try
                {
                    switch (requestParams.TableChangeInfo.Type)
                    {
                        case DesignerEditType.Add:
                            this.HandleAddItemRequest(requestParams);
                            break;
                        case DesignerEditType.Remove:
                            this.HandleRemoveItemRequest(requestParams);
                            break;
                        case DesignerEditType.Update:
                            refreshViewRequired = this.HandleUpdateItemRequest(requestParams);
                            break;
                        default:
                            break;
                    }
                }
                catch (DesignerValidationException e)
                {
                    inputValidationError = e.Message;
                }
                var designer = this.GetTableDesigner(requestParams.TableInfo);
                var issues = TableDesignerValidator.Validate(designer);
                await requestContext.SendResult(new ProcessTableDesignerEditResponse()
                {
                    ViewModel = this.GetTableViewModel(requestParams.TableInfo),
                    IsValid = issues.Where(i => i.Severity == IssueSeverity.Error).Count() == 0,
                    Issues = issues.ToArray(),
                    View = refreshViewRequired ? this.GetDesignerViewInfo(requestParams.TableInfo) : null,
                    Metadata = this.GetMetadata(requestParams.TableInfo),
                    InputValidationError = inputValidationError
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
                    ViewModel = this.GetTableViewModel(tableInfo),
                    View = GetDesignerViewInfo(tableInfo),
                    Metadata = this.GetMetadata(tableInfo)
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

        private Task HandleGeneratePreviewReportRequest(TableInfo tableInfo, RequestContext<GeneratePreviewReportResult> requestContext)
        {
            return this.HandleRequest<GeneratePreviewReportResult>(requestContext, async () =>
            {
                var generatePreviewReportResult = new GeneratePreviewReportResult();
                try
                {
                    var table = this.GetTableDesigner(tableInfo);
                    var report = table.GenerateReport();
                    generatePreviewReportResult.Report = report;
                    generatePreviewReportResult.MimeType = "text/markdown";
                    generatePreviewReportResult.Metadata = this.GetMetadata(tableInfo);
                    await requestContext.SendResult(generatePreviewReportResult);
                }
                catch (DesignerValidationException e)
                {
                    generatePreviewReportResult.SchemaValidationError = e.Message;
                    await requestContext.SendResult(generatePreviewReportResult);
                }
            });
        }

        private Task HandleDisposeTableDesignerRequest(TableInfo tableInfo, RequestContext<DisposeTableDesignerResponse> requestContext)
        {
            return this.HandleRequest<DisposeTableDesignerResponse>(requestContext, async () =>
            {
                var td = this.GetTableDesigner(tableInfo);
                td.Dispose();
                this.idTableMap.Remove(tableInfo.Id);
                await requestContext.SendResult(new DisposeTableDesignerResponse());
            });
        }

        private void HandleAddItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var table = this.GetTableDesigner(requestParams.TableInfo).TableViewModel;
            var path = requestParams.TableChangeInfo.Path;
            // Handle the add item request on top level table properties, e.g. Columns, Indexes.
            if (path.Length == 2)
            {
                var propertyName = path[0] as string;
                var index = path[1] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        // TODO change the api call to include index
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
                    case TablePropertyNames.EdgeConstraints:
                        table.EdgeConstraints.AddNew();
                        break;
                    case TablePropertyNames.PrimaryKeyColumns:
                        if (table.PrimaryKey == null)
                        {
                            table.CreatePrimaryKey();
                        }
                        table.PrimaryKey.AddNewColumnSpecification();
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
                    case TablePropertyNames.EdgeConstraints:
                        switch (propertyNameL2)
                        {
                            case EdgeConstraintPropertyNames.Clauses:
                                table.EdgeConstraints.Items[indexL1].AddNewClause();
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
                    case TablePropertyNames.EdgeConstraints:
                        table.EdgeConstraints.RemoveAt(objIndex);
                        break;
                    case TablePropertyNames.PrimaryKeyColumns:
                        table.PrimaryKey.RemoveColumnSpecification(objIndex);
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
                    case TablePropertyNames.EdgeConstraints:
                        switch (propertyNameL2)
                        {
                            case EdgeConstraintPropertyNames.Clauses:
                                table.EdgeConstraints.Items[indexL1].RemoveClause(indexL2);
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

        private bool HandleUpdateItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var refreshView = false;
            var tableDesigner = this.GetTableDesigner(requestParams.TableInfo);
            var table = tableDesigner.TableViewModel;
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
                    case TablePropertyNames.GraphTableType:
                        var wasEdgeTable = table.IsEdge;
                        table.IsEdge = false;
                        table.IsNode = false;

                        var newType = GetStringValue(newValue);
                        if (newType == SR.TableDesignerGraphTableTypeNode)
                        {
                            table.IsNode = true;
                        }
                        else if (newType == SR.TableDesignerGraphTableTypeEdge)
                        {
                            table.IsEdge = true;
                        }
                        refreshView = (wasEdgeTable || table.IsEdge) && tableDesigner.IsEdgeConstraintSupported;
                        break;
                    case TablePropertyNames.IsSystemVersioningEnabled:
                        table.IsSystemVersioningEnabled = GetBooleanValue(newValue);
                        refreshView = true;
                        break;
                    case TablePropertyNames.AutoCreateHistoryTable:
                        table.AutoCreateHistoryTable = GetBooleanValue(newValue);
                        refreshView = true;
                        break;
                    case TablePropertyNames.NewHistoryTableTable:
                        table.NewHistoryTableName = GetStringValue(newValue);
                        break;
                    case TablePropertyNames.ExistingHistoryTableName:
                        table.ExistingHistoryTable = GetStringValue(newValue);
                        break;
                    case TablePropertyNames.IsMemoryOptimized:
                        table.IsMemoryOptimized = GetBooleanValue(newValue);
                        refreshView = true;
                        break;
                    case TablePropertyNames.Durability:
                        table.Durability = SqlTableDurabilityUtil.Instance.GetValue(GetStringValue(newValue));
                        break;
                    case TablePropertyNames.PrimaryKeyName:
                        if (table.PrimaryKey != null)
                        {
                            table.PrimaryKey.Name = GetStringValue(newValue);
                        }
                        break;
                    case TablePropertyNames.PrimaryKeyDescription:
                        if (table.PrimaryKey != null)
                        {
                            table.PrimaryKey.Description = GetStringValue(newValue);
                        }
                        break;
                    case TablePropertyNames.PrimaryKeyIsClustered:
                        if (table.PrimaryKey != null)
                        {
                            table.PrimaryKey.IsClustered = GetBooleanValue(newValue);
                        }
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
                            case TableColumnPropertyNames.AdvancedType:
                                column.AdvancedDataType = GetStringValue(newValue);
                                break;
                            case TableColumnPropertyNames.Description:
                                column.Description = GetStringValue(newValue);
                                break;
                            case TableColumnPropertyNames.GeneratedAlwaysAs:
                                column.GeneratedAlwaysAs = ColumnGeneratedAlwaysAsTypeUtil.Instance.GetValue(GetStringValue(newValue));
                                break;
                            case TableColumnPropertyNames.IsHidden:
                                column.IsHidden = GetBooleanValue(newValue);
                                break;
                            case TableColumnPropertyNames.DefaultConstraintName:
                                column.DefaultConstraintName = GetStringValue(newValue);
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
                            case CheckConstraintPropertyNames.Description:
                                checkConstraint.Description = GetStringValue(newValue);
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
                            case ForeignKeyPropertyNames.Description:
                                foreignKey.Description = GetStringValue(newValue);
                                break;
                            case ForeignKeyPropertyNames.OnDeleteAction:
                                foreignKey.OnDeleteAction = SqlForeignKeyActionUtil.Instance.GetValue(GetStringValue(newValue));
                                break;
                            case ForeignKeyPropertyNames.OnUpdateAction:
                                foreignKey.OnUpdateAction = SqlForeignKeyActionUtil.Instance.GetValue(GetStringValue(newValue));
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
                            case IndexPropertyNames.Description:
                                sqlIndex.Description = GetStringValue(newValue);
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.EdgeConstraints:
                        var constraint = table.EdgeConstraints.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case EdgeConstraintPropertyNames.Enabled:
                                constraint.Enabled = GetBooleanValue(newValue);
                                break;
                            case EdgeConstraintPropertyNames.Name:
                                constraint.Name = GetStringValue(newValue);
                                break;
                            case EdgeConstraintPropertyNames.OnDeleteAction:
                                constraint.OnDeleteAction = SqlForeignKeyActionUtil.Instance.GetValue(GetStringValue(newValue));
                                break;
                            default:
                                break;
                        }
                        break;
                    case TablePropertyNames.PrimaryKeyColumns:
                        switch (propertyNameL2)
                        {
                            case IndexColumnSpecificationPropertyNames.Column:
                                table.PrimaryKey.UpdateColumnName(indexL1, GetStringValue(newValue));
                                break;
                            case IndexColumnSpecificationPropertyNames.Ascending:
                                table.PrimaryKey.UpdateIsAscending(indexL1, GetBooleanValue(newValue));
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
                    case TablePropertyNames.EdgeConstraints:
                        var constraint = table.EdgeConstraints.Items[indexL1];
                        switch (propertyNameL2)
                        {
                            case EdgeConstraintPropertyNames.Clauses:
                                switch (propertyNameL3)
                                {
                                    case EdgeConstraintClausePropertyNames.FromTable:
                                        constraint.UpdateFromTable(indexL2, GetStringValue(newValue));
                                        break;
                                    case EdgeConstraintClausePropertyNames.ToTable:
                                        constraint.UpdateToTable(indexL2, GetStringValue(newValue));
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
            return refreshView;
        }

        private void HandleMoveItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var table = this.GetTableDesigner(requestParams.TableInfo).TableViewModel;
            var path = requestParams.TableChangeInfo.Path;
            // Handle the move item request on top level table properties, e.g. Columns, Indexes.
            if (path.Length == 3)
            {
                var propertyName = path[0] as string;
                var fromIndex = path[1] as string;
                var toIndex = path[2] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        // TODO add the actual api call
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
            tableViewModel.Schema.Values = tableDesigner.Schemas.ToList();
            tableViewModel.Description.Value = table.Description;
            var primaryKey = table.PrimaryKey;
            tableViewModel.PrimaryKeyName.Enabled = primaryKey != null;
            tableViewModel.PrimaryKeyIsClustered.Enabled = primaryKey != null;
            tableViewModel.PrimaryKeyDescription.Enabled = primaryKey != null && primaryKey.CanEditDescription;
            if (primaryKey != null)
            {
                tableViewModel.PrimaryKeyName.Value = primaryKey.Name;
                tableViewModel.PrimaryKeyDescription.Value = primaryKey.Description;
                tableViewModel.PrimaryKeyIsClustered.Checked = primaryKey.IsClustered;
                foreach (var cs in primaryKey.Columns)
                {
                    var columnSpecVM = new IndexedColumnSpecification();
                    columnSpecVM.Ascending.Checked = cs.IsAscending;
                    columnSpecVM.Column.Value = cs.Column;
                    columnSpecVM.Column.Values = tableDesigner.GetColumnsForTable(table.FullName).ToList();
                    tableViewModel.PrimaryKeyColumns.Data.Add(columnSpecVM);
                }
            }

            // Graph table related properties
            tableViewModel.GraphTableType.Enabled = table.CanEditGraphTableType;
            tableViewModel.GraphTableType.Values = new List<string>() { SR.TableDesignerGraphTableTypeNone, SR.TableDesignerGraphTableTypeEdge, SR.TableDesignerGraphTableTypeNode };
            tableViewModel.GraphTableType.Value = (table.IsEdge || table.IsNode) ? (table.IsEdge ? SR.TableDesignerGraphTableTypeEdge : SR.TableDesignerGraphTableTypeNode) : SR.TableDesignerGraphTableTypeNone;

            // Memory-optimized related properties
            tableViewModel.IsMemoryOptimized.Checked = table.IsMemoryOptimized;
            tableViewModel.IsMemoryOptimized.Enabled = table.CanEditIsMemoryOptimized;
            tableViewModel.Durability.Enabled = table.CanEditDurability;
            tableViewModel.Durability.Value = SqlTableDurabilityUtil.Instance.GetName(table.Durability);
            tableViewModel.Durability.Values = SqlTableDurabilityUtil.Instance.DisplayNames;

            // Temporal related properties
            var isTemporalTable = table.SystemVersioningHistoryTable != null;
            tableViewModel.IsSystemVersioningEnabled.Enabled = table.CanEditIsSystemVersioningEnabled;
            tableViewModel.IsSystemVersioningEnabled.Checked = isTemporalTable;
            tableViewModel.AutoCreateHistoryTable.Enabled = table.CanEditAutoCreateHistoryTable;
            tableViewModel.AutoCreateHistoryTable.Checked = table.AutoCreateHistoryTable;
            tableViewModel.NewHistoryTableName.Enabled = table.CanEditNewHistoryTableName;
            tableViewModel.NewHistoryTableName.Value = table.NewHistoryTableName;
            tableViewModel.ExistingHistoryTable.Enabled = table.CanEditExistingHistoryTable;
            tableViewModel.ExistingHistoryTable.Value = table.SystemVersioningHistoryTable;
            tableViewModel.ExistingHistoryTable.Values = table.ExistingHistoryTablePropertyOptionalValues.ToList();

            foreach (var column in table.Columns.Items)
            {
                var columnViewModel = new TableColumnViewModel();
                columnViewModel.Name.Value = column.Name;
                columnViewModel.Name.Enabled = column.CanEditName;
                columnViewModel.Description.Value = column.Description;
                columnViewModel.Description.Enabled = column.CanEditDescription;
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
                columnViewModel.IsPrimaryKey.Enabled = true; // To be consistent with SSDT, any column can be a primary key.
                columnViewModel.Type.Value = column.DataType;
                columnViewModel.Type.Enabled = column.CanEditDataType;
                columnViewModel.Type.Values = tableDesigner.DataTypes.ToList();
                columnViewModel.AdvancedType.Value = column.AdvancedDataType;
                columnViewModel.AdvancedType.Enabled = column.CanEditDataType;
                columnViewModel.AdvancedType.Values = column.AdvancedDataTypes.ToList();
                columnViewModel.IsIdentity.Enabled = column.CanEditIsIdentity;
                columnViewModel.IsIdentity.Checked = column.IsIdentity;
                columnViewModel.IdentitySeed.Enabled = column.CanEditIdentityValues;
                columnViewModel.IdentitySeed.Value = column.IdentitySeed?.ToString();
                columnViewModel.IdentityIncrement.Enabled = column.CanEditIdentityValues;
                columnViewModel.IdentityIncrement.Value = column.IdentityIncrement?.ToString();
                columnViewModel.CanBeDeleted = column.CanBeDeleted;
                columnViewModel.GeneratedAlwaysAs.Value = ColumnGeneratedAlwaysAsTypeUtil.Instance.GetName(column.GeneratedAlwaysAs);
                columnViewModel.GeneratedAlwaysAs.Values = ColumnGeneratedAlwaysAsTypeUtil.Instance.DisplayNames;
                columnViewModel.GeneratedAlwaysAs.Enabled = column.CanEditGeneratedAlwaysAs;
                columnViewModel.IsHidden.Checked = column.IsHidden;
                columnViewModel.IsHidden.Enabled = column.CanEditIsHidden;
                columnViewModel.DefaultConstraintName.Enabled = column.CanEditDefaultConstraintName;
                columnViewModel.DefaultConstraintName.Value = column.DefaultConstraintName;
                tableViewModel.Columns.Data.Add(columnViewModel);
            }

            foreach (var foreignKey in table.ForeignKeys.Items)
            {
                var foreignKeyViewModel = new ForeignKeyViewModel();
                foreignKeyViewModel.Name.Value = foreignKey.Name;
                foreignKeyViewModel.Description.Value = foreignKey.Description;
                foreignKeyViewModel.Description.Enabled = foreignKey.CanEditDescription;
                foreignKeyViewModel.Enabled.Checked = foreignKey.Enabled;
                foreignKeyViewModel.OnDeleteAction.Value = SqlForeignKeyActionUtil.Instance.GetName(foreignKey.OnDeleteAction);
                foreignKeyViewModel.OnDeleteAction.Values = SqlForeignKeyActionUtil.Instance.DisplayNames;
                foreignKeyViewModel.OnUpdateAction.Value = SqlForeignKeyActionUtil.Instance.GetName(foreignKey.OnUpdateAction);
                foreignKeyViewModel.OnUpdateAction.Values = SqlForeignKeyActionUtil.Instance.DisplayNames;
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
                constraint.Description.Value = checkConstraint.Description;
                constraint.Description.Enabled = checkConstraint.CanEditDescription;
                constraint.Expression.Value = checkConstraint.Expression;
                constraint.Enabled.Checked = checkConstraint.Enabled;
                tableViewModel.CheckConstraints.Data.Add(constraint);
            }

            foreach (var index in table.Indexes.Items)
            {
                var indexVM = new IndexViewModel();
                indexVM.Name.Value = index.Name;
                indexVM.Name.Enabled = tableInfo.IsNewTable; // renaming an index is not supported, it will cause a new index to be created.
                indexVM.Description.Value = index.Description;
                indexVM.Description.Enabled = index.CanEditDescription;
                indexVM.IsClustered.Checked = index.IsClustered;
                indexVM.Enabled.Checked = index.Enabled;
                indexVM.IsUnique.Checked = index.IsUnique;
                foreach (var columnSpec in index.Columns)
                {
                    var columnSpecVM = new IndexedColumnSpecification();
                    columnSpecVM.Ascending.Checked = columnSpec.IsAscending;
                    columnSpecVM.Column.Value = columnSpec.Column;
                    columnSpecVM.Column.Values = tableDesigner.GetColumnsForTable(table.FullName).ToList();
                    indexVM.Columns.Data.Add(columnSpecVM);
                }
                indexVM.ColumnsDisplayValue.Value = index.ColumnsDisplayValue;
                indexVM.ColumnsDisplayValue.Enabled = false;
                tableViewModel.Indexes.Data.Add(indexVM);
            }

            foreach (var constraint in table.EdgeConstraints.Items)
            {
                var constraintVM = new EdgeConstraintViewModel();
                constraintVM.Name.Value = constraint.Name;
                constraintVM.Description.Value = constraint.Description;
                constraintVM.Description.Enabled = constraint.CanEditDescription;
                constraintVM.Enabled.Checked = constraint.Enabled;
                constraintVM.OnDeleteAction.Value = SqlForeignKeyActionUtil.Instance.GetName(constraint.OnDeleteAction);
                constraintVM.OnDeleteAction.Values = SqlForeignKeyActionUtil.Instance.EdgeConstraintOnDeleteActionNames;
                constraintVM.ClausesDisplayValue.Value = constraint.ClausesDisplayValue;
                constraintVM.ClausesDisplayValue.Enabled = false;
                foreach (var clause in constraint.Clauses)
                {
                    var clauseVM = new EdgeConstraintClause();
                    clauseVM.FromTable.Value = clause.FromTable;
                    clauseVM.FromTable.Values = tableDesigner.AllNodeTables.ToList();
                    clauseVM.ToTable.Value = clause.ToTable;
                    clauseVM.ToTable.Values = tableDesigner.AllNodeTables.ToList();
                    constraintVM.Clauses.Data.Add(clauseVM);
                }
                tableViewModel.EdgeConstraints.Data.Add(constraintVM);
            }
            tableViewModel.Script.Enabled = false;
            tableViewModel.Script.Value = tableDesigner.Script;
            return tableViewModel;
        }

        private TableDesignerView GetDesignerViewInfo(TableInfo tableInfo)
        {
            var tableDesigner = this.GetTableDesigner(tableInfo);
            var view = new TableDesignerView();
            this.SetPrimaryKeyViewInfo(view);
            this.SetColumnsViewInfo(view);
            this.SetForeignKeysViewInfo(view);
            this.SetCheckConstraintsViewInfo(view);
            this.SetIndexesViewInfo(view);
            this.SetGraphTableViewInfo(view, tableDesigner);
            this.SetEdgeConstraintsViewInfo(view, tableDesigner);
            this.SetTemporalTableViewInfo(view, tableDesigner);
            this.SetMemoryOptimizedTableViewInfo(view, tableDesigner);
            return view;
        }

        private void SetPrimaryKeyViewInfo(TableDesignerView view)
        {
            view.AdditionalPrimaryKeyProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TablePropertyNames.PrimaryKeyIsClustered,
                ComponentType = DesignerComponentType.Checkbox,
                Description = SR.IndexIsClusteredPropertyDescription,
                ComponentProperties = new CheckBoxProperties()
                {
                    Title = SR.TableDesignerIndexIsClusteredPropertyTitle
                }
            });
            view.PrimaryKeyColumnSpecificationTableOptions.AdditionalProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = IndexColumnSpecificationPropertyNames.Ascending,
                Description = SR.IndexColumnIsAscendingPropertyDescription,
                ComponentType = DesignerComponentType.Checkbox,
                ComponentProperties = new CheckBoxProperties()
                {
                    Title = SR.IndexColumnIsAscendingPropertyTitle
                }
            });
            view.PrimaryKeyColumnSpecificationTableOptions.PropertiesToDisplay.Add(IndexColumnSpecificationPropertyNames.Column);
            view.PrimaryKeyColumnSpecificationTableOptions.PropertiesToDisplay.Add(IndexColumnSpecificationPropertyNames.Ascending);
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
                        Title = SR.TableColumnIdentitySeedPropertyTitle,
                        InputType = InputType.Number
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
                        Title = SR.TableColumnIdentityIncrementPropertyTitle,
                        InputType = InputType.Number
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = TableColumnPropertyNames.DefaultConstraintName,
                    Description = SR.TableColumnDefaultConstraintNamePropertyDescription,
                    ComponentType = DesignerComponentType.Input,
                    ComponentProperties = new InputBoxProperties()
                    {
                        Title = SR.TableColumnDefaultConstraintNamePropertyTitle
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

        private void SetGraphTableViewInfo(TableDesignerView view, Dac.TableDesigner tableDesigner)
        {
            if (tableDesigner.IsGraphTableSupported && (tableDesigner.IsNewTable || tableDesigner.TableViewModel.IsEdge || tableDesigner.TableViewModel.IsNode))
            {
                view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
                {
                    PropertyName = TablePropertyNames.GraphTableType,
                    ComponentType = DesignerComponentType.Dropdown,
                    Description = SR.TableDesignerGraphTableTypeDescription,
                    Group = SR.TableDesignerGraphTableGroupTitle,
                    ComponentProperties = new DropdownProperties()
                    {
                        Title = SR.TableDesignerGraphTableTypeTitle
                    }
                });
            }
        }

        private void SetEdgeConstraintsViewInfo(TableDesignerView view, Dac.TableDesigner tableDesigner)
        {
            if (!(tableDesigner.TableViewModel.IsEdge && tableDesigner.IsEdgeConstraintSupported))
            {
                return;
            }
            var tab = new DesignerTabView()
            {
                Title = SR.TableDesignerEdgeConstraintsTabTitle
            };
            var constraintsTableProperties = new TableComponentProperties<EdgeConstraintViewModel>()
            {
                Title = SR.TableDesignerEdgeConstraintsTabTitle,
                ObjectTypeDisplayName = SR.TableDesignerEdgeConstraintObjectType,
                LabelForAddNewButton = SR.AddNewEdgeConstraintLabel
            };
            constraintsTableProperties.Columns.AddRange(new string[] { EdgeConstraintPropertyNames.Name, EdgeConstraintPropertyNames.ClausesDisplayValue });
            constraintsTableProperties.ItemProperties.AddRange(new DesignerDataPropertyInfo[] {
                new DesignerDataPropertyInfo()
                {
                    PropertyName = EdgeConstraintPropertyNames.Name,
                    Description = SR.TableDesignerEdgeConstraintNamePropertyDescription,
                    ComponentType = DesignerComponentType.Input,
                    ComponentProperties = new InputBoxProperties()
                    {
                        Width = 200,
                        Title = SR.TableDesignerEdgeConstraintNamePropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = EdgeConstraintPropertyNames.ClausesDisplayValue,
                    ComponentType = DesignerComponentType.Input,
                    ShowInPropertiesView = false,
                    ComponentProperties = new InputBoxProperties()
                    {
                        Width = 300,
                        Title = SR.TableDesignerEdgeConstraintClausesPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = EdgeConstraintPropertyNames.Enabled,
                    Description = SR.TableDesignerEdgeConstraintIsEnabledPropertyDescription,
                    ComponentType = DesignerComponentType.Checkbox,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerEdgeConstraintIsEnabledPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = EdgeConstraintPropertyNames.OnDeleteAction,
                    Description = SR.TableDesignerEdgeConstraintOnDeleteActionPropertyDescription,
                    ComponentType = DesignerComponentType.Dropdown,
                    ComponentProperties = new DropdownProperties()
                    {
                        Title = SR.TableDesignerEdgeConstraintOnDeleteActionPropertyTitle
                    }
                },
                new DesignerDataPropertyInfo()
                {
                    PropertyName = EdgeConstraintPropertyNames.Clauses,
                    Description = SR.TableDesignerEdgeConstraintClausesPropertyDescription,
                    ComponentType = DesignerComponentType.Table,
                    ComponentProperties = new TableComponentProperties<EdgeConstraintClause>()
                    {
                        Title = SR.TableDesignerEdgeConstraintClausesPropertyTitle,
                        ObjectTypeDisplayName = SR.TableDesignerEdgeConstraintClauseObjectType,
                        Columns = new List<string> () { EdgeConstraintClausePropertyNames.FromTable, EdgeConstraintClausePropertyNames.ToTable},
                        LabelForAddNewButton = SR.AddNewClauseLabel,
                        ItemProperties = new List<DesignerDataPropertyInfo>()
                        {
                            new DesignerDataPropertyInfo()
                            {
                                PropertyName = EdgeConstraintClausePropertyNames.FromTable,
                                ComponentType = DesignerComponentType.Dropdown,
                                ComponentProperties = new DropdownProperties()
                                {
                                    Title = SR.TableDesignerEdgeConstraintClauseFromTablePropertyName,
                                    Width = 150
                                }
                            },
                            new DesignerDataPropertyInfo()
                            {
                                PropertyName = EdgeConstraintClausePropertyNames.ToTable,
                                ComponentType = DesignerComponentType.Dropdown,
                                ComponentProperties = new DropdownProperties()
                                {
                                    Title = SR.TableDesignerEdgeConstraintClauseToTablePropertyName,
                                    Width = 150
                                }
                            }
                        }
                    }
                }
             });
            tab.Components.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TablePropertyNames.EdgeConstraints,
                ComponentType = DesignerComponentType.Table,
                ComponentProperties = constraintsTableProperties,
                ShowInPropertiesView = false
            });
            view.AdditionalTabs.Add(tab);
        }

        private void SetTemporalTableViewInfo(TableDesignerView view, Dac.TableDesigner tableDesigner)
        {
            if (!tableDesigner.IsTemporalTableSupported)
            {
                return;
            }
            var table = tableDesigner.TableViewModel;
            view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TablePropertyNames.IsSystemVersioningEnabled,
                ComponentType = DesignerComponentType.Checkbox,
                Description = SR.TableDesignerIsSystemVersioningEnabledDescription,
                Group = SR.TableDesignerSystemVersioningGroupTitle,
                ComponentProperties = new CheckBoxProperties()
                {
                    Title = SR.TableDesignerIsSystemVersioningEnabledTitle
                }
            });

            if (table.OriginalHistoryTable == null && table.SystemVersioningHistoryTable != null)
            {
                view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
                {
                    PropertyName = TablePropertyNames.AutoCreateHistoryTable,
                    ComponentType = DesignerComponentType.Checkbox,
                    Description = SR.TableDesignerAutoCreateHistoryTableDescription,
                    Group = SR.TableDesignerSystemVersioningGroupTitle,
                    ComponentProperties = new CheckBoxProperties()
                    {
                        Title = SR.TableDesignerAutoCreateHistoryTableTitle
                    }
                });
                if (table.AutoCreateHistoryTable)
                {
                    view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
                    {
                        PropertyName = TablePropertyNames.NewHistoryTableTable,
                        ComponentType = DesignerComponentType.Input,
                        Description = SR.TableDesignerNewHistoryTableDescription,
                        Group = SR.TableDesignerSystemVersioningGroupTitle,
                        ComponentProperties = new InputBoxProperties()
                        {
                            Title = SR.TableDesignerNewHistoryTableTitle
                        }
                    });
                }
                else
                {
                    view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
                    {
                        PropertyName = TablePropertyNames.ExistingHistoryTableName,
                        ComponentType = DesignerComponentType.Dropdown,
                        Description = SR.TableDesignerHistoryTableDescription,
                        Group = SR.TableDesignerSystemVersioningGroupTitle,
                        ComponentProperties = new DropdownProperties()
                        {
                            Title = SR.TableDesignerHistoryTableTitle
                        }
                    });
                }
            }
            else if (table.SystemVersioningHistoryTable != null)
            {
                view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
                {
                    PropertyName = TablePropertyNames.ExistingHistoryTableName,
                    ComponentType = DesignerComponentType.Dropdown,
                    Description = SR.TableDesignerHistoryTableDescription,
                    Group = SR.TableDesignerSystemVersioningGroupTitle,
                    ComponentProperties = new DropdownProperties()
                    {
                        Title = SR.TableDesignerHistoryTableTitle
                    }
                });
            }
            view.ColumnTableOptions.AdditionalProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.GeneratedAlwaysAs,
                ComponentType = DesignerComponentType.Dropdown,
                Description = SR.TableDesignerColumnGeneratedAlwaysAsDescription,
                Group = SR.TableDesignerSystemVersioningGroupTitle,
                ComponentProperties = new DropdownProperties()
                {
                    Title = SR.TableDesignerColumnGeneratedAlwaysAsTitle
                }
            });
            view.ColumnTableOptions.AdditionalProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.IsHidden,
                ComponentType = DesignerComponentType.Checkbox,
                Description = SR.TableDesignerColumnIsHiddenDescription,
                Group = SR.TableDesignerSystemVersioningGroupTitle,
                ComponentProperties = new CheckBoxProperties()
                {
                    Title = SR.TableDesignerColumnIsHiddenTitle
                }
            });
        }

        private void SetMemoryOptimizedTableViewInfo(TableDesignerView view, Dac.TableDesigner tableDesigner)
        {
            if (!tableDesigner.IsMemoryOptimizedTableSupported)
            {
                return;
            }
            view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TablePropertyNames.IsMemoryOptimized,
                ComponentType = DesignerComponentType.Checkbox,
                Description = SR.TableDesignerIsMemoryOptimizedDescription,
                Group = SR.TableDesignerMemoryOptimizedGroupTitle,
                ComponentProperties = new CheckBoxProperties()
                {
                    Title = SR.TableDesignerIsMemoryOptimizedTitle
                }
            });
            if (tableDesigner.TableViewModel.IsMemoryOptimized)
            {
                view.AdditionalTableProperties.Add(new DesignerDataPropertyInfo()
                {
                    PropertyName = TablePropertyNames.Durability,
                    ComponentType = DesignerComponentType.Dropdown,
                    Description = SR.TableDesignerDurabilityDescription,
                    Group = SR.TableDesignerMemoryOptimizedGroupTitle,
                    ComponentProperties = new DropdownProperties()
                    {
                        Title = SR.TableDesignerDurabilityTitle
                    }
                });
            }
        }

        private Dac.TableDesigner CreateTableDesigner(TableInfo tableInfo)
        {
            var connectionStringbuilder = new SqlConnectionStringBuilder(tableInfo.ConnectionString);
            connectionStringbuilder.InitialCatalog = tableInfo.Database;
            var connectionString = connectionStringbuilder.ToString();
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

        private Dictionary<string, string> GetMetadata(TableInfo tableInfo)
        {
            var tableDesigner = this.GetTableDesigner(tableInfo);
            var metadata = new Dictionary<string, string>()
            {
                { "IsEdge", tableDesigner.IsEdge().ToString() },
                { "IsNode", tableDesigner.IsNode().ToString() },
                { "IsSystemVersioned", tableDesigner.IsSystemVersioned().ToString() }
            };
            return metadata;
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
