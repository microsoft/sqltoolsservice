using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx request.
    /// </summary>
    public class DacFxExportParams
    {
        /// <summary>
        /// Gets or sets connection string of the target database the scripting operation will run against.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets package file name for exported bacpac
        /// </summary>
        public string PackageFileName { get; set; }
    }

    /// <summary>
    /// Parameters returned from a DacFx export request.
    /// </summary>
    public class DacFxExportResult : ResultStatus
    {
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Defines the DacFx request type
    /// </summary>
    class DacFxExportRequest
    {
        public static readonly RequestType<DacFxExportParams, DacFxExportResult> Type =
            RequestType<DacFxExportParams, DacFxExportResult>.Create("dacfx/export");
    }
}
