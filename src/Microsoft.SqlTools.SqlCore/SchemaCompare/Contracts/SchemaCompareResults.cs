//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.SqlCore.DacFx.Contracts;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts
{
    /// <summary>
    /// Result from a schema comparison
    /// </summary>
    public class SchemaCompareResult : SchemaCompareResultBase
    {
        /// <summary>
        /// Unique identifier for the schema compare operation.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Whether the source and target schemas are identical.
        /// </summary>
        public bool AreEqual { get; set; }

        /// <summary>
        /// List of differences found between source and target.
        /// </summary>
        public List<DiffEntry> Differences { get; set; }
    }

    /// <summary>
    /// Result from opening an SCMP file
    /// </summary>
    public class SchemaCompareOpenScmpResult : SchemaCompareResultBase
    {
        /// <summary>
        /// Gets or sets the current source endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo SourceEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the current target endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo TargetEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the original target name
        /// </summary>
        public string OriginalTargetName { get; set; }

        /// <summary>
        /// Gets or sets the original target connection string
        /// </summary>
        public string OriginalTargetConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the deployment options
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }

        /// <summary>
        /// Gets or sets the excluded source elements
        /// </summary>
        public List<SchemaCompareObjectId> ExcludedSourceElements { get; set; }

        /// <summary>
        /// Gets or sets the excluded target elements
        /// </summary>
        public List<SchemaCompareObjectId> ExcludedTargetElements { get; set; }
    }

    /// <summary>
    /// Result from include/exclude node operation
    /// </summary>
    public class SchemaCompareIncludeExcludeResult : SchemaCompareResultBase
    {
        /// <summary>
        /// Dependencies that may have been affected by the include/exclude request
        /// </summary>
        public List<DiffEntry> AffectedDependencies { get; set; }

        /// <summary>
        /// Dependencies that caused the include/exclude to fail
        /// </summary>
        public List<DiffEntry> BlockingDependencies { get; set; }
    }

    /// <summary>
    /// Result from include/exclude all nodes operation
    /// </summary>
    public class SchemaCompareIncludeExcludeAllNodesResult : SchemaCompareResultBase
    {
        /// <summary>
        /// Differences that were all included or excluded
        /// </summary>
        public List<DiffEntry> AllIncludedOrExcludedDifferences { get; set; }
    }

    /// <summary>
    /// Result from get default options
    /// </summary>
    public class SchemaCompareOptionsResult : SchemaCompareResultBase
    {
        /// <summary>
        /// The default deployment options for schema compare.
        /// </summary>
        public DeploymentOptions DefaultDeploymentOptions { get; set; }
    }

    /// <summary>
    /// Result containing generated scripts (useful for SSMS direct-call API)
    /// </summary>
    public class SchemaCompareScriptResult : SchemaCompareResultBase
    {
        /// <summary>
        /// The generated deployment SQL script.
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// The generated master database SQL script for Azure SQL DB.
        /// </summary>
        public string MasterScript { get; set; }
    }
}
