//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    /// Host-agnostic schema compare operation.
    /// Connection resolution is handled by ISchemaCompareConnectionProvider.
    /// </summary>
    public class SchemaCompareOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;
        private readonly ISchemaCompareConnectionProvider _connectionProvider;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets or sets the parameters for the schema compare operation.
        /// </summary>
        public SchemaCompareParams Parameters { get; set; }

        /// <summary>
        /// The schema comparison result from DacFx.
        /// </summary>
        public SchemaComparisonResult ComparisonResult { get; set; }

        /// <summary>
        /// List of diff entries representing the differences found.
        /// </summary>
        public List<DiffEntry> Differences;

        /// <summary>
        /// Initializes a new schema compare operation with parameters and a connection provider.
        /// </summary>
        public SchemaCompareOperation(SchemaCompareParams parameters, ISchemaCompareConnectionProvider connectionProvider)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            this._connectionProvider = connectionProvider;
            this.OperationId = !string.IsNullOrEmpty(parameters.OperationId) ? parameters.OperationId : Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Cancels the running schema comparison.
        /// </summary>
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

        /// <summary>
        /// Executes the schema comparison between source and target endpoints.
        /// </summary>
        public void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaCompareEndpoint sourceEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.SourceEndpointInfo, this._connectionProvider);
                SchemaCompareEndpoint targetEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.TargetEndpointInfo, this._connectionProvider);

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

                // Since DacFx does not throw on schema comparison cancellation, throwing here explicitly
                if (!this.ComparisonResult.IsValid && this.CancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(this.CancellationToken);
                }

                this.Differences = new List<DiffEntry>();
                if (this.ComparisonResult.Differences != null)
                {
                    (this.ComparisonResult.Differences as List<SchemaDifference>).RemoveAll(d => !d.Included && !d.IsExcludable);

                    foreach (SchemaDifference difference in this.ComparisonResult.Differences)
                    {
                        DiffEntry diffEntry = SchemaCompareUtils.CreateDiffEntry(difference, null, this.ComparisonResult);
                        this.Differences.Add(diffEntry);
                    }
                }

                var errorsList = ComparisonResult.GetErrors().Where(x => x.MessageType.Equals(Microsoft.SqlServer.Dac.DacMessageType.Error)).Select(e => e.Message).Distinct().ToList();
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
