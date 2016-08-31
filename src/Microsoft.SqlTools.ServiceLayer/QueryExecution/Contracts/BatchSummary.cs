//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Summary of a batch within a query
    /// </summary>
    public class BatchSummary
    {
        /// <summary>
        /// Whether or not the batch was successful. True indicates errors, false indicates success
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// The ID of the result set within the query results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Any messages that came back from the server during execution of the batch
        /// </summary>
        public string[] Messages { get; set; }

        /// <summary>
        /// The summaries of the result sets inside the batch
        /// </summary>
        public ResultSetSummary[] ResultSetSummaries { get; set; }
    }
}
