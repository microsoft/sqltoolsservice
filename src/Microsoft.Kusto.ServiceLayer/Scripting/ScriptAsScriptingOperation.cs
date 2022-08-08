﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Diagnostics;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to generate script as for one smo object
    /// </summary>
    public class ScriptAsScriptingOperation : SmoScriptingOperation
    {
        private readonly IScripter _scripter;
        private static readonly Dictionary<string, SqlServerVersion> scriptCompatibilityMap = LoadScriptCompatibilityMap();
        private string _serverName;
        private string _databaseName;

        public ScriptAsScriptingOperation(ScriptingParams parameters, IScripter scripter, IDataSource datasource) :
            base(parameters, datasource)
        {
            _scripter = scripter;
        }

        public override void Execute()
        {
            try
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                this.ValidateScriptDatabaseParams();

                this.CancellationToken.ThrowIfCancellationRequested();
                string resultScript = string.Empty;
                
                UrnCollection urns = CreateUrns(_dataSource);
                ScriptingOptions options = new ScriptingOptions();
                SetScriptBehavior(options);
                ScriptAsOptions scriptAsOptions = new ScriptAsOptions(this.Parameters.ScriptOptions);
                PopulateAdvancedScriptOptions(scriptAsOptions, options);
                options.WithDependencies = false;
                // TODO: Not including the header by default. We have to get this option from client
                options.IncludeHeaders = false;

                // Scripting data is not available in the scripter
                options.ScriptData = false;
                SetScriptingOptions(options);

                switch (this.Parameters.Operation)
                {
                    case ScriptingOperationType.Select:
                        resultScript = GenerateScriptSelect(_dataSource, urns);
                        break;
                    
                    case ScriptingOperationType.Alter:
                    case ScriptingOperationType.Execute:
                        resultScript = GenerateScriptForFunction(_dataSource);
                        break;
                }

                this.CancellationToken.ThrowIfCancellationRequested();

                Logger.Write(
                    TraceEventType.Verbose,
                    string.Format(
                        "Sending script complete notification event for operation {0}",
                        this.OperationId
                        ));

                ScriptText = resultScript;

                this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                {
                    Success = true,
                });

                this.SendPlanNotificationEvent(new ScriptingPlanNotificationParams
                {
                    ScriptingObjects = this.Parameters.ScriptingObjects,
                    Count = 1,
                });
            }
            catch (Exception e)
            {
                if (e.IsOperationCanceledException())
                {
                    Logger.Write(TraceEventType.Information, string.Format("Scripting operation {0} was canceled", this.OperationId));
                    this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                    {
                        Canceled = true,
                    });
                }
                else
                {
                    Logger.Write(TraceEventType.Error, string.Format("Scripting operation {0} failed with exception {1}", this.OperationId, e));
                    this.SendCompletionNotificationEvent(new ScriptingCompleteParams
                    {
                        OperationId = OperationId,
                        HasError = true,
                        ErrorMessage = $"{SR.ScriptingGeneralError} {e.Message}",
                        ErrorDetails = e.ToString(),
                    });
                }
            }
        }

        private string GenerateScriptSelect(IDataSource dataSource, UrnCollection urns)
        {
            ScriptingObject scriptingObject = this.Parameters.ScriptingObjects[0];
            Urn objectUrn = urns[0];

            // select from table
            if (string.Equals(scriptingObject.Type, "Table", StringComparison.CurrentCultureIgnoreCase))
            {
                return _scripter.SelectFromTableOrView(dataSource, objectUrn);
            }

            return string.Empty;
        }

        private string GenerateScriptForFunction(IDataSource dataSource)
        {
            ScriptingObject scriptingObject = this.Parameters.ScriptingObjects[0];

            if (!string.Equals(scriptingObject.Type, "Function", StringComparison.CurrentCultureIgnoreCase))
            {
                return string.Empty;
            }

            if (Parameters.Operation == ScriptingOperationType.Alter)
            {
                return _scripter.AlterFunction(dataSource, scriptingObject);    
            }

            if (Parameters.Operation == ScriptingOperationType.Execute)
            {
                return _scripter.ExecuteFunction(dataSource, scriptingObject);
            }

            return string.Empty;
        }


        private UrnCollection CreateUrns(IDataSource dataSource)
        {
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>(this.Parameters.ScriptingObjects);

            _serverName = dataSource.ClusterName;
            _databaseName = Parameters.DatabaseName;
            UrnCollection urnCollection = new UrnCollection();
            foreach (var scriptingObject in selectedObjects)
            {
                if(string.IsNullOrEmpty(scriptingObject.Schema))
                {
                    // TODO: get the default schema
                    scriptingObject.Schema = "dbo";
                }
                urnCollection.Add(scriptingObject.ToUrn(_serverName, _databaseName));
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

        private static Dictionary<string, SqlServerVersion> LoadScriptCompatibilityMap()
        {
            return new Dictionary<string, SqlServerVersion>
            {
                {SqlScriptOptions.ScriptCompatibilityOptions.Script160Compat.ToString(), SqlServerVersion.Version160},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script150Compat.ToString(), SqlServerVersion.Version150},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script140Compat.ToString(), SqlServerVersion.Version140},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script130Compat.ToString(), SqlServerVersion.Version130},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script120Compat.ToString(), SqlServerVersion.Version120},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script110Compat.ToString(), SqlServerVersion.Version110},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script105Compat.ToString(), SqlServerVersion.Version105},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script100Compat.ToString(), SqlServerVersion.Version100},
                {SqlScriptOptions.ScriptCompatibilityOptions.Script90Compat.ToString(), SqlServerVersion.Version90}
            };

        }

        private void SetScriptingOptions(ScriptingOptions scriptingOptions)
        {
            scriptingOptions.AllowSystemObjects = true;

            // setting this forces SMO to correctly script objects that have been renamed
            scriptingOptions.EnforceScriptingOptions = true;

            //We always want role memberships for users and database roles to be scripted
            scriptingOptions.IncludeDatabaseRoleMemberships = true;
            SqlServerVersion targetServerVersion;
            if(scriptCompatibilityMap.TryGetValue(this.Parameters.ScriptOptions.ScriptCompatibilityOption, out targetServerVersion))
            {
                scriptingOptions.TargetServerVersion = targetServerVersion;
            }
            else
            {
                //If you are getting this assertion fail it means you are working for higher
                //version of SQL Server. You need to update this part of code.
                 Logger.Write(TraceEventType.Warning, "This part of the code is not updated corresponding to latest version change");
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
    }
}
