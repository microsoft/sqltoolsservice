//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress deploy operation
    /// </summary>
    class DeployOperation : DacFxOperation
    {
        public DeployParams Parameters { get; }

        public DeployOperation(DeployParams parameters, ConnectionInfo connInfo) : base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            try {
                DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
                DacDeployOptions options = this.Parameters.DeploymentOptions != null ? SchemaCompareUtils.CreateSchemaCompareOptions(this.Parameters.DeploymentOptions) : this.GetDefaultDeployOptions();

                if (this.Parameters.SqlCommandVariableValues != null)
                {
                    foreach (string key in this.Parameters.SqlCommandVariableValues.Keys)
                    {
                        options.SqlCommandVariableValues[key] = this.Parameters.SqlCommandVariableValues[key];
                    }
                }

                // Set diagnostics logging
                DacFxUtils utils = new DacFxUtils();
                utils.SetUpDiagnosticsLogging(this.Parameters.DiagnosticsLogFilePath, this.DacServices);

                this.DacServices.Deploy(dacpac, this.Parameters.DatabaseName, this.Parameters.UpgradeExisting, options, this.CancellationToken);
            }
            finally
            {
                // Remove the diagnostic tracer for the current operation based on Name:path
                DacFxUtils utils = new DacFxUtils();
                utils.RemoveDiagnosticListener(this.Parameters.DiagnosticsLogFilePath, this.DacServices);
            }
        }
    }
}
