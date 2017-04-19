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
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to represent an in-progress script operation.
    /// </summary>
    public sealed class ScriptingScriptOperation : ScriptingOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        private bool disposed = false;

        private int scriptedObjectCount = 0;

        private int totalScriptedObjectCount = 0;

        private ScriptingParams Parameters { get; set; }

        public RequestContext<ScriptingResult> RequestContext { get; private set; }

        public ScriptingScriptOperation(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
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

                publishModel = BuildPublishModel();
                publishModel.ScriptItemsCollected += this.OnPublishModelScriptItemsCollected;
                publishModel.ScriptProgress += this.OnPublishModelScriptProgress;
                publishModel.ScriptError += this.OnPublishModelScriptError;

                ScriptOutputOptions outputOptions = new ScriptOutputOptions
                {
                    SaveFileMode = ScriptFileMode.Overwrite,
                    SaveFileType = ScriptFileType.Unicode,
                    SaveFileName = this.Parameters.FilePath,
                };

                publishModel.GenerateScript(outputOptions);

                Logger.Write(
                    LogLevel.Verbose,
                    string.Format(
                        "Sending script complete notification event with total count {0} and scripted count {1}",
                        this.totalScriptedObjectCount,
                        this.scriptedObjectCount));

                this.SendJsonRpcEventAsync(
                    ScriptingCompleteEvent.Type,
                    new ScriptingCompleteParameters { OperationId = this.OperationId });
            }
            catch (Exception e)
            {

                if (e.IsOperationCanceledException())
                {
                    Logger.Write(LogLevel.Normal, string.Format("Scripting operation {0} failed was canceled", this.OperationId));

                    this.SendJsonRpcEventAsync(
                        ScriptingCancelEvent.Type,
                        new ScriptingCancelParameters { OperationId = this.OperationId });
                }
                else
                {
                    Logger.Write(LogLevel.Error, string.Format("Scripting operation {0} failed with exception {1}", this.OperationId, e));

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

        private SqlScriptPublishModel BuildPublishModel()
        {
            SqlScriptPublishModel publishModel = new SqlScriptPublishModel(this.Parameters.ConnectionString);
            PopulateAdvancedScriptOptions(publishModel.AdvancedOptions);

            bool hasIncludeCriteria = this.Parameters.IncludeObjectCriteria != null && this.Parameters.IncludeObjectCriteria.Any();
            bool hasExcludeCriteria = this.Parameters.ExcludeObjectCriteria != null && this.Parameters.ExcludeObjectCriteria.Any();
            bool hasObjectsSpecified = this.Parameters.ScriptingObjects != null && this.Parameters.ScriptingObjects.Any();

            // If no object selection criteria was specified, we're scripting the entire database
            publishModel.ScriptAllObjects = !(hasIncludeCriteria || hasExcludeCriteria || hasObjectsSpecified);
            if (publishModel.ScriptAllObjects)
            {
                return publishModel;
            }

            // An object selection criteria was specified, so now we need to resolve the SMO Urn instances to script.
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>();

            if (hasIncludeCriteria || hasExcludeCriteria)
            {
                List<ScriptingObject> allObjects = publishModel.GetDatabaseObjects();
                selectedObjects = ScriptingObjectMatchProcessor.Match(
                    this.Parameters.IncludeObjectCriteria,
                    this.Parameters.ExcludeObjectCriteria,
                    allObjects);
            }

            // If specific objects are specified, include them.
            if (hasObjectsSpecified)
            {
                selectedObjects = selectedObjects.Union(this.Parameters.ScriptingObjects);
            }

            Logger.Write(
                LogLevel.Normal,
                string.Format(
                    "Scripting object count {0}, objects: {1}",
                    selectedObjects.Count(),
                    string.Join(", ", selectedObjects)));

            string database = new SqlConnectionStringBuilder(this.Parameters.ConnectionString).InitialCatalog;
            foreach (ScriptingObject scriptingObject in selectedObjects)
            {
                publishModel.SelectedObjects.Add(scriptingObject.ToUrn(database));
            }

            return publishModel;
        }

        private void PopulateAdvancedScriptOptions(SqlScriptOptions advancedOptions)
        {
            foreach (PropertyInfo optionPropInfo in this.Parameters.ScriptOptions.GetType().GetProperties())
            {
                string optionName = optionPropInfo.Name;
                object optionValue = optionPropInfo.GetValue(this.Parameters.ScriptOptions, index: null);
                string optionStringValue = optionValue != null ? optionValue.ToString() : null;
                if (optionStringValue != null)
                {
                    PropertyInfo advancedOptionPropInfo = advancedOptions.GetType().GetProperty(optionName);
                    if (advancedOptionPropInfo != null)
                    {
                        try
                        {
                            advancedOptionPropInfo.SetValue(advancedOptions, Enum.Parse(advancedOptionPropInfo.PropertyType, optionStringValue, ignoreCase: true));
                        }
                        catch (Exception e)
                        {
                            Logger.Write(
                                LogLevel.Warning,
                                string.Format("ScriptingOperation.PopulateAdvancedScriptOptions exception {0} {1}: {2}", optionName, optionStringValue, e));
                        }
                    }
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

            if (!Directory.Exists(Path.GetDirectoryName(this.Parameters.FilePath)))
            {
                throw new ArgumentException("Invalid directory specified by the FilePath property.");
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

            Logger.Write(
                LogLevel.Verbose,
                string.Format(
                    "Sending plan notification event with count {0}, objects: {1}", 
                    this.totalScriptedObjectCount, 
                    string.Join(", ", e.Urns)));

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

            Logger.Write(
                LogLevel.Verbose,
                string.Format(
                    "Sending progress event, Urn={0}, OperationId={1}, Completed={2}, Error={3}",
                    e.Urn,
                    this.OperationId,
                    e.Completed,
                    e.Error));

            this.SendJsonRpcEventAsync(ScriptingProgressNotificationEvent.Type, eventParams);
        }

        public override void Cancel()
        {
            if (this.cancellation != null && !this.cancellation.IsCancellationRequested)
            {
                Logger.Write(LogLevel.Verbose, string.Format("ScriptingOperation.Cancel invoked for OperationId {0}", this.OperationId));
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
