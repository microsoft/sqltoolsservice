//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ImportModelRequestParams : ModelRequestBase
    {
        /// <summary>
        /// Model metadata
        /// </summary>
        public ModelMetadata Model { get; set; }
    }

    /// <summary>
    /// Response class for import model
    /// </summary>
    public class ImportModelResponseParams : ModelResponseBase
    {
    }

    /// <summary>
    /// Request class to import a model
    /// </summary>
    public class ImportModelRequest
    {
        public static readonly
            RequestType<ImportModelRequestParams, ImportModelResponseParams> Type =
                RequestType<ImportModelRequestParams, ImportModelResponseParams>.Create("models/import");
    }
}
