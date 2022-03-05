//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class FindNextNonIgnoreNodeParams
    {
        /// <summary>
        /// Starting node to find the next non ignore node.
        /// </summary>
        public Node Node { get; set; }
    }

    public class FindNextNonIgnoreNodeResult
    {
        /// <summary>
        /// Next node that should not be ignored for show plan comparisons.
        /// </summary>
        public Node NextNonIgnoreNode { get; set; }
    }

    public class FindNextNonIgnoreNodeRequest
    {
        public static readonly
            RequestType<FindNextNonIgnoreNodeParams, FindNextNonIgnoreNodeResult> Type =
                RequestType<FindNextNonIgnoreNodeParams, FindNextNonIgnoreNodeResult>.Create("showplan/findnextnonignorenode");
    }
}
