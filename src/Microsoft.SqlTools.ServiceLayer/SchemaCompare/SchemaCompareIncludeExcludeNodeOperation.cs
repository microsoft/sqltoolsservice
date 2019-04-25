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
using System.IO;
using System.Text.RegularExpressions;
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

        public SchemaCompareIncludeExcludeNodeOperation(SchemaCompareNodeParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

        public void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaDifference node = this.FindDifference(this.ComparisonResult.Differences, this.Parameters.DiffEntry);
                if (node == null)
                {
                    throw new ArgumentException(SR.SchemaCompareExcludeIncludeNodeNotFound);
                }

                this.Success = this.Parameters.IncludeRequest ? this.ComparisonResult.Include(node) : this.ComparisonResult.Exclude(node);
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
            //create a diff entry from difference and check if it matches the diffentr passed
            DiffEntry entryFromDifference = SchemaCompareOperation.CreateDiffEntry(difference, null);

            System.Reflection.PropertyInfo[] properties = diffEntry.GetType().GetProperties();
            foreach (var prop in properties)
            {
                result = result && ((prop.GetValue(diffEntry) == null && prop.GetValue(entryFromDifference) == null) || prop.GetValue(diffEntry).SafeToString().Equals(prop.GetValue(entryFromDifference).SafeToString()));
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
