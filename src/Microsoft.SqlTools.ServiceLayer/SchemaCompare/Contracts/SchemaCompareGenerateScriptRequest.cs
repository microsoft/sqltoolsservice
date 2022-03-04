//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare generate script request.
    /// </summary>
    public class SchemaCompareGenerateScriptParams : SchemaComparePublishDatabaseChangesParams
    {
    }

    /// <summary>
    /// Defines the Schema Compare generate script request type
    /// </summary>
    class SchemaCompareGenerateScriptRequest
    {
        public static readonly RequestType<SchemaCompareGenerateScriptParams, ResultStatus> Type =
            RequestType<SchemaCompareGenerateScriptParams, ResultStatus>.Create("schemaCompare/generateScript");
    }
}
