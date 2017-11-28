//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Specialized;
using System.Text;
using System.Globalization;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to generate script as for one smo object
    /// </summary>
    public class ScriptAsScriptingOperation : SmoScriptingOperation
    {
        private static Dictionary<string, SqlServerVersion> scriptCompatabilityMap = LoadScriptCompatabilityMap();

        public ScriptAsScriptingOperation(ScriptingParams parameters): base(parameters)
        {
        }

        public override void Execute()
        {
            SqlServer.Management.Smo.Scripter scripter = null;
            try
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                this.ValidateScriptDatabaseParams();

                this.CancellationToken.ThrowIfCancellationRequested();
                string resultScript = string.Empty;
                // TODO: try to use one of the existing connections
                using (SqlConnection sqlConnection = new SqlConnection(this.Parameters.ConnectionString))
                {
                    sqlConnection.Open();
                    ServerConnection serverConnection = new ServerConnection(sqlConnection);
                    Server server = new Server(serverConnection);
                    scripter = new SqlServer.Management.Smo.Scripter(server);
                    ScriptingOptions options = new ScriptingOptions();
                    SetScriptBehavior(options);
                    PopulateAdvancedScriptOptions(this.Parameters.ScriptOptions, options);
                    options.WithDependencies = false;
                    options.ScriptData = false;
                    SetScriptingOptions(options);

                    // TODO: Not including the header by default. We have to get this option from client
                    options.IncludeHeaders = false;
                    scripter.Options = options;
                    scripter.Options.ScriptData = false;
                    scripter.ScriptingError += ScripterScriptingError;
                    UrnCollection urns = CreateUrns(serverConnection);
                    var result = scripter.Script(urns);
                    resultScript = GetScript(options, result);
                }

                this.CancellationToken.ThrowIfCancellationRequested();

                Logger.Write(
                    LogLevel.Verbose,
                    string.Format(
                        "Sending script complete notification event for operation {0}",
                        this.OperationId
                        ));

                ScriptText = resultScript;

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
                        OperationId = OperationId,
                        HasError = true,
                        ErrorMessage = e.Message,
                        ErrorDetails = e.ToString(),
                    });
                }
            }
            finally
            {
                if (scripter != null)
                {
                    scripter.ScriptingError -= this.ScripterScriptingError;
                }
            }
        }

        private string GetScript(ScriptingOptions options, StringCollection stringCollection)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in stringCollection)
            {
                sb.Append(item);
                if (options != null && !options.NoCommandTerminator)
                {
                    //Ensure the batch separator is always on a new line (to avoid syntax errors)
                    //but don't write an extra if we already have one as this can affect definitions
                    //of objects such as Stored Procedures (see TFS#9125366)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}{2}",
                        item.EndsWith(Environment.NewLine) ? string.Empty : Environment.NewLine,
                        CommonConstants.DefaultBatchSeperator,
                        Environment.NewLine);
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        private UrnCollection CreateUrns(ServerConnection serverConnection)
        {
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>(this.Parameters.ScriptingObjects);

            string server = serverConnection.TrueName;
            string database = new SqlConnectionStringBuilder(this.Parameters.ConnectionString).InitialCatalog;
            UrnCollection urnCollection = new UrnCollection();
            foreach (var scriptingObject in selectedObjects)
            {
                if(string.IsNullOrEmpty(scriptingObject.Schema))
                {
                    // TODO: get the default schema
                    scriptingObject.Schema = "dbo";
                }
                urnCollection.Add(scriptingObject.ToUrn(server, database));
            }
            return urnCollection;
        }

        private void SetScriptBehavior(ScriptingOptions options)
        {
            // TODO: have to add Scripting behavior to Smo ScriptingOptions class 
            // so it would support ScriptDropAndScreate
            switch (this.Parameters.ScriptOptions.ScriptCreateDrop)
            {
                case "ScriptCreate":
                    options.ScriptDrops = false;
                    break;
                case "ScriptDrop":
                    options.ScriptDrops = true;
                    break;
                default:
                    options.ScriptDrops = false;
                    break;

            }
        }

        private static Dictionary<string, SqlServerVersion> LoadScriptCompatabilityMap()
        {
            Dictionary<string, SqlServerVersion> map = new Dictionary<string, SqlServerVersion>();
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script140Compat.ToString(), SqlServerVersion.Version140);
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script130Compat.ToString(), SqlServerVersion.Version130);
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script120Compat.ToString(), SqlServerVersion.Version120);
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script110Compat.ToString(), SqlServerVersion.Version110);
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script105Compat.ToString(), SqlServerVersion.Version105);
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script100Compat.ToString(), SqlServerVersion.Version100);
            map.Add(SqlScriptOptions.ScriptCompatabilityOptions.Script90Compat.ToString(), SqlServerVersion.Version90);

            return map;
        }

        private void SetScriptingOptions(ScriptingOptions scriptingOptions)
        {
            scriptingOptions.AllowSystemObjects = true;

            // setting this forces SMO to correctly script objects that have been renamed
            scriptingOptions.EnforceScriptingOptions = true;

            //We always want role memberships for users and database roles to be scripted
            scriptingOptions.IncludeDatabaseRoleMemberships = true;
            SqlServerVersion targetServerVersion;
            if(scriptCompatabilityMap.TryGetValue(this.Parameters.ScriptOptions.ScriptCompatibilityOption, out targetServerVersion))
            {
                scriptingOptions.TargetServerVersion = targetServerVersion;
            }
            else
            {
                //If you are getting this assertion fail it means you are working for higher
                //version of SQL Server. You need to update this part of code.
                 Logger.Write(LogLevel.Warning, "This part of the code is not updated corresponding to latest version change");
            }

            // for cloud scripting to work we also have to have Script Compat set to 105.
            // the defaults from scripting options should take care of it
            object targetDatabaseEngineType;
            if (Enum.TryParse(typeof(SqlScriptOptions.ScriptDatabaseEngineType), this.Parameters.ScriptOptions.TargetDatabaseEngineType, out targetDatabaseEngineType))
            {
                switch ((SqlScriptOptions.ScriptDatabaseEngineType)targetDatabaseEngineType)
                {
                    case SqlScriptOptions.ScriptDatabaseEngineType.SingleInstance:
                        scriptingOptions.TargetDatabaseEngineType = DatabaseEngineType.Standalone;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineType.SqlAzure:
                        scriptingOptions.TargetDatabaseEngineType = DatabaseEngineType.SqlAzureDatabase;
                        break;
                }
            }

            object targetDatabaseEngineEdition;
            if (Enum.TryParse(typeof(SqlScriptOptions.ScriptDatabaseEngineEdition), this.Parameters.ScriptOptions.TargetDatabaseEngineEdition, out targetDatabaseEngineEdition))
            {
                switch ((SqlScriptOptions.ScriptDatabaseEngineEdition)targetDatabaseEngineEdition)
                {
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerPersonalEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Personal;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerStandardEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Standard;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Enterprise;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerExpressEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Express;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDatabase;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlDatawarehouseEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDataWarehouse;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerStretchEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlStretchDatabase;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlManagedInstance;
                        break;
                    default:
                        scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Standard;
                        break;
                }
            }

            if (scriptingOptions.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                scriptingOptions.Triggers = false;
            }

            scriptingOptions.NoVardecimal = false; //making IncludeVarDecimal true for DPW

            // scripting of stats is a combination of the Statistics
            // and the OptimizerData flag
            object scriptStatistics;
            if (Enum.TryParse(typeof(SqlScriptOptions.ScriptStatisticsOptions), this.Parameters.ScriptOptions.ScriptStatistics, out scriptStatistics))
            {
                switch ((SqlScriptOptions.ScriptStatisticsOptions)scriptStatistics)
                {
                    case SqlScriptOptions.ScriptStatisticsOptions.ScriptStatsAll:
                        scriptingOptions.Statistics = true;
                        scriptingOptions.OptimizerData = true;
                        break;
                    case SqlScriptOptions.ScriptStatisticsOptions.ScriptStatsDDL:
                        scriptingOptions.Statistics = true;
                        scriptingOptions.OptimizerData = false;
                        break;
                    case SqlScriptOptions.ScriptStatisticsOptions.ScriptStatsNone:
                        scriptingOptions.Statistics = false;
                        scriptingOptions.OptimizerData = false;
                        break;
                }
            }

            // If Histogram and Update Statics are True then include DriIncludeSystemNames and AnsiPadding by default
            if (scriptingOptions.Statistics == true && scriptingOptions.OptimizerData == true)
            {
                scriptingOptions.DriIncludeSystemNames = true;
                scriptingOptions.AnsiPadding = true;
            }
        }

        private void ScripterScriptingError(object sender, ScriptingErrorEventArgs e)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            Logger.Write(
                LogLevel.Verbose,
                string.Format(
                    "Sending scripting error progress event, Urn={0}, OperationId={1}, Completed={2}, Error={3}",
                    e.Current,
                    this.OperationId,
                    false,
                    e?.InnerException?.ToString() ?? "null"));

            this.SendProgressNotificationEvent(new ScriptingProgressNotificationParams
            {
                ScriptingObject = e.Current?.ToScriptingObject(),
                Status = "Failed",
                ErrorMessage = e?.InnerException?.Message,
                ErrorDetails = e?.InnerException?.ToString(),
            });
        }
    }
}
