//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.SqlCore.Metadata;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class MetadataQueryParams 
    {
        public string OwnerUri { get; set; }        
    }

    public class MetadataQueryResult
    {
        public ObjectMetadata[] Metadata { get; set; }
    }

    public class MetadataListRequest
    {
        public static readonly
            RequestType<MetadataQueryParams, MetadataQueryResult> Type =
                RequestType<MetadataQueryParams, MetadataQueryResult>.Create("metadata/list");
    }
}
