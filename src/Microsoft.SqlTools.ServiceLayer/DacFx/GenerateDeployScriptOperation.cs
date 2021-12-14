//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress generate deploy script operation
    /// </summary>
    class GenerateDeployScriptOperation : DacFxOperation
    {
        public GenerateDeployScriptParams Parameters { get; }

        public PublishResult Result { get; set; }

        public GenerateDeployScriptOperation(GenerateDeployScriptParams parameters, ConnectionInfo connInfo) : base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            PublishOptions publishOptions = new PublishOptions();
            publishOptions.GenerateDeploymentReport = this.Parameters.GenerateDeploymentReport;
            publishOptions.CancelToken = this.CancellationToken;
            publishOptions.DeployOptions = this.Parameters.DeploymentOptions != null ? SchemaCompareUtils.CreateSchemaCompareOptions(this.Parameters.DeploymentOptions) : this.GetDefaultDeployOptions();

            if (this.Parameters.SqlCommandVariableValues != null)
            {
                foreach (string key in this.Parameters.SqlCommandVariableValues.Keys)
                {
                    publishOptions.DeployOptions.SqlCommandVariableValues[key] = this.Parameters.SqlCommandVariableValues[key];
                }
            }

            this.Result = this.DacServices.Script(dacpac, this.Parameters.DatabaseName, publishOptions);

            // tests don't create a SqlTask, so only add the script when the SqlTask isn't null
            if (this.SqlTask != null)
            {
                this.SqlTask.AddScript(SqlTaskStatus.Succeeded, Result.DatabaseScript);
                if (!string.IsNullOrEmpty(this.Result.MasterDbScript))
                {
                    // master script is only used if the target is Azure SQL db and the script contains all operations that must be done against the master database
                    this.SqlTask.AddScript(SqlTaskStatus.Succeeded, this.Result.MasterDbScript);
                }
            }

            if (this.Parameters.GenerateDeploymentReport && !string.IsNullOrEmpty(this.Parameters.DeploymentReportFilePath))
            {
                File.WriteAllText(this.Parameters.DeploymentReportFilePath, this.Result.DeploymentReport);
            }
        }
    }
}
