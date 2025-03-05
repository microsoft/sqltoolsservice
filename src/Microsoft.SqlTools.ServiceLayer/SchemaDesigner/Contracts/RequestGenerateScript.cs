//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GenerateScriptRequest
    {
        public string? SessionId { get; set; }
        public SchemaDesignerModel? UpdatedSchema { get; set; }
    }

    public class GenerateScriptResponse
    {
        public List<SchemaDesignerScriptObject>? Scripts { get; set; }
        public string? CombinedScript { get; set; }
    }

    public class GenerateScript
    {
        public static readonly RequestType<GenerateScriptRequest, GenerateScriptResponse> Type = RequestType<GenerateScriptRequest, GenerateScriptResponse>.Create("schemaDesigner/generateScript");
    }
}