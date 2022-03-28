﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress generate deploy plan operation
    /// </summary>
    class GenerateDeployPlanOperation : DacFxOperation
    {
        public GenerateDeployPlanParams Parameters { get; }

        public string DeployReport { get; set; }

        public GenerateDeployPlanOperation(GenerateDeployPlanParams parameters, ConnectionInfo connInfo): base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            DacDeployOptions options = GetDefaultDeployOptions();
            DeployReport = this.DacServices.GenerateDeployReport(dacpac, this.Parameters.DatabaseName, options, this.CancellationToken);
        }
    }
}
