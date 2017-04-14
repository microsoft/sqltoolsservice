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

        private RequestContext<ScriptingResult> RequestContext { get; set; }

        public ScriptingScriptOperation(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

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

                this.SendJsonRpcEventAsync(
                    ScriptingCompleteEvent.Type,
                    new ScriptingCompleteParameters { OperationId = this.OperationId });
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
                        DiagnosticMessage = e.ToString(),
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

            bool hasIncludeCriteria = this.Parameters.IncludeObjectCriteria != null && this.Parameters.IncludeObjectCriteria.Count > 0;
            bool hasExcludeCriteria = this.Parameters.ExcludeObjectCriteria != null && this.Parameters.ExcludeObjectCriteria.Count > 0;
            bool hasObjectsSpecified = this.Parameters.DatabaseObjects != null && this.Parameters.DatabaseObjects.Count > 0;

            // If no object selection criteria was specified, we're scripting the entire database
            publishModel.ScriptAllObjects = !(hasIncludeCriteria || hasExcludeCriteria || hasObjectsSpecified);
            if (publishModel.ScriptAllObjects)
            {
                return publishModel;
            }

            // An object selection criteria was specified, so now we need to resolve the SMO Urn instances to script.
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>();

            // The serverName and databaseName are needed to construct the SMO Urn instances we wan't to include/exclude.  We 
            // lazily load these these values, hopefully piggy-backing on the the publishModel.GetDatabaseObjects()  method 
            // call.  This avoids a roundtrip to the server for just the serverName.
            string serverName = null;
            string databaseName = null;

            if (hasIncludeCriteria || hasExcludeCriteria)
            {
                List<ScriptingObject> allObjects = publishModel.GetDatabaseObjects(out serverName, out databaseName);
                selectedObjects = ScriptingObjectMatchProcessor.Match(
                    this.Parameters.IncludeObjectCriteria,
                    this.Parameters.ExcludeObjectCriteria,
                    allObjects);
            }

            // If specific objects are specified, include them.
            if (hasObjectsSpecified)
            {
                selectedObjects = selectedObjects.Union(this.Parameters.DatabaseObjects);
            }

            Logger.Write(
                LogLevel.Normal,
                string.Format(
                    "Scripting object count {0}, objects: {1}",
                    selectedObjects.Count(),
                    string.Join(", ", selectedObjects)));

            if (string.IsNullOrEmpty(serverName))
            {
                serverName = GetServerName(this.Parameters.ConnectionString);
            }

            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = new SqlConnectionStringBuilder(this.Parameters.ConnectionString).InitialCatalog;
            }

            foreach (ScriptingObject scriptingObject in selectedObjects)
            {
                publishModel.SelectedObjects.Add(scriptingObject.ToUrn(serverName, databaseName));
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
                DiagnosticMessage = e.Error.ToString(),
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
                DatabaseObjects = scriptingObjects,
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
                Count = this.scriptedObjectCount,
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

        /// <summary>
        /// Gets the server name from using SMO using the passed connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The server name.</returns>
        /// <remarks>
        /// We resolve the server name using SMO instead of connection string due to docker.  When connecting
        /// to a docker instance, the connection string Server='localhost' however the server name used to construct
        /// the SMO Urn instances must user the docker server name, which is a random name and is not generally specified
        /// in the connection string.
        /// </remarks>
        private static string GetServerName(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                Server server = new Server(new ServerConnection(connection));
                return server.Name;
            }
        }
    }
}
