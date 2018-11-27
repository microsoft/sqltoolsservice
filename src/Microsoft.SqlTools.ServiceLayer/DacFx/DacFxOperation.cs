//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Base class for DacFx operations
    /// </summary>
    abstract class DacFxOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        protected SqlConnection SqlConnection { get; set; }

        protected DacServices DacServices { get; set; }

        protected DacFxOperation(SqlConnection sqlConnection)
        {
            Validate.IsNotNull("sqlConnection", sqlConnection);
            this.SqlConnection = sqlConnection;
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Cancel operation
        /// </summary>
        public void Cancel()
        {
            if (!this.cancellation.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, string.Format("Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
            }
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
                this.DacServices = new DacServices(this.SqlConnection.ConnectionString);
                Execute();
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("DacFx import operation {0} failed with exception {1}", this.OperationId, e));
                throw;
            }
        }

        public abstract void Execute();
    }
}
