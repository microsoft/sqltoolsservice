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
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

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
            this.tableDesignerManager.ProgressChanged += async (_, args) =>
            {
                if (this.ServiceHost != null)
                {
                    await this.SendProgress(args.SessionId, args.Operation, args.Status, args.Message);
                }
            };
            this.tableDesignerManager.MessageReceived += async (_, args) =>
            {
                if (this.ServiceHost != null)
                {
                    await this.SendMessage(
                        args.SessionId,
                        args.Operation,
                        args.MessageType,
                        args.Message,
                        args.Number,
                        args.Prefix,
                        args.Progress,
                        args.SchemaName,
                        args.TableName);
                }
            };
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

        private Task HandleInitializeTableDesignerRequest(InitializeTableDesignerRequestParams requestParams, RequestContext<TableDesignerInfo> requestContext)
        {
            return Utils.HandleRequest<TableDesignerInfo>(requestContext, async () =>
            {
                string sessionId = requestParams.SessionId;
                TableInfo tableInfo = requestParams.TableInfo;
                tableInfo.Id = sessionId;
                await this.SendProgress(sessionId, "initialize", "started", "Initializing table designer");
                try
                {
                    TableDesignerInfo result = this.tableDesignerManager.InitializeTableDesigner(tableInfo);
                    await this.SendProgress(sessionId, "initialize", "completed", "Table designer initialized");
                    await requestContext.SendResult(result);
                }
                catch (Exception ex)
                {
                    await this.SendMessage(sessionId, "initialize", "error", ex.Message);
                    await this.SendProgress(sessionId, "initialize", "error", ex.Message);
                    throw;
                }
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
                await this.SendProgress(tableInfo.Id, "publish", "started", "Publishing table changes");
                try
                {
                    string originalId = tableInfo.Id;
                    PublishTableChangesResponse result = this.tableDesignerManager.PublishTableChanges(tableInfo);
                    await this.SendProgress(originalId, "publish", "completed", "Table changes published");
                    await requestContext.SendResult(result);
                }
                catch (Exception ex)
                {
                    await this.SendMessage(tableInfo.Id, "publish", "error", ex.Message);
                    await this.SendProgress(tableInfo.Id, "publish", "error", ex.Message);
                    throw;
                }
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
                await this.SendProgress(tableInfo.Id, "generatePreviewReport", "started", "Generating preview report");
                try
                {
                    GeneratePreviewReportResult result = this.tableDesignerManager.GeneratePreviewReport(tableInfo);
                    await this.SendProgress(tableInfo.Id, "generatePreviewReport", "completed", "Preview report generated");
                    await requestContext.SendResult(result);
                }
                catch (Exception ex)
                {
                    await this.SendMessage(tableInfo.Id, "generatePreviewReport", "error", ex.Message);
                    await this.SendProgress(tableInfo.Id, "generatePreviewReport", "error", ex.Message);
                    throw;
                }
            });
        }

        private Task SendProgress(string sessionId, string operation, string status, string message)
        {
            return this.ServiceHost.SendEvent(TableDesignerProgressNotification.Type, new TableDesignerProgressNotificationParams
            {
                SessionId = sessionId,
                Operation = operation,
                Status = status,
                Message = message
            });
        }

        private Task SendMessage(
            string sessionId,
            string operation,
            string messageType,
            string message,
            int number = 0,
            string prefix = null,
            double? progress = null,
            string schemaName = null,
            string tableName = null)
        {
            return this.ServiceHost.SendEvent(TableDesignerMessageNotification.Type, new TableDesignerMessageNotificationParams
            {
                SessionId = sessionId,
                Operation = operation,
                MessageType = messageType,
                Message = message,
                Number = number,
                Prefix = prefix,
                Progress = progress,
                SchemaName = schemaName,
                TableName = tableName
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
