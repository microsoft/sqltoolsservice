//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx generate deploy script request.
    /// </summary>
    public class GenerateDeployScriptParams : DacFxParams
    {
        /// <summary>
        /// Gets or sets whether a Deployment Report should be generated during deploy.
        /// </summary>
        public bool GenerateDeploymentReport { get; set; }

        /// <summary>
        /// Gets or sets the filepath where to save the deployment report
        /// </summary>
        public string DeploymentReportFilePath { get; set; }

        /// <summary>
        /// Gets or sets SQLCMD variables for script generation
        /// </summary>
        public IDictionary<string, string> SqlCommandVariableValues { get; set; }

        /// <summary>
        /// Gets or sets the options for deployment
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }
    }

    /// <summary>
    /// Defines the DacFx generate deploy script request type
    /// </summary>
    class GenerateDeployScriptRequest
    {
        public static readonly RequestType<GenerateDeployScriptParams, DacFxResult> Type =
            RequestType<GenerateDeployScriptParams, DacFxResult>.Create("dacfx/generateDeploymentScript");
    }
}
