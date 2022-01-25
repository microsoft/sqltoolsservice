//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class StartPerfDataCollectionParams
    {
        public string OwnerUri { get; set; }

        /// <summary>
        /// Path to SqlAssessment executable, installed by the SQL migration extension
        /// </summary>
        public string SqlAssessmentPath { get; set; }

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

    }

    public class StartPerfDataCollectionResult
    {
        public DateTime DateTimeStarted { get; set; }
    }

    public class StopPerfDataCollectionResult
    {
        public DateTime DateTimeStopped { get; set; }
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
}