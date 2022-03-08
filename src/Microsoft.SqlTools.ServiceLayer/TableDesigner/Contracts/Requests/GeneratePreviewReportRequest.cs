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
    public class GeneratePreviewReportResult 
    {
        // The script generated in plaintext or markdown
        public string Script;

        // format of the string (plaintext or mardown)
        public string Format;
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
