//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for ManageSchedulesControl.
    /// </summary>
    internal class ManageSchedulesControl : ManagementActionBase
    {
        /// <summary>
        /// any schedules that should not be shown in the list. Used when launched from
        /// the Job dialog as we don't want to list any schedules it already uses.
        /// </summary>
        private List<int> excludedScheduleId = null;

        /// <summary>
        /// any schedules that are attached to the job, but are slated to be removed so should have job in list
        /// count decremented
        /// </summary>
        private List<int> removedScheduleId = null;

        /// <summary>
        /// if null we are in "manage schedules" mode (OE dbCommander)
        /// if not null we are in "pick schedule" mode (modal dialog)
        /// </summary>
        private string m_jobName = null;


        #region Constructors/Dispose


        /// <summary>
        /// initialization - passing context (coming from parent dialog)
        /// 
        /// display in "Pick up a schedule for job" mode
        /// </summary>
        /// <param name="context"></param>
        public ManageSchedulesControl(CDataContainer context, string jobName)
        {        
            m_jobName = jobName;
            DataContainer = context;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {               
            }
            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// Get the list of any schedules that should not be shown in the UI.
        /// </summary>
        private void InitializeExcludedSchedules()
        {        
            if(DataContainer == null)
            {
                throw new InvalidOperationException();
            }

            string excludedIdList = string.Empty;
            string removedIdList = string.Empty;

            STParameters param = new STParameters();

            param.SetDocument(DataContainer.Document);

            param.GetParam("excludedschedules", ref excludedIdList);
            param.GetParam("removedschedules", ref removedIdList);

            excludedScheduleId = ConvertCommaSeparatedIdList(excludedIdList);
            removedScheduleId = ConvertCommaSeparatedIdList(removedIdList);

        }

        private List<int> ConvertCommaSeparatedIdList(string list)
        {
            List<int> idList = new List<int>();

            if(list != null && list.Length > 0)
            {

                string[] splidId = list.Split(',');

                int id;
                for(int i = 0; i < splidId.Length; ++i)
                {
                    if(int.TryParse(splidId[i].Trim(), out id))
                    {
                        idList.Add(id);
                    }
                }
                idList.Sort();
            }
            return idList;
        }        

        /// <summary>
        /// applies changes to server
        /// 
        /// iterates through the grid rows (list of schedules)
        ///     if [ ] enabled changed update the schedule
        /// iterates through the list of schedules marked for deletion
        ///     delete them
        /// </summary>
        private void SendDataToServer()
        {
            // if(m_jobName != null)
            // {
            //     // we are in read-only mode
            //     return;
            // }

            // System.Diagnostics.Debug.Assert(m_grid != null);
            // Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

            // for(int iRow = 0; iRow < grid.RowsNumber; ++iRow)
            // {
            //     GridCell cell = grid.GetCellInfo(iRow, colTagSmoObject);
            //     System.Diagnostics.Debug.Assert(cell != null);
            //     System.Diagnostics.Debug.Assert(cell.Tag != null);

            //     JobSchedule schedule = cell.Tag as JobSchedule;
            //     System.Diagnostics.Debug.Assert(schedule != null);

            //     bool enabled = IsEmbeededCheckboxChecked(grid, iRow, colScheduleEnabled);

            //     if(enabled != schedule.IsEnabled)
            //     {
            //         // use the schedule coming form current server (maybe switched by scriping logic)
            //         schedule = this.DataContainer.Server.JobServer.SharedSchedules.ItemById(schedule.ID);
            //         System.Diagnostics.Debug.Assert(schedule != null);

            //         schedule.IsEnabled = enabled;
            //         schedule.Alter();

            //         ReportUpdate(schedule, (iRow * 50) / grid.RowsNumber);
            //     }
            // }

            // System.Diagnostics.Debug.Assert(m_schedulesMarkedForDelete != null);
            // int i = 0;
            // foreach(object o in m_schedulesMarkedForDelete)
            // {
            //     System.Diagnostics.Debug.Assert(o != null);

            //     JobSchedule schedule = o as JobSchedule;
            //     System.Diagnostics.Debug.Assert(schedule != null);

            //     // use the schedule coming form current server (maybe switched by scriping logic)
            //     schedule = this.DataContainer.Server.JobServer.SharedSchedules.ItemById(schedule.ID);
            //     System.Diagnostics.Debug.Assert(schedule != null);

            //     // if there are jobs referencing this schedule remove them
            //     if(schedule.JobCount != 0)
            //     {
            //         Guid[] jobGuids = schedule.EnumJobReferences();

            //         foreach(Guid guid in jobGuids)
            //         {
            //             Job jobRefrencingSchedule = DataContainer.Server.JobServer.Jobs.ItemById(guid);

            //             jobRefrencingSchedule.RemoveSharedSchedule(schedule.ID);
            //             jobRefrencingSchedule.Alter();
            //         }
            //     }

            //     schedule.Drop();
            //     ReportUpdate(schedule, (i * 50) / m_schedulesMarkedForDelete.Count + 50);

            //     ++i;
            // }
        }



        /// <summary>
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            base.OnRunNow(sender);
            SendDataToServer();
        }

       

        #region Helpers - Get Shared Schedule Description, some extra Initialization
        private string GetScheduleDescription(JobSchedule schedule)
        {
            SimpleJobSchedule sjs = SimpleJobSchedule.FromJobScheduleData
                              (
                              new JobScheduleData(schedule)
                              );
            string description = sjs.ToString();

            return description != null ? description : string.Empty;
        }

        // /// <summary>
        // /// on 'New...' clicked we display Schedule Dialog in 'New Schedule' mode
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // private void OnNewSharedSchedule(object sender, EventArgs e)
        // {
        //     JobScheduleData scheduleData = new JobScheduleData();
        //     using(ScheduleDialog scheduleDialog = new ScheduleDialog(scheduleData,DataContainer,this.ServiceProvider))
        //     {
        //         scheduleDialog.SetSite((m_serviceProviderInPickScheduleMode != null) ? m_serviceProviderInPickScheduleMode : this.ServiceProvider);
        //         if(DialogResult.OK == scheduleDialog.ShowDialog())
        //         {
        //             JobScheduleData jsd = scheduleDialog.Schedule;
        //             jsd.SetJobServer(this.DataContainer.Server.JobServer);
        //             jsd.ApplyChanges();
        //         }

        //         this.OnReset(this);
        //     }
        // }

        #endregion
    }
}
