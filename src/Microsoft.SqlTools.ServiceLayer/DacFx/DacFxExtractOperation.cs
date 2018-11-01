using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress extract operation
    /// </summary>
    class DacFxExtractOperation : DacFxOperation
    {
        private bool disposed = false;

        public DacFxExtractParams Parameters { get; }

        public DacFxExtractOperation(DacFxExtractParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);

            this.Parameters = parameters;
        }

        public override void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
                DacServices ds = new DacServices(this.Parameters.ConnectionString);
                ds.Extract(this.Parameters.PackageFileName, builder.InitialCatalog, this.Parameters.ApplicationName, this.Parameters.ApplicationVersion);
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("DacFx extract operation {0} failed with exception {1}", this.OperationId, e));
            }
        }
    }
}
