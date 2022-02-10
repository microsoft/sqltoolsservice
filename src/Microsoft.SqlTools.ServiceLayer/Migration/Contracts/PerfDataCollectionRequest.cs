//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class StartPerfDataCollectionParams
    {
        /// <summary>
        /// Uri identifier for the connection
        /// </summary>
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

    public class StopPerfDataCollectionParams
    {
        // TO-DO: currently stop data collection doesn't require any parameters
    }

    public class RefreshPerfDataCollectionParams
    {
        /// <summary>
        /// The last time data collection status was refreshed
        /// </summary>
        public DateTime LastRefreshedTime { get; set; }
    }

    public class StartPerfDataCollectionResult
    {
        /// <summary>
        /// The time data collection started
        /// </summary>
        public DateTime DateTimeStarted { get; set; }
    }

    public class StopPerfDataCollectionResult
    {
        /// <summary>
        /// The time data collection stopped
        /// </summary>
        public DateTime DateTimeStopped { get; set; }
    }

    public class RefreshPerfDataCollectionResult
    {
        /// <summary>
        /// List of status messages captured during data collection
        /// </summary>
        public List<string> Messages { get; set; }

        /// <summary>
        /// List of error messages captured during data collection
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// The last time data collecton status was refreshed
        /// </summary>
        public DateTime RefreshTime { get; set; }

        /// <summary>
        /// Whether or not data collection is currently running
        /// </summary>
        public bool IsCollecting { get; set; }
    }

    public class StartPerfDataCollectionRequest
    {
        public static readonly
            RequestType<StartPerfDataCollectionParams, StartPerfDataCollectionResult> Type =
                RequestType<StartPerfDataCollectionParams, StartPerfDataCollectionResult>.Create("migration/startperfdatacollection");
    }

    public class StopPerfDataCollectionRequest
    {
        public static readonly
            RequestType<StopPerfDataCollectionParams, StopPerfDataCollectionResult> Type =
                RequestType<StopPerfDataCollectionParams, StopPerfDataCollectionResult>.Create("migration/stopperfdatacollection");
    }

    public class RefreshPerfDataCollectionRequest
    {
        public static readonly
            RequestType<RefreshPerfDataCollectionParams, RefreshPerfDataCollectionResult> Type =
                RequestType<RefreshPerfDataCollectionParams, RefreshPerfDataCollectionResult>.Create("migration/refreshperfdatacollection");
    }
}