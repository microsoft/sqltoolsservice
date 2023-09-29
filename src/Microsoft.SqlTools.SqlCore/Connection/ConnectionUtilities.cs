//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.Connection
{
    public class ConnectionUtilities
    {
        /// <summary>
        /// Default Application name as received in service startup
        /// </summary>
        public static string ApplicationName { get; set; }

        /// <summary>
        /// Enables connection pooling for all SQL connections, removing feature name identifier from application name to prevent unwanted connection pools.
        /// </summary>
        public static bool EnableConnectionPooling { get; set; }

        public static string GetApplicationNameWithFeature(string applicationName, string featureName)
        {
            string appNameWithFeature = applicationName;
            // Connection Service will not set custom application name if connection pooling is enabled on service.
            if (!EnableConnectionPooling && !string.IsNullOrWhiteSpace(applicationName) && !string.IsNullOrWhiteSpace(featureName) && !applicationName.EndsWith(featureName))
            {
                int appNameStartIndex = applicationName.IndexOf(ApplicationName);
                string originalAppName = appNameStartIndex != -1
                    ? applicationName.Substring(0, appNameStartIndex + ApplicationName.Length)
                    : applicationName; // Reset to default if azdata not found.
                appNameWithFeature = $"{originalAppName}-{featureName}";
            }

            return appNameWithFeature;
        }
    }
}
