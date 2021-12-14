//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.Utility;

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
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            DacDeployOptions options = this.Parameters.DeploymentOptions != null ? SchemaCompareUtils.CreateSchemaCompareOptions(this.Parameters.DeploymentOptions) : this.GetDefaultDeployOptions();

            if (this.Parameters.SqlCommandVariableValues != null)
            {
                foreach (string key in this.Parameters.SqlCommandVariableValues.Keys)
                {
                    options.SqlCommandVariableValues[key] = this.Parameters.SqlCommandVariableValues[key];
                }
            }

            this.DacServices.Deploy(dacpac, this.Parameters.DatabaseName, this.Parameters.UpgradeExisting, options, this.CancellationToken);
        }
    }
}
