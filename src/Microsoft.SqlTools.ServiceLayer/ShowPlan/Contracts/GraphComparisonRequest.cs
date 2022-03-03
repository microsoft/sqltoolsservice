//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class GetGraphComparisonParams
    {
        /// <summary>
        /// First query plan's XML file text for comparison.
        /// </summary>
        public string FirstQueryPlanXmlText { get; set; }

        /// <summary>
        /// Second query plan's XML file text for comparison.
        /// </summary>
        public string SecondQueryPlanXmlText { get; set; }

        /// <summary>
        /// Flag to indicate if the database name should be ignored
        /// during comparisons.
        /// </summary>
        public bool IgnoreDatabaseName { get; set; }
    }

    public class GetGraphComparisonResult
    {
        /// <summary>
        /// Flag indicating the compared graphs are the same when
        /// true and different when false.
        /// </summary>
        public bool IsEquivalent { get; set; }
    }

    public class GraphComparisonRequest
    {
        public static readonly
            RequestType<GetGraphComparisonParams, GetGraphComparisonResult> Type =
                RequestType<GetGraphComparisonParams, GetGraphComparisonResult>.Create("showplan/compareshowplans");
    }
}
