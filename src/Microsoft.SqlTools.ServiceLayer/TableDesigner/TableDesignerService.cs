//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    /// <summary>
    /// Class that handles the Table Designer related requests
    /// </summary>
    public sealed class TableDesignerService : IDisposable
    {
        private TableDesignerManager tableDesignerManager = new TableDesignerManager();
        private readonly ConcurrentDictionary<string, SqlTask> publishSqlTasks = new ConcurrentDictionary<string, SqlTask>();
        private bool disposed = false;
        private static readonly Lazy<TableDesignerService> instance = new Lazy<TableDesignerService>(() => new TableDesignerService());

        public TableDesignerService()
        {
            this.tableDesignerManager.ProgressChanged += async (_, args) =>
            {
                this.ReportTaskProgress(args);
                if (this.ServiceHost != null)
                {
                    await this.SendProgress(args.SessionId, args.Operation, args.Status, args.Message);
                }
            };
            this.tableDesignerManager.MessageReceived += async (_, args) =>
            {
                this.ReportTaskMessage(args);
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
                TableInfo tableInfo = requestParams.TableInfo ?? throw new ArgumentNullException(nameof(requestParams.TableInfo));
                string sessionId = string.IsNullOrWhiteSpace(requestParams.SessionId)
                    ? !string.IsNullOrWhiteSpace(tableInfo.Id) ? tableInfo.Id : Guid.NewGuid().ToString()
                    : requestParams.SessionId;
                tableInfo.Id = sessionId;
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
                string originalId = tableInfo.Id;
                var metadata = new TaskMetadata()
                {
                    Name = SR.TableDesignerPublishTaskName,
                    Description = SR.TableDesignerPublishTaskDescription,
                    TaskExecutionMode = TaskExecutionMode.Execute,
                    DatabaseName = tableInfo.Database,
                    ServerName = tableInfo.Server,
                    TargetLocation = tableInfo.ProjectFilePath,
                    OperationName = "TableDesignerPublish",
                };

                SqlTask sqlTask = SqlTaskManager.Instance.CreateTask<SqlTask>(metadata, async (task) =>
                {
                    this.publishSqlTasks[originalId] = task;
                    try
                    {
                        PublishTableChangesResponse result = await Task.Run(() =>
                        {
                            return this.tableDesignerManager.PublishTableChanges(tableInfo);
                        });

                        await requestContext.SendResult(result);

                        return new TaskResult()
                        {
                            TaskStatus = SqlTaskStatus.Succeeded,
                        };
                    }
                    catch (Exception ex)
                    {
                        return new TaskResult()
                        {
                            TaskStatus = SqlTaskStatus.Failed,
                            ErrorMessage = ex.Message,
                        };
                    }
                    finally
                    {
                        this.publishSqlTasks.TryRemove(originalId, out _);
                    }
                });

                await sqlTask.RunAsync();
                if (sqlTask.TaskStatus == SqlTaskStatus.Failed)
                {
                    throw new Exception(sqlTask.GetLastMessage()?.Description ?? SR.TableDesignerPublishFailed);
                }
            });
        }

        private void ReportTaskProgress(TableDesignerProgressEventArgs args)
        {
            if (IsPublishOperation(args.Operation) && this.publishSqlTasks.TryGetValue(args.SessionId, out SqlTask sqlTask))
            {
                sqlTask.ReportProgress(GetTaskPercent(args.Status), args.Message);
            }
        }

        private void ReportTaskMessage(TableDesignerMessageEventArgs args)
        {
            if (IsPublishOperation(args.Operation) && this.publishSqlTasks.TryGetValue(args.SessionId, out SqlTask sqlTask))
            {
                if (!string.IsNullOrWhiteSpace(args.Message))
                {
                    sqlTask.AddMessage(
                        args.Message,
                        IsErrorMessage(args.MessageType) ? SqlTaskStatus.Failed : SqlTaskStatus.InProgress);
                }

                if (args.Progress.HasValue)
                {
                    sqlTask.ReportProgress(
                        Math.Min(100, Math.Max(0, (int)Math.Round(args.Progress.Value * 100))),
                        args.Message);
                }
            }
        }

        private static bool IsPublishOperation(string operation)
        {
            return string.Equals(operation, "Publish", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsErrorMessage(string messageType)
        {
            return string.Equals(messageType, "Error", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetTaskPercent(string status)
        {
            return string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ? 100 : -1;
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

        private Task SendProgress(string sessionId, string operation, string status, string message)
        {
            if (this.ServiceHost == null)
            {
                return Task.CompletedTask;
            }

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
            if (this.ServiceHost == null)
            {
                return Task.CompletedTask;
            }

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
