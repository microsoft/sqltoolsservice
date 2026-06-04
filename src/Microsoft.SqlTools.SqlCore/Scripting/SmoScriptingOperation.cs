//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.SqlCore.Connection;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using static Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptions;

namespace Microsoft.SqlTools.SqlCore.Scripting
{
    /// <summary>
    /// Base class for all SMO scripting operations
    /// </summary>
    public abstract class SmoScriptingOperation : ScriptingOperation
    {
        private bool disposed = false;

        public SmoScriptingOperation(ScriptingParams parameters)
        {
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

        protected string GetServerNameFromLiveInstance(string connectionString, string azureAccessToken)
        {
            string serverName = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.RetryLogicProvider = SqlRetryProviders.ServerlessDBRetryProvider();
                if (azureAccessToken != null)
                {
                    connection.AccessToken = azureAccessToken;
                }
                connection.Open();

                try
                {
                    ServerConnection serverConnection;
                    if (azureAccessToken == null)
                    {
                        serverConnection = new ServerConnection(connection);
                    }
                    else
                    {
                        serverConnection = new ServerConnection(connection, new AzureAccessToken(azureAccessToken));
                    }
                    serverName = serverConnection.TrueName;
                }
                catch (SqlException e)
                {
                    Logger.Verbose(
                        string.Format("Exception getting server name", e));
                }
            }

            Logger.Verbose(string.Format("Resolved server name '{0}'", serverName));
            return serverName;
        }

        protected void ValidateScriptDatabaseParams()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
            }
            catch (Exception e)
            {
                throw new ArgumentException(SR.ScriptingParams_ConnectionString_Property_Invalid, e);
            }
            if (this.Parameters.FilePath == null && this.Parameters.ScriptDestination != "ToEditor")
            {
                throw new ArgumentException(SR.ScriptingParams_FilePath_Property_Invalid);
            }
            else if (this.Parameters.FilePath != null && this.Parameters.ScriptDestination != "ToEditor")
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
                Logger.Verbose("No advanced options set, the ScriptOptions object is null.");
                return;
            }

            foreach (PropertyInfo optionPropInfo in scriptOptionsParameters.GetType().GetProperties())
            {
                PropertyInfo advancedOptionPropInfo = advancedOptions.GetType().GetProperty(optionPropInfo.Name);
                if (advancedOptionPropInfo == null)
                {
                    Logger.Warning(string.Format("Invalid property info name {0} could not be mapped to a property on SqlScriptOptions.", optionPropInfo.Name));
                    continue;
                }

                object optionValue = optionPropInfo.GetValue(scriptOptionsParameters, index: null);
                if (optionValue == null)
                {
                    Logger.Verbose(string.Format("Skipping ScriptOptions.{0} since value is null", optionPropInfo.Name));
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
                        string stringValue = (string)optionValue;

                        if (advancedOptionPropInfo.PropertyType.IsEnum)
                        {
                            // The same option string may target either of SMO's two parallel enum systems:
                            // the SqlScriptPublish enums (e.g. SqlScriptOptions.ScriptDatabaseEngineType, whose
                            // names match the values STS sends) or the core enums (Common.DatabaseEngineType /
                            // DatabaseEngineEdition, which use different names). When the value already exists in
                            // the target enum it is parsed as-is; otherwise it is mapped to the core enum name.
                            string enumValue = IsDefinedEnumName(advancedOptionPropInfo.PropertyType, stringValue)
                                ? stringValue
                                : MapEnumValue(optionPropInfo.Name, stringValue);

                            // If the value still does not match a name on the target enum, log a clear
                            // warning so consumers can identify values (for example newly added SMO
                            // editions) that lack a mapping. Enum.Parse below would otherwise throw and
                            // be swallowed with only a generic message.
                            if (!IsDefinedEnumName(advancedOptionPropInfo.PropertyType, enumValue))
                            {
                                Logger.Warning(string.Format(
                                    "Option {0} value '{1}' (mapped to '{2}') is not a defined name on enum {3}. A mapping may be missing for a newly added SMO {3} value.",
                                    optionPropInfo.Name, stringValue, enumValue, advancedOptionPropInfo.PropertyType.Name));
                            }

                            smoValue = Enum.Parse(advancedOptionPropInfo.PropertyType, enumValue, ignoreCase: true);
                        }
                        else if (advancedOptionPropInfo.PropertyType.IsAssignableFrom(optionPropInfo.PropertyType))
                        {
                            smoValue = optionValue;
                        }
                        else
                        {
                            Logger.Warning(string.Format(
                                "Skipping ScriptOptions.{0}: target property type {1} is not an enum and cannot be assigned from source type {2}.",
                                optionPropInfo.Name, advancedOptionPropInfo.PropertyType.Name, optionPropInfo.PropertyType.Name));
                            continue;
                        }
                    }

                    Logger.Verbose(string.Format("Setting ScriptOptions.{0} to value {1}", optionPropInfo.Name, smoValue));
                    advancedOptionPropInfo.SetValue(advancedOptions, smoValue);
                }
                catch (Exception e)
                {
                    Logger.Warning(
                        string.Format("An exception occurred setting option {0} to value {1}: {2}", optionPropInfo.Name, optionValue, e));
                }
            }

        }

        /// <summary>
        /// Returns true when the supplied value matches one of the names defined on the given enum
        /// type (case-insensitive). Used to decide whether an option value can be parsed directly or
        /// needs to be mapped between SMO's two enum naming conventions.
        /// </summary>
        internal static bool IsDefinedEnumName(Type enumType, string value)
        {
            if (!enumType.IsEnum)
            {
                return false;
            }

            foreach (string name in Enum.GetNames(enumType))
            {
                if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Maps enum value names used by SQL Tools Service (which mirror the SqlScriptPublish
        /// naming convention) to the names expected by SMO's core DatabaseEngineType and
        /// DatabaseEngineEdition enums. Without this mapping, Enum.Parse fails for values such as
        /// "SqlAzure" or "SqlAzureDatabaseEdition", causing SMO to fall back to non-cloud defaults
        /// and produce invalid scripts (for example, temporal tables with PRIMARY KEY constraints
        /// emitted as separate ALTER TABLE statements). Unmapped values are returned unchanged.
        /// </summary>
        internal static string MapEnumValue(string propertyName, string value)
        {
            if (propertyName == nameof(ScriptOptions.TargetDatabaseEngineType))
            {
                return value switch
                {
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineType.SqlAzure), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineType.SqlAzureDatabase),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineType.SingleInstance), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineType.Standalone),
                    _ => value
                };
            }

            if (propertyName == nameof(ScriptOptions.TargetDatabaseEngineEdition))
            {
                return value switch
                {
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlDatabase),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlDatawarehouseEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlDataWarehouse),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerStretchEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlStretchDatabase),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlManagedInstance),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerOnDemandEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlOnDemand),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerPersonalEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.Personal),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerStandardEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.Standard),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.Enterprise),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlServerExpressEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.Express),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlDatabaseEdgeEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlDatabaseEdge),
                    var v when string.Equals(v, nameof(ScriptDatabaseEngineEdition.SqlAzureArcManagedInstanceEdition), StringComparison.OrdinalIgnoreCase) => nameof(DatabaseEngineEdition.SqlAzureArcManagedInstance),
                    var v when string.Equals(v, "SqlFabricSqlDatabaseEdition", StringComparison.OrdinalIgnoreCase) => "FabricSqlDatabase",
                    _ => value
                };
            }

            return value;
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
