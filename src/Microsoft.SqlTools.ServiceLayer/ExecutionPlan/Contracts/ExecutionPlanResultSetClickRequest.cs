//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class IsExecutionPlanXmlParams
    {
        /// <summary>
        /// Execution plan XML that originated from a DMV or database table.
        /// </summary>
        public string ExecutionPlanXml { get; set; } = string.Empty;
    }

    public class IsExecutionPlanXmlResult
    {
        /// <summary>
        /// Flag that indicates if the XML for the request is for an execution plan.
        /// </summary>
        public bool IsExecutionPlanXml { get; set; }

        /// <summary>
        /// Execution plan file extension that allows the execution plan viewer to render a graphical
        /// view for the execution plan. The file extension should not be preceded by a dot.
        /// </summary>
        public string QueryExecutionPlanFileExtension { get; set; } = string.Empty;
    }

    public class IsExecutionPlanXmlRequest
    {
        public static readonly
            RequestType<IsExecutionPlanXmlParams, IsExecutionPlanXmlResult> Type =
                RequestType<IsExecutionPlanXmlParams, IsExecutionPlanXmlResult>.Create("queryExecutionPlan/isExecutionPlanXml");
    }
}
