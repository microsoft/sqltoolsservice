//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class FindNextNonIgnoreNodeParams
    {
        /// <summary>
        /// the query plans XML file text to find the next non-ignore node.
        /// </summary>
        public string QueryPlanXmlText { get; set; }

        /// <summary>
        /// The node ID that indicates where searching will begin for the next
        /// non-ignore node.
        /// </summary>
        public int StartingNodeID { get; set; }
    }

    public class FindNextNonIgnoreNodeResult
    {
        /// <summary>
        /// Next node that should not be ignored for show plan comparisons.
        /// </summary>
        public NodeDTO NextNonIgnoreNode { get; set; }
    }

    public class FindNextNonIgnoreNodeRequest
    {
        public static readonly
            RequestType<FindNextNonIgnoreNodeParams, FindNextNonIgnoreNodeResult> Type =
                RequestType<FindNextNonIgnoreNodeParams, FindNextNonIgnoreNodeResult>.Create("queryexecutionplan/findnextnonignorenode");
    }
}
