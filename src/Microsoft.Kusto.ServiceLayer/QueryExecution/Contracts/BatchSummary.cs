//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Summary of a batch within a query
    /// </summary>
    public class BatchSummary
    {
        /// <summary>
        /// Localized timestamp for how long it took for the execution to complete
        /// </summary>
        public string ExecutionElapsed { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution completed.
        /// </summary>
        public string ExecutionEnd { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution started.
        /// </summary>
        public string ExecutionStart { get; set; }

        /// <summary>
        /// Whether or not the batch encountered an error that halted execution
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// The ID of the result set within the query results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The selection from the file for this batch
        /// </summary>
        public SelectionData Selection { get; set; }

        /// <summary>
        /// The summaries of the result sets inside the batch
        /// </summary>
        public ResultSetSummary[] ResultSetSummaries { get; set; }

        /// <summary>
        /// The special action of the batch 
        /// </summary>
        public SpecialAction SpecialAction { get; set; }

        public override string ToString() => $"Batch Id:'{Id}', Elapsed:'{ExecutionElapsed}', HasError:'{HasError}'";
    }
}
