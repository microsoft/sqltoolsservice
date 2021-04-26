using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;

namespace Microsoft.SqlTools.Hosting.DataContracts.Metadata.Models
{
    public class MetadataQueryResult
    {
        public ObjectMetadata[] Metadata { get; set; }
    }
}