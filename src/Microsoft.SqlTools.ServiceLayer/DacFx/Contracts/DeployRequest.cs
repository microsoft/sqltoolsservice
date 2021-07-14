//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx deploy request.
    /// </summary>
    public class DeployParams : DacFxParams
    {
        /// <summary>
        /// Gets or sets if upgrading existing database
        /// </summary>
        public bool UpgradeExisting { get; set; }

        /// <summary>
        /// Gets or sets SQLCMD variables for deployment
        /// </summary>
        public IDictionary<string, string> SqlCommandVariableValues { get; set; }
    }

    /// <summary>
    /// Defines the DacFx deploy request type
    /// </summary>
    class DeployRequest
    {
        public static readonly RequestType<DeployParams, DacFxResult> Type =
            RequestType<DeployParams, DacFxResult>.Create("dacfx/deploy");
    }
}
