//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class ColorMatchingSectionsParams
    {
        /// <summary>
        /// First query execution plan for comparison.
        /// </summary>
        public ExecutionPlanGraphInfo FirstExecutionPlanGraphInfo { get; set; }

        /// <summary>
        /// Second query execution plan for comparison.
        /// </summary>
        public ExecutionPlanGraphInfo SecondExecutionPlanGraphInfo { get; set; }

        /// <summary>
        /// Flag to indicate if the database name should be ignored
        /// during comparisons.
        /// </summary>
        public bool IgnoreDatabaseName { get; set; }
    }

    public class ColorMatchingSectionsResult
    {
        /// <summary>
        /// Created Skeleton Node for a show plan
        /// </summary>
        public SkeletonNodeDTO FirstSkeletonNode { get; set; }

        /// <summary>
        /// Created Skeleton Node for a show plan
        /// </summary>
        public SkeletonNodeDTO SecondSkeletonNode { get; set; }
    }

    public class ColorMatchingSectionsRequest
    {
        public static readonly
            RequestType<ColorMatchingSectionsParams, ColorMatchingSectionsResult> Type =
                RequestType<ColorMatchingSectionsParams, ColorMatchingSectionsResult>.Create("queryExecutionPlan/colorMatchingSections");
    }
}
