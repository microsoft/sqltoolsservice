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
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

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
    sealed class ScheduleDialog : ManagementActionBase
    {
#region UI Variables

        // private Microsoft.SqlServer.Management.SqlManagerUI.Schedule.RecurrencePatternControl recurrancePattern;
        // private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.Button buttonOK;
        // private System.Windows.Forms.Button buttonCancel;
        // private System.Windows.Forms.Button buttonHelp;
        // private System.Windows.Forms.Label labelName;
        // private System.Windows.Forms.TextBox textboxScheduleName;
        // private System.Windows.Forms.CheckBox checkboxEnabled;
        // private System.Windows.Forms.Label labelScheduleType;
        // private System.Windows.Forms.ComboBox comboScheduleType;
        // private System.Windows.Forms.Button buttonJobsInSchedule;

        private IServiceProvider serviceProvider;
        // private IHelpProvider helpProvider;
        // private ILaunchFormHost2 launchFormHost2 = null;
        // private string helpKeyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + ".ag.job.scheduleproperties.f1";
        private ArrayList jobSchedules=null;

#endregion

#region Variables - apply on close / schedule data / context to enable "Jobs In Schedule"

        private bool applyOnClose = false;
        private JobScheduleData scheduleData = null;

        private CDataContainer dataContainerContext = null; // must be non-null to display JobsInSchedule

#endregion

#region Constructors / Dispose
        /// <summary>
        ///  constructs an empty schedule dialog.
        /// </summary>
        public ScheduleDialog()
        {
            // this.applyOnClose = false;
            // InitializeComponent();
            // this.scheduleData = new JobScheduleData();
            // InitializeControls();
            // InitializeData();

        }

        /// <summary>
        /// constructs an empty schedule dialog but passed in the data context
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleDialog(CDataContainer context)
        {
            // STrace.Assert(context != null);
            // this.dataContainerContext = context;
            // this.applyOnClose = false;
            // InitializeComponent();
            // this.scheduleData = new JobScheduleData();
            // InitializeControls();
            // InitializeData();
        }

        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo schedule. Will
        /// automatically save changes on ok.
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleDialog(JobSchedule source)
        {
            // this.applyOnClose = true;
            // InitializeComponent();
            // this.scheduleData = new JobScheduleData(source);
            // InitializeControls();
            // InitializeData();
        }
        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo schedule. Will
        /// automatically save changes on ok.
        /// 
        /// context is provided to enabled 'Jobs In Schedule' button
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleDialog(JobSchedule source, CDataContainer context)
        {
            // this.dataContainerContext = context;

            // this.applyOnClose = true;
            // InitializeComponent();
            // this.scheduleData = new JobScheduleData(source);
            // InitializeControls();
            // InitializeData();
        }

        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo Job. Will
        /// automatically save changes on ok.
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleDialog(Job source)
        {
            // this.applyOnClose = true;
            // InitializeComponent();
            // this.scheduleData = new JobScheduleData(source);
            // InitializeControls();
            // InitializeData();
        }
        /// <summary>
        /// Constructs a new ScheduleDialog based upon a JobScheduleData object.
        /// </summary>
        /// <param name="source"></param>
        public ScheduleDialog(JobScheduleData source)
        {
            // InitializeComponent();
            // this.scheduleData = source;
            // InitializeControls();
            // InitializeData();
        }

        /// <summary>
        /// Constructs a new ScheduleDialog based upon a JobScheduleData object
        /// And provides context for that dialog so it can enable 'JobsInSchedule' button
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        public ScheduleDialog(JobScheduleData source, CDataContainer context, IServiceProvider provider)
        {
            // this.dataContainerContext = context;
            // this.serviceProvider = provider;
            // this.launchFormHost2 = (ILaunchFormHost2)this.serviceProvider.GetService(typeof(ILaunchFormHost2));
            // this.helpProvider = (IHelpProvider)serviceProvider.GetService(typeof(IHelpProvider));

            // InitializeComponent();
            // this.scheduleData = source;
            // InitializeControls();
            // InitializeData();
        }


        /// <summary>
        /// Initializes a new ScheduleDialog using the jobSchedules array to ensure the duplicate schedule names
        /// are not create (for Shiloh support).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="jobSchedules"></param>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        [Obsolete("This method will be removed. Please use alternate constructor - ScheduleDialog(JobScheduleData, List<JobScheduleData>, context,  provider) ")] 
        public ScheduleDialog(JobScheduleData source, ArrayList jobSchedules, CDataContainer context, IServiceProvider provider)
        {
            // this.dataContainerContext = context;
            // this.jobSchedules = jobSchedules;

            // this.serviceProvider = provider;

            // this.launchFormHost2 = (ILaunchFormHost2)this.serviceProvider.GetService(typeof(ILaunchFormHost2));
            // this.helpProvider = (IHelpProvider)serviceProvider.GetService(typeof(IHelpProvider));

            // InitializeComponent();
            // this.scheduleData = source;
            // InitializeControls();
            // InitializeData();
        }

        /// <summary>
        /// Initializes a new ScheduleDialog using List<JobScheduleData>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="jobScheduleList"></param>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        public ScheduleDialog(JobScheduleData source, List<JobScheduleData> jobScheduleList, CDataContainer context, IServiceProvider provider)
        {
            // if (null == source)
            // {
            //     throw new ArgumentNullException("source");
            // }

            // if (null == jobScheduleList)
            // {
            //     throw new ArgumentNullException("jobScheduleList");
            // }

            // if (null == context)
            // {
            //     throw new ArgumentNullException("context");
            // }

            // if (null == provider)
            // {
            //     throw new ArgumentNullException("provider");
            // }

            // this.dataContainerContext = context;
            // this.jobSchedules = new ArrayList(jobScheduleList.ToArray());

            // this.serviceProvider = provider;

            // this.launchFormHost2 = (ILaunchFormHost2)this.serviceProvider.GetService(typeof(ILaunchFormHost2));
            // this.helpProvider = (IHelpProvider)serviceProvider.GetService(typeof(IHelpProvider));

            // this.InitializeComponent();
            // this.scheduleData = source;
            // this.InitializeControls();
            // this.InitializeData();
        }

        
        /// <summary>
        /// Constructs a new Schedule dialog based upon a SimpleJobSchedule structure
        /// </summary>
        /// <param name="source"></param>
        public ScheduleDialog(SimpleJobSchedule source)
        {
            // InitializeComponent();
            // this.scheduleData = source.ToJobScheduleData();
            // InitializeControls();
            // InitializeData();
        }

        public ScheduleDialog(XmlDocument xmlDoc, IServiceProvider provider)
        {
            // this.applyOnClose = true;

            // InitializeComponent();
            // this.scheduleData = ExtractScheduleDataFromXml(xmlDoc);

            // IManagedConnection svcIMC = (IManagedConnection)provider.GetService(typeof(IManagedConnection));

            // System.Diagnostics.Debug.Assert(svcIMC != null);

            // SqlOlapConnectionInfoBase connectionInfo = svcIMC.Connection;

            // SqlConnectionInfo sci = connectionInfo as SqlConnectionInfo;
            // ServerConnection serverConnection = new ServerConnection(sci);

            // Smo.Server server = new Smo.Server(serverConnection);

            // JobServer jobServer = server.JobServer;

            // this.scheduleData.SetJobServer(jobServer);

            // this.serviceProvider = provider;
            // this.launchFormHost2 = (ILaunchFormHost2)this.serviceProvider.GetService(typeof(ILaunchFormHost2));
            // this.helpProvider = (IHelpProvider)serviceProvider.GetService(typeof(IHelpProvider));

            // InitializeControls();
            // InitializeData();
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
                //InitializeData();
            }

        }

        /// <summary>
        /// Indicates whether or not the user can edit the Name of the schedule.
        /// </summary>
        // public bool AllowNameChange
        // {
        //     get
        //     {
        //         return this.textboxScheduleName.Enabled;
        //     }
        //     set
        //     {
        //         this.textboxScheduleName.Enabled = value;
        //         this.UpdateOkCancelStatus();
        //     }
        // }
        /// <summary>
        /// Indicates whether or not the user can edit the type of the schedule.
        /// </summary>
        // public bool AllowScheduleTypeChange
        // {
        //     get
        //     {
        //         return this.comboScheduleType.Enabled;
        //     }
        //     set
        //     {
        //         this.comboScheduleType.Enabled = value;
        //     }
        // }
        /// <summary>
        /// Indicates whether or not the user can enable/disable the schedule.
        /// </summary>
        // public bool AllowEnableDisable
        // {
        //     get
        //     {
        //         return this.checkboxEnabled.Enabled;
        //     }
        //     set
        //     {
        //         this.checkboxEnabled.Enabled = value;
        //     }
        // }
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

