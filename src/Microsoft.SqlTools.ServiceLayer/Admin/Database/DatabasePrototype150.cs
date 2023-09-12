//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2019
    /// </summary>
    internal class DatabasePrototype150 : DatabasePrototype140
    {
        /// <summary>
        /// Database properties for SqlServer 2019 class constructor
        /// </summary>
        public DatabasePrototype150(CDataContainer context)
            : base(context)
        {
        }

        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            if (db.IsSupportedObject<QueryStoreOptions>() && this.currentState.queryStoreOptions != null)
            {
                if (this.currentState.queryStoreOptions.QueryCaptureMode == QueryStoreCaptureMode.Custom && this.currentState.queryStoreOptions.DesiredState != QueryStoreOperationMode.Off)
                {
                    if (!this.Exists || (db.QueryStoreOptions.CapturePolicyExecutionCount != this.currentState.queryStoreOptions.CapturePolicyExecutionCount))
                    {
                        db.QueryStoreOptions.CapturePolicyExecutionCount = this.currentState.queryStoreOptions.CapturePolicyExecutionCount;
                    }

                    if (!this.Exists || (db.QueryStoreOptions.CapturePolicyStaleThresholdInHrs != this.currentState.queryStoreOptions.CapturePolicyStaleThresholdInHrs))
                    {
                        db.QueryStoreOptions.CapturePolicyStaleThresholdInHrs = this.currentState.queryStoreOptions.CapturePolicyStaleThresholdInHrs;
                    }

                    if (!this.Exists || (db.QueryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS != this.currentState.queryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS))
                    {
                        db.QueryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS = this.currentState.queryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS;
                    }

                    if (!this.Exists || (db.QueryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS != this.currentState.queryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS))
                    {
                        db.QueryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS = this.currentState.queryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS;
                    }
                }
            }
        }
    }
}
