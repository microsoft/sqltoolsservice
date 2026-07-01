//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.DacFx;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare operation
    /// </summary>
    public class SchemaCompareOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SchemaCompareParams Parameters { get; set; }

        public ISchemaCompareConnectionProvider ConnectionProvider { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public List<DiffEntry> Differences;

        /// <summary>
        /// The platform the comparison ran under (DacFx <c>SqlPlatforms</c> enum name, e.g.
        /// "Sql160", "SqlAzureV12", "SqlDwUnified"). Sourced from the comparison's
        /// <c>DatabaseSchemaProvider.Platform</c> (the unified DSP the comparison normalized
        /// to). Source and Target carry the same value because DacFx runs a single comparison
        /// under a single DSP — see <see cref="SchemaCompareUtils.GetComparisonPlatform"/>.
        /// Null if the
        /// platform could not be detected (e.g. the comparison was never run).
        /// </summary>
        public string SourcePlatform { get; set; }

        /// <summary>
        /// The platform the comparison ran under. See <see cref="SourcePlatform"/>.
        /// </summary>
        public string TargetPlatform { get; set; }

        public SchemaCompareOperation(SchemaCompareParams parameters, ISchemaCompareConnectionProvider connectionProvider)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            this.ConnectionProvider = connectionProvider;
            this.OperationId = !string.IsNullOrEmpty(parameters.OperationId) ? parameters.OperationId : Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; set; }

        public void Cancel()
        {
            this.cancellation.Cancel();
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

        public void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaCompareEndpoint sourceEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.SourceEndpointInfo, this.ConnectionProvider);
                SchemaCompareEndpoint targetEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.TargetEndpointInfo, this.ConnectionProvider);

                SchemaComparison comparison = new SchemaComparison(sourceEndpoint, targetEndpoint);

                if (this.Parameters.DeploymentOptions != null)
                {
                    comparison.Options = DacFxUtils.CreateDeploymentOptions(this.Parameters.DeploymentOptions);
                }

                // for testing
                schemaCompareStarted?.Invoke(this, new EventArgs());

                this.ComparisonResult = comparison.Compare(this.CancellationToken);

                // try one more time if it didn't work the first time
                if (!this.ComparisonResult.IsValid)
                {
                    this.ComparisonResult = comparison.Compare(this.CancellationToken);
                }

                // Since DacFx does not throw on schema comparison cancellation, throwing here explicitly to ensure consistency of behavior
                if (!this.ComparisonResult.IsValid && this.CancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(this.CancellationToken);
                }

                this.Differences = new List<DiffEntry>();
                if (this.ComparisonResult.Differences != null)
                {
                    // filter out not included and not excludeable differences
                    (this.ComparisonResult.Differences as List<SchemaDifference>).RemoveAll(d => !d.Included && !d.IsExcludable);

                    foreach (SchemaDifference difference in this.ComparisonResult.Differences)
                    {
                        DiffEntry diffEntry = SchemaCompareUtils.CreateDiffEntry(difference, null, this.ComparisonResult);
                        this.Differences.Add(diffEntry);
                    }
                }

                // Expose the platform the comparison actually ran under so clients can show
                // the user which T-SQL dialect Schema Compare is using (e.g. "SqlDwUnified"
                // when comparing Fabric Warehouse endpoints).
                //
                // We CANNOT use ComparisonResult.SourceModel.Version because DacFx maps the
                // SqlDwUnified platform to SqlServerVersion.Sql150 in
                // InternalModelUtils.CalculateVersionsForPlatform (it predates the
                // SqlServerVersion.SqlDwUnified enum value). The TSqlModel.Version surface
                // therefore reports "Sql150" for every Fabric Warehouse model, which would
                // mislabel the platform pill in the UI.
                //
                // Instead, read the comparison's normalized DSP from
                // SchemaComparisonResult.DataModel.DatabaseSchemaProvider.Platform via
                // reflection. DatabaseSchemaProvider is internal-abstract in DacFx, but
                // .Platform is a public abstract SqlPlatforms property (a public enum), and
                // the cascade fix in PR 2143938 confirms this value is reliably populated.
                string comparisonPlatform = SchemaCompareUtils.GetComparisonPlatform(this.ComparisonResult);
                this.SourcePlatform = comparisonPlatform;
                this.TargetPlatform = comparisonPlatform;

                // Appending the set of errors that are stopping the schema compare to the ErrorMessage
                // GetErrors return all type of warnings, and error messages. Only filtering the error type messages here
                var errorsList = ComparisonResult.GetErrors().Where(x => x.MessageType.Equals(DacMessageType.Error)).Select(e => e.Message).Distinct().ToList();
                if (errorsList.Count > 0)
                {
                    ErrorMessage = string.Join("\n", errorsList);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        internal event EventHandler<EventArgs> schemaCompareStarted;
    }
}
