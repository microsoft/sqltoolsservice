//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    public class TelemetrySettingsValues
    {
        /// <summary>
        /// Gets or sets the telemetry level setting 
        /// </summary>
        [JsonProperty("telemetryLevel")]
        public TelemetryLevel Telemetry { get; set; }
    }
}
