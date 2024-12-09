//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Request to get the schema model
    /// </summary>
    public class GetSchemaModelRequest
    {
        public static readonly
            RequestType<ConnectionDetails, SchemaModel> Type =
            RequestType<ConnectionDetails, SchemaModel>.Create("schemaDesigner/getSchemaModel");
    }
}