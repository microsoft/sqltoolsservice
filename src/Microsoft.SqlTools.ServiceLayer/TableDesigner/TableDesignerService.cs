//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    /// <summary>
    /// Class that handles the Table Designer related requests
    /// </summary>
    public sealed class TableDesignerService : IDisposable
    {
        // The query is copied from SSMS table designer, sys and INFORMATION_SCHEMA can not be selected.
        const string GetSchemasQuery = "select name from sys.schemas where principal_id <> 3 and principal_id <> 4 order by name";

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
            this.ServiceHost.SetRequestHandler(GetTableDesignerInfoRequest.Type, HandleGetTableDesignerInfoRequest);
            this.ServiceHost.SetRequestHandler(ProcessTableDesignerEditRequest.Type, HandleProcessTableDesignerEditRequest);
            this.ServiceHost.SetRequestHandler(SaveTableChangesRequest.Type, HandleSaveTableChangesRequest);
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

        private Task HandleGetTableDesignerInfoRequest(TableInfo tableInfo, RequestContext<TableDesignerInfo> requestContext)
        {
            return this.HandleRequest<TableDesignerInfo>(requestContext, async () =>
            {
                var schemas = this.GetSchemas(tableInfo);
                var viewModel = this.GetTableViewModel(tableInfo, schemas);
                var view = this.GetDesignerViewInfo(tableInfo);
                await requestContext.SendResult(new TableDesignerInfo()
                {
                    ViewModel = viewModel,
                    View = view,
                    ColumnTypes = this.GetSupportedColumnTypes(tableInfo),
                    Schemas = schemas
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
                    default:
                        // TODO: Handle 'Update' request
                        break;
                }
                await requestContext.SendResult(new ProcessTableDesignerEditResponse()
                {
                    ViewModel = requestParams.ViewModel,
                    IsValid = true
                });
            });
        }

        private Task HandleSaveTableChangesRequest(SaveTableChangesRequestParams requestParams, RequestContext<SaveTableChangesResponse> requestContext)
        {
            return this.HandleRequest<SaveTableChangesResponse>(requestContext, async () =>
            {
                // TODO: Handle the save changes request.
                await requestContext.SendResult(new SaveTableChangesResponse());
            });
        }


        private Task HandleDisposeTableDesignerRequest(TableInfo tableInfo, RequestContext<DisposeTableDesignerResponse> requestContext)
        {
            return this.HandleRequest<DisposeTableDesignerResponse>(requestContext, async () =>
            {
                // TODO: Handle the save changes request.
                await requestContext.SendResult(new DisposeTableDesignerResponse());
            });
        }

        private void HandleAddItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var path = requestParams.TableChangeInfo.Path;
            // Handle the add item request on top level table properties, e.g. Columns, Indexes.
            if (path.Length == 1)
            {
                var propertyName = path[0] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        requestParams.ViewModel.Columns.AddNew();
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
            var path = requestParams.TableChangeInfo.Path;
            // Handle the add item request on top level table properties, e.g. Columns, Indexes.
            if (path.Length == 2)
            {
                var propertyName = path[0] as string;
                switch (propertyName)
                {
                    case TablePropertyNames.Columns:
                        requestParams.ViewModel.Columns.Data.RemoveAt(Convert.ToInt32(path[1]));
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

        private List<string> GetSupportedColumnTypes(TableInfo tableInfo)
        {
            //TODO: get the supported column types.
            var columnTypes = new List<string>();
            return columnTypes;
        }

        private TableViewModel GetTableViewModel(TableInfo tableInfo, List<string> schemas)
        {
            var tableViewModel = new TableViewModel();
            // Schema
            if (tableInfo.IsNewTable)
            {
                tableViewModel.Schema.Value = schemas.Contains("dbo") ? "dbo" : schemas[0];
            }
            else
            {
                tableViewModel.Schema.Value = tableInfo.Schema;
            }

            // Table Name
            if (!tableInfo.IsNewTable)
            {
                tableViewModel.Name.Value = tableInfo.Name;
            }

            // TODO: set other properties of the table
            return tableViewModel;
        }

        private TableDesignerView GetDesignerViewInfo(TableInfo tableInfo)
        {
            var view = new TableDesignerView();
            view.AdditionalTableColumnProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.IsIdentity,
                Description = SR.TableColumnIsIdentityPropertyDescription,
                Group = SR.TableColumnIdentityGroupName,
                ComponentType = DesignerComponentType.Checkbox,
                ComponentProperties = new CheckBoxProperties()
                {
                    Title = SR.TableColumnIsIdentityPropertyTitle
                }
            });
            view.AdditionalTableColumnProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.IdentitySeed,
                Description = SR.TableColumnIdentitySeedPropertyDescription,
                Group = SR.TableColumnIdentityGroupName,
                ComponentType = DesignerComponentType.Input,
                ComponentProperties = new InputBoxProperties()
                {
                    Title = SR.TableColumnIdentitySeedPropertyTitle
                }
            });
            view.AdditionalTableColumnProperties.Add(new DesignerDataPropertyInfo()
            {
                PropertyName = TableColumnPropertyNames.IdentityIncrement,
                Description = SR.TableColumnIdentityIncrementPropertyDescription,
                Group = SR.TableColumnIdentityGroupName,
                ComponentType = DesignerComponentType.Input,
                ComponentProperties = new InputBoxProperties()
                {
                    Title = SR.TableColumnIdentityIncrementPropertyTitle
                }
            });
            view.CanAddColumns = true;
            view.CanRemoveColumns = true;
            return view;
        }

        private List<string> GetSchemas(TableInfo tableInfo)
        {
            var schemas = new List<string>();
            ReliableConnectionHelper.ExecuteReader(tableInfo.ConnectionString, GetSchemasQuery, (reader) =>
            {
                while (reader.Read())
                {
                    schemas.Add(reader[0].ToString());
                }
            });
            return schemas;
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
