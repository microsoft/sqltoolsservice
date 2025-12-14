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
    public class SqlPackageCommandParams
    {
        /// <summary>
        /// Action to perform: Publish, Extract, Script, Export, or Import
        /// </summary>
        public CommandLineToolAction Action { get; set; }

        /// <summary>
        /// Serialized command-line arguments string containing source/target paths, connection strings, etc.
        /// JSON-serialized from VSCode and deserialized by SqlPackage API
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Deployment options from VSCode (for Publish, Script operations)
        /// Will be converted to DacDeployOptions using DacFxUtils.CreateDeploymentOptions
        /// </summary>
        public DacFx.Contracts.DeploymentOptions DeploymentOptions { get; set; }

        /// <summary>
        /// Extract options (for Extract operation)
        /// </summary>
        public DacExtractOptions ExtractOptions { get; set; }

        /// <summary>
        /// Export options (for Export operation)
        /// </summary>
        public DacExportOptions ExportOptions { get; set; }

        /// <summary>
        /// Import options (for Import operation)
        /// </summary>
        public DacImportOptions ImportOptions { get; set; }

        /// <summary>
        /// SQLCMD variables (for Publish, Script operations)
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }

    /// <summary>
    /// Result for SqlPackage command generation operations
    /// </summary>
    public class SqlPackageCommandResult
    {
        /// <summary>
        /// Gets or sets the generated command string
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request to generate SqlPackage command based on action
    /// </summary>
    public class GenerateSqlPackageCommandRequest
    {
        public static readonly RequestType<SqlPackageCommandParams, SqlPackageCommandResult> Type =
            RequestType<SqlPackageCommandParams, SqlPackageCommandResult>.Create("sqlpackage/generateCommand");
    }
}
