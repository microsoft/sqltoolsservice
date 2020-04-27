//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Class for serialization and deserialization of the settings the SQL Tools Service needs.
    /// </summary>
    public class SqlToolsSettings
    {
        private ISqlToolsSettingsValues sqlTools = null; 
        private SqlToolsSettingsValues mssqlTools = null; 
        private SqlToolsSettingsValues allSqlTools = null; 

        public ISqlToolsSettingsValues SqlTools 
        { 
            get
            {
                if (this.sqlTools == null)
                {
                    this.sqlTools = new CompoundToolsSettingsValues(MssqlTools, AllSqlTools);
                }
                return this.sqlTools;
            } 
            set
            {
                this.sqlTools = value;
            }
        }

        /// <summary>
        /// Gets or sets the underlying settings value object
        /// </summary>
        [JsonProperty("mssql")]
        public SqlToolsSettingsValues MssqlTools 
        { 
            get
            {
                if (this.mssqlTools == null)
                {
                    this.mssqlTools = new SqlToolsSettingsValues(false);
                }
                return this.mssqlTools;
            } 
            set
            {
                this.mssqlTools = value;
            }
        }

        /// <summary>
        /// Gets or sets the underlying settings value object
        /// </summary>
        [JsonProperty("sql")]
        public SqlToolsSettingsValues AllSqlTools 
        { 
            get
            {
                if (this.allSqlTools == null)
                {
                    this.allSqlTools = new SqlToolsSettingsValues(false);
                }
                return this.allSqlTools;
            } 
            set
            {
                this.sqlTools = value;
            }
        }

        /// <summary>
        /// Query execution settings forwarding property
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
                this.SqlTools.IntelliSense.Update(settings.SqlTools.IntelliSense);
                this.SqlTools.QueryExecutionSettings.Update(settings.SqlTools.QueryExecutionSettings);
            }
        }

        /// <summary>
        /// Gets a flag determining if diagnostics are enabled
        /// </summary>
        public bool IsDiagnosticsEnabled
        {
            get
            {
                return this.SqlTools.IntelliSense.EnableIntellisense
                    && this.SqlTools.IntelliSense.EnableErrorChecking.Value;
            }
        }

        /// <summary>
        /// Gets a flag determining if suggestions are enabled
        /// </summary>
        public bool IsSuggestionsEnabled
        {
            get
            {
                return this.SqlTools.IntelliSense.EnableIntellisense
                    && this.SqlTools.IntelliSense.EnableSuggestions.Value;
            }
        }

        /// <summary>
        /// Gets a flag determining if quick info is enabled
        /// </summary>
        public bool IsQuickInfoEnabled
        {
            get
            {
                return this.SqlTools.IntelliSense.EnableIntellisense
                    && this.SqlTools.IntelliSense.EnableQuickInfo.Value;
            }
        }

        /// <summary>
        /// Gets a flag determining if IntelliSense is enabled
        /// </summary>
        public bool IsIntelliSenseEnabled
        {
            get
            {
                return this.SqlTools.IntelliSense.EnableIntellisense;
            }
        }
    }
}
