//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.Kusto.ServiceLayer.Management;
using Microsoft.Kusto.ServiceLayer.Admin;

namespace Microsoft.Kusto.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public sealed class ScheduleDialog : ManagementActionBase
    {
        private JobScheduleData scheduleData = null;

        private CDataContainer dataContainerContext = null; // must be non-null to display JobsInSchedule

#region Constructors / Dispose

        

        /// <summary>
        /// Constructs a new ScheduleDialog based upon a JobScheduleData object
        /// And provides context for that dialog so it can enable 'JobsInSchedule' button
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        public ScheduleDialog(JobScheduleData source, CDataContainer context)
        {
            this.dataContainerContext = context;
            this.scheduleData = source;
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

#endregion

#region Public Properties
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

        /// <summary>
        /// text description of the supplied schedule   
        /// </summary>
        public string Description
        {
            get
            {
                return this.ToString();
            }
        }
#endregion

        private JobScheduleData ExtractScheduleDataFromXml(XmlDocument xmlDoc)
        {
            JobScheduleData jobscheduledata = new JobScheduleData();

            string stringNewScheduleMode = null;
            string serverName = String.Empty;
            string scheduleUrn = String.Empty;

            STParameters param = new STParameters();
            bool bStatus = true;

            param.SetDocument(xmlDoc);

            bStatus = param.GetParam("servername", ref serverName);
            bStatus = param.GetParam("urn", ref scheduleUrn);
            bStatus = param.GetParam("itemtype", ref stringNewScheduleMode);
            if ((stringNewScheduleMode != null) && (stringNewScheduleMode.Length > 0))
            {
                return jobscheduledata; // new schedule
            }

            Microsoft.SqlServer.Management.Common.ServerConnection connInfo =
                new Microsoft.SqlServer.Management.Common.ServerConnection(serverName);

            Enumerator en = new Enumerator();
            Request req = new Request();

            req.Urn = scheduleUrn;

            DataTable dt = en.Process(connInfo, req);

            if (dt.Rows.Count == 0)
            {
                return jobscheduledata;
            }

            DataRow dr = dt.Rows[0];

            jobscheduledata.Enabled = Convert.ToBoolean(dr["IsEnabled"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.Name = Convert.ToString(dr["Name"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.FrequencyTypes = (FrequencyTypes)Convert.ToInt32(dr["FrequencyTypes"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.FrequencyInterval = Convert.ToInt32(dr["FrequencyInterval"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.FrequencySubDayTypes = (FrequencySubDayTypes)Convert.ToInt32(dr["FrequencySubDayTypes"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.FrequencyRelativeIntervals = (FrequencyRelativeIntervals)Convert.ToInt32(dr["FrequencyRelativeIntervals"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.FrequencyRecurranceFactor = Convert.ToInt32(dr["FrequencyRecurrenceFactor"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.ActiveStartDate = Convert.ToDateTime(dr["ActiveStartDate"], System.Globalization.CultureInfo.InvariantCulture);
            jobscheduledata.ActiveEndDate = Convert.ToDateTime(dr["ActiveEndDate"], System.Globalization.CultureInfo.InvariantCulture);
            return jobscheduledata;
        }


#region Events
        // private void OK_Click(System.Object sender, System.EventArgs e)
        // {
        //     try
        //     {
        //         if (this.textboxScheduleName.Text.Length > 128)
        //         {
        //             throw new ApplicationException(SR.ScheduleNameTooLong);
        //         }
                
        //         scheduleData.Name = this.textboxScheduleName.Text;
        //         this.scheduleData.Enabled = this.checkboxEnabled.Checked;

        //         if (this.comboScheduleType.SelectedIndex == 0)
        //         {
        //             scheduleData.FrequencyTypes = FrequencyTypes.AutoStart;
        //         }
        //         else if (this.comboScheduleType.SelectedIndex == 1)
        //         {
        //             scheduleData.FrequencyTypes = FrequencyTypes.OnIdle;
        //         }
        //         else
        //         {
        //             this.recurrancePattern.SaveData(this.scheduleData);
        //         }

        //         // if we're updating the server
        //         if (this.applyOnClose)
        //         {
        //             // all validations will be performed by server
        //             this.scheduleData.ApplyChanges();
        //         }
        //         else
        //         {
        //             // we will perform ourselfs some mininal validations - since otherwise user will obtain some
        //             // schedule definition that only later (after n-steps) will be validated and he will may not
        //             // be able to fix/recover from that point - see also 149485 opened by replication
        //             // If the job schedules array is null then we will pass an empty arraylist to the validate method
        //             // In some cases (such as when replication components call the schedule dialog) no datacontainer
        //             // will be available.  In these cases, we will pass nulls for both parameters in the validate
        //             // method which will by-pass the duplicate schedule checks.  The other validations will still 
        //             // be performed, however.
        //             if (this.dataContainerContext == null)
        //             {
        //                 scheduleData.Validate();
        //             }
        //             else
        //             {
        //                 scheduleData.Validate(this.dataContainerContext.Server.Information.Version, this.jobSchedules);
        //             }
        //         }
        //         this.DialogResult = DialogResult.OK;
        //         this.Close();
        //     }
        //     catch (ApplicationException error)
        //     {
        //         DisplayExceptionMessage(error);
        //         this.DialogResult = DialogResult.None;
        //     }
        // }

#endregion


    }
}
