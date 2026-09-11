//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare include/exclude node operation
    /// </summary>
    public class SchemaCompareIncludeExcludeNodeOperation : IDisposable
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
            this.OperationId = !string.IsNullOrEmpty(parameters.OperationId) ? parameters.OperationId : Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Exclude will return false if included dependencies are found. Include will also include dependencies that need to be included. 
        /// This is the same behavior as SSDT
        /// </summary>
        public void Execute()
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

                // if include request (pass or fail), send dependencies that might have been affected by this request, given by GetIncludeDependencies()
                if (this.Parameters.IncludeRequest)
                {
                    IEnumerable<SchemaDifference> affectedDependencies = this.ComparisonResult.GetIncludeDependencies(node);
                    this.AffectedDependencies = affectedDependencies.Select(SchemaCompareUtils.CreateDiffEntrySummary).ToList();
                }
                else
                {   // if exclude was successful, the possible affected dependencies are given by GetIncludedDependencies()
                    if (this.Success)
                    {
                        IEnumerable<SchemaDifference> affectedDependencies = this.ComparisonResult.GetIncludeDependencies(node);
                        this.AffectedDependencies = affectedDependencies.Select(SchemaCompareUtils.CreateDiffEntrySummary).ToList();
                    }
                    // if not successful, send back the exclude dependencies that caused it to fail
                    else
                    {
                        IEnumerable<SchemaDifference> blockingDependencies = this.ComparisonResult.GetExcludeDependencies(node);
                        blockingDependencies = blockingDependencies.Where(difference => difference.Included == node.Included);
                        this.BlockingDependencies = blockingDependencies.Select(SchemaCompareUtils.CreateDiffEntrySummary).ToList();
                    }

                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare publish changes operation {0} failed with exception {1}", this.OperationId, e.Message));
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

        private static bool IsEqual(SchemaDifference difference, DiffEntry diffEntry)
        {
            if (difference == null || diffEntry == null ||
                difference.UpdateAction != diffEntry.UpdateAction ||
                difference.DifferenceType != diffEntry.DifferenceType ||
                !string.Equals(difference.Name, diffEntry.Name, StringComparison.Ordinal))
            {
                return false;
            }

            return IsObjectEqual(
                    difference.SourceObject,
                    diffEntry.SourceValue,
                    diffEntry.SourceObjectType) &&
                IsObjectEqual(
                    difference.TargetObject,
                    diffEntry.TargetValue,
                    diffEntry.TargetObjectType);
        }

        private static bool IsObjectEqual(
            Microsoft.SqlServer.Dac.Model.TSqlObject schemaObject,
            string[] nameParts,
            string objectType)
        {
            if (schemaObject == null)
            {
                return nameParts == null && string.IsNullOrEmpty(objectType);
            }

            if (nameParts == null ||
                !schemaObject.Name.Parts.SequenceEqual(nameParts, StringComparer.Ordinal))
            {
                return false;
            }

            string schemaObjectType = new Microsoft.SqlServer.Dac.Compare.SchemaComparisonExcludedObjectId(
                schemaObject.ObjectType,
                schemaObject.Name).TypeName;
            return string.Equals(schemaObjectType, objectType, StringComparison.Ordinal);
        }

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
