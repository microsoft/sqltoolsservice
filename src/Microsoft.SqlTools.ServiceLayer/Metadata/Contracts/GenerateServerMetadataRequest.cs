//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GenerateServerMetadataParams
    {
        public string OwnerUri { get; set; }
    }

    public class GenerateServerMetadataResult
    {
        public bool Success { get; set; }
    }

    public class GenerateServerMetadataRequest
    {
        public static readonly RequestType<GenerateServerMetadataParams, GenerateServerMetadataResult> Type =
            RequestType<GenerateServerMetadataParams, GenerateServerMetadataResult>.Create("metadata/generateServerMetadata");
    }
}
