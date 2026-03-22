//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.DacFx;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Threading;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare save SCMP operation.
    /// Connection resolution is handled by ISchemaCompareConnectionProvider.
    /// </summary>
    public class SchemaCompareSaveScmpOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;
        private readonly ISchemaCompareConnectionProvider _connectionProvider;

        /// <summary>
        /// Gets the unique identifier for this save operation.
        /// </summary>
        public string OperationId { get; private set; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error message if the save operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the save SCMP operation.
        /// </summary>
        public SchemaCompareSaveScmpParams Parameters { get; set; }

        /// <summary>
        /// Initializes a new save SCMP operation with parameters and a connection provider.
        /// </summary>
        public SchemaCompareSaveScmpOperation(SchemaCompareSaveScmpParams parameters, ISchemaCompareConnectionProvider connectionProvider)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("parameters.ScmpFilePath", parameters.ScmpFilePath);
            this.Parameters = parameters;
            this._connectionProvider = connectionProvider;
            this.OperationId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Executes the save operation, writing the comparison configuration to an SCMP file.
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

                if (Parameters.ExcludedSourceObjects != null)
                {
                    foreach (var sourceObj in this.Parameters.ExcludedSourceObjects)
                    {
                        SchemaComparisonExcludedObjectId excludedObjId = SchemaCompareUtils.CreateExcludedObject(sourceObj);
                        if (excludedObjId != null)
                        {
                            comparison.ExcludedSourceObjects.Add(excludedObjId);
                        }
                    }
                }

                if (Parameters.ExcludedTargetObjects != null)
                {
                    foreach (var targetObj in this.Parameters.ExcludedTargetObjects)
                    {
                        SchemaComparisonExcludedObjectId excludedObjId = SchemaCompareUtils.CreateExcludedObject(targetObj);
                        if (excludedObjId != null)
                        {
                            comparison.ExcludedTargetObjects.Add(excludedObjId);
                        }
                    }
                }

                if (this.Parameters.DeploymentOptions != null)
                {
                    comparison.Options = DacFxUtils.CreateDeploymentOptions(this.Parameters.DeploymentOptions);
                }

                comparison.SaveToFile(this.Parameters.ScmpFilePath, true);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare save settings operation {0} failed with exception {1}", this.OperationId, e));
                throw;
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
