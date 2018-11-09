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
    /// Class to represent an in-progress deploy operation
    /// </summary>
    class DacFxDeployOperation : DacFxOperation
    {
        public DacFxDeployParams Parameters { get; }
        private SqlConnection sqlConnection { get; set; }

        public DacFxDeployOperation(DacFxDeployParams parameters, SqlConnection sqlConnection)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("sqlConnection", sqlConnection);
            this.Parameters = parameters;
            this.sqlConnection = sqlConnection;
        }

        public override void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                DacServices ds = new DacServices(this.sqlConnection.ConnectionString);
                DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
                ds.Deploy(dacpac, this.Parameters.TargetDatabaseName, false, null, this.CancellationToken);
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("DacFx deploy operation {0} failed with exception {1}", this.OperationId, e));
                throw;
            }
        }
    }
}
