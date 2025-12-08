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
    /// Parameters for generating SqlPackage command
    /// </summary>
    public class GenerateSqlPackageCommandParams
    {
        /// <summary>
        /// Action to perform: Publish, Extract, Script, Export, or Import
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Source file path (for Publish, Script, Import operations)
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Target file path (for Extract, Script, Export operations)
        /// </summary>
        public string TargetFile { get; set; }

        /// <summary>
        /// Source connection string (for Extract, Export operations)
        /// </summary>
        public string SourceConnectionString { get; set; }

        /// <summary>
        /// Source server name (for Extract, Export operations)
        /// </summary>
        public string SourceServerName { get; set; }

        /// <summary>
        /// Source database name (for Extract, Export operations)
        /// </summary>
        public string SourceDatabaseName { get; set; }

        /// <summary>
        /// Target connection string (for Publish, Script, Import operations)
        /// </summary>
        public string TargetConnectionString { get; set; }

        /// <summary>
        /// Target server name (for Publish, Script, Import operations)
        /// </summary>
        public string TargetServerName { get; set; }

        /// <summary>
        /// Target database name (for Publish, Script, Import operations)
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Profile path (for Publish, Script operations)
        /// </summary>
        public string ProfilePath { get; set; }

        /// <summary>
        /// Variables (for Publish, Script operations)
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }

        /// <summary>
        /// Deployment options (for Publish, Script operations)
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }

        /// <summary>
        /// Application name (for Extract operation)
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application version (for Extract operation)
        /// </summary>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Additional properties (for Extract, Export, Import operations)
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
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