#region Public Methods - ToString()
        /// <summary>
        /// Converts the schedule into a user friendly description
        /// </summary>
        /// <returns></returns>
        // public override string ToString()
        // {
        //     return ToString(System.Globalization.CultureInfo.InvariantCulture);
        // }
        /// <summary>
        /// Converts the schedule into a user friendly description using the provided IFormatProvider
        /// </summary>
        /// <returns></returns>
        // public string ToString(IFormatProvider format)
        // {
        //     return this.recurrancePattern.Description;
        // }
#endregion
#region Internal Helpers
        // private void UpdateOkCancelStatus()
        // {

        //     //Always enable the Cancel button
        //     this.buttonCancel.Enabled = true;

        //     //if this schedule is set to read only then disable the OK button.  There's no need
        //     //to check for any other conditions to enable this button, so exit.
        //     if (scheduleData.IsReadOnly && !scheduleData.AllowEnableDisable)
        //     {
        //         this.buttonOK.Enabled = false;
        //         return;
        //     }

        //     //If Allow Name change is set to true then enable the OK button only if there is a non-blank
        //     //schedule name.
        //     if (this.AllowNameChange)
        //     {
        //         this.buttonOK.Enabled = this.textboxScheduleName.Text.Trim().Length > 0;
        //     }
        //     //Otherwise, the current action does not require a schedule name and will be provided
        //     //elsewhere.  There's no need to check the schedule name in this case so enable the OK
        //     //button.
        //     else
        //     {

        //         this.buttonOK.Enabled = true;
        //     }


        // }
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
#endregion

