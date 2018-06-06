//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.SqlManagerUI;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobSchedules.
    /// </summary>
    internal class JobSchedules : ManagementActionBase
    {
        private bool sharedSchedulesSupported = false;
        private JobData data;

        public JobSchedules(CDataContainer dataContainer, JobData data)
        {
            this.DataContainer = dataContainer;
            this.data = data;

            this.sharedSchedulesSupported = this.DataContainer.Server.Information.Version.Major >= 9;
        }

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

        #region ui stuff
        private void InitializeData()
        {
            if (this.data == null)
            {
                return;
            }
            // load the grid
            //PopulateGrid(this.data.JobSchedules);
        }
        // private void PopulateGrid(JobSchedulesData schedules)
        // {
        //     // add non-shared schedules
        //     for (int i = 0; i < schedules.Schedules.Count; i++)
        //     {
        //         JobScheduleData schedule = schedules.Schedules[i] as JobScheduleData;
        //         if (schedule != null)
        //         {
        //             // add rows to the grid
        //             GridCellCollection row = new GridCellCollection();
        //             GridCell cell;

        //             // ID
        //             cell = new GridCell(ConvertIdToDisplayName(schedule.ID));
        //             row.Add(cell);
        //             // Name
        //             cell = new GridCell(schedule.Name);
        //             row.Add(cell);
        //             // Enabled
        //             cell = new GridCell(schedule.Enabled ? JobSR.Yes : JobSR.No);
        //             row.Add(cell);
        //             // Description
        //             cell = new GridCell(schedule.Description);
        //             row.Add(cell);

        //             // Hyperlink 'Jobs in Schedule'
        //             if (this.sharedSchedulesSupported)
        //             {
        //                 // don't add a hyperlink if the schedule has not yet been created
        //                 cell = new GridCell(schedule.Created ? JobSR.ViewJobsInScheduleHyperlink : String.Empty);
        //                 row.Add(cell);
        //             }

        //             this.scheduleList.AddRow(row);
        //         }
        //     }
        // }
        /// <summary>
        /// Convert an id into a user friendly name. Converts new id to "new"
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string ConvertIdToDisplayName(int id)
        {
            string rv;
            // convert -1 into New
            if (id < 0)
            {
                rv = "JobSR.New";
            }
            else
            {
                rv = Convert.ToString(id, System.Globalization.CultureInfo.CurrentCulture);
            }
            return rv;
        }
       
        #endregion

        #region ui event handlers
      

        // private void addSchedule_Click(object sender, System.EventArgs e)
        // {
        //     System.Diagnostics.Debug.Assert(this.DataContainer.Server.Information.Version.Major >= 9, "Shared Schedules supported only for Yukon - this button should be disabled if target server is Shiloh/Sphinx");

        //     StringBuilder excludedSchedules = this.BuildListOfScheduleId(this.data.JobSchedules.Schedules);
        //     StringBuilder removedSchedules = this.BuildListOfScheduleId(this.data.JobSchedules.RemovedSchedules);

        //     STParameters param = new STParameters();

        //     param.SetDocument(this.DataContainer.Document);

        //     param.SetParam("excludedschedules", excludedSchedules.ToString());
        //     if (this.data.Mode == JobData.DialogMode.Properties)
        //     {
        //         param.SetParam("removedschedules", removedSchedules.ToString());
        //         param.SetParam("joburn", this.data.Urn);
        //     }

        //     ManageSchedulesForm formPickUpSharedSchedule = new ManageSchedulesForm
        //                                                    (
        //                                                    this.DataContainer,
        //                                                    ((this.data != null) && (this.data.Name != null)) ? this.data.Name : String.Empty,
        //                                                    this.ServiceProvider
        //                                                    );
        //     using (formPickUpSharedSchedule)
        //     {
        //         DialogResult dr = formPickUpSharedSchedule.ShowDialog();

        //         // cleanup the datacontainer
        //         param.SetParam("excludedschedules", String.Empty);
        //         if (this.data.Mode == JobData.DialogMode.Properties)
        //         {
        //             param.SetParam("removedschedules", String.Empty);
        //             param.SetParam("joburn", String.Empty);
        //         }

        //         if (dr == DialogResult.OK)
        //         {
        //             JobSchedule schedule = formPickUpSharedSchedule.SelectedSchedule;
        //             System.Diagnostics.Debug.Assert(schedule != null);
        //             System.Diagnostics.Debug.Assert(schedule.Name != null);

        //             bool scheduleAlreadyAdded = false;
        //             foreach (object o in this.data.JobSchedules.Schedules)
        //             {
        //                 JobScheduleData jsd = o as JobScheduleData;

        //                 System.Diagnostics.Debug.Assert(jsd != null, "non JobScheduleData found in this.data.Schedules");
        //                 if ((jsd != null) && (jsd.ID == schedule.ID))
        //                 {
        //                     scheduleAlreadyAdded = true;
        //                     break;
        //                 }
        //             }

        //             if (scheduleAlreadyAdded == false)
        //             {
        //                 JobScheduleData scheduleData = new JobScheduleData(schedule);
        //                 this.data.JobSchedules.AddSchedule(scheduleData);

        //                 this.scheduleList.DeleteAllRows();
        //                 PopulateGrid(this.data.JobSchedules);
        //                 UpdateControlStatus();
        //             }
        //         }
        //     }
        // }

        private void editSchedule_Click(object sender, System.EventArgs e)
        {
            // EditSelectedSchedule();
        }

        // private void deleteSchedule_Click(object sender, System.EventArgs e)
        // {
        //     // check that a row is selected first
        //     STrace.Assert(this.scheduleList.SelectedCells.Count > 0, "there are no selected rows");
        //     if (this.scheduleList.SelectedCells.Count == 0)
        //     {
        //         return;
        //     }
        //     int row = (int)this.scheduleList.SelectedCells[0].Y;

        //     // check that this is a valid row
        //     STrace.Assert(row <= this.data.JobSchedules.Schedules.Count, "selected row does not exist in data structures");
        //     if (row > this.data.JobSchedules.Schedules.Count)
        //     {
        //         return;
        //     }

        //     JobScheduleData data = this.data.JobSchedules.Schedules[row] as JobScheduleData;
        //     if (data != null)
        //     {
        //         this.data.JobSchedules.DeleteSchedule(data);        // if is non-shared it will be marked for deletion here

        //         this.scheduleList.DeleteAllRows();
        //         PopulateGrid(this.data.JobSchedules);
        //     }
        //     UpdateControlStatus();
        // }

        // private void EditSelectedSchedule()
        // {
        //     // check that a row is selected first
        //     STrace.Assert(this.scheduleList.SelectedCells.Count > 0, "there are no selected rows");
        //     if (this.scheduleList.SelectedCells.Count == 0)
        //     {
        //         return;
        //     }
        //     int row = (int)this.scheduleList.SelectedCells[0].Y;

        //     // check that this is a valid row
        //     STrace.Assert(row <= this.data.JobSchedules.Schedules.Count, "selected row does not exist in data structures");
        //     if (row > this.data.JobSchedules.Schedules.Count)
        //     {
        //         return;
        //     }

        //     JobScheduleData data = this.data.JobSchedules.Schedules[row] as JobScheduleData;
        //     if (data != null)
        //     {
        //         try
        //         {
        //             using (ScheduleDialog jobSchedule = new ScheduleDialog(data,
        //                                                                    this.data.JobSchedules.Schedules,
        //                                                                    this.DataContainer,
        //                                                                    this.ServiceProvider))
        //             {
        //                 jobSchedule.Text = JobSR.EditSchedule(data.Name);
        //                 jobSchedule.SetSite(this.ServiceProvider);

        //                 try
        //                 {
        //                     if (DialogResult.OK == jobSchedule.ShowDialog())
        //                     {
        //                         this.scheduleList.DeleteAllRows();
        //                         PopulateGrid(this.data.JobSchedules);
        //                         UpdateControlStatus();
        //                     }
        //                 }
        //                 catch (ApplicationException error)
        //                 {
        //                     DisplayExceptionMessage(error);
        //                     jobSchedule.DialogResult = DialogResult.None;
        //                 }

        //             }
        //         }
        //         catch (ApplicationException error)
        //         {
        //             DisplayExceptionMessage(error);
        //         }
        //     }
        // }

        // }
        #endregion

        /// <summary>
        /// enumerates schedules list and returns back as s string in format 1,2,3
        /// </summary>
        /// <param name="schedules"></param>
        /// <returns></returns>
        private StringBuilder BuildListOfScheduleId(List<JobScheduleData> schedules)
        {
            if (schedules == null)
            {
                throw new ArgumentNullException("schedules");
            }
            StringBuilder scheduleIdList = new StringBuilder();
            foreach (JobScheduleData schedule in schedules)
            {
                if (schedule != null)
                {
                    if (scheduleIdList.Length > 0)
                    {
                        scheduleIdList.AppendFormat(CultureInfo.InvariantCulture, ", {0}", schedule.ID);
                    }
                    else
                    {
                        scheduleIdList.Append(schedule.ID);
                    }
                }
            }
            return scheduleIdList;
        }
    }
}
