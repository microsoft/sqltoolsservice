//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class Common
    {
        /// <summary>
        /// The name of the test db to be used for performance tests. Prefix "keep" is used so the db doesn't get deleted by cleanup jobs
        /// </summary>
        public const string PerfTestDatabaseName = "keep_SQLToolsCrossPlatPerfTestDb";
    }
}
