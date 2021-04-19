using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Hosting.Contracts;

namespace Microsoft.AzureMonitor.ServiceLayer
{
    internal static class FeaturesMetadataProviderHelper
    {
        internal static FeatureMetadataProvider[] CreateFeatureMetadataProviders()
        {
            return new[]
            {
                new FeatureMetadataProvider
                {
                    FeatureName = "serializationService", 
                    Enabled = true, 
                    OptionsMetadata = new ServiceOption[0]
                }
            };
        }
    }
}