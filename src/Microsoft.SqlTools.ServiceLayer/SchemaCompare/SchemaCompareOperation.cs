//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.Connection;
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
    /// Schema compare operation
    /// </summary>
    class SchemaCompareOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        public SchemaCompareParams Parameters { get; set; }

        public string SourceConnectionString { get; set; }

        public string TargetConnectionString { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public List<DiffEntry> Differences;

        public SchemaCompareOperation(SchemaCompareParams parameters, ConnectionInfo sourceConnInfo, ConnectionInfo targetConnInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            this.SourceConnectionString = SchemaCompareUtils.GetConnectionString(sourceConnInfo, parameters.SourceEndpointInfo.DatabaseName);
            this.TargetConnectionString = SchemaCompareUtils.GetConnectionString(targetConnInfo, parameters.TargetEndpointInfo.DatabaseName);
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; set; }

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

        public void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaCompareEndpoint sourceEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.SourceEndpointInfo, this.SourceConnectionString);
                SchemaCompareEndpoint targetEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.TargetEndpointInfo, this.TargetConnectionString);

                SchemaComparison comparison = new SchemaComparison(sourceEndpoint, targetEndpoint);

                if (this.Parameters.DeploymentOptions != null)
                {
                    comparison.Options = SchemaCompareUtils.CreateSchemaCompareOptions(this.Parameters.DeploymentOptions);
                }

                this.ComparisonResult = comparison.Compare();

                // try one more time if it didn't work the first time
                if (!this.ComparisonResult.IsValid)
                {
                    this.ComparisonResult = comparison.Compare();
                }

                this.Differences = new List<DiffEntry>();
                foreach (SchemaDifference difference in this.ComparisonResult.Differences)
                {
                    DiffEntry diffEntry = SchemaCompareUtils.CreateDiffEntry(difference, null);
                    this.Differences.Add(diffEntry);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }
    }
}
