//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class GridSelectionSummaryResponse
    {
        public long Count { get; set; }
        public double? Average { get; set; }
        public decimal Sum { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public long DistinctCount { get; set; }
        public long NullCount { get; set; }
    }

    public class GridSelectionSummaryRequestParams : SubsetParams
    {
        public TableSelectionRange[] Selections { get; set; }
    }

    public class GridSelectionSummaryRequest
    {
        public static readonly RequestType<GridSelectionSummaryRequestParams, GridSelectionSummaryResponse> Type =
            RequestType<GridSelectionSummaryRequestParams, GridSelectionSummaryResponse>.Create("query/selectionsummary");
    }
}