//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GetDefinitionRequest
    {
        public string? SessionId { get; set; }
        public SchemaDesignerModel? UpdatedSchema { get; set; }
    }

    public class GetDefinitionResponse
    {
        public string? Script { get; set; }
    }

    public class GetDefinition
    {
        /// <summary>
        /// Request to generate create script for the schema model
        /// </summary>
        public static readonly RequestType<GetDefinitionRequest, GetDefinitionResponse> Type = RequestType<GetDefinitionRequest, GetDefinitionResponse>.Create("schemaDesigner/getDefinition");
    }
}