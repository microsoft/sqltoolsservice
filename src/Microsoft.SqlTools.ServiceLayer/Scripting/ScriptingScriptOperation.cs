//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to represent an in-progress script operation.
    /// </summary>
    public sealed class ScriptingScriptOperation : SmoScriptingOperation
    {

        private int scriptedObjectCount = 0;

        private int totalScriptedObjectCount = 0;

        private int eventSequenceNumber = 1;

        public ScriptingScriptOperation(ScriptingParams parameters): base(parameters)
        {
        }

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
                publishModel.AllowSystemObjects = true;

                ScriptDestination destination = !string.IsNullOrWhiteSpace(this.Parameters.ScriptDestination)
                    ? (ScriptDestination)Enum.Parse(typeof(ScriptDestination), this.Parameters.ScriptDestination)
                    : ScriptDestination.ToSingleFile;

                // SMO is currently hardcoded to produce UTF-8 encoding when running on dotnet core.
                ScriptOutputOptions outputOptions = new ScriptOutputOptions
                {
                    SaveFileMode = ScriptFileMode.Overwrite,
                    SaveFileName = this.Parameters.FilePath,
                    ScriptDestination = destination,
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
                
                ScriptText = publishModel.RawScript;

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

        protected override void SendCompletionNotificationEvent(ScriptingCompleteParams parameters)
        {
            this.SetCommonEventProperties(parameters);
            base.SendCompletionNotificationEvent(parameters);
        }

        protected override void SendPlanNotificationEvent(ScriptingPlanNotificationParams parameters)
        {
            this.SetCommonEventProperties(parameters);
        }

        protected override void SendProgressNotificationEvent(ScriptingProgressNotificationParams parameters)
        {
            this.SetCommonEventProperties(parameters);
            base.SendProgressNotificationEvent(parameters);
        }

        private void SetCommonEventProperties(ScriptingEventParams parameters)
        {
            parameters.OperationId = this.OperationId;
            parameters.SequenceNumber = this.eventSequenceNumber;
            this.eventSequenceNumber += 1;
        }

        private SqlScriptPublishModel BuildPublishModel()
        {
            SqlScriptPublishModel publishModel = new SqlScriptPublishModel(this.Parameters.ConnectionString);

            // See if any filtering criteria was specified.  If not, we're scripting the entire database.  Otherwise, the filtering
            // criteria should include the target objects to script.
            //
            bool hasObjectsSpecified = this.Parameters.ScriptingObjects != null && this.Parameters.ScriptingObjects.Any();
            bool hasCriteriaSpecified = 
                (this.Parameters.IncludeObjectCriteria != null && this.Parameters.IncludeObjectCriteria.Any()) ||
                (this.Parameters.ExcludeObjectCriteria != null && this.Parameters.ExcludeObjectCriteria.Any()) ||
                (this.Parameters.IncludeSchemas != null && this.Parameters.IncludeSchemas.Any()) ||
                (this.Parameters.ExcludeSchemas != null && this.Parameters.ExcludeSchemas.Any()) ||
                (this.Parameters.IncludeTypes != null && this.Parameters.IncludeTypes.Any()) ||
                (this.Parameters.ExcludeTypes != null && this.Parameters.ExcludeTypes.Any());
            bool scriptAllObjects = !hasObjectsSpecified && !hasCriteriaSpecified;

            // In the getter for SqlScriptPublishModel.AdvancedOptions, there is some strange logic which will 
            // cause the SqlScriptPublishModel.AdvancedOptions to get reset and lose all values based the ordering
            // of when SqlScriptPublishModel.ScriptAllObjects is set.  
            //
            publishModel.ScriptAllObjects = scriptAllObjects;
            if (scriptAllObjects)
            {
                // Due to the getter logic within publishModel.AdvancedOptions, we explicitly populate the options
                // after we determine what objects we are scripting.
                //
                PopulateAdvancedScriptOptions(this.Parameters.ScriptOptions, publishModel.AdvancedOptions);
                return publishModel;
            }

            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>();

            if (hasCriteriaSpecified)
            {
                // This is an expensive remote call to load all objects from the database.
                //
                List<ScriptingObject> allObjects = publishModel.GetDatabaseObjects();
                selectedObjects = ScriptingObjectMatcher.Match(
                    this.Parameters.IncludeObjectCriteria,
                    this.Parameters.ExcludeObjectCriteria,
                    this.Parameters.IncludeSchemas,
                    this.Parameters.ExcludeSchemas,
                    this.Parameters.IncludeTypes,
                    this.Parameters.ExcludeTypes,
                    allObjects);
            }

            if (hasObjectsSpecified)
            {
                selectedObjects = selectedObjects.Union(this.Parameters.ScriptingObjects);
            }

            // Populating advanced options after we select our objects in question, otherwise we lose all
            // advanced options.  After this call to PopulateAdvancedScriptOptions, DO NOT reference the
            // publishModel.AdvancedOptions getter as it will reset the options in the model.
            //
            PopulateAdvancedScriptOptions(this.Parameters.ScriptOptions, publishModel.AdvancedOptions);

            Logger.Write(
                LogLevel.Normal,
                string.Format(
                    "Scripting object count {0}, objects: {1}",
                    selectedObjects.Count(),
                    string.Join(", ", selectedObjects)));

            string server = GetServerNameFromLiveInstance(this.Parameters.ConnectionString);
            string database = new SqlConnectionStringBuilder(this.Parameters.ConnectionString).InitialCatalog;

            foreach (ScriptingObject scriptingObject in selectedObjects)
            {
                publishModel.SelectedObjects.Add(scriptingObject.ToUrn(server, database));
            }
            return publishModel;
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
        
    }
}
