//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The service request to generate preview report describing the changes.
    /// </summary>

<<<<<<< HEAD
    public class PreviewReportResult 
=======
    public class GeneratePreviewReportResult 
>>>>>>> main
    {
        /// <summary>
        /// The report generated for publish preview
        /// </summary>
        public string Report;

        /// <summary>
        /// format (mimetype) of the string
        /// </summary>
        public string MimeType;
    }
    
    /// <summary>
    /// The service request to generate preview report describing the changes.
    /// </summary>
    public class GeneratePreviewReportRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
<<<<<<< HEAD
        public static readonly RequestType<TableInfo, PreviewReportResult> Type = RequestType<TableInfo, PreviewReportResult>.Create("tabledesigner/generatepreviewreport");
=======
        public static readonly RequestType<TableInfo, GeneratePreviewReportResult> Type = RequestType<TableInfo, GeneratePreviewReportResult>.Create("tabledesigner/generatepreviewreport");
>>>>>>> main
    }
}
