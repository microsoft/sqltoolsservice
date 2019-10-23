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

        public List<DiffEntry> ChangedDifferences;


        public SchemaCompareIncludeExcludeNodeOperation(SchemaCompareNodeParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

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

                // Check first if the dependencies will allow this if it's an exclude request
                if (!this.Parameters.IncludeRequest)
                {
                    IEnumerable<SchemaDifference> dependencies = this.ComparisonResult.GetExcludeDependencies(node);

                    bool block = false;
                    foreach (SchemaDifference entry in dependencies)
                    {
                        if (entry.Included)
                        {
                            block = true;
                            break;
                        }
                    }

                    if (block)
                    {
                        this.Success = false;
                        return;
                    }
                }

                this.Success = this.Parameters.IncludeRequest ? this.ComparisonResult.Include(node) : this.ComparisonResult.Exclude(node);

                // send affected dependencies of this request
                if (this.Success)
                {
                    IEnumerable<SchemaDifference> dependencies = this.ComparisonResult.GetIncludeDependencies(node);

                    this.ChangedDifferences = new List<DiffEntry>();
                    if (dependencies != null)
                    {
                        foreach (SchemaDifference difference in dependencies)
                        {
                            DiffEntry diffEntry = SchemaCompareUtils.CreateDiffEntry(difference, null);
                            this.ChangedDifferences.Add(diffEntry);
                        }
                    }
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
                if (prop.Name != "Included")
                {
                    result = result &&
                        ((prop.GetValue(diffEntry) == null &&
                        prop.GetValue(entryFromDifference) == null) ||
                        prop.GetValue(diffEntry).SafeToString().Equals(prop.GetValue(entryFromDifference).SafeToString()));
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
