//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GetSchemaDesignerCreateAsScriptParams
    {
        public string? SessionId { get; set; }
        public SchemaDesignerModel? UpdatedModel { get; set; }
    }

    public class GetSchemaDesignerCreateAsScriptResponse
    {
        public List<SchemaDesignerScriptObject>? Scripts { get; set; }
        public string? CombinedScript { get; set; }
    }

    public class GetSchemaDesignerCreateAsScriptRequest
    {
        public static readonly RequestType<GetSchemaDesignerCreateAsScriptParams, GetSchemaDesignerCreateAsScriptResponse> Type = RequestType<GetSchemaDesignerCreateAsScriptParams, GetSchemaDesignerCreateAsScriptResponse>.Create("schemaDesigner/getScript");
    }
}