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

        private async Task HandleGetTableDesignerInfoRequest(TableInfo tableInfo, RequestContext<TableDesignerInfo> requestContext)
        {
            await Task.Run(async () =>
                       {
                           try
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
                           }
                           catch (Exception e)
                           {
                               await requestContext.SendError(e);
                           }
                       });
        }

        private async Task HandleProcessTableDesignerEditRequest(ProcessTableDesignerEditRequestParams requestParams, RequestContext<ProcessTableDesignerEditResponse> requestContext)
        {
            await Task.Run(async () =>
                       {
                           try
                           {
                               switch (requestParams.TableChangeInfo.Type)
                               {
                                   case DesignerEditType.Add:
                                       this.HandleAddItemRequest(requestParams);
                                       break;
                                   case DesignerEditType.Remove:
                                       // TODO: Handle 'Remove' request
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
                           }
                           catch (Exception e)
                           {
                               await requestContext.SendError(e);
                           }
                       });
        }

        private async Task HandleSaveTableChangesRequest(SaveTableChangesRequestParams requestParams, RequestContext<SaveTableChangesResponse> requestContext)
        {
            await Task.Run(async () =>
                       {
                           try
                           {
                               // TODO: Handle the save changes request.
                               await requestContext.SendResult(new SaveTableChangesResponse());
                           }
                           catch (Exception e)
                           {
                               await requestContext.SendError(e);
                           }
                       });
        }


        private async Task HandleDisposeTableDesignerRequest(TableInfo tableInfo, RequestContext<DisposeTableDesignerResponse> requestContext)
        {
            await Task.Run(async () =>
                       {
                           try
                           {
                               // TODO: Handle the dispose table designer request.
                               await requestContext.SendResult(new DisposeTableDesignerResponse());
                           }
                           catch (Exception e)
                           {
                               await requestContext.SendError(e);
                           }
                       });
        }

        private void HandleAddItemRequest(ProcessTableDesignerEditRequestParams requestParams)
        {
            var property = requestParams.TableChangeInfo.Property;
            // Handle the add item request on top level table properties, e.g. Columns, Indexes.
            if (property.GetType() == typeof(string))
            {
                string propertyName = property as string;
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
            // TODO: set the view information
            var view = new TableDesignerView();
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
