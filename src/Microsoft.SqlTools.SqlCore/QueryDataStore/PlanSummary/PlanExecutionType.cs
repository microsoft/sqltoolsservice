//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary
{
    /// <summary>
    /// The execution type for each query plan.
    /// </summary>
    public enum PlanExecutionType
    {
        /// <summary>
        /// Query successfully completed
        /// </summary>
        Completed = 0,

        /// <summary>
        /// Query was canceled by the user
        /// </summary>
        Canceled = 3,

        /// <summary>
        /// Query failed due to an error or exception
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Invalid execution type used as a place holder for uninitialized execution types
        /// </summary>
        Invalid = 255
    }
}
