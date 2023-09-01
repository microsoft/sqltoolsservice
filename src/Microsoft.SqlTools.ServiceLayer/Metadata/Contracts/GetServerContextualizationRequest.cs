//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GetServerContextualizationParams
    {
        /// <summary>
        /// The URI to generate and retrieve server contextualization for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class GetServerContextualizationResult
    {
        /// <summary>
        /// The generated context.
        /// </summary>
        public string Context { get; set; }
    }

    public class GetServerContextualizationRequest
    {
        public static RequestType<GetServerContextualizationParams, GetServerContextualizationResult> Type =
            RequestType<GetServerContextualizationParams, GetServerContextualizationResult>.Create("metadata/getServerContext");
    }
}
