//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare include/exclude all Node operation
    /// </summary>
    class SchemaCompareIncludeExcludeAllNodesOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SchemaCompareIncludeExcludeAllNodesParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        public string ErrorMessage { get; set; }

        public SqlTask SqlTask { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public bool Success { get; set; }

        public List<DiffEntry> AllIncludedOrExcludedDifferences;


        public SchemaCompareIncludeExcludeAllNodesOperation(SchemaCompareIncludeExcludeAllNodesParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

        /// <summary>
        /// Execute will include/exclude all differences in the schema compare result.
        /// </summary>
        /// <param name="mode"></param>
        public void Execute(TaskExecutionMode mode)
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
                this.Success = this.Parameters.IncludeRequest ? this.ComparisonResult.Include(difference) : this.ComparisonResult.Exclude(difference);

                if (!this.Success)
                {
                    problematicDifferences.Add(difference);
                }
            }

            if (problematicDifferences.Count != 0)
            {
                IncludeExcludeAllDifferences(problematicDifferences);
            }
        }

        /// <summary>
        /// The schema compare public api doesn't currently take a cancellation token so the operation can't be cancelled 
        /// </summary>
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
