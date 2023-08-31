//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GenerateServerContextualizationParams
    {
        /// <summary>
        /// The URI of the connection to generate context for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class GenerateServerContextualizationResult
    {
        /// <summary>
        /// The generated server context.
        /// </summary>
        public string? Context { get; set; }
    }

    /// <summary>
    /// Generate server context request endpoint.
    /// </summary>
    public class GenerateServerContextualizationRequest
    {
        public static readonly RequestType<GenerateServerContextualizationParams, GenerateServerContextualizationResult> Type =
            RequestType<GenerateServerContextualizationParams, GenerateServerContextualizationResult>.Create("metadata/generateServerContext");
    }
}
