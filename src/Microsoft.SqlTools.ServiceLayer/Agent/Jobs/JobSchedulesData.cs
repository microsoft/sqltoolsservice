//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class JobSchedulesData
    {
        #region data members
        
        // Typed List of job schedules (unshared)
        private List<JobScheduleData> jobSchedules = new List<JobScheduleData>();
        private List<JobScheduleData> deletedJobSchedues = new List<JobScheduleData>();

        private JobData parent;
        private CDataContainer context;
        private bool isReadOnly;
        private bool allowEnableDisable;

        #endregion

        #region construction
        public JobSchedulesData(CDataContainer context, JobData parent)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            this.context = context;
            this.parent = parent;

            this.isReadOnly = parent.IsReadOnly;
            this.allowEnableDisable = parent.AllowEnableDisable;

            // if we're creating a new job
            if (this.parent.Mode != JobData.ActionMode.Edit)
            {
                this.SetDefaults();
            }
            else
            {
                // load the schedule data from server to local copy
                this.LoadData();
            }
        }
        #endregion

        #region public methods/properties

        /// <summary>
        /// List of JobScheduleData - in memory copy
        /// </summary>
        public List<JobScheduleData> Schedules
        {
            get
            {
                return this.jobSchedules;
            }
        }

        /// <summary>
        /// List of removed schedules - in memory copy
        /// </summary>
        public List<JobScheduleData> RemovedSchedules
        {
            get
            {
                return this.deletedJobSchedues;
            }
        }

        /// <summary>
        /// Add a schedule to JobScheduleData list - this does not apply changes on server
        /// </summary>
        /// <param name="schedule"></param>
        public void AddSchedule(JobScheduleData schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException("schedule");
            }

            this.jobSchedules.Add(schedule);
            
            // check to see if it has been previously removed, if so delete it from the
            // removed schedules list
            for (int i = deletedJobSchedues.Count - 1; i >= 0; i--)
            {
                JobScheduleData removedSchedule = this.deletedJobSchedues[i] as JobScheduleData;
                if (removedSchedule.ID == schedule.ID)
                {
                    this.deletedJobSchedues.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// delete a schedule from JobScheduleData list - this does not apply changes on server
        /// </summary>
        /// <param name="schedule"></param>
        public void DeleteSchedule(JobScheduleData schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException("schedule");
            }
            if (this.jobSchedules.Contains(schedule))
            {
                this.jobSchedules.Remove(schedule);
                // if it exists on the server then mark it for deletion later. Otherwise just discard the schedule.
                if (schedule.Created)
                {
                    this.deletedJobSchedues.Add(schedule);
                }
            }
        }

        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }

        public bool AllowEnableDisable
        {
            get { return this.allowEnableDisable; }
        }

        #endregion

        #region data loading
        private void LoadData()
        {
            Job job = this.Job;
            // load the data
            if (job != null)
            {
                JobScheduleCollection schedules = job.JobSchedules;
                
                for (int i = 0; i < schedules.Count; i++)
                {
                    this.jobSchedules.Add(new JobScheduleData(schedules[i], this.IsReadOnly, this.AllowEnableDisable));
                }
            }
            else
            {
                SetDefaults();
            }
        }
        private void SetDefaults()
        {
            this.jobSchedules.Clear();
        }

        #endregion

        #region events

        #endregion

        #region private helpers
        private Job Job
        {
            get
            {
                // try and do the lookup at the parent level
                Job job = null;
                if (this.parent != null)
                {
                    job = parent.Job;
                }
                else if (this.context != null)
                {
                    STParameters parameters = new STParameters(this.context.Document);
                    string urn = String.Empty;
                    parameters.GetParam("urn", ref urn);

                    job = this.context.Server.GetSmoObject(new Urn(urn)) as Job;
                }
                return job;
            }
        }
        #endregion

        #region saving
        /// <summary>
        /// Save changes to the job schedules - this applies changes to server
        /// </summary>
        /// <param name="job">owner job</param>
        /// <returns>true if any changes were commited</returns>
        public bool ApplyChanges(Job job)
        {
            bool changesMade = false;
            if (!this.IsReadOnly)
            {
                // delete any deleted schedules; 
                foreach (JobScheduleData schedule in this.deletedJobSchedues)
                {
                    if (!this.IsSharedSchedule(job.Parent as JobServer, schedule))
                    {
                        // non-shared
                        if (schedule.Created)
                        {
                            schedule.SetJob(job);
                            schedule.Delete();
                            changesMade = true;
                        }
                    }
                    else if (null != job.JobSchedules.ItemById(schedule.ID))
                    {
                        // shared
                        // Remove the selected schedule from the job. If no other jobs use the schedule, the schedule is deleted from the database.
                        // This is now the default behavior of RemoveSharedSchedule thus we do not need an extra parameter (false) with it.
                        job.RemoveSharedSchedule(schedule.ID);
                        changesMade = true;
                    }
                }

                // clear the deleted Job ScheduleList
                this.deletedJobSchedues.Clear();

                // update the remaining schedules
                foreach (JobScheduleData schedule in this.jobSchedules)
                {
                    if (!this.IsSharedSchedule(job.Parent as JobServer, schedule))
                    {
                        // non-shared
                        schedule.SetJob(job);
                        if (schedule.ApplyChanges())
                        {
                            changesMade = true;
                        }
                    }
                    else
                    {
                        // create and attach if the the schedule is not shared
                        if (!schedule.Created)
                        {
                            schedule.SetJob(job);
                            changesMade = true;
                        }
                        else
                        {
                            job.AddSharedSchedule(schedule.ID);
                        }
                        if (schedule.ApplyChanges())
                        {
                            changesMade = true;
                        }
                    }
                }
            }
            else if (this.AllowEnableDisable)
            {
                // if the schedules are readonly give them an opportunity to update themselves
                foreach (JobScheduleData schedule in this.jobSchedules)
                {
                    if (schedule.ApplyChanges())
                    {
                        changesMade = true;
                    }
                }
            }
            return changesMade;
        }
        #endregion

        /// <summary>
        /// check if given schedule data is shared schedule or not
        /// </summary>
        /// <param name="js"></param>
        /// <param name="jobScheduleData"></param>
        /// <returns></returns>
        private bool IsSharedSchedule(JobServer js, JobScheduleData jobScheduleData)
        {
            if (js == null)
            {
                throw new ArgumentNullException("js");
            }

            SqlServer.Management.Smo.Server srv = js.Parent as SqlServer.Management.Smo.Server;

            if ((srv == null) || (srv.Information.Version.Major < 9))
            {
                // Shared Schedules not supported prior Yukon
                return false;
            }
            else
            {
                // with Yukon all schedules are now shared
                return true;
            }
        }
    }
}







