//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The service request to generate preview report describing the changes.
    /// </summary>

    public class GeneratePreviewReportResult 
    {
        /// <summary>
        /// The report generated for publish preview
        /// </summary>
        public string Report;

        /// <summary>
        /// format (mimetype) of the string
        /// </summary>
        public string MimeType;

        /// <summary>
        /// Metadata about the table to be captured
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
    }
    
    /// <summary>
    /// The service request to generate preview report describing the changes.
    /// </summary>
    public class GeneratePreviewReportRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<TableInfo, GeneratePreviewReportResult> Type = RequestType<TableInfo, GeneratePreviewReportResult>.Create("tabledesigner/generatepreviewreport");
    }
}