#region Custom UI Initalization
        /// <summary>
        /// load data controls.
        /// </summary>
        // private void InitializeData()
        // {
        //     string name = this.scheduleData.Name;

        //     // if name is null or schedule data was initialized locally but not yet created on server ; then disable "Jobs In Schedule" button
        //     if (String.IsNullOrEmpty(name) || !this.scheduleData.Created)
        //     {
        //         this.Text = SR.ScheduleDialogTitleNew;
        //         this.buttonJobsInSchedule.Enabled = false;
        //     }
        //     else
        //     {
        //         this.Text = SR.ScheduleDialogTitleProperties(name);
        //         this.buttonJobsInSchedule.Enabled =
        //             (this.dataContainerContext != null) && // if luanch context is provided
        //             (this.dataContainerContext.Server != null) && // and we have a server to use
        //             (this.dataContainerContext.Server.Information.Version.Major >= 9) // and its version is Yukon + (we shupport shared schedules)
        //         && !this.scheduleData.IsReadOnly;
        //     }

        //     this.AllowEnableDisable = this.scheduleData.AllowEnableDisable;


        //     this.textboxScheduleName.Text = this.scheduleData.Name;
        //     this.checkboxEnabled.Checked = this.scheduleData.Enabled;

        //     int index = idxRecurringSchedule;
        //     if (scheduleData.FrequencyTypes == FrequencyTypes.AutoStart)
        //     {
        //         index = idxAutoStartSchedule;
        //     }
        //     else if (scheduleData.FrequencyTypes == FrequencyTypes.OnIdle)
        //     {
        //         index = idxCpuIdleSchedule;
        //     }
        //     else if (scheduleData.FrequencyTypes == FrequencyTypes.OneTime)
        //     {
        //         index = idxOneTimeSchedule;
        //         this.recurrancePattern.OneTime = true;
        //         this.recurrancePattern.LoadData(scheduleData);
        //     }
        //     else if (this.scheduleData != null)
        //     {
        //         // only load data if there is something to load
        //         this.recurrancePattern.OneTime = false;
        //         this.recurrancePattern.LoadData(scheduleData);
        //     }

        //     this.comboScheduleType.SelectedIndex = index;
            
        //     ///Check the status of the isReadOnly flag.  If it is set to readonly make turn the
        //     ///controls off so that the user can see the schedule but can't change it.
        //     if (scheduleData.IsReadOnly)
        //     {
        //         recurrancePattern.ReadOnly = true;
        //         buttonOK.Enabled = false;
        //         buttonCancel.Enabled = false;
        //         textboxScheduleName.ReadOnly = true;
        //         comboScheduleType.Enabled = false;
        //         buttonJobsInSchedule.Enabled = false;
        //     }

        //     UpdateOkCancelStatus();
        // }

        private int idxAutoStartSchedule = 0;
        private int idxCpuIdleSchedule = 1;
        private int idxRecurringSchedule = 2;
        private int idxOneTimeSchedule = 3;

        // private List<string> agentSchedulerTypes = new List<string>()
        // {
        //    SR.AutoStartSchedule,
        //    SR.CPUIdleSchedule,
        //    SR.RecurringSchedule,
        //    SR.OneTimeSchedule
        // };

        // private void InitializeControls()
        // {
        //     this.comboScheduleType.Items.AddRange(agentSchedulerTypes.ToArray());

        //     // Managed instances do not support scheduling 'whenever cpu becomes idle'
        //     // Remove the options from the combo box
        //     //
        //     if (this.dataContainerContext != null &&
        //         this.dataContainerContext.Server != null &&
        //         this.dataContainerContext.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
        //     {
        //         this.comboScheduleType.Items.RemoveAt(1);
        //     }

        //     this.idxAutoStartSchedule = this.comboScheduleType.Items.IndexOf(SR.AutoStartSchedule);
        //     this.idxRecurringSchedule = this.comboScheduleType.Items.IndexOf(SR.RecurringSchedule);
        //     this.idxOneTimeSchedule = this.comboScheduleType.Items.IndexOf(SR.OneTimeSchedule);
        //     this.idxCpuIdleSchedule = this.comboScheduleType.Items.IndexOf(SR.CPUIdleSchedule);

        //     // Assign the help button
        //     HelpControl = this.buttonHelp;

        //     // Set the help topic
        //     HelpF1Keyword = helpKeyword;


        // }
