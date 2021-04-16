//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Contracts.Metadata
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
