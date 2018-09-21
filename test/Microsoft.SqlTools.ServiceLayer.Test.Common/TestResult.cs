//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestResult
    {
        [JsonProperty("elapsedTime")]
        public double ElapsedTime { get; set; }

        [JsonProperty("metricValue")]
        public double MetricValue { get; set; }

        [JsonProperty("iterations")]
        public double[] Iterations { get; set; }

        [JsonProperty("ninetiethPercentile")]
        public double NinetiethPercentile { get; set; }

        [JsonProperty("fiftiethPercentile")]
        public double FiftiethPercentile { get; set; }

        [JsonProperty("average")]
        public double Average { get; set; }

        [JsonProperty("primaryMetric")]
        public string PrimaryMetric { get; set; }
    }
}
