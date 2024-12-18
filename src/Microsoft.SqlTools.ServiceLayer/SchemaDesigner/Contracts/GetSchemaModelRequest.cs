//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GetSchemaModelRequestParams
    {
        public string OwnerUri { get; set; }
        public string DatabaseName { get; set; }
    }
    /// <summary>
    /// Request to get the schema model
    /// </summary>
    public class GetSchemaModelRequest
    {
        public static readonly
            RequestType<GetSchemaModelRequestParams, SchemaModel> Type =
            RequestType<GetSchemaModelRequestParams, SchemaModel>.Create("schemaDesigner/getSchemaModel");
    }
}