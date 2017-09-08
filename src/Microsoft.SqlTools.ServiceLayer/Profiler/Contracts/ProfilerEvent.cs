//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Class that contains data for a single profile event
    /// </summary>
    public class ProfilerEvent
    {
        public ProfilerEvent()
        {
            this.Values = new Dictionary<string, string>();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Timestamp { get; set; }

        public Dictionary<string, string> Values { get; set; }
    }
}
