//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Collections;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    sealed class ScheduleScriptExecution : ManagementActionBase
    {
        private SqlConnectionInfo ci;
        private JobScheduleData scheduleData = null;

        #region object construction
        /// <summary>
        ///  constructs an empty schedule dialog.
        /// </summary>
        public ScheduleScriptExecution()
        {
            this.scheduleData = new JobScheduleData();
        }

        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo schedule. Will
        /// automatically save changes on ok.
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleScriptExecution(JobSchedule source)
        {
            this.scheduleData = new JobScheduleData(source);
        }
        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo Job. Will
        /// automatically save changes on ok.
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleScriptExecution(Job source)
        {
            this.scheduleData = new JobScheduleData(source);
        }
        /// <summary>
        /// Constructs a new ScheduleDialog based upon a JobScheduleData object.
        /// </summary>
        /// <param name="source"></param>
        public ScheduleScriptExecution(JobScheduleData source, SqlConnectionInfo ci)
        {
            this.scheduleData = source;
            this.ci = ci;
        }
        /// <summary>
        /// Constructs a new Schedule dialog based upon a SimpleJobSchedule structure
        /// </summary>
        /// <param name="source"></param>
        public ScheduleScriptExecution(SimpleJobSchedule source)
        {
            this.scheduleData = source.ToJobScheduleData();
        }
        #endregion

        #region public properties
        /// <summary>
        /// Underlying JobScheduleData object
        /// </summary>
        public JobScheduleData Schedule
        {
            get
            {
                return this.scheduleData;
            }
        }

        /// <summary>
        /// SimpleJobSchedule structure
        /// </summary>
        public SimpleJobSchedule SimpleSchedule
        {
            get
            {
                SimpleJobSchedule s = SimpleJobSchedule.FromJobScheduleData(this.scheduleData);
                s.Description = this.ToString();
                return s;
            }
            set
            {
                this.scheduleData = value.ToJobScheduleData();
            }
        }

        #endregion

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

        #region ui event handlers

        // private void OK_Click(System.Object sender, System.EventArgs e)
        // {
        //     Microsoft.SqlServer.Management.Smo.Server smoServer;

        //     scheduleData.Name = this.scheduleName.Text;

        //     if (this.scheduleType.SelectedIndex == idxAutoStartSchedule)
        //     {
        //         scheduleData.FrequencyTypes = FrequencyTypes.AutoStart;
        //     }
        //     else if (this.scheduleType.SelectedIndex == idxCpuIdleSchedule)
        //     {
        //         scheduleData.FrequencyTypes = FrequencyTypes.OnIdle;
        //     }
        //     else
        //     {
        //         this.recurrancePattern.SaveData(this.scheduleData);
        //     }

        //     ///For methods which pass a connection object, connect to smo and get information about the job server and the
        //     ///job inventory to pass to the validate method
        //     try
        //     {
        //         if (ci != null)
        //         {
        //             smoServer = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(ci));
        //             System.Version version = smoServer.Information.Version;
        //             ///This is the creation of a new job. We won't need to pass schedule information to Validate
        //             ///because this a new job.  
        //             ///But first make sure the job has not already been created
        //             if (smoServer.JobServer.Jobs.Contains(this.jobName.Text.Trim()))
        //             {
        //                 throw new ApplicationException(SRError.JobAlreadyExists(this.jobName.Text));
        //             }
        //             //If we have not failed.  The job doesn't exist.  Now check to make sure the schedule data
        //             //is valid
        //             ArrayList nullArrayList=null;
        //             scheduleData.Validate(version, nullArrayList);
        //         }
        //         this.DialogResult = DialogResult.OK;
        //         this.Close();
        //     }
        //     catch (ApplicationException error)
        //     {
        //         DisplayExceptionMessage(error);
        //         this.DialogResult = DialogResult.None;

        //     }
        //     finally
        //     {
        //         smoServer = null;
        //     }

        // }

        // private void Cancel_Click(System.Object sender, System.EventArgs e)
        // {
        //     this.DialogResult = DialogResult.Cancel;
        //     this.Close();
        // }
 
        #endregion
    }
}
