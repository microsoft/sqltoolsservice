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

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to generate script as for one smo object
    /// </summary>
    public class ScriptAsScriptingOperation : SmoScriptingOperation
    {
        public ScriptAsScriptingOperation(ScriptingParams parameters): base(parameters)
        {
        }

        private string BatchTerminator = "GO";

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
                    ServerConnection serverConnection = new ServerConnection(sqlConnection);
                    Server server = new Server(serverConnection);
                    scripter = new SqlServer.Management.Smo.Scripter(server);
                    ScriptingOptions options = new ScriptingOptions();
                    SetScriptBehavior(options);
                    PopulateAdvancedScriptOptions(this.Parameters.ScriptOptions, options);
                    options.WithDependencies = false;
                    // TODO: Not including the header by default. We have to get this option from client
                    options.IncludeHeaders = false;
                    scripter.Options = options;
                    scripter.ScriptingError += ScripterScriptingError;
                    UrnCollection urns = CreateUrns();
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
                        this.BatchTerminator,
                        Environment.NewLine);
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        private UrnCollection CreateUrns()
        {
            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>(this.Parameters.ScriptingObjects);

            string server = GetServerNameFromLiveInstance(this.Parameters.ConnectionString);
            string database = new SqlConnectionStringBuilder(this.Parameters.ConnectionString).InitialCatalog;
            UrnCollection urnCollection = new UrnCollection();
            foreach (var scriptingObject in selectedObjects)
            {
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
