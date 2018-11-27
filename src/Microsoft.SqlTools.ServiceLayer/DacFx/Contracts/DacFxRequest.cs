//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx request.
    /// </summary>
    public interface IDacFxParams : IScriptableRequestParams
    {
        /// <summary>
        /// Gets or sets package filepath
        /// </summary>
        string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for database
        /// </summary>
        string DatabaseName { get; set; }
    }

    /// <summary>
    /// Parameters returned from a DacFx request.
    /// </summary>
    public class DacFxResult : ResultStatus
    {
        public string OperationId { get; set; }
    }
}
