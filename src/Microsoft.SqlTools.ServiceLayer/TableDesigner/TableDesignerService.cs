//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
        }

        private async Task HandleGetTableDesignerInfoRequest(TableInfo tableInfo, RequestContext<TableDesignerInfo> requestContext)
        {
            await Task.Run(async () =>
                       {
                           try
                           {
                               // TODO
                               TableDataModel tableModel = new TableDataModel();
                               TableDesignerView view = new TableDesignerView();
                               await requestContext.SendResult(new TableDesignerInfo()
                               {
                                   Data = tableModel,
                                   View = view,
                                   ColumnTypes = new string[] { },
                                   Schemas = new string[] { }
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
                               // TODO
                               await requestContext.SendResult(new ProcessTableDesignerEditResponse()
                               {
                                   Data = requestParams.Data,
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
                               // TODO
                               await requestContext.SendResult(new SaveTableChangesResponse());
                           }
                           catch (Exception e)
                           {
                               await requestContext.SendError(e);
                           }
                       });
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
