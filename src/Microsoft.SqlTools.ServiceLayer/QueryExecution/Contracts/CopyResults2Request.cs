//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{

    public enum CopyType
    {
        Text = 0,
        JSON = 1,
        CSV = 2,
        INSERT = 3,
        IN = 4,
    }
    /// <summary>
    /// Parameters for the copy results request
    /// </summary>
    public class CopyResults2RequestParams : SubsetParams
    {
        /// <summary>
        /// The type of copy operation to perform.
        /// </summary>
        public CopyType CopyType { get; set; }

        /// <summary>
        /// The selections.
        /// </summary>
        public TableSelectionRange[] Selections { get; set; }

        /// <summary>
        /// Include headers of columns.
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// Delimiter for separating data items in CSV, ignored if CopyType is not CSV
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// Line separator
        /// </summary>
        public string LineSeparator { get; set; }

        /// <summary>
        /// Text identifier for alphanumeric columns in CSV
        /// </summary>
        public string TextIdentifier { get; set; }

        /// <summary>
        /// Encoding of copied results
        /// </summary>
        public string Encoding { get; set; }
    }

    /// <summary>
    /// Result for the copy results request
    /// </summary>
    public class CopyResults2RequestResult
    {
        /// <summary>
        /// The content to be copied to the clipboard.
        /// If this is populated, the client is responsible for copying it to the clipboard.
        /// </summary>
        public string Context { get; set; }
    }

    /// <summary>
    /// Copy Results Request. This API is named "copy2" to avoid conflict with the existing
    /// "copy" API which is currently in used by Azure Data Studio.
    /// </summary>
    public class CopyResults2Request
    {
        public static readonly RequestType<CopyResults2RequestParams, CopyResults2RequestResult> Type =
            RequestType<CopyResults2RequestParams, CopyResults2RequestResult>.Create("query/copy2");
    }

    public class Copy2CancelEventParams
    {
    }
    
    /// <summary>
    /// Parameters for the cancel copy results request
    /// </summary>
    public class Copy2CancelEvent
    {
        public static readonly EventType<Copy2CancelEventParams> Type =
            EventType<Copy2CancelEventParams>.Create("query/cancelCopy2");
    }
}