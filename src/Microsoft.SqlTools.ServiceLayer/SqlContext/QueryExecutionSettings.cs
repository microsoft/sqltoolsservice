//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Collection of settings related to the execution of queries
    /// </summary>
    public class QueryExecutionSettings
    {
        /// <summary>
        /// Default value for batch separator (de facto standard as per SSMS)
        /// </summary>
        private const string DefaultBatchSeparator = "GO";

        private string batchSeparator;

        /// <summary>
        /// The configured batch separator, will use a default if a value was not configured
        /// </summary>
        public string BatchSeparator
        {
            get { return batchSeparator ?? DefaultBatchSeparator; }
            set { batchSeparator = value; }
        }

        /// <summary>
        /// Update the current settings with the new settings
        /// </summary>
        /// <param name="newSettings">The new settings</param>
        public void Update(QueryExecutionSettings newSettings)
        {
            BatchSeparator = newSettings.BatchSeparator;
        }
    }
}
