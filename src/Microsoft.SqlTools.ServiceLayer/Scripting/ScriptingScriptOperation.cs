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
using static Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptions;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to represent an in-progress script operation.
    /// </summary>
    public sealed class ScriptingScriptOperation : ScriptingOperation
    {
        private bool disposed = false;

        private int scriptedObjectCount = 0;

        private int totalScriptedObjectCount = 0;

        public ScriptingScriptOperation(ScriptingParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);

            this.Parameters = parameters;
        }

        private ScriptingParams Parameters { get; set; }

        /// <summary>
        /// Event raised when a scripting operation has resolved which database objects will be scripted.
        /// </summary>
        public event EventHandler<ScriptingPlanNotificationParams> PlanNotification;

        /// <summary>
        /// Event raised when a scripting operation has made forward progress.
        /// </summary>
        public event EventHandler<ScriptingProgressNotificationParams> ProgressNotification;

        /// <summary>
        /// Event raised when a scripting operation is complete.
        /// </summary>
        /// <remarks>
        /// An event can be completed by the following conditions: success, cancel, error.
        /// </remarks>
        public event EventHandler<ScriptingCompleteParams> CompleteNotification;

        public override void Execute()
        {
            SqlScriptPublishModel publishModel = null;

            try
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                this.ValidateScriptDatabaseParams();

                publishModel = BuildPublishModel();
                publishModel.ScriptItemsCollected += this.OnPublishModelScriptItemsCollected;
                publishModel.ScriptProgress += this.OnPublishModelScriptProgress;
                publishModel.ScriptError += this.OnPublishModelScriptError;

                ScriptOutputOptions outputOptions = new ScriptOutputOptions
                {
                    SaveFileMode = ScriptFileMode.Overwrite,
                    SaveFileType = ScriptFileType.Unicode,          // UTF-16
                    SaveFileName = this.Parameters.FilePath,
                };

                this.CancellationToken.ThrowIfCancellationRequested();

                publishModel.GenerateScript(outputOptions);

                this.CancellationToken.ThrowIfCancellationRequested();

                Logger.Write(
                    LogLevel.Verbose,
                    string.Format(
                        "Sending script complete notification event with total count {0} and scripted count {1}",
                        this.totalScriptedObjectCount,
                        this.scriptedObjectCount));

                this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                {
                    OperationId = this.OperationId,
                    Success = true,
                });
            }
            catch (Exception e)
            {
                if (e.IsOperationCanceledException())
                {
                    Logger.Write(LogLevel.Normal, string.Format("Scripting operation {0} was canceled", this.OperationId));
                    this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                    { 
                        OperationId = this.OperationId,
                        Canceled = true,
                    });
                }
                else
                {
                    Logger.Write(LogLevel.Error, string.Format("Scripting operation {0} failed with exception {1}", this.OperationId, e));
                    this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                    {
                        OperationId = this.OperationId,
                        HasError = true,
                        ErrorMessage = e.Message,
                        ErrorDetails = e.ToString(),
                    });
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

        private void SendCompletionNotificationEvent(ScriptingCompleteParams parameters)
        {
            this.CompleteNotification?.Invoke(this, parameters);
        }

        private void SendPlanNotificationEvent(ScriptingPlanNotificationParams parameters)
        {
            this.PlanNotification?.Invoke(this, parameters);
        }

        private void SendProgressNotificationEvent(ScriptingProgressNotificationParams parameters)
        {
            this.ProgressNotification?.Invoke(this, parameters);
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
                publishModel.AdvancedOptions.GenerateScriptForDependentObjects = BooleanTypeOptions.True;
                return publishModel;
            }

            // An object selection criteria was specified, so now we need to resolve the SMO Urn instances to script.
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>();

            if (hasIncludeCriteria || hasExcludeCriteria)
            {
                // This is an expensive remote call to load all objects from the database.
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
                PropertyInfo advancedOptionPropInfo = advancedOptions.GetType().GetProperty(optionPropInfo.Name);
                if (advancedOptionPropInfo == null)
                {
                    Logger.Write(LogLevel.Warning, string.Format("Invalid property info name {0} could not be mapped to a property on SqlScriptOptions.", optionPropInfo.Name));
                    continue;
                }

                object optionValue = optionPropInfo.GetValue(this.Parameters.ScriptOptions, index: null);
                if (optionValue == null)
                {
#if DEBUG
                    Logger.Write(LogLevel.Verbose, string.Format("Skipping ScriptOptions.{0} since value is null", optionPropInfo.Name));
#endif
                    continue;
                }

                //
                // The ScriptOptions property types from the request will be either a string or a bool?.  
                // The SqlScriptOptions property types from SMO will all be an Enum.  Using reflection, we
                // map the request ScriptOptions values to the SMO SqlScriptOptions values.
                //

                try
                {
                    object smoValue = null;
                    if (optionPropInfo.PropertyType == typeof(bool?))
                    {
                        smoValue = (bool)optionValue ? BooleanTypeOptions.True : BooleanTypeOptions.False;
                    }
                    else
                    {
                        smoValue = Enum.Parse(advancedOptionPropInfo.PropertyType, (string)optionValue, ignoreCase: true);
                    }

#if DEBUG
                    Logger.Write(LogLevel.Verbose, string.Format("Setting ScriptOptions.{0} to value {1}", optionPropInfo.Name, smoValue));
#endif
                    advancedOptionPropInfo.SetValue(advancedOptions, smoValue);
                }
                catch (Exception e)
                {
                    Logger.Write(
                        LogLevel.Warning,
                        string.Format("An exception occurred setting option {0} to value {1}: {2}", optionPropInfo.Name, optionValue, e));
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
                throw new ArgumentException(SR.ScriptingParams_ConnectionString_Property_Invalid, e);
            }

            if (!Directory.Exists(Path.GetDirectoryName(this.Parameters.FilePath)))
            {
                throw new ArgumentException(SR.ScriptingParams_FilePath_Property_Invalid);
            }
        }

        private void OnPublishModelScriptError(object sender, ScriptEventArgs e)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            Logger.Write(
                LogLevel.Verbose,
                string.Format(
                    "Sending scripting error progress event, Urn={0}, OperationId={1}, Completed={2}, Error={3}",
                    e.Urn,
                    this.OperationId,
                    e.Completed,
                    e.Error));

            // Keep scripting...it's a best effort operation.
            e.ContinueScripting = true;

            this.SendProgressNotificationEvent(new ScriptingProgressNotificationParams
            {
                OperationId = this.OperationId,
                ScriptingObject = e.Urn?.ToScriptingObject(),
                Status = "Error",
                CompletedCount = this.scriptedObjectCount,
                TotalCount = this.totalScriptedObjectCount,
                ErrorDetails = e?.ToString(),
            });
        }

        private void OnPublishModelScriptItemsCollected(object sender, ScriptItemsArgs e)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            List<ScriptingObject> scriptingObjects = e.Urns.Select(urn => urn.ToScriptingObject()).ToList();
            this.totalScriptedObjectCount = scriptingObjects.Count;

            Logger.Write(
                LogLevel.Verbose,
                string.Format(
                    "Sending plan notification event with count {0}, objects: {1}", 
                    this.totalScriptedObjectCount, 
                    string.Join(", ", e.Urns)));

            this.SendPlanNotificationEvent(new ScriptingPlanNotificationParams
            {
                OperationId = this.OperationId,
                ScriptingObjects = scriptingObjects,
                Count = scriptingObjects.Count,
            });
        }

        private void OnPublishModelScriptProgress(object sender, ScriptEventArgs e)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            if (e.Completed)
            {
                this.scriptedObjectCount += 1;
            }

            Logger.Write(
                LogLevel.Verbose,
                string.Format(
                    "Sending progress event, Urn={0}, OperationId={1}, Completed={2}, Error={3}",
                    e.Urn,
                    this.OperationId,
                    e.Completed,
                    e.Error));

            this.SendProgressNotificationEvent(new ScriptingProgressNotificationParams
            {
                OperationId = this.OperationId,
                ScriptingObject = e.Urn.ToScriptingObject(),
                Status = e.Completed ? "Completed" : "Progress",
                CompletedCount = this.scriptedObjectCount,
                TotalCount = this.totalScriptedObjectCount,
                ErrorDetails = e?.ToString(),
            });
        }
        
        /// <summary>
        /// Disposes the scripting operation.
        /// </summary>
        public override void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
