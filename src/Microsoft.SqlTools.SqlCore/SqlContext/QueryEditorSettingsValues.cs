//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    public class QueryEditorSettingsValues
    {
        /// <summary>
        /// Gets or sets the results setting 
        /// </summary>
        [JsonProperty("results")]
        public QueryEditorResultSettingsValues? Results { get; set; }

        /// <summary>
        /// Update the current settings with the new settings
        /// </summary>
        /// <param name="newSettings">The new settings</param>
        public void Update(QueryEditorSettingsValues newSettings)
        {
            if (newSettings != null)
            {
                Results = newSettings.Results ?? Results;
            }
        }
    }
}
