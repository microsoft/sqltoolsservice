#if false
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesHistory.
    /// </summary>	
    internal class SqlServerAgentPropertiesHistory : ManagementActionBase
    {
      

        #region ctors

        public SqlServerAgentPropertiesHistory(CDataContainer dataContainer)
        {         
            DataContainer = dataContainer;
        }

        #endregion

        #region Implementation

        // private void ApplyChanges()
        // {
        //     this.ExecutionMode = ExecutionMode.Success;

        //     JobServer agent = DataContainer.Server.JobServer;

        //     bool LimitHistory = this.checkLimitHistorySize.Checked;
        //     bool DeleteHistory = this.checkRemoveHistory.Checked;
        //     bool AlterValues = false;
        //     int MaxLogRows = -1;
        //     int MaxRowsJob = -1;

        //     try
        //     {
        //         if (true == LimitHistory)
        //         {
        //             MaxLogRows = (int) this.textMaxHistoryRows.Value;
        //             MaxRowsJob = (int) this.textMaxHistoryRowsPerJob.Value;
        //         }
        //         if (MaxLogRows != agent.MaximumHistoryRows)
        //         {
        //             agent.MaximumHistoryRows = MaxLogRows;
        //             AlterValues = true;
        //         }
        //         if (MaxRowsJob != agent.MaximumJobHistoryRows)
        //         {
        //             agent.MaximumJobHistoryRows = MaxRowsJob;
        //             AlterValues = true;
        //         }
        //         if (true == DeleteHistory)
        //         {
        //             int timeunits = (int) this.numTimeUnits.Value;
        //             JobHistoryFilter jobHistoryFilter = new JobHistoryFilter();
        //             jobHistoryFilter.EndRunDate = CUtils.GetOldestDate(timeunits,
        //                 (TimeUnitType) (this.comboTimeUnits.SelectedIndex));
        //             agent.PurgeJobHistory(jobHistoryFilter);
        //         }

        //         if (true == AlterValues)
        //         {
        //             agent.Alter();
        //         }
        //     }
        //     catch (SmoException smoex)
        //     {
        //         DisplayExceptionMessage(smoex);
        //         this.ExecutionMode = ExecutionMode.Failure;
        //     }
        // }

        #endregion

        #region Dispose

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
#endif