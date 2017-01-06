// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Collection of settings related to the execution of queries
    /// </summary>
    public class QueryExecutionSettings
    {
        #region Constants

        /// <summary>
        /// Default value for batch separator (de facto standard as per SSMS)
        /// </summary>
        private const string DefaultBatchSeparator = "GO";

        /// <summary>
        /// Default number of chars to store for long text fields (de facto standard as per SSMS)
        /// </summary>
        private const int DefaultMaxCharsToStore = 65535; // 64 KB - QE default

        /// <summary>
        /// Default number of chars to store of XML values (de facto standard as per SSMS)
        /// xml is a special case so number of chars to store is usually greater than for other long types 
        /// </summary>
        private const int DefaultMaxXmlCharsToStore = 2097152; // 2 MB - QE default

        /// <summary>
        /// Default selection of returning an actual XML showplan with all batches
        /// </summary>
        private ExecutionPlanOptions DefaultExecutionPlanOptions = new ExecutionPlanOptions() { 
            IncludeActualExecutionPlanXml = false,
            IncludeEstimatedExecutionPlanXml = false,
            IncludeActualExecutionPlanText = false,
            IncludeEstimatedExecutionPlanText = false
        }; // do not return any execution plan by default

        #endregion

        #region Member Variables

        private string batchSeparator;

        private int? maxCharsToStore;

        private int? maxXmlCharsToStore;

        private ExecutionPlanOptions? executionPlanOptions;

        #endregion

        #region Properties

        /// <summary>
        /// The configured batch separator, will use a default if a value was not configured
        /// </summary>
        public string BatchSeparator
        {
            get { return batchSeparator ?? DefaultBatchSeparator; }
            set { batchSeparator = value; }
        }

        public int MaxCharsToStore
        {
            get { return maxCharsToStore ?? DefaultMaxCharsToStore; }
            set { maxCharsToStore = value; }
        }

        public int MaxXmlCharsToStore
        {
            get { return maxXmlCharsToStore ?? DefaultMaxXmlCharsToStore; }
            set { maxXmlCharsToStore = value; }
        }

        public ExecutionPlanOptions ExecutionPlanOptions
        {
            get { return executionPlanOptions ?? DefaultExecutionPlanOptions; }
            set { executionPlanOptions = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the current settings with the new settings
        /// </summary>
        /// <param name="newSettings">The new settings</param>
        public void Update(QueryExecutionSettings newSettings)
        {
            BatchSeparator = newSettings.BatchSeparator;
            MaxCharsToStore = newSettings.MaxCharsToStore;
            MaxXmlCharsToStore = newSettings.MaxXmlCharsToStore;
            ExecutionPlanOptions = newSettings.ExecutionPlanOptions;
        }

        #endregion
    }
}
