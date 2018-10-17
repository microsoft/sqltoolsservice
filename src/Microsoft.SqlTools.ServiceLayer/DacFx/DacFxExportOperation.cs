using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress export operation
    /// </summary>
    class DacFxExportOperation : DacFxOperation
    {
        private bool disposed = false;

        public DacFxExportParams Parameters { get; }

        public DacFxExportOperation(DacFxExportParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);

            this.Parameters = parameters;
        }

        public override void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
                DacServices ds = new DacServices(this.Parameters.ConnectionString);
                string filepath = string.Format("C:\\Users\\kisantia\\bacpac\\{0}{1}.bacpac", builder.InitialCatalog, Guid.NewGuid());
                ds.ExportBacpac(filepath, builder.InitialCatalog, null, null);
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("DacFx export operation {0} failed with exception {1}", this.OperationId, e));
            }
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
