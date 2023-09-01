//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GetOrGenerateServerContextualizationParams
    {
        /// <summary>
        /// The URI to generate server contextualization scripts for, if needed, and to retrieve for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class GetOrGenerateServerContextualizationResult
    {
        /// <summary>
        /// The generated context.
        /// </summary>
        public string Context { get; set; }
    }

    public class GetOrGenerateServerContextualizationRequest
    {
        public static RequestType<GetOrGenerateServerContextualizationParams, GetOrGenerateServerContextualizationResult> Type =
            RequestType<GetOrGenerateServerContextualizationParams, GetOrGenerateServerContextualizationResult>.Create("metadata/getOrGenerateServerContextualizationRequest");
    }
}
