//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.TaskServices;
using Microsoft.Kusto.ServiceLayer.Utility;
using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare generate script request.
    /// </summary>
    public class SchemaCompareGenerateScriptParams : SchemaComparePublishChangesParams
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
