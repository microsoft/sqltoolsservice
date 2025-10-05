//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Settings for files (typically from VS Code files.* settings)
    /// </summary>
    public class FilesSettings
    {
        /// <summary>
        /// End of line character to use (auto, \n, \r\n)
        /// </summary>
        [JsonProperty("eol")]
        public string Eol { get; set; }
    }
}
