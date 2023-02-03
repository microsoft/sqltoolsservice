//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
//using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx save publish profile request.
    /// </summary>
    public class SaveProfileParams
    {
        /// <summary>
        /// Gets or sets the profile path
        /// </summary>
        public string ProfilePath { get; set; }

        /// <summary>
        /// Gets or sets name for database
        /// </summary>
        public string? DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets target connection string
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets SQLCMD variables for deployment
        /// </summary>
        public IDictionary<string, string>? SqlCommandVariableValues { get; set; }

        /// <summary>
        /// Gets or sets the options for deployment
        /// </summary>
        public DeploymentOptions? DeploymentOptions { get; set; }
    }

    /// <summary>
    /// Defines the DacFx save publish profile request type
    /// </summary>
    class SavePublishProfileRequest
    {
        public static readonly RequestType<SaveProfileParams, bool> Type =
            RequestType<SaveProfileParams, bool>.Create("dacfx/savePublishProfile");
    }
}



