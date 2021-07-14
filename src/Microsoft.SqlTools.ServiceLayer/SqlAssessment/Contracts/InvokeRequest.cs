//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts
{
    /// <summary>
    /// Parameters for executing a query from a provided string
    /// </summary>
    public class InvokeParams : AssessmentParams
    {
        // a placeholder for future specialization
    }

    /// <summary>
    /// SQL Assessment result item kind.
    /// </summary>
    /// <remarks>
    /// SQL Assessment run is a set of checks. Every check
    /// may return a result item. Normally it is a note containing
    /// recommendations on improving target's configuration.
    /// But some checks may fail to obtain data due to access
    /// restrictions or data integrity. In this case
    /// the check produces an error or a warning.
    /// </remarks>
    public enum AssessmentResultItemKind
    {
        /// <summary>
        /// SQL Assessment item contains recommendation
        /// </summary>
        Note = 0,

        /// <summary>
        /// SQL Assessment item contains a warning on
        /// limited assessment capabilities
        /// </summary>
        Warning = 1,

        /// <summary>
        /// SQL Assessment item contain a description of
        /// error occured in the course of assessment run
        /// </summary>
        Error = 2
    }

    /// <summary>
    /// Describes an assessment result item
    /// containing a recommendation based on best practices.
    /// </summary>
    public class AssessmentResultItem : AssessmentItemInfo
    {
        /// <summary>
        /// Gets or sets a message to the user
        /// containing the recommendation.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets result type:
        /// 0 - real result, 1 - warning, 2 - error.
        /// </summary>
        public AssessmentResultItemKind Kind { get; set; }

        /// <summary>
        /// Gets or sets date and time
        /// when the item had been acquired.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }

    public class InvokeRequest
    {
        public static readonly
            RequestType<InvokeParams, AssessmentResult<AssessmentResultItem>> Type =
            RequestType<InvokeParams, AssessmentResult<AssessmentResultItem>>.Create("assessment/invoke");
    }
}
