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
    /// Class that is used to serialize and deserialize SQL Tools settings
    /// </summary>
    public class SqlToolsSettingsValues : ISqlToolsSettingsValues
    {
        /// <summary>
        /// Initializes the Sql Tools settings values
        /// </summary>
        public SqlToolsSettingsValues(bool createDefaults = true)
        {
            if (createDefaults)
            {
                IntelliSense = new IntelliSenseSettings();
                QueryExecutionSettings = new QueryExecutionSettings();
                Format = new FormatterSettings();
            }
        }

        /// <summary>
        /// Gets or sets the detailed IntelliSense settings
        /// </summary>
        public IntelliSenseSettings IntelliSense { get; set; }

        /// <summary>
        /// Gets or sets the query execution settings
        /// </summary>
        [JsonProperty("query")]
        public QueryExecutionSettings QueryExecutionSettings { get; set; }

        /// <summary>
        /// Gets or sets the formatter settings
        /// </summary>
        [JsonProperty("format")]
        public FormatterSettings Format { get; set; }

        /// <summary>
        /// Gets or sets the formatter settings
        /// </summary>
        [JsonProperty("objectExplorer")]
        public ObjectExplorerSettings ObjectExplorer { get; set; }
    }
}
