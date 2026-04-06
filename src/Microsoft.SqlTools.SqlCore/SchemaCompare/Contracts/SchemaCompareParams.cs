//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.SqlCore.DacFx.Contracts;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare operation.
    /// Host-agnostic: does not include TaskExecutionMode (ServiceLayer-specific).
    /// </summary>
    public class SchemaCompareParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets the source endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo SourceEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the target endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo TargetEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the deployment options for schema compare
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }
    }

    /// <summary>
    /// Parameters for schema compare publish database changes
    /// </summary>
    public class SchemaComparePublishDatabaseChangesParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Name of target server
        /// </summary>
        public string TargetServerName { get; set; }

        /// <summary>
        /// Name of target database
        /// </summary>
        public string TargetDatabaseName { get; set; }
    }

    /// <summary>
    /// Parameters for schema compare generate script (same shape as publish db changes)
    /// </summary>
    public class SchemaCompareGenerateScriptParams : SchemaComparePublishDatabaseChangesParams
    {
    }

    /// <summary>
    /// Parameters for schema compare publish project changes
    /// </summary>
    public class SchemaComparePublishProjectChangesParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Path of project folder
        /// </summary>
        public string TargetProjectPath { get; set; }

        /// <summary>
        /// Folder structure of target folder
        /// </summary>
        public DacExtractTarget TargetFolderStructure { get; set; }
    }

    /// <summary>
    /// Parameters for schema compare include/exclude node
    /// </summary>
    public class SchemaCompareNodeParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Difference to Include or exclude
        /// </summary>
        public DiffEntry DiffEntry { get; set; }

        /// <summary>
        /// Indicator for include or exclude request
        /// </summary>
        public bool IncludeRequest { get; set; }
    }

    /// <summary>
    /// Parameters for schema compare include/exclude all nodes
    /// </summary>
    public class SchemaCompareIncludeExcludeAllNodesParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Indicator for include or exclude request
        /// </summary>
        public bool IncludeRequest { get; set; }
    }

    /// <summary>
    /// Parameters for opening an SCMP file
    /// </summary>
    public class SchemaCompareOpenScmpParams
    {
        /// <summary>
        /// Filepath of scmp
        /// </summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Parameters for saving an SCMP file
    /// </summary>
    public class SchemaCompareSaveScmpParams : SchemaCompareParams
    {
        /// <summary>
        /// Gets or sets the File Path for scmp
        /// </summary>
        public string ScmpFilePath { get; set; }

        /// <summary>
        /// Excluded source objects
        /// </summary>
        public SchemaCompareObjectId[] ExcludedSourceObjects { get; set; }

        /// <summary>
        /// Excluded Target objects
        /// </summary>
        public SchemaCompareObjectId[] ExcludedTargetObjects { get; set; }
    }

    /// <summary>
    /// Parameters for getting default options (empty)
    /// </summary>
    public class SchemaCompareGetOptionsParams
    {
    }

    /// <summary>
    /// Parameters for cancelling a comparison
    /// </summary>
    public class SchemaCompareCancelParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }
    }
}
