//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    /// <summary>
    /// Class that handles the Table Designer related requests
    /// </summary>
    public sealed class TableDesignerService : IDisposable
    {
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
                               // TODO: populate the data and view information
                               TableViewModel tableModel = new TableViewModel();
                               TableDesignerView view = new TableDesignerView();
                               await requestContext.SendResult(new TableDesignerInfo()
                               {
                                   ViewModel = tableModel,
                                   View = view,
                                   ColumnTypes = this.GetSupportedColumnTypes(tableInfo),
                                   Schemas = this.GetSchemas(tableInfo)
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

        private List<string> GetSchemas(TableInfo tableInfo)
        {
            //TODO: get the schemas.
            var schemas = new List<string>();
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
