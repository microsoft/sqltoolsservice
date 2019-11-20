//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.SchemaCompare.Contracts
{
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
