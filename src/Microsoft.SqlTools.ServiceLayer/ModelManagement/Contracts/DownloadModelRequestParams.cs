//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class DownloadModelRequestParams : ModelRequestBase
    {
        /// <summary>
        /// Model id
        /// </summary>
        public int ModelId { get; set; }
    }

    /// <summary>
    /// Response class for import model
    /// </summary>
    public class DownloadModelResponseParams : ModelResponseBase
    {
        /// <summary>
        /// Downloaded file path
        /// </summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Request class to delete a model
    /// </summary>
    public class DownloadModelRequest
    {
        public static readonly
            RequestType<DownloadModelRequestParams, DownloadModelResponseParams> Type =
                RequestType<DownloadModelRequestParams, DownloadModelResponseParams>.Create("models/download");
    }
}
