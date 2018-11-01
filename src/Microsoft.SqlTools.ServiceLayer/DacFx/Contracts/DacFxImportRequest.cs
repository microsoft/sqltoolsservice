using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx import request.
    /// </summary>
    public class DacFxImportParams : IScriptableRequestParams
    {
        /// <summary>
        /// Gets or sets connection string of the target database the import operation will run against.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets bacpac package filepath
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for imported database
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Executation mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Parameters returned from a DacFx import request.
    /// </summary>
    public class DacFxImportResult : ResultStatus
    {
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Defines the DacFx import request type
    /// </summary>
    class DacFxImportRequest
    {
        public static readonly RequestType<DacFxImportParams, DacFxImportResult> Type =
            RequestType<DacFxImportParams, DacFxImportResult>.Create("dacfx/import");
    }
}
