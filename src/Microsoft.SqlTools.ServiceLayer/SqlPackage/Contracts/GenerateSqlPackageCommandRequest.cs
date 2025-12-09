//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts
{
    /// <summary>
    /// Parameters for generating SqlPackage command
    /// </summary>
    public class GenerateSqlPackageCommandParams
    {
        /// <summary>
        /// Action to perform: Publish, Extract, Script, Export, or Import
        /// </summary>
        public CommandLineToolAction Action { get; set; }

        /// <summary>
        /// Serialized command-line arguments string containing source/target paths, connection strings, etc.
        /// SqlPackage API will deserialize this to extract individual parameters
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Deployment options (for Publish, Script operations)
        /// </summary>
        public DacDeployOptions DeploymentOptions { get; set; }

        /// <summary>
        /// Additional properties (for Extract, Export, Import operations)
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// SQLCMD variables (for Publish, Script operations)
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }

    /// <summary>
    /// Request to generate SqlPackage command based on action
    /// </summary>
    public class GenerateSqlPackageCommandRequest
    {
        public static readonly RequestType<GenerateSqlPackageCommandParams, SqlPackageCommandResult> Type =
            RequestType<GenerateSqlPackageCommandParams, SqlPackageCommandResult>.Create("sqlpackage/generateCommand");
    }
}
