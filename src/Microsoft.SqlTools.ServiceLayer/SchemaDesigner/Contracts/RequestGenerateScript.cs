//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GenerateScriptRequest
    {
        public string? SessionId { get; set; }
    }

    public class GenerateScriptResponse
    {
        public string? Script { get; set; }
    }

    public class GenerateScript
    {
        /// <summary>
        /// Request to generate create script for the schema model
        /// </summary>
        public static readonly RequestType<GenerateScriptRequest, GenerateScriptResponse> Type = RequestType<GenerateScriptRequest, GenerateScriptResponse>.Create("schemaDesigner/generateScript");
    }
}