//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    class SchemaCompareSaveScmpOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        public string ErrorMessage { get; set; }

        public SqlTask SqlTask { get; set; }

        public SchemaCompareSaveScmpParams Parameters { get; set; }

        public string SourceConnectionString { get; set; }

        public string TargetConnectionString { get; set; }

        public ConnectionInfo SourceConnectionInfo { get; set; }

        public ConnectionInfo TargetConnectionInfo { get; set; }

        public SchemaCompareSaveScmpOperation(SchemaCompareSaveScmpParams parameters, ConnectionInfo sourceConnInfo, ConnectionInfo targetConnInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("parameters.ScmpFilePath", parameters.ScmpFilePath);
            this.Parameters = parameters;
            this.SourceConnectionInfo = sourceConnInfo;
            this.TargetConnectionInfo = targetConnInfo;
            this.SourceConnectionString = SchemaCompareUtils.GetConnectionString(sourceConnInfo, parameters.SourceEndpointInfo.DatabaseName);
            this.TargetConnectionString = SchemaCompareUtils.GetConnectionString(targetConnInfo, parameters.TargetEndpointInfo.DatabaseName);
            this.OperationId = Guid.NewGuid().ToString();
        }

        public void Execute(TaskExecutionMode mode = TaskExecutionMode.Execute)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaCompareEndpoint sourceEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.SourceEndpointInfo, this.SourceConnectionString, this.SourceConnectionInfo);
                SchemaCompareEndpoint targetEndpoint = SchemaCompareUtils.CreateSchemaCompareEndpoint(this.Parameters.TargetEndpointInfo, this.TargetConnectionString, this.TargetConnectionInfo);

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
                    comparison.Options = SchemaCompareUtils.CreateSchemaCompareOptions(this.Parameters.DeploymentOptions);
                }

                comparison.SaveToFile(this.Parameters.ScmpFilePath, true);

            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare save settings operation {0} failed with exception {1}", this.OperationId, e));
                throw;
            }
        }

        // The schema compare public api doesn't currently take a cancellation token for scmp save so the operation can't be cancelled
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
