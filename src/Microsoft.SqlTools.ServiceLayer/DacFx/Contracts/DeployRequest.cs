//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx deploy request.
    /// </summary>
    public class DeployParams : IScriptableRequestParams
    {
        /// <summary>
        /// Gets or sets dacpac package filepath
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for deployed database
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Executation mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Parameters returned from a DacFx deploy request.
    /// </summary>
    public class DeployResult : ResultStatus
    {
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Defines the DacFx deploy request type
    /// </summary>
    class DeployRequest
    {
        public static readonly RequestType<DeployParams, DeployResult> Type =
            RequestType<DeployParams, DeployResult>.Create("dacfx/deploy");
    }
}
