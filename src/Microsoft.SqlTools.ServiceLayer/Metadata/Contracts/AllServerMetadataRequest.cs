

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class AllServerMetadataParams
    {
        public string OwnerUri { get; set; }
    }

    public class AllServerMetadataResult
    {
        public string ServerMetadataXml { get; set; }
    }

    public class AllMetadataRequest
    {
        public static readonly RequestType<AllServerMetadataParams, AllServerMetadataResult> Type =
            RequestType<AllServerMetadataParams, AllServerMetadataResult>.Create("metadata/getAll");
    }
}
