//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class TableSelectionRange
    {
        public int FromRow { get; set; }
        public int ToRow { get; set; }
        public int FromColumn { get; set; }
        public int ToColumn { get; set; }
    }

    /// <summary>
    /// Parameters for the copy results request
    /// </summary>
    public class CopyResultsRequestParams : SubsetParams
    {
        /// <summary>
        /// Whether to remove the line break from cell values.
        /// </summary>
        public bool RemoveNewLines { get; set; }

        /// <summary>
        /// Whether to include the column headers.
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// The selections.
        /// </summary>
        public TableSelectionRange[] Selections { get; set; }
    }

    /// <summary>
    /// Result for the copy results request
    /// </summary>
    public class CopyResultsRequestResult
    {
    }

    /// <summary>
    /// Copy Results Request
    /// </summary>
    public class CopyResultsRequest
    {
        public static readonly RequestType<CopyResultsRequestParams, CopyResultsRequestResult> Type =
            RequestType<CopyResultsRequestParams, CopyResultsRequestResult>.Create("query/copy");
    }
}