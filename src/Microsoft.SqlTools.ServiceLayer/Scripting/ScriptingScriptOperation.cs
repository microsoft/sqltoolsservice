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
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
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

        private int eventSequenceNumber = 1;

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
                    ScriptDestination = (ScriptDestination)Enum.Parse(typeof(ScriptDestination), this.Parameters.ScriptDestination)
                };

                this.CancellationToken.ThrowIfCancellationRequested();

                publishModel.GenerateScript(outputOptions);

                this.CancellationToken.ThrowIfCancellationRequested();

                Logger.Write(
                    LogLevel.Verbose,
                    string.Format(
                        "Sending script complete notification event for operation {0}, sequence number {1} with total count {2} and scripted count {3}",
                        this.OperationId,
                        this.eventSequenceNumber,
                        this.totalScriptedObjectCount,
                        this.scriptedObjectCount));

                this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                {
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
                        Canceled = true,
                    });
                }
                else
                {
                    Logger.Write(LogLevel.Error, string.Format("Scripting operation {0} failed with exception {1}", this.OperationId, e));
                    this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                    {
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
            this.SetCommomEventProperties(parameters);
            this.CompleteNotification?.Invoke(this, parameters);
        }

        private void SendPlanNotificationEvent(ScriptingPlanNotificationParams parameters)
        {
            this.SetCommomEventProperties(parameters);
            this.PlanNotification?.Invoke(this, parameters);
        }

        private void SendProgressNotificationEvent(ScriptingProgressNotificationParams parameters)
        {
            this.SetCommomEventProperties(parameters);
            this.ProgressNotification?.Invoke(this, parameters);
        }

        private void SetCommomEventProperties(ScriptingEventParams parameters)
        {
            parameters.OperationId = this.OperationId;
            parameters.SequenceNumber = this.eventSequenceNumber;
            this.eventSequenceNumber += 1;
        }

        private SqlScriptPublishModel BuildPublishModel()
        {
            SqlScriptPublishModel publishModel = new SqlScriptPublishModel(this.Parameters.ConnectionString);

            // In the getter for SqlScriptPublishModel.AdvancedOptions, there is some strange logic which will 
            // cause the SqlScriptPublishModel.AdvancedOptions to get reset and lose all values based the ordering
            // of when SqlScriptPublishModel.ScriptAllObjects is set.  To workaround this, we initialize with 
            // SqlScriptPublishModel.ScriptAllObjects to true.  If we need to set SqlScriptPublishModel.ScriptAllObjects 
            // to false, it must the last thing we do after setting all SqlScriptPublishModel.AdvancedOptions values.  
            // If we call the SqlScriptPublishModel.AdvancedOptions getter afterwards, all options will be reset.
            //
            publishModel.ScriptAllObjects = true;

            PopulateAdvancedScriptOptions(this.Parameters.ScriptOptions, publishModel.AdvancedOptions);

            // See if any filtering criteria was specified.  If not, we're scripting the entire database.  Otherwise, the filtering
            // criteria should include the target objects to script.
            //
            bool hasIncludeCriteria = this.Parameters.IncludeObjectCriteria != null && this.Parameters.IncludeObjectCriteria.Any();
            bool hasExcludeCriteria = this.Parameters.ExcludeObjectCriteria != null && this.Parameters.ExcludeObjectCriteria.Any();
            bool hasObjectsSpecified = this.Parameters.ScriptingObjects != null && this.Parameters.ScriptingObjects.Any();
            bool hasIncludeSchema = this.Parameters.IncludeSchema != null && this.Parameters.IncludeSchema.Any();
            bool hasExcludeSchema = this.Parameters.ExcludeSchema != null && this.Parameters.ExcludeSchema.Any();
            bool hasIncludeType = this.Parameters.IncludeType != null && this.Parameters.IncludeType.Any();
            bool hasExcludeType = this.Parameters.ExcludeType != null && this.Parameters.ExcludeType.Any();
            bool scriptAllObjects = !(hasIncludeCriteria || hasExcludeCriteria || hasObjectsSpecified || 
                                        hasIncludeSchema || hasExcludeSchema || hasIncludeType || hasExcludeType);
            if (scriptAllObjects)
            {
                Logger.Write(LogLevel.Verbose, "ScriptAllObjects is True");
                return publishModel;
            }

            // After setting this property, SqlScriptPublishModel.AdvancedOptions should NOT be referenced again
            // or all SqlScriptPublishModel.AdvancedOptions will be reset.
            //
            publishModel.ScriptAllObjects = false;
            Logger.Write(LogLevel.Verbose, "ScriptAllObjects is False");

            // An object selection criteria was specified, so now we need to resolve the SMO Urn instances to script.
            //
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>();
            if (hasIncludeCriteria || hasExcludeCriteria || hasIncludeSchema || hasExcludeSchema || hasIncludeType || hasExcludeType)
            {
                // This is an expensive remote call to load all objects from the database.
                List<ScriptingObject> allObjects = publishModel.GetDatabaseObjects();

                selectedObjects = ScriptingObjectMatcher.Match(
                    this.Parameters.IncludeObjectCriteria,
                    this.Parameters.ExcludeObjectCriteria,
                    this.Parameters.IncludeSchema,
                    this.Parameters.ExcludeSchema,
                    this.Parameters.IncludeType,
                    this.Parameters.ExcludeType,
                    allObjects);
            }

            // If specific objects are specified, include them.
            //
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

        private static void PopulateAdvancedScriptOptions(ScriptOptions scriptOptionsParameters, SqlScriptOptions advancedOptions)
        {
            if (scriptOptionsParameters == null)
            {
                Logger.Write(LogLevel.Verbose, "No advanced options set, the ScriptOptions object is null.");
                return;
            }

            foreach (PropertyInfo optionPropInfo in scriptOptionsParameters.GetType().GetProperties())
            {
                PropertyInfo advancedOptionPropInfo = advancedOptions.GetType().GetProperty(optionPropInfo.Name);
                if (advancedOptionPropInfo == null)
                {
                    Logger.Write(LogLevel.Warning, string.Format("Invalid property info name {0} could not be mapped to a property on SqlScriptOptions.", optionPropInfo.Name));
                    continue;
                }

                object optionValue = optionPropInfo.GetValue(scriptOptionsParameters, index: null);
                if (optionValue == null)
                {
                    Logger.Write(LogLevel.Verbose, string.Format("Skipping ScriptOptions.{0} since value is null", optionPropInfo.Name));
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

                    Logger.Write(LogLevel.Verbose, string.Format("Setting ScriptOptions.{0} to value {1}", optionPropInfo.Name, smoValue));
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
                    "Sending scripting error progress event, Urn={0}, OperationId={1}, Sequence={2}, Completed={3}, Error={4}",
                    e.Urn,
                    this.OperationId,
                    this.eventSequenceNumber,
                    e.Completed,
                    e?.Error?.ToString() ?? "null"));

            // Keep scripting...it's a best effort operation.
            e.ContinueScripting = true;

            this.SendProgressNotificationEvent(new ScriptingProgressNotificationParams
            {
                ScriptingObject = e.Urn?.ToScriptingObject(),
                Status = e.GetStatus(),
                CompletedCount = this.scriptedObjectCount,
                TotalCount = this.totalScriptedObjectCount,
                ErrorMessage = e?.Error?.Message,
                ErrorDetails = e?.Error?.ToString(),
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
                    "Sending scripting plan notification event OperationId={0}, Sequence={1}, Count={2}, Objects: {3}",
                    this.OperationId,
                    this.eventSequenceNumber,
                    this.totalScriptedObjectCount, 
                    string.Join(", ", e.Urns)));

            this.SendPlanNotificationEvent(new ScriptingPlanNotificationParams
            {
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
                    "Sending progress event, Urn={0}, OperationId={1}, Sequence={2}, Status={3}, Error={4}",
                    e.Urn,
                    this.OperationId,
                    this.eventSequenceNumber,
                    e.GetStatus(),
                    e?.Error?.ToString() ?? "null"));

            this.SendProgressNotificationEvent(new ScriptingProgressNotificationParams
            {
                ScriptingObject = e.Urn.ToScriptingObject(),
                Status = e.GetStatus(),
                CompletedCount = this.scriptedObjectCount,
                TotalCount = this.totalScriptedObjectCount,
                ErrorMessage = e?.Error?.Message,
                ErrorDetails = e?.Error?.ToString(),
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
