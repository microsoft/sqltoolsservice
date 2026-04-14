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
    /// Host-agnostic schema compare save SCMP file operation
    /// </summary>
    public class SchemaCompareSaveScmpOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        public string ErrorMessage { get; set; }

        public SchemaCompareSaveScmpParams Parameters { get; set; }

        public ISchemaCompareConnectionProvider ConnectionProvider { get; set; }

        public SchemaCompareSaveScmpOperation(SchemaCompareSaveScmpParams parameters, ISchemaCompareConnectionProvider connectionProvider)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("parameters.ScmpFilePath", parameters.ScmpFilePath);
            this.Parameters = parameters;
            this.ConnectionProvider = connectionProvider;
            this.OperationId = Guid.NewGuid().ToString();
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
