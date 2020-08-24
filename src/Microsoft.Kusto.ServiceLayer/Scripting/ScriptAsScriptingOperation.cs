//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Specialized;
using System.Text;
using System.Globalization;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.Kusto.ServiceLayer.Utility;
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
        /// <summary>
        /// Left delimiter for an named object
        /// </summary>
        public const char LeftDelimiter = '[';

        /// <summary>
        /// right delimiter for a named object
        /// </summary>
        public const char RightDelimiter = ']';

        public ScriptAsScriptingOperation(ScriptingParams parameters, string azureAccountToken, IScripter scripter) : base(parameters)
        {
            DataSource = DataSourceFactory.Create(DataSourceType.Kusto, this.Parameters.ConnectionString,
                azureAccountToken);
            _scripter = scripter;
        }

        internal IDataSource DataSource { get; set; }

        private string serverName;
        private string databaseName;
        private bool disconnectAtDispose = false;

        public override void Execute()
        {
            try
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                this.ValidateScriptDatabaseParams();

                this.CancellationToken.ThrowIfCancellationRequested();
                string resultScript = string.Empty;
                
                UrnCollection urns = CreateUrns(DataSource);
                ScriptingOptions options = new ScriptingOptions();
                SetScriptBehavior(options);
                ScriptAsOptions scriptAsOptions = new ScriptAsOptions(this.Parameters.ScriptOptions);
                PopulateAdvancedScriptOptions(scriptAsOptions, options);
                options.WithDependencies = false;
                // TODO: Not including the header by default. We have to get this option from client
                options.IncludeHeaders = false;

                // Scripting data is not avaialable in the scripter
                options.ScriptData = false;
                SetScriptingOptions(options);

                switch (this.Parameters.Operation)
                {
                    case ScriptingOperationType.Select:
                        resultScript = GenerateScriptSelect(DataSource, urns);
                        break;
                    
                    case ScriptingOperationType.Alter:
                        resultScript = GenerateScriptAlter(DataSource, urns);
                        break;
                    
                    case ScriptingOperationType.Execute:
                        resultScript = GenerateScriptExecute(DataSource, urns);
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
            finally
            {
                if (disconnectAtDispose && DataSource != null)
                {
                    DataSource.Dispose();
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

        private string GenerateScriptAlter(IDataSource dataSource, UrnCollection urns)
        {
            ScriptingObject scriptingObject = this.Parameters.ScriptingObjects[0];
            Urn objectUrn = urns[0];

            if (string.Equals(scriptingObject.Type, "Function", StringComparison.CurrentCultureIgnoreCase))
            {
                return _scripter.AlterFunction(dataSource, scriptingObject);
            }
            
            return string.Empty;
        }

        private string GenerateScriptExecute(IDataSource dataSource, UrnCollection urns)
        {
            ScriptingObject scriptingObject = this.Parameters.ScriptingObjects[0];
            Urn objectUrn = urns[0];

            if (string.Equals(scriptingObject.Type, "Function", StringComparison.CurrentCultureIgnoreCase))
            {
                return _scripter.ExecuteFunction(dataSource, scriptingObject);
            }
            
            return string.Empty;
        }


        /// <summary>
        /// Generate a schema qualified name (e.g. [schema].[objectName]) for an object if the option for SchemaQualify is true
        /// </summary>
        /// <param name="schema">The schema name. May be null or empty in which case it will be ignored</param>
        /// <param name="objectName">The object name.</param>
        /// <param name="schemaQualify">Whether to schema qualify the object or not</param>
        /// <returns>The object name, quoted as appropriate and schema-qualified if the option is set</returns>
        private static string GenerateSchemaQualifiedName(string schema, string objectName, bool schemaQualify)
        {
            var qualifiedName = new StringBuilder();

            if (schemaQualify && !String.IsNullOrEmpty(schema))
            {
                // schema.name
                qualifiedName.AppendFormat(CultureInfo.InvariantCulture, "{0}.{1}", GetDelimitedString(schema), GetDelimitedString(objectName));
            }
            else
            {
                // name
                qualifiedName.AppendFormat(CultureInfo.InvariantCulture, "{0}", GetDelimitedString(objectName));
            }

            return qualifiedName.ToString();
        }

        /// <summary>
        /// getting delimited string
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>string</returns>
        static private string GetDelimitedString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return String.Empty;
            }
            else
            {
                StringBuilder qualifiedName = new StringBuilder();
                qualifiedName.AppendFormat("{0}{1}{2}",
                                       LeftDelimiter,
                                       QuoteObjectName(str),
                                       RightDelimiter);
                return qualifiedName.ToString();
            }
        }

        /// <summary>
        /// turn a smo datatype object into a type that can be inserted into tsql, e.g. nvarchar(20)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static string GetDatatype(DataType type, ScriptingOptions options)
        {
            // string we'll return.
            string rv = string.Empty;

            string dataType = type.Name;
            switch (type.SqlDataType)
            {
                // char, nchar, nchar, nvarchar, varbinary, nvarbinary are all displayed as type(length)
                // length of -1 is taken to be type(max). max isn't localizable.
                case SqlDataType.Char:
                case SqlDataType.NChar:
                case SqlDataType.VarChar:
                case SqlDataType.NVarChar:
                case SqlDataType.Binary:
                case SqlDataType.VarBinary:
                    rv = string.Format(CultureInfo.InvariantCulture,
                                       "{0}({1})",
                                       dataType,
                                       type.MaximumLength);
                    break;
                case SqlDataType.VarCharMax:
                case SqlDataType.NVarCharMax:
                case SqlDataType.VarBinaryMax:
                    rv = string.Format(CultureInfo.InvariantCulture,
                                       "{0}(max)",
                                       dataType);
                    break;
                // numeric and decimal are displayed as type precision,scale
                case SqlDataType.Numeric:
                case SqlDataType.Decimal:
                    rv = string.Format(CultureInfo.InvariantCulture,
                                       "{0}({1},{2})",
                                       dataType,
                                       type.NumericPrecision,
                                       type.NumericScale);
                    break;
                //time, datetimeoffset and datetime2 are displayed as type scale
                case SqlDataType.Time:
                case SqlDataType.DateTimeOffset:
                case SqlDataType.DateTime2:
                    rv = string.Format(CultureInfo.InvariantCulture,
                                       "{0}({1})",
                                       dataType,
                                       type.NumericScale);
                    break;
                // anything else is just type.
                case SqlDataType.Xml:
                    if (type.Schema != null && type.Schema.Length > 0 && dataType != null && dataType.Length > 0)
                    {
                        rv = String.Format(CultureInfo.InvariantCulture
                            , "xml ({0}{2}{1}.{0}{3}{1})"
                            , LeftDelimiter
                            , RightDelimiter
                            , QuoteObjectName(type.Schema)
                            , QuoteObjectName(dataType));
                    }
                    else
                    {
                        rv = "xml";
                    }
                    break;
                case SqlDataType.UserDefinedDataType:
                case SqlDataType.UserDefinedTableType:
                case SqlDataType.UserDefinedType:
                    //User defined types may be in a non-DBO schema so append it if necessary
                    rv = GenerateSchemaQualifiedName(type.Schema, dataType, options.SchemaQualify);
                    break;
                default:
                    rv = dataType;
                    break;

            }
            return rv;
        }

        /// <summary>
        /// Double quotes certain characters in object name
        /// </summary>
        /// <param name="sqlObject"></param>
        public static string QuoteObjectName(string sqlObject)
        {

            int len = sqlObject.Length;
            StringBuilder result = new StringBuilder(sqlObject.Length);
            for (int i = 0; i < len; i++)
            {
                if (sqlObject[i] == ']')
                {
                    result.Append(']');
                }
                result.Append(sqlObject[i]);
            }

            return result.ToString();
        }

        private static void WriteUseDatabase(Database parentObject, StringBuilder stringBuilder , ScriptingOptions options)
        {
            if (options.IncludeDatabaseContext)
            {
                string useDb = string.Format(CultureInfo.InvariantCulture, "USE {0}", CommonConstants.DefaultBatchSeperator);
                if (!options.NoCommandTerminator)
                {
                    stringBuilder.Append(useDb);
                    
                }
                else
                {
                    stringBuilder.Append(useDb);
                    stringBuilder.Append(Environment.NewLine);
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

        private UrnCollection CreateUrns(IDataSource dataSource)
        {
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>(this.Parameters.ScriptingObjects);

            serverName = dataSource.ClusterName;
            databaseName = new SqlConnectionStringBuilder(this.Parameters.ConnectionString).InitialCatalog;
            UrnCollection urnCollection = new UrnCollection();
            foreach (var scriptingObject in selectedObjects)
            {
                if(string.IsNullOrEmpty(scriptingObject.Schema))
                {
                    // TODO: get the default schema
                    scriptingObject.Schema = "dbo";
                }
                urnCollection.Add(scriptingObject.ToUrn(serverName, databaseName));
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

        private void ScripterScriptingError(object sender, ScriptingErrorEventArgs e)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            Logger.Write(
                TraceEventType.Verbose,
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
