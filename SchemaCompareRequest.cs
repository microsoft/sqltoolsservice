// Updated SchemaCompareRequest.cs

using CoreContracts;

namespace ServiceLayer
{
    public class SchemaCompareEndpointInfo : CoreContracts.SchemaCompareEndpointInfo
    {
        // Additional properties for ConnectionDetails
        public object ConnectionDetails { get; set; } // Replace with actual type
    }

    public class SchemaCompareParams : CoreContracts.SchemaCompareParams
    {
        // Additional properties for TaskExecutionMode
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    // Removed SchemaCompareSaveScmpParams since it correctly inherits now
}

// Fixed SchemaCompareSaveScmpRequest.cs
namespace ServiceLayer
{
    public class SchemaCompareSaveScmpRequest
    {
        public SchemaCompareSaveScmpParams SchemaCompareSaveScmpParams { get; set; }
        // Ensures SchemaCompareSaveScmpParams extends CoreContracts.SchemaCompareSaveScmpParams
    }
}

// Note: Make sure to remove the local duplicate SchemaCompareSaveScmpParams file from the repository if it still exists.