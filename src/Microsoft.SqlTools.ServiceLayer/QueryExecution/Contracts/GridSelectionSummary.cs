//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{

    /// <summary>
    /// Response for grid selection summary request
    /// </summary>
    public class GridSelectionSummaryResponse
    {
        /// <summary>
        /// Count of selected cells
        /// </summary>
        public long Count { get; set; }
        /// <summary>
        /// Average of selected cells if they are numeric
        /// </summary>
        public double? Average { get; set; }
        /// <summary>
        /// Sum of selected cells if they are numeric
        /// </summary>
        public decimal Sum { get; set; }
        /// <summary>
        /// Minimum of selected cells if they are numeric
        /// </summary>
        public double? Min { get; set; }
        /// <summary>
        /// Maximum of selected cells if they are numeric
        /// </summary>
        public double? Max { get; set; }
        /// <summary>
        /// Count of distinct values in selected cells
        /// </summary>
        public long DistinctCount { get; set; }
        /// <summary>
        /// Count of null values in selected cells
        /// </summary>
        public long NullCount { get; set; }
    }

    /// <summary>
    /// Parameters for grid selection summary request
    /// </summary>
    public class GridSelectionSummaryRequestParams : SubsetParams
    {
        /// <summary>
        /// Selection ranges
        /// </summary>
        public TableSelectionRange[] Selections { get; set; }
    }

    public class GridSelectionSummaryRequest
    {
        public static readonly RequestType<GridSelectionSummaryRequestParams, GridSelectionSummaryResponse> Type =
            RequestType<GridSelectionSummaryRequestParams, GridSelectionSummaryResponse>.Create("query/selectionsummary");
    }

    public class GridSelectionSummaryCancelParams
    {
        /// <summary>
        /// Uri to cancel the selection summary for
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class GridSelectionSummaryCancelEvent
    {
        public static readonly EventType<GridSelectionSummaryCancelParams> Type =
            EventType<GridSelectionSummaryCancelParams>.Create("query/selectionsummary/cancel");
    }
}