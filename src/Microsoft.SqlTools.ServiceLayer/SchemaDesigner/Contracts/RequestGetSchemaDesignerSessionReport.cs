//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GetSchemaDesignerSessionReportParams
    {
        public string? SessionId { get; set; }
        public SchemaDesignerModel? UpdatedModel { get; set; }
    }

    public class GetSchemaDesignerSessionReportResponse
    {
        public List<SchemaDesignerReportObject>? Reports { get; set; }
    }

    public class GetSchemaDesignerSessionReportRequest
    {
        public static readonly RequestType<GetSchemaDesignerSessionReportParams, GetSchemaDesignerSessionReportResponse> Type = RequestType<GetSchemaDesignerSessionReportParams, GetSchemaDesignerSessionReportResponse>.Create("schemaDesigner/getReport");
    }
}