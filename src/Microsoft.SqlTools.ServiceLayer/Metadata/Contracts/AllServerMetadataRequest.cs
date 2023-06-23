//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class AllServerMetadataParams
    {
        public string OwnerUri { get; set; }
    }

    public class AllServerMetadataResult
    {
        public string Scripts { get; set; }
    }

    public class AllMetadataRequest
    {
        public static readonly RequestType<AllServerMetadataParams, AllServerMetadataResult> Type =
            RequestType<AllServerMetadataParams, AllServerMetadataResult>.Create("metadata/getAll");
    }
}
