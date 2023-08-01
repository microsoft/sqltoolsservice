//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GetServerMetadataParams
    {
        public string OwnerUri { get; set; }
    }

    public class GetServerMetadataResult
    {
        public bool Success { get; set; }
        public string[] Scripts { get; set; }
    }

    public class GetServerMetadataRequest
    {
        public static readonly RequestType<GetServerMetadataParams, GetServerMetadataResult> Type =
            RequestType<GetServerMetadataParams, GetServerMetadataResult>.Create("metadata/getServerMetadata");
    }
}
