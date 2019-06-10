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


        public SchemaCompareSaveScmpOperation(SchemaCompareSaveScmpParams parameters, ConnectionInfo sourceConnInfo, ConnectionInfo targetConnInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("parameters.scmpFilePath", parameters.scmpFilePath);
            this.Parameters = parameters;
            this.SourceConnectionString = SchemaCompareUtils.GetConnectionString(sourceConnInfo, parameters.SourceEndpointInfo.DatabaseName);
            this.TargetConnectionString = SchemaCompareUtils.GetConnectionString(targetConnInfo, parameters.TargetEndpointInfo.DatabaseName);
            this.OperationId = Guid.NewGuid().ToString();
        }

        public void Execute(TaskExecutionMode mode)
        {
            Execute();
        }

        public void Execute()
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

                comparison.SaveToFile(this.Parameters.scmpFilePath, true);
                
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare save settings operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
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
