//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Defines the deployment scenario for determining default options
    /// </summary>
    public enum DeploymentScenario
    {
        /// <summary>
        /// Deployment/Publish scenario - uses DacFx native defaults
        /// </summary>
        Deployment = 0,

        /// <summary>
        /// Schema Compare scenario - uses modified defaults
        /// </summary>
        SchemaCompare = 1
    }

    /// <summary>
    /// Parameters for getting deployment options based on scenario
    /// </summary>
    public class GetDeploymentOptionsParams
    {
        /// <summary>
        /// Specifies the scenario for which to retrieve default deployment options.
        /// Deployment (default): Returns DacFx native defaults (for Publish operations).
        /// SchemaCompare: Returns modified defaults.
        /// </summary>
        public DeploymentScenario Scenario { get; set; } = DeploymentScenario.Deployment;
    }

    /// <summary>
    /// Result containing deployment options for the requested scenario
    /// </summary>
    public class GetDeploymentOptionsResult : ResultStatus
    {
        public DeploymentOptions DefaultDeploymentOptions { get; set; }        
    }

    /// <summary>
    /// Request to get deployment options for a specific scenario (Deployment/Publish or Schema Compare)
    /// </summary>
    class GetDeploymentOptionsRequest
    {
        public static readonly RequestType<GetDeploymentOptionsParams, GetDeploymentOptionsResult> Type =
            RequestType<GetDeploymentOptionsParams, GetDeploymentOptionsResult>.Create("dacfx/getDeploymentOptions");
    }
}
