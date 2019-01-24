//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress upgrade plan operation
    /// </summary>
    class UpgradePlanOperation : DacFxOperation
    {
        public UpgradePlanParams Parameters { get; }

        public UpgradePlanOperation(UpgradePlanParams parameters, SqlConnection sqlConnection): base(sqlConnection)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            string report = this.DacServices.GenerateDeployReport(dacpac, this.Parameters.DatabaseName, null, this.CancellationToken);
        }

        public string ExecuteGenerateDeployReport()
        {
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            DacServices ds = new DacServices(this.SqlConnection.ConnectionString);
            string report = ds.GenerateDeployReport(dacpac, this.Parameters.DatabaseName, null, this.CancellationToken);
            return report;
        }
    }
}
