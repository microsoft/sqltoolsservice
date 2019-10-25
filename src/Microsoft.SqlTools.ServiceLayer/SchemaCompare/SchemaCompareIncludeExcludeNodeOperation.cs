//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare include/exclude Node operation
    /// </summary>
    class SchemaCompareIncludeExcludeNodeOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SchemaCompareNodeParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        public string ErrorMessage { get; set; }

        public SqlTask SqlTask { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public bool Success { get; set; }

        public List<DiffEntry> AffectedDependencies;
        public List<DiffEntry> BlockingDependencies;


        public SchemaCompareIncludeExcludeNodeOperation(SchemaCompareNodeParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

        /// <summary>
        /// Exclude will return false if included dependencies are found. Include will also include dependencies that need to be included. 
        /// This is the same behavior as SSDT
        /// </summary>
        /// <param name="mode"></param>
        public void Execute(TaskExecutionMode mode)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                SchemaDifference node = this.FindDifference(this.ComparisonResult.Differences, this.Parameters.DiffEntry);
                if (node == null)
                {
                    throw new InvalidOperationException(SR.SchemaCompareExcludeIncludeNodeNotFound);
                }

                this.Success = this.Parameters.IncludeRequest ? this.ComparisonResult.Include(node) : this.ComparisonResult.Exclude(node);

                // if successful, send dependencies that might have been affected by this request
                if(this.Success)
                {
                    IEnumerable<SchemaDifference> affectedDependencies = this.ComparisonResult.GetIncludeDependencies(node);
                    this.AffectedDependencies = affectedDependencies.Select(difference => SchemaCompareUtils.CreateDiffEntry(difference, null)).ToList();
                }
                else
                {
                    // if not successful, send exclude dependencies that caused it to fail
                    IEnumerable<SchemaDifference> blockingDependencies = this.ComparisonResult.GetExcludeDependencies(node);
                    blockingDependencies = blockingDependencies.Where(difference => difference.Included == node.Included);
                    this.BlockingDependencies = blockingDependencies.Select(difference => SchemaCompareUtils.CreateDiffEntry(difference, null)).ToList();
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare publish changes operation {0} failed with exception {1}", this.OperationId, e.Message));
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
            // Create a diff entry from difference and check if it matches the diff entry passed
            DiffEntry entryFromDifference = SchemaCompareUtils.CreateDiffEntry(difference, null);

            System.Reflection.PropertyInfo[] properties = diffEntry.GetType().GetProperties();
            foreach (var prop in properties)
            {
                // Don't need to check if included is the same when verifying if the difference is equal
                if (prop.Name != "Included")
                {
                    if(!((prop.GetValue(diffEntry) == null &&
                        prop.GetValue(entryFromDifference) == null) ||
                        prop.GetValue(diffEntry).SafeToString().Equals(prop.GetValue(entryFromDifference).SafeToString())))
                    {
                        return false;
                    }
                }
            }

            return result;
        }

        // The schema compare public api doesn't currently take a cancellation token so the operation can't be cancelled
        public void Cancel()
        {
        }

        /// <summary>
        /// Disposes the operation.
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
