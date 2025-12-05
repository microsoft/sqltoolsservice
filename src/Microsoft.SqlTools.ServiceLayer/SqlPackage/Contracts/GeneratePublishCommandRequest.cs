//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts
{
    /// <summary>
    /// Parameters for generating a SqlPackage publish command
    /// </summary>
    public class GeneratePublishCommandParams
    {
        /// <summary>
        /// Gets or sets the source file path (.dacpac)
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Gets or sets the target file path (.dacpac) - optional
        /// </summary>
        public string TargetFile { get; set; }

        /// <summary>
        /// Gets or sets the target connection string - alternative to TargetServerName/TargetDatabaseName
        /// </summary>
        public string TargetConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the target server name
        /// </summary>
        public string TargetServerName { get; set; }

        /// <summary>
        /// Gets or sets the target database name
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the publish profile path (.publish.xml) - optional
        /// </summary>
        public string ProfilePath { get; set; }

        /// <summary>
        /// Gets or sets SQLCMD variables
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }

        /// <summary>
        /// Gets or sets deployment options
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }

        /// <summary>
        /// Gets or sets whether to include the executable name (sqlpackage) in the command
        /// </summary>
        public bool IncludeExecutableName { get; set; } = true;
    }

    /// <summary>
    /// Result for generating a SqlPackage publish command
    /// </summary>
    public class GeneratePublishCommandResult
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
    /// Request definition for generating a SqlPackage publish command
    /// </summary>
    public class GeneratePublishCommandRequest
    {
        public static readonly RequestType<GeneratePublishCommandParams, GeneratePublishCommandResult> Type =
            RequestType<GeneratePublishCommandParams, GeneratePublishCommandResult>.Create("sqlpackage/generatePublishCommand");
    }
}
