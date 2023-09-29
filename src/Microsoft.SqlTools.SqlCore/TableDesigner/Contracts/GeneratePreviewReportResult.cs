//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The service request to generate preview report describing the changes.
    /// </summary>
    public class GeneratePreviewReportResult
    {
        /// <summary>
        /// The report generated for publish preview
        /// </summary>
        public string? Report { get; set; }

        /// <summary>
        /// format (mimetype) of the string
        /// </summary>
        public string? MimeType { get; set; }

        /// <summary>
        /// Whether user confirmation is required.
        /// </summary>
        public bool RequireConfirmation { get; set; }

        /// <summary>
        /// The confirmation text.
        /// </summary>
        public string? ConfirmationText { get; set; }

        /// <summary>
        /// Metadata about the table
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// The table schema validation error
        /// </summary>
        public string? SchemaValidationError { get; set; }
    }
}
