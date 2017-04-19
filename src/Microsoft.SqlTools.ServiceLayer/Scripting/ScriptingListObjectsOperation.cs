//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to represent an in-progress list objects operation.
    /// </summary>
    public sealed class ScriptingListObjectsOperation : ScriptingOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        private bool disposed = false;

        private int scriptedObjectCount = 0;

        private int totalScriptedObjectCount = 0;

        private ScriptingListObjectsParams Parameters { get; set; }

        public RequestContext<ScriptingListObjectsResult> RequestContext { get; private set; }

        public ScriptingListObjectsOperation(ScriptingListObjectsParams parameters, RequestContext<ScriptingListObjectsResult> requestContext)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("requestContext", requestContext);

            this.OperationId = Guid.NewGuid().ToString();
            this.Parameters = parameters;
            this.RequestContext = requestContext;
        }

        public Task ActiveTask { get; private set; }

        public override Task Execute()
        {
            string operationId = Guid.NewGuid().ToString();
            this.ActiveTask = Task.Run(() => this.InternalExecute(), this.cancellation.Token);
            return this.ActiveTask;
        }

        private void InternalExecute()
        {
            if (this.cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.cancellation.Token);
            }

            SqlScriptPublishModel publishModel = null;

            try
            {
                this.ValidateScriptDatabaseParams();

                publishModel = new SqlScriptPublishModel(this.Parameters.ConnectionString);
                publishModel.ScriptItemsCollected += this.OnPublishModelScriptItemsCollected;
                publishModel.ScriptProgress += this.OnPublishModelScriptProgress;
                publishModel.ScriptError += this.OnPublishModelScriptError;

                List<ScriptingObject> databaseObjects = publishModel.GetDatabaseObjects();

                ScriptingListObjectsCompleteParameters eventParameters = new ScriptingListObjectsCompleteParameters
                {
                    OperationId = this.OperationId,
                    DatabaseObjects = databaseObjects,
                    Count = databaseObjects.Count,
                };

                this.SendJsonRpcEventAsync(ScriptingListObjectsCompleteEvent.Type, eventParameters);
            }
            catch (Exception e)
            {
                if (e.IsOperationCanceledException())
                {
                    this.SendJsonRpcEventAsync(
                        ScriptingCancelEvent.Type,
                        new ScriptingCancelParameters { OperationId = this.OperationId });
                }
                else
                {
                    ScriptingErrorParams eventParams = new ScriptingErrorParams
                    {
                        OperationId = this.OperationId,
                        Message = e.Message,
                        Details = e.ToString(),
                    };

                    this.SendJsonRpcEventAsync(ScriptingErrorEvent.Type, eventParams);
                }
            }
            finally
            {
                if (publishModel != null)
                {
                    publishModel.ScriptItemsCollected -= this.OnPublishModelScriptItemsCollected;
                    publishModel.ScriptProgress -= this.OnPublishModelScriptProgress;
                    publishModel.ScriptError -= this.OnPublishModelScriptError;
                }
            }
        }

        private void ValidateScriptDatabaseParams()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error parsing ConnectionString property", e);
            }
        }

        private void OnPublishModelScriptError(object sender, ScriptEventArgs e)
        {
            ScriptingErrorParams eventParams = new ScriptingErrorParams
            {
                OperationId = this.OperationId,
                Message = e.Error.Message,
                Details = e.Error.ToString(),
            };

            this.SendJsonRpcEventAsync(ScriptingErrorEvent.Type, eventParams);
        }

        private void OnPublishModelScriptItemsCollected(object sender, ScriptItemsArgs e)
        {
            if (this.cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.cancellation.Token);
            }

            List<ScriptingObject> scriptingObjects = e.Urns.Select(urn => urn.ToScriptingObject()).ToList();
            this.totalScriptedObjectCount = scriptingObjects.Count;

            ScriptingPlanNotificationParams eventParams = new ScriptingPlanNotificationParams
            {
                OperationId = this.OperationId,
                ScriptingObjects = scriptingObjects,
                Count = scriptingObjects.Count,
            };

            this.SendJsonRpcEventAsync(ScriptingPlanNotificationEvent.Type, eventParams);
        }

        private void OnPublishModelScriptProgress(object sender, ScriptEventArgs e)
        {
            if (this.cancellation.IsCancellationRequested)
            {
                e.ContinueScripting = false;
                throw new OperationCanceledException(this.cancellation.Token);
            }

            if (e.Completed)
            {
                this.scriptedObjectCount += 1;
            }

            // TODO: Handle the e.Error case.
            ScriptingProgressNotificationParams eventParams = new ScriptingProgressNotificationParams
            {
                OperationId = this.OperationId,
                ScriptingObject = e.Urn.ToScriptingObject(),
                Status = e.Completed ? "Completed" : "Progress",
                CompletedCount = this.scriptedObjectCount,
                TotalCount = this.totalScriptedObjectCount,
            };

            this.SendJsonRpcEventAsync(ScriptingProgressNotificationEvent.Type, eventParams);
        }

        public override void Cancel()
        {
            if (this.cancellation != null && !this.cancellation.IsCancellationRequested)
            {
                Logger.Write(LogLevel.Normal, string.Format("ScriptingOperation.Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
            }
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }

        private void SendJsonRpcEventAsync<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            Task.Run(async () => await this.RequestContext.SendEvent(eventType, eventParams));
        }
    }
}
