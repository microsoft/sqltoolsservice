//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare include/exclude node operation.
    /// </summary>
    public class SchemaCompareIncludeExcludeNodeOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique identifier for this operation.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets the parameters for the include/exclude node operation.
        /// </summary>
        public SchemaCompareNodeParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The schema comparison result to include/exclude a node from.
        /// </summary>
        public SchemaComparisonResult ComparisonResult { get; set; }

        /// <summary>
        /// Whether the include/exclude operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of diff entries affected by the include/exclude operation.
        /// </summary>
        public List<DiffEntry> AffectedDependencies;

        /// <summary>
        /// List of diff entries blocking an exclude operation.
        /// </summary>
        public List<DiffEntry> BlockingDependencies;

        /// <summary>
        /// Initializes a new include/exclude node operation with parameters and comparison result.
        /// </summary>
        public SchemaCompareIncludeExcludeNodeOperation(SchemaCompareNodeParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

        /// <summary>
        /// Exclude will return false if included dependencies are found.
        /// Include will also include dependencies that need to be included.
        /// </summary>
        public void Execute()
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                SchemaDifference node = this.FindDifference(this.ComparisonResult.Differences, this.Parameters.DiffEntry)
                    ?? throw new InvalidOperationException("Schema compare include/exclude node not found.");
                this.Success = this.Parameters.IncludeRequest ? this.ComparisonResult.Include(node) : this.ComparisonResult.Exclude(node);

                if (this.Parameters.IncludeRequest)
                {
                    IEnumerable<SchemaDifference> affectedDependencies = this.ComparisonResult.GetIncludeDependencies(node);
                    this.AffectedDependencies = affectedDependencies.Select(difference => SchemaCompareUtils.CreateDiffEntry(difference: difference, parent: null, schemaComparisonResult: this.ComparisonResult)).ToList();
                }
                else
                {
                    if (this.Success)
                    {
                        IEnumerable<SchemaDifference> affectedDependencies = this.ComparisonResult.GetIncludeDependencies(node);
                        this.AffectedDependencies = affectedDependencies.Select(difference => SchemaCompareUtils.CreateDiffEntry(difference: difference, parent: null, schemaComparisonResult: this.ComparisonResult)).ToList();
                    }
                    else
                    {
                        IEnumerable<SchemaDifference> blockingDependencies = this.ComparisonResult.GetExcludeDependencies(node);
                        blockingDependencies = blockingDependencies.Where(difference => difference.Included == node.Included);
                        this.BlockingDependencies = blockingDependencies.Select(difference => SchemaCompareUtils.CreateDiffEntry(difference: difference, parent: null, schemaComparisonResult: this.ComparisonResult)).ToList();
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare include/exclude operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        private SchemaDifference FindDifference(IEnumerable<SchemaDifference> differences, DiffEntry diffEntry)
        {
            foreach (var difference in differences)
            {
                if (IsEqual(difference, diffEntry))
                {
                    return difference;
                }
                else
                {
                    var childDiff = FindDifference(difference.Children, diffEntry);
                    if (childDiff != null)
                    {
                        return childDiff;
                    }
                }
            }
            return null;
        }

        private bool IsEqual(SchemaDifference difference, DiffEntry diffEntry)
        {
            bool result = true;
            DiffEntry entryFromDifference = SchemaCompareUtils.CreateDiffEntry(difference, null, schemaComparisonResult: this.ComparisonResult);

            System.Reflection.PropertyInfo[] properties = diffEntry.GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name != "Included")
                {
                    var diffVal = prop.GetValue(diffEntry);
                    var entryVal = prop.GetValue(entryFromDifference);
                    if (!((diffVal == null && entryVal == null) ||
                        (diffVal != null && entryVal != null && diffVal.ToString().Equals(entryVal.ToString()))))
                    {
                        return false;
                    }
                }
            }

            return result;
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
