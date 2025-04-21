//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Data.Tools.Sql.DesignServices;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

/// <summary>
/// Schema Designer Report Request and Response classes.
/// </summary>
namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Request to get the report of the schema designer session.
    /// </summary>
    public class GetReportRequest
    {
        public string? SessionId { get; set; }
        public SchemaDesignerModel? UpdatedSchema { get; set; }
    }

    /// <summary>
    /// Response containing the report of the schema designer session.
    /// </summary>
    public class GetReportResponse
    {
        public List<SchemaDesignerChangeReport>? Reports { get; set; }
        public PreviewReport? DacReport { get; set; }
        public string? UpdateScript { get; set; }
    }

    /// <summary>
    /// Request to get the report of the schema designer session.
    /// </summary>
    public class GetReport
    {
        public static readonly RequestType<GetReportRequest, GetReportResponse> Type = RequestType<GetReportRequest, GetReportResponse>.Create("schemaDesigner/getReport");
    }
}