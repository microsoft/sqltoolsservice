using Microsoft.SqlTools.Hosting.Protocol.Contracts;

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
    }

    /// <summary>
    /// Parameters returned from a DacFx request.
    /// </summary>
    public class DacFxExportResult
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
