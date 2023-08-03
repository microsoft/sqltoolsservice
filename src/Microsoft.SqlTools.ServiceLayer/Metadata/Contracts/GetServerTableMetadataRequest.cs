//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GetServerTableMetadataParams
    {
        public string OwnerUri { get; set; }
    }

    public class GetServerTableMetadataResult
    {
        public string[] Scripts { get; set; }
    }

    public class GetServerTableMetadataRequest
    {
        public static readonly RequestType<GetServerTableMetadataParams, GetServerTableMetadataResult> Type =
            RequestType<GetServerTableMetadataParams, GetServerTableMetadataResult>.Create("metadata/getServerTableMetadata");
    }
}
