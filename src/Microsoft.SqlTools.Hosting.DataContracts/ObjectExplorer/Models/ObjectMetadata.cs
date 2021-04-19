using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models
{
    public class ObjectMetadata
    {
        public string PrettyName { get; set; }
        
        public string Name { get; set; }
        public long SizeInMb { get; set; }
        public string MetadataTypeName { get; set; }
    }
}