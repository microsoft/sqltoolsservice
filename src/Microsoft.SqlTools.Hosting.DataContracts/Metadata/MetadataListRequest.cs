using Microsoft.SqlTools.Hosting.DataContracts.Metadata.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Metadata
{
    public class MetadataListRequest
    {
        public static readonly
            RequestType<MetadataQueryParams, MetadataQueryResult> Type =
                RequestType<MetadataQueryParams, MetadataQueryResult>.Create("metadata/list");
    }
}