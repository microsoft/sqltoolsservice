//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.Tools.Schema.CommandLineTool;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts
{
    /// <summary>
    /// Command-line arguments for SqlPackage operations containing source/target paths, connection strings, etc.
    /// These parameters are typically populated from the publish dialog or other UI interactions.
    /// </summary>
    public class SqlPackageCommandLineArguments
    {
        /// <summary>
        /// Action to perform: Publish, Extract, Script, Export, or Import
        /// </summary>
        public CommandLineToolAction Action { get; set; }

        /// <summary>
        /// Source file path (for Publish, Script, Extract operations)
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Target file path (for Extract, Export operations)
        /// </summary>
        public string TargetFile { get; set; }

        /// <summary>
        /// Source server name
        /// </summary>
        public string SourceServerName { get; set; }

        /// <summary>
        /// Source database name
        /// </summary>
        public string SourceDatabaseName { get; set; }

        /// <summary>
        /// Source connection string
        /// </summary>
        public string SourceConnectionString { get; set; }

        /// <summary>
        /// Target server name
        /// </summary>
        public string TargetServerName { get; set; }

        /// <summary>
        /// Target database name
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Target connection string
        /// </summary>
        public string TargetConnectionString { get; set; }

        /// <summary>
        /// Output path for Script operation
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Profile file path (for loading saved deployment profiles)
        /// </summary>
        public string Profile { get; set; }
    }
}
