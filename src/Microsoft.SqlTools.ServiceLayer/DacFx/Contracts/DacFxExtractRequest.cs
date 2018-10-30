using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx extract request.
    /// </summary>
    public class DacFxExtractParams
    {
        /// <summary>
        /// Gets or sets connection string of the target database the extract operation will run against.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets package file name for extracted dacpac
        /// </summary>
        public string PackageFileName { get; set; }

        /// <summary>
        /// Gets or sets the string identifier for the DAC application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the version of the DAC application
        /// </summary>
        public Version ApplicationVersion { get; set; }
    }

    /// <summary>
    /// Parameters returned from a DacFx extract request.
    /// </summary>
    public class DacFxExtractResult : ResultStatus
    {
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Defines the DacFx extract request type
    /// </summary>
    class DacFxExtractRequest
    {
        public static readonly RequestType<DacFxExtractParams, DacFxExtractResult> Type =
            RequestType<DacFxExtractParams, DacFxExtractResult>.Create("dacfx/extract");
    }
}
