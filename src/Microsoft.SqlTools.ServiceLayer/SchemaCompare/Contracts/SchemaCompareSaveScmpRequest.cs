// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare save SCMP request.
    /// Extends ServiceLayer.SchemaCompareParams (which itself extends CoreContracts.SchemaCompareParams),
    /// picking up TaskExecutionMode through the chain. ScmpFilePath and excluded objects are
    /// defined here; all other fields (OperationId, Source/TargetEndpointInfo, DeploymentOptions)
    /// come from the base classes.
    /// </summary>
    internal class SchemaCompareSaveScmpParams : SchemaCompareParams
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

    internal class SchemaCompareSaveScmpRequest
    {
        public static readonly RequestType<SchemaCompareSaveScmpParams, ResultStatus> Type =
            RequestType<SchemaCompareSaveScmpParams, ResultStatus>.Create("schemaCompare/saveScmp");
    }
}