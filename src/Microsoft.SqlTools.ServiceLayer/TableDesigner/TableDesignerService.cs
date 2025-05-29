//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    /// <summary>
    /// Class that handles the Table Designer related requests
    /// </summary>
    public sealed class TableDesignerService : IDisposable
    {
        private TableDesignerManager tableDesignerManager = new TableDesignerManager();
        private bool disposed = false;
        private static readonly Lazy<TableDesignerService> instance = new Lazy<TableDesignerService>(() => new TableDesignerService());

        public TableDesignerService()
        {
        }

        public TableDesignerSettings Settings { get; private set; } = new TableDesignerSettings();

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
            this.ServiceHost.SetRequestHandler(InitializeTableDesignerRequest.Type, HandleInitializeTableDesignerRequest, true);
            this.ServiceHost.SetRequestHandler(ProcessTableDesignerEditRequest.Type, HandleProcessTableDesignerEditRequest, true);
            this.ServiceHost.SetRequestHandler(PublishTableChangesRequest.Type, HandlePublishTableChangesRequest, true);
            this.ServiceHost.SetRequestHandler(GenerateScriptRequest.Type, HandleGenerateScriptRequest, true);
            this.ServiceHost.SetRequestHandler(GeneratePreviewReportRequest.Type, HandleGeneratePreviewReportRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeTableDesignerRequest.Type, HandleDisposeTableDesignerRequest, true);
            Workspace.WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback(UpdateSettings);

        }

        internal Task UpdateSettings(SqlToolsSettings newSettings, SqlToolsSettings oldSettings, EventContext eventContext)
        {
            Settings.PreloadDatabaseModel = newSettings.MssqlTools.TableDesigner != null ? newSettings.MssqlTools.TableDesigner.PreloadDatabaseModel : false;
            this.tableDesignerManager.AllowDisableAndReenableDdlTriggers = newSettings.MssqlTools.TableDesigner != null ? newSettings.MssqlTools.TableDesigner.AllowDisableAndReenableDdlTriggers : true;
            return Task.FromResult(0);
        }

        private Task HandleInitializeTableDesignerRequest(TableInfo tableInfo, RequestContext<TableDesignerInfo> requestContext)
        {
            return Utils.HandleRequest<TableDesignerInfo>(requestContext, async () =>
            {
                await requestContext.SendResult(this.tableDesignerManager.InitializeTableDesigner(tableInfo));
            });
        }

        private Task HandleProcessTableDesignerEditRequest(ProcessTableDesignerEditRequestParams requestParams, RequestContext<ProcessTableDesignerEditResponse> requestContext)
        {
            return Utils.HandleRequest<ProcessTableDesignerEditResponse>(requestContext, async () =>
            {
                await requestContext.SendResult(this.tableDesignerManager.TableDesignerEdit(requestParams));
            });
        }

        private Task HandlePublishTableChangesRequest(TableInfo tableInfo, RequestContext<PublishTableChangesResponse> requestContext)
        {
            return Utils.HandleRequest<PublishTableChangesResponse>(requestContext, async () =>
            {
                await requestContext.SendResult(this.tableDesignerManager.PublishTableChanges(tableInfo));
            });
        }

        private Task HandleGenerateScriptRequest(TableInfo tableInfo, RequestContext<string> requestContext)
        {
            return Utils.HandleRequest<string>(requestContext, async () =>
            {
                await requestContext.SendResult(this.tableDesignerManager.GenerateScript(tableInfo));
            });
        }

        private Task HandleGeneratePreviewReportRequest(TableInfo tableInfo, RequestContext<GeneratePreviewReportResult> requestContext)
        {
            return Utils.HandleRequest<GeneratePreviewReportResult>(requestContext, async () =>
            {
                await requestContext.SendResult(this.tableDesignerManager.GeneratePreviewReport(tableInfo));
            });
        }

        private Task HandleDisposeTableDesignerRequest(TableInfo tableInfo, RequestContext<DisposeTableDesignerResponse> requestContext)
        {
            return Utils.HandleRequest<DisposeTableDesignerResponse>(requestContext, async () =>
            {
                this.tableDesignerManager.DisposeTableDesigner(tableInfo);
                await requestContext.SendResult(new DisposeTableDesignerResponse());
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
