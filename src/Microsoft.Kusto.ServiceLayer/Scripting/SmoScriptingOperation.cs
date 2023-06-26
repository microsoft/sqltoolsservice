﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using static Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptions;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    /// <summary>
    /// Base class for all SMO scripting operations
    /// </summary>
    public abstract class SmoScriptingOperation : ScriptingOperation
    {
        protected readonly IDataSource _dataSource;
        private bool _disposed;

        protected SmoScriptingOperation(ScriptingParams parameters, IDataSource datasource)
        {
            _dataSource = datasource;
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        protected ScriptingParams Parameters { get; set; }

        public string ScriptText { get; protected set; }

        /// <remarks>
        /// An event can be completed by the following conditions: success, cancel, error.
        /// </remarks>
        public event EventHandler<ScriptingCompleteParams> CompleteNotification;

        /// <summary>
        /// Event raised when a scripting operation has made forward progress.
        /// </summary>
        public event EventHandler<ScriptingProgressNotificationParams> ProgressNotification;

        /// <summary>
        /// Event raised when a scripting operation has resolved which database objects will be scripted.
        /// </summary>
        public event EventHandler<ScriptingPlanNotificationParams> PlanNotification;

        protected virtual void SendCompletionNotificationEvent(ScriptingCompleteParams parameters)
        {
            this.SetCommonEventProperties(parameters);
            this.CompleteNotification?.Invoke(this, parameters);
        }

        protected virtual void SendProgressNotificationEvent(ScriptingProgressNotificationParams parameters)
        {
            this.SetCommonEventProperties(parameters);
            this.ProgressNotification?.Invoke(this, parameters);
        }

        protected virtual void SendPlanNotificationEvent(ScriptingPlanNotificationParams parameters)
        {
            this.SetCommonEventProperties(parameters);
            this.PlanNotification?.Invoke(this, parameters);
        }

        protected virtual void SetCommonEventProperties(ScriptingEventParams parameters)
        {
            parameters.OperationId = this.OperationId;
        }

        protected string GetServerNameFromLiveInstance()
        {
            Logger.Write(TraceEventType.Verbose, string.Format("Resolved server name '{0}'", _dataSource.ClusterName));
            return _dataSource.ClusterName;
        }

        protected void ValidateScriptDatabaseParams()
        {
            if (string.IsNullOrWhiteSpace(Parameters.DatabaseName))
            {
                throw new ArgumentException(SR.ScriptingParams_ConnectionString_Property_Invalid);
            }
            if (Parameters.FilePath == null && this.Parameters.ScriptDestination != "ToEditor")
            {
                throw new ArgumentException(SR.ScriptingParams_FilePath_Property_Invalid);
            }
            if (Parameters.FilePath != null && this.Parameters.ScriptDestination != "ToEditor")
            {
                if (!Directory.Exists(Path.GetDirectoryName(this.Parameters.FilePath)))
                {
                    throw new ArgumentException(SR.ScriptingParams_FilePath_Property_Invalid);
                }
            }
        }

        protected static void PopulateAdvancedScriptOptions(ScriptOptions scriptOptionsParameters, object advancedOptions)
        {
            if (scriptOptionsParameters == null)
            {
                Logger.Write(TraceEventType.Verbose, "No advanced options set, the ScriptOptions object is null.");
                return;
            }

            foreach (PropertyInfo optionPropInfo in scriptOptionsParameters.GetType().GetProperties())
            {
                PropertyInfo advancedOptionPropInfo = advancedOptions.GetType().GetProperty(optionPropInfo.Name);
                if (advancedOptionPropInfo == null)
                {
                    Logger.Write(TraceEventType.Warning, string.Format("Invalid property info name {0} could not be mapped to a property on SqlScriptOptions.", optionPropInfo.Name));
                    continue;
                }

                object optionValue = optionPropInfo.GetValue(scriptOptionsParameters, index: null);
                if (optionValue == null)
                {
                    Logger.Write(TraceEventType.Verbose, string.Format("Skipping ScriptOptions.{0} since value is null", optionPropInfo.Name));
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
                        if (advancedOptionPropInfo.PropertyType == typeof(bool))
                        {

                            smoValue = (bool)optionValue;
                        }
                        else
                        {
                            smoValue = (bool)optionValue ? BooleanTypeOptions.True : BooleanTypeOptions.False;
                        }
                    }
                    else
                    {
                        smoValue = Enum.Parse(advancedOptionPropInfo.PropertyType, (string)optionValue, ignoreCase: true);
                    }

                    Logger.Write(TraceEventType.Verbose, string.Format("Setting ScriptOptions.{0} to value {1}", optionPropInfo.Name, smoValue));
                    advancedOptionPropInfo.SetValue(advancedOptions, smoValue);
                }
                catch (Exception e)
                {
                    Logger.Write(
                        TraceEventType.Warning,
                        string.Format("An exception occurred setting option {0} to value {1}: {2}", optionPropInfo.Name, optionValue, e));
                }
            }

        }

        /// <summary>
        /// Disposes the scripting operation.
        /// </summary>
        public override void Dispose()
        {
            if (!_disposed)
            {
                this.Cancel();
                _disposed = true;
            }
        }

    }
}
