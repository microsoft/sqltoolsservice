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
                RequestType<ColorMatchingSectionsParams, ColorMatchingSectionsResult>.Create("queryexecutionplan/colormatchingsections");
    }
}
