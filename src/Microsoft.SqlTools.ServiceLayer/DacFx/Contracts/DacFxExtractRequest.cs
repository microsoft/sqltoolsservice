using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx extract request.
    /// </summary>
    public class DacFxExtractParams : IScriptableRequestParams
    {
        /// <summary>
        /// Gets or sets the target database name the extract operation will run against.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets package file path for extracted dacpac
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets the string identifier for the DAC application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the version of the DAC application
        /// </summary>
        public Version ApplicationVersion { get; set; }

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
