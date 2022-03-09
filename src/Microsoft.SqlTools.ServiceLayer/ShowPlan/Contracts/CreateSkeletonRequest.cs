//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph.Comparison;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class CreateSkeletonParams
    {
        /// <summary>
        /// Query plan's XML file text.
        /// </summary>
        public string QueryPlanXmlText { get; set; }
    }

    public class CreateSkeletonResult
    {
        /// <summary>
        /// Created Skeleton Node for a show plan
        /// </summary>
        public SkeletonNodeDTO SkeletonNode { get; set; }
    }

    public class CreateSkeletonRequest
    {
        public static readonly
            RequestType<CreateSkeletonParams, CreateSkeletonResult> Type =
                RequestType<CreateSkeletonParams, CreateSkeletonResult>.Create("showplan/createskeleton");
    }
}
