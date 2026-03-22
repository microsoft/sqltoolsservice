//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare include/exclude all nodes operation.
    /// </summary>
    public class SchemaCompareIncludeExcludeAllNodesOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique identifier for this operation.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets the parameters for the include/exclude all nodes operation.
        /// </summary>
        public SchemaCompareIncludeExcludeAllNodesParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The schema comparison result to include/exclude nodes from.
        /// </summary>
        public SchemaComparisonResult ComparisonResult { get; set; }

        /// <summary>
        /// Whether the include/exclude operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of all diff entries after the include/exclude all operation.
        /// </summary>
        public List<DiffEntry> AllIncludedOrExcludedDifferences;

        /// <summary>
        /// Initializes a new include/exclude all nodes operation with parameters and comparison result.
        /// </summary>
        public SchemaCompareIncludeExcludeAllNodesOperation(SchemaCompareIncludeExcludeAllNodesParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

        /// <summary>
        /// Executes the include or exclude operation on all schema differences.
        /// </summary>
        public void Execute()
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                var schemaDifferences = new List<SchemaDifference>(this.ComparisonResult.Differences);
                this.IncludeExcludeAllDifferences(schemaDifferences);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare include/exclude all operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }

            this.AllIncludedOrExcludedDifferences = new List<DiffEntry>();
            if (this.ComparisonResult.Differences != null)
            {
                foreach (SchemaDifference difference in this.ComparisonResult.Differences)
                {
                    DiffEntry diffEntry = SchemaCompareUtils.CreateDiffEntry(difference, null, this.ComparisonResult);
                    this.AllIncludedOrExcludedDifferences.Add(diffEntry);
                }
            }
        }

        private void IncludeExcludeAllDifferences(List<SchemaDifference> schemaDifferences)
        {
            var problematicDifferences = new List<SchemaDifference>();
            foreach (SchemaDifference difference in schemaDifferences)
            {
                // Non-excludable items (e.g. Filegroup) will always fail Exclude().
                // Skip them to avoid infinite retry loops.
                if (!Parameters.IncludeRequest && !difference.IsExcludable)
                {
                    continue;
                }

                this.Success = this.Parameters.IncludeRequest ? this.ComparisonResult.Include(difference) : this.ComparisonResult.Exclude(difference);

                if (!this.Success)
                {
                    problematicDifferences.Add(difference);
                }
            }

            // Retry only if progress was made (list shrank), otherwise stop to prevent
            // infinite recursion when remaining items can never be excluded.
            if (problematicDifferences.Count != 0 && problematicDifferences.Count < schemaDifferences.Count)
            {
                IncludeExcludeAllDifferences(problematicDifferences);
            }
        }

        /// <summary>
        /// Cancels the running operation.
        /// </summary>
        public void Cancel()
        {
        }

        /// <summary>
        /// Disposes the operation and cancels any pending work.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
