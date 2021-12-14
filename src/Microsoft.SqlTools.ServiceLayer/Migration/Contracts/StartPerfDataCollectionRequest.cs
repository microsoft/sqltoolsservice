
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class StartPerfDataCollectionParams
    {
        public string OwnerUri { get; set; }

        /// <summary>
        /// Folder from which collected performance data will be written to
        /// </summary>
        public string DataFolder { get; set; }

        /// <summary>
        /// Interval at which performance data will be collected, in seconds
        /// </summary>
        public int PerfQueryIntervalInSec { get; set; }

        /// <summary>
        /// Interval at which static (common) data will be collected, in seconds
        /// </summary>
        public int StaticQueryIntervalInSec { get; set; }

        /// <summary>
        /// Number of iterations of performance data collection to run before aggregating and saving to disk
        /// </summary>
        public int NumberOfIterations { get; set; }
    }

    // TO-DO: what should this return? right now ADS will simply just launch SqlAssessment.exe as a child process so there isn't really much to track, except maybe the process ID for now 
    // public class StartPerfDataCollectionResult
    // {
    //
    // }

    public class StartPerfDataCollectionRequest
    {
        public static readonly
            RequestType<StartPerfDataCollectionParams, int> Type =
                RequestType<StartPerfDataCollectionParams, int>.Create("migration/startperfdatacollection");
    }
}
