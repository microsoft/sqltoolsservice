//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Class for serialization and deserialization of the settings the SQL Tools Service needs.
    /// </summary>
    public class SqlToolsSettings
    {
        private SqlToolsSettingsValues sqlTools = null; 

        /// <summary>
        /// Gets or sets the underlying settings value object
        /// </summary>
        public SqlToolsSettingsValues SqlTools 
        { 
            get
            {
                if (this.sqlTools == null)
                {
                    this.sqlTools = new SqlToolsSettingsValues();
                }
                return this.sqlTools;
            } 
            set
            {
                this.sqlTools = value;
            }
        }

        /// <summary>
        /// Query excution settings forwarding property
        /// </summary>
        public QueryExecutionSettings QueryExecutionSettings 
        { 
            get { return this.SqlTools.QueryExecutionSettings; } 
        }

        /// <summary>
        /// Updates the extension settings
        /// </summary>
        /// <param name="settings"></param>
        public void Update(SqlToolsSettings settings)
        {
            if (settings != null)
            {
                this.SqlTools.EnableIntellisense = settings.SqlTools.EnableIntellisense;
                this.SqlTools.IntelliSense.Update(settings.SqlTools.IntelliSense);
            }
        }

        /// <summary>
        /// Gets a flag determining if diagnostics are enabled
        /// </summary>
        public bool IsDiagnositicsEnabled
        {
            get
            {
                return this.SqlTools.EnableIntellisense
                    && this.SqlTools.IntelliSense.EnableDiagnostics.Value;
            }
        }

        /// <summary>
        /// Gets a flag determining if suggestons are enabled
        /// </summary>
        public bool IsSuggestionsEnabled
        {
            get
            {
                return this.SqlTools.EnableIntellisense
                    && this.SqlTools.IntelliSense.EnableSuggestions.Value;
            }
        }
    }

    /// <summary>
    /// Class that is used to serialize and deserialize SQL Tools settings
    /// </summary>
    public class SqlToolsSettingsValues
    {
        /// <summary>
        /// Initializes the Sql Tools settings values
        /// </summary>
        public SqlToolsSettingsValues()
        {
            this.EnableIntellisense = true;
            this.IntelliSense = new IntelliSenseSettings();
            this.QueryExecutionSettings = new QueryExecutionSettings();
        }

        /// <summary>
        /// Gets or sets a flag determining if IntelliSense is enabled
        /// </summary>
        /// <returns></returns>
        public bool EnableIntellisense { get; set; }

        /// <summary>
        /// Gets or sets the detailed IntelliSense settings
        /// </summary>
        public IntelliSenseSettings IntelliSense { get; set; }

        /// <summary>
        /// Gets or sets the query execution settings
        /// </summary>
        public QueryExecutionSettings QueryExecutionSettings { get; set; }
    }
}
