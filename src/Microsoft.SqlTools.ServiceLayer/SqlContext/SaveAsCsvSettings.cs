//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Settings for saving results as CSV
    /// </summary>
    public class SaveAsCsvSettings
    {
        /// <summary>
        /// Include headers of columns in CSV
        /// </summary>
        [JsonProperty("includeHeaders")]
        public bool? IncludeHeaders { get; set; }

        /// <summary>
        /// Delimiter for separating data items in CSV
        /// </summary>
        [JsonProperty("delimiter")]
        public string Delimiter { get; set; }

        /// <summary>
        /// Line separator for rows in CSV (CR, CRLF, or LF)
        /// </summary>
        [JsonProperty("lineSeparator")]
        public string LineSeparator { get; set; }

        /// <summary>
        /// Text identifier for alphanumeric columns in CSV
        /// </summary>
        [JsonProperty("textIdentifier")]
        public string TextIdentifier { get; set; }

        /// <summary>
        /// Encoding of the CSV file
        /// </summary>
        [JsonProperty("encoding")]
        public string Encoding { get; set; }
    }
}