#endregion

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

        // private void Cancel_Click(System.Object sender, System.EventArgs e)
        // {
        //     this.DialogResult = DialogResult.Cancel;
        //     this.Close();
        // }

        // private void scheduleName_TextChanged(System.Object sender, System.EventArgs e)
        // {
        //     UpdateOkCancelStatus();
        // }


        // private void scheduleType_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     this.recurrancePattern.ReadOnly =
        //         !((this.comboScheduleType.SelectedIndex == idxRecurringSchedule) ||
        //         (this.comboScheduleType.SelectedIndex == idxOneTimeSchedule)) ||
        //         this.scheduleData.IsReadOnly;
        //     this.recurrancePattern.OneTime = (this.comboScheduleType.SelectedIndex == idxOneTimeSchedule);
        // }

        // private void buttonJobsInSchedule_Click(object sender, System.EventArgs e)
        // {
        //     System.Diagnostics.Debug.Assert(this.dataContainerContext != null, "schedule properties details can be displayed only in properties mode and if context CDataContainer is available");
        //     using (JobsReferencingScheduleForm formJobsInSchedule = 
        //            new JobsReferencingScheduleForm(this.dataContainerContext, 
        //                                            this.scheduleData.ID, 
        //                                            this.scheduleData.Name, 
        //                                            true, 
        //                                            this.serviceProvider))
        //     {
        //         formJobsInSchedule.ShowDialog(this);
        //     }
        // }
#endregion


        /// <summary>
        /// Handle F1 help
        /// </summary>
        /// <param name="hevent"></param>
        /// 

        // protected override void OnHelpRequested(HelpEventArgs hevent)
        // {
        //     base.OnHelpRequested(hevent);

        //     hevent.Handled = true;

        //     if (helpProvider != null)
        //     {
        //         helpProvider.DisplayTopicFromF1Keyword(helpKeyword);
        //     }
        //     else if (launchFormHost2 != null)
        //     {
        //         launchFormHost2.ShowHelp(helpKeyword);
        //     }
        // }

    }
}
