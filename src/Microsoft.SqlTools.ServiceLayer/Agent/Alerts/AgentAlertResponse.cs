//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Agent alert response
    /// </summary>
    internal class AgentAlertResponse : AgentControlBase
    {
        #region Private types

        enum OperatorGridColumn
        {
            Name = 0,
            Email = 1,
            Pager = 2,            
        }

        // columns in data table returned by EnumNotifications()
        private const int enumColId = 0;
        private const int enumColName = 1;
        private const int enumColEmail = 2;
        private const int enumColPager = 3;
        
        private class JobInfoWithNameAndID
        {
            private string jobName;
            private Guid jobId;

            private JobInfoWithNameAndID()
            { }

            public JobInfoWithNameAndID(string n, Guid i)
            {
                this.jobName = n;
                this.jobId = i;
            }

            public string JobName
            {
                get
                {
                    return this.jobName;
                }
            }

            public Guid JobID
            {
                get
                {
                    return this.jobId;
                }
            }

            public override string ToString()
            {
                return this.jobName;
            }
        }
        #endregion

        #region Members

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.CheckBox executeJob;
        // private System.Windows.Forms.Button newJob;
        // private System.Windows.Forms.Button viewJob;
        // private System.Windows.Forms.CheckBox notifyOperators;
        // private System.Windows.Forms.Label operatorListLabel;
        // private System.Windows.Forms.Button newOperator;
        // private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid operatorList;
        // private System.Windows.Forms.Button viewOperator;
        // private System.Windows.Forms.ComboBox comboBoxJob;

        /// <summary>
        /// Agent alert being edited
        /// </summary>
        private string agentAlertName = null;

        private string agentJobName = string.Empty;

        private bool enableNewJobs = true;

        private bool readOnly = false;

        #endregion

        #region Constructors

        Dictionary<string,Guid> jobNameToGuidDict;

        private AgentAlertResponse()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.alert.response.f1";
            // this.comboBoxJob.Enabled = false;

        }

        private string FormatFullJobName(string jobName, string categoryName )
        {
            return string.Format(CultureInfo.InvariantCulture, "AgentAlertSR.FullJobNameFormat", jobName, categoryName );
        }

        public AgentAlertResponse(CDataContainer dataContainer, string agentAlertName)
            : this()
        {
            if (dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }

            DataContainer = dataContainer;
            //this.AllUIEnabled = true;
            this.agentAlertName = agentAlertName;

            // this.operatorList.SelectionType = GridSelectionType.SingleRow;

            // this.executeJob.CheckedChanged += new EventHandler(OnExecuteJobCheckedChanged);
            // this.notifyOperators.CheckedChanged += new EventHandler(OnNotifyOperatorsCheckedChanged);
            // this.operatorList.MouseButtonClicked += new MouseButtonClickedEventHandler(OnOperatorListMouseButtonClicked);
            // this.operatorList.SelectionChanged += new SelectionChangedEventHandler(OnOperatorListSelectionChanged);
            // this.viewOperator.Click += new EventHandler(OnViewOperatorClick);
            // this.newOperator.Click += new EventHandler(OnNewOperatorClick);
            // this.viewJob.Click += new EventHandler(OnViewJobClick);
            // this.newJob.Click += new EventHandler(OnNewJobClick);

            // Initialize and fill operator list
            //InitializeOperatorListGrid();
            FillOperatorListGrid();

            SqlServer.Management.Smo.Server srv = this.DataContainer.Server;
            srv.SetDefaultInitFields(typeof(Job), new string[] {"Name", "JobID", "CategoryID"}); // reduce the number of queries
            srv.SetDefaultInitFields(typeof(JobCategory), new string[] {"Name", "ID"});

            this.readOnly = !srv.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);

            //
            // should we allow new jobs to be added to this alert?
            //
            // string enableNewJobs = string.Empty;
            // STParameters parameters = new STParameters(dataContainer.Document);
            // parameters.GetParam("enableNewJobs", ref enableNewJobs);
            // if (enableNewJobs == "false")
            // {
            //     this.executeJob.Enabled = false;
            //     this.newJob.Enabled = false;
            //     this.viewJob.Enabled = false;
            //     this.comboBoxJob.Enabled = false;
            //     this.enableNewJobs = false;

            //     string jobName = string.Empty;
            //     string categoryID = string.Empty;
            //     parameters.GetParam("job", ref jobName);
            //     parameters.GetParam("categoryid", ref categoryID);
            //     if (!string.IsNullOrEmpty(jobName) && !string.IsNullOrEmpty(categoryID))
            //     {
            //         // The combobox is disabled and has only one item
            //         int id = Int32.Parse(categoryID,System.Globalization.CultureInfo.InvariantCulture);
            //         this.comboBoxJob.Items.Add(FormatFullJobName(jobName,srv.JobServer.JobCategories.ItemById(id).Name));
            //         this.comboBoxJob.SelectedIndex = 0;
            //         this.executeJob.Checked = true;
            //     }
            // }
            // else
            // {
            //     RefreshJobComboBox();
            //     this.executeJob.Checked = this.comboBoxJob.Text.Length > 0;
            // }
        }

        #endregion

        /// <summary>
        /// Returns the SMO Job object based on currently selected (or types) job name in the combo box
        /// Returns null if the job cannot be found, or multiple jobs with the same names exist
        /// </summary>
        /// <param name="alert"></param>
        private Job GetJobFromCurrentSelection()
        {
            Guid id = Guid.Empty;
            Job theJob = null;
            string fullJobName = "this.comboBoxJob.Text";

            jobNameToGuidDict.TryGetValue(fullJobName, out id);

            if( id == Guid.Empty )
            {
                // The name is typed by the user. Let's see if we can find the unambiguous Job
                foreach( Job job in this.DataContainer.Server.JobServer.Jobs )
                {
                    //if( this.DataContainer.Server.JobServer.Jobs.StringComparer.Compare(fullJobName,job.Name) == 0 )
                    if (String.Compare(fullJobName,job.Name) == 0)
                    {
                        if( theJob == null )
                        {
                            theJob = job; // Found it, but wait: maybe we'll find another one, so this one would not be unique
                        }
                        else
                        {
                            theJob = null; // Rats! found another job with the same name. So theJob is not unique after all. Return null
                            break;
                        }
                    }
                }
            }
            else
            {
                theJob = this.DataContainer.Server.JobServer.Jobs.ItemById(id); // can be null if the job was just deleted. Caller will handle
            }
            return theJob;
        }

        #region Public methods

        /// <summary>
        /// Updates alert fields with data from this page
        /// </summary>
        /// <param name="alert"></param>
        public void UpdateAlert(Alert alert)
        {
            if (alert == null)
                throw new ArgumentNullException("alert");

            if (this.agentAlertName != null)
                this.agentAlertName = alert.Name;

            // update job
            // if (executeJob.Checked == true) 
            // {
            //     if (enableNewJobs)
            //     {
            //         Job job = GetJobFromCurrentSelection();
            //         if (job == null )
            //         {
            //             alert.JobID = Guid.Empty;
            //             ShowMessage(new ApplicationException(AgentAlertResponseSR.JobNotFound(this.comboBoxJob.Text)),
            //                         AgentAlertSR.AgentAlertError,
            //                         ExceptionMessageBoxButtons.OK,
            //                         ExceptionMessageBoxSymbol.Error);
            //         }
            //         else
            //         {
            //             JobInfoWithNameAndID selJobInfo = new JobInfoWithNameAndID(job.Name, job.JobID);
            //             STrace.Assert(selJobInfo != null);
            //             alert.JobID = selJobInfo.JobID;
            //         }
            //     }
            // }
            // else
            {
                alert.JobID = Guid.Empty;
            }
            // operators must be altered only after alert is created
        }

        #endregion


        #region Overrides
        // public override void OnReset(object sender)
        // {
        //     if (!readOnly)
        //     {
        //         this.DataContainer.Server.JobServer.Operators.Refresh();
        //         this.DataContainer.Server.JobServer.Jobs.Refresh();
        //         FillOperatorListGrid();
        //         RefreshJobComboBox();
        //         base.OnReset(sender);
        //         this.executeJob.Enabled = true;
        //         this.executeJob.Checked = false;
        //         this.comboBoxJob.Enabled = false;
        //         this.notifyOperators.Checked = false;
        //     }
        // }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
//         private void InitializeComponent()
//         {
//             System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentAlertResponse));
//             this.executeJob = new System.Windows.Forms.CheckBox();
//             this.newJob = new System.Windows.Forms.Button();
//             this.viewJob = new System.Windows.Forms.Button();
//             this.notifyOperators = new System.Windows.Forms.CheckBox();
//             this.operatorListLabel = new System.Windows.Forms.Label();
//             this.operatorList = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
//             this.newOperator = new System.Windows.Forms.Button();
//             this.viewOperator = new System.Windows.Forms.Button();
//             this.comboBoxJob = new System.Windows.Forms.ComboBox();
//             ((System.ComponentModel.ISupportInitialize)(this.operatorList)).BeginInit();
//             this.SuspendLayout();
//             this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//             this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//             // 
//             // executeJob
//             // 
//             resources.ApplyResources(this.executeJob, "executeJob");
//             this.executeJob.Name = "executeJob";
//             this.executeJob.CheckedChanged += new System.EventHandler(this.executeJob_CheckedChanged);
//             // 
//             // newJob
//             // 
//             resources.ApplyResources(this.newJob, "newJob");
//             this.newJob.Name = "newJob";
//             // 
//             // viewJob
//             // 
//             resources.ApplyResources(this.viewJob, "viewJob");
//             this.viewJob.Name = "viewJob";
//             // 
//             // notifyOperators
//             // 
//             resources.ApplyResources(this.notifyOperators, "notifyOperators");
//             this.notifyOperators.Name = "notifyOperators";
//             this.notifyOperators.CheckedChanged += new System.EventHandler(this.notifyOperators_CheckedChanged);
//             // 
//             // operatorListLabel
//             // 
//             resources.ApplyResources(this.operatorListLabel, "operatorListLabel");
//             this.operatorListLabel.Name = "operatorListLabel";
//             // 
//             // operatorList
//             // 
//             resources.ApplyResources(this.operatorList, "operatorList");
//             this.operatorList.BackColor = System.Drawing.SystemColors.Window;
//             this.operatorList.ForceEnabled = false;
//             this.operatorList.Name = "operatorList";
//             // 
//             // newOperator
//             // 
//             resources.ApplyResources(this.newOperator, "newOperator");
//             this.newOperator.Name = "newOperator";
//             // 
//             // viewOperator
//             // 
//             resources.ApplyResources(this.viewOperator, "viewOperator");
//             this.viewOperator.Name = "viewOperator";
//             // 
//             // comboBoxJob
//             // 
//             resources.ApplyResources(this.comboBoxJob, "comboBoxJob");
// //            this.comboBoxJob.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
//             this.comboBoxJob.FormattingEnabled = true;
//             this.comboBoxJob.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
//             this.comboBoxJob.AutoCompleteSource = AutoCompleteSource.ListItems;
//             this.comboBoxJob.Name = "comboBoxJob";
//             this.comboBoxJob.Sorted = true;
//             // 
//             // AgentAlertResponse
//             // 
//             this.Controls.Add(this.comboBoxJob);
//             this.Controls.Add(this.executeJob);
//             this.Controls.Add(this.newJob);
//             this.Controls.Add(this.viewJob);
//             this.Controls.Add(this.notifyOperators);
//             this.Controls.Add(this.operatorListLabel);
//             this.Controls.Add(this.operatorList);
//             this.Controls.Add(this.newOperator);
//             this.Controls.Add(this.viewOperator);
//             this.Name = "AgentAlertResponse";
//             resources.ApplyResources(this, "$this");
//             ((System.ComponentModel.ISupportInitialize)(this.operatorList)).EndInit();
//             this.ResumeLayout(false);

//         }
        #endregion

        #region Event handlers

        private void LaunchJobProperties(Job job)
        {
            STParameters param = new STParameters();
            param.SetDocument(this.DataContainer.Document);
            param.SetParam("alert", this.agentAlertName);
            param.SetParam("enableNewAlerts", "false");

// TODO: Need to optimize communication between dialogs: pass in urn, get back name and categoryID. Either Urn alone or ther other two should be sufficient. Pick one!
            if (job != null)
            {
                string urn = string.Format(CultureInfo.InvariantCulture, "Server/JobServer/Job[@Name='{0}' and @CategoryID='{1}']", Urn.EscapeString(job.Name), job.CategoryID );
                param.SetParam("urn", urn);
                param.SetParam("job", job.Name);
            }
            else
            {
                param.SetParam("urn", null);
                param.SetParam("job", null);
            }

            // using (LaunchForm launchForm = new LaunchForm(new JobPropertySheet(this.DataContainer), this.ServiceProvider))
            // {
            //     if (launchForm.ShowDialog() == DialogResult.OK)
            //     {
            //         string name = string.Empty;
            //         string categoryID = string.Empty;
            //         param.GetParam("job", ref name);
            //         param.GetParam("CategoryID", ref categoryID);

            //         int id = Int32.Parse(categoryID,System.Globalization.CultureInfo.InvariantCulture);

            //         this.DataContainer.Server.JobServer.Jobs.Refresh();
            //         Job newJob = this.DataContainer.Server.JobServer.Jobs[name,id]; // note this this can be different from job

            //         RefreshJobComboBoxAndSelectJob(newJob);
            //     }
            // }
        }

        private void OnNewJobClick(object sender, EventArgs e)
        {
            LaunchJobProperties(null);
        }

        private void OnViewJobClick(object sender, EventArgs e)
        {
            // if (this.comboBoxJob.Text.Length == 0)
            // {
            //     ShowMessage(new ApplicationException(AgentAlertResponseSR.NoJobSelected),
            //                 AgentAlertSR.AgentAlertError,
            //                 ExceptionMessageBoxButtons.OK,
            //                 ExceptionMessageBoxSymbol.Error);
            //     return;
            // }

            // this.DataContainer.Server.JobServer.Refresh();

            Job job = GetJobFromCurrentSelection();

            // if (job == null)
            // {
            //     ShowMessage(new ApplicationException(AgentAlertResponseSR.JobNotFound(this.comboBoxJob.Text)),
            //                 AgentAlertSR.AgentAlertError,
            //                 ExceptionMessageBoxButtons.OK,
            //                 ExceptionMessageBoxSymbol.Error);
            //     return;
            // }

            LaunchJobProperties(job);
        }

        private void OnExecuteJobCheckedChanged(object sender, EventArgs e)
        {
            // bool newValue;
            // if (this.executeJob.Checked && this.enableNewJobs)
            // {
            //     newValue = true;
            // }
            // else
            // {
            //     newValue = false;
            // }

            // this.comboBoxJob.Enabled = newValue;
            // this.newJob.Enabled = newValue;
            // this.viewJob.Enabled = newValue;

            // RefreshJobComboBox();
        }

        private void OnNotifyOperatorsCheckedChanged(object sender, EventArgs e)
        {
            // if (this.notifyOperators.Checked)
            // {
            //     this.operatorListLabel.Enabled = true;
            //     this.operatorList.Enabled = true;
            //     this.newOperator.Enabled = true;
            //     // Select row if it wasn't selected before
            //     if (this.operatorList.RowsNumber > 0 && this.operatorList.SelectedRow < 0)
            //         this.operatorList.SelectedRow = 0;

            //     if (this.operatorList.SelectedRow >= 0)
            //         this.viewOperator.Enabled = true;
            //     else
            //         this.viewOperator.Enabled = false;
            // }
            // else
            // {
            //     this.operatorListLabel.Enabled = false;
            //     this.operatorList.Enabled = false;
            //     this.newOperator.Enabled = false;
            //     this.viewOperator.Enabled = false;
            // }
        }

        private void OnViewOperatorClick(object sender, EventArgs e)
        {
            // try
            // {
            //     GridCell gridCell = this.operatorList.GetCellInfo(this.operatorList.SelectedRow, 0);
            //     StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture);
            //     XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);

            //     Microsoft.SqlServer.Management.Smo.Agent.Operator agentOperator = gridCell.Tag as Microsoft.SqlServer.Management.Smo.Agent.Operator;

            //     xmlTextWriter.WriteStartElement("operator");
            //     xmlTextWriter.WriteString(agentOperator.Name);
            //     xmlTextWriter.WriteEndElement();

            //     CDataContainer dataContainer = new CDataContainer(this.DataContainer, stringWriter.ToString());

            //     using (LaunchForm launchForm = new LaunchForm(new AgentOperators(dataContainer), ServiceProvider))
            //     {
            //         launchForm.ShowDialog(this);
            //     }
            // }
            // catch (Exception exception)
            // {
            //     ApplicationException applicationException = new ApplicationException(AgentAlertResponseSR.CannotViewOperator, exception);

            //     ShowMessage(applicationException,
            //                 AgentAlertSR.AgentAlertError,
            //                 ExceptionMessageBoxButtons.OK,
            //                 ExceptionMessageBoxSymbol.Error);
            // }
        }

     
        private void OnNewOperatorClick(object sender, EventArgs e)
        {
            try
            {
                CDataContainer dataContainer = new CDataContainer(this.DataContainer, null);

                // using (LaunchForm launchForm = new LaunchForm(new AgentOperators(dataContainer), ServiceProvider))
                // {
                //     launchForm.ShowDialog(this);

                //     if (launchForm.DialogResult == DialogResult.OK)
                //     {
                //         ///Refresh the operators grid list
                //         this.DataContainer.Server.JobServer.Operators.Refresh();
                //         this.DataContainer.Server.JobServer.Jobs.Refresh();
                //         FillOperatorListGrid();
                //     }
                // }

            }
            catch (Exception exception)
            {
                // ApplicationException applicationException = new ApplicationException(AgentAlertResponseSR.CannotCreateNewOperator, exception);

                // ShowMessage(applicationException,
                //             AgentAlertSR.AgentAlertError,
                //             ExceptionMessageBoxButtons.OK,
                //             ExceptionMessageBoxSymbol.Error);
            }
        }

        // private void executeJob_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     this.comboBoxJob.Enabled = this.executeJob.Checked;
        // }

        // private void notifyOperators_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     this.operatorList.Enabled = this.notifyOperators.Checked;
        // }

        #endregion

        #region Private helpers

        /// <summary>
        /// Initializes operator list grid
        /// </summary>
        // void InitializeOperatorListGrid()
        // {
        //     GridColumnInfo gridColumnInfo;
        //     int operatorListWidth = this.operatorList.ClientRectangle.Width;

        //     // Add operator column
        //     gridColumnInfo = new GridColumnInfo();
        //     gridColumnInfo.ColumnType = GridColumnType.Text;
        //     gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
        //     gridColumnInfo.ColumnWidth = (int)(Math.Floor(operatorListWidth * 0.58)); // 58%
        //     gridColumnInfo.IsUserResizable = true;
        //     this.operatorList.AddColumn(gridColumnInfo);
        //     this.operatorList.SetHeaderInfo((int)OperatorGridColumn.Name, AgentAlertResponseSR.Operator, null);
        //     // Add e-mail column
        //     gridColumnInfo = new GridColumnInfo();
        //     gridColumnInfo.ColumnType = GridColumnType.Checkbox;
        //     gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
        //     gridColumnInfo.ColumnWidth = (int)(Math.Floor(operatorListWidth * 0.13)); // 13%
        //     gridColumnInfo.ColumnAlignment = HorizontalAlignment.Center;
        //     gridColumnInfo.HeaderAlignment = HorizontalAlignment.Center;
        //     gridColumnInfo.IsUserResizable = true;
        //     this.operatorList.AddColumn(gridColumnInfo);
        //     this.operatorList.SetHeaderInfo((int)OperatorGridColumn.Email, AgentAlertResponseSR.Email, null);
        //     // Add pager column
        //     gridColumnInfo = new GridColumnInfo();
        //     gridColumnInfo.ColumnType = GridColumnType.Checkbox;
        //     gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
        //     gridColumnInfo.ColumnWidth = (int)(Math.Floor(operatorListWidth * 0.13)); // 13%
        //     gridColumnInfo.ColumnAlignment = HorizontalAlignment.Center;
        //     gridColumnInfo.HeaderAlignment = HorizontalAlignment.Center;
        //     gridColumnInfo.IsUserResizable = true;
        //     this.operatorList.AddColumn(gridColumnInfo);
        //     this.operatorList.SetHeaderInfo((int)OperatorGridColumn.Pager, AgentAlertResponseSR.Pager, null);            
        // }

        /// <summary>
        /// Fill job combobox and select the appropriate job if it is available
        /// </summary>
        private void RefreshJobComboBox()
        {
            if( !this.enableNewJobs )
            {
                return;
            }

            Job thisJob = null;
            SqlServer.Management.Smo.Server srv = this.DataContainer.Server;

            if (this.agentAlertName != null)
            {
                Alert alert = srv.JobServer.Alerts[this.agentAlertName];
                if (alert != null)
                {
                    thisJob = srv.JobServer.Jobs.ItemById(alert.JobID );
                }
            }

            RefreshJobComboBoxAndSelectJob(thisJob);

        }

        /// <summary>
        /// Fill job combobox and select the job specified by the caller
        /// </summary>
        private void RefreshJobComboBoxAndSelectJob( Job currentJob )
        {
            SqlServer.Management.Smo.Server srv = this.DataContainer.Server;

            if( jobNameToGuidDict == null )
            {
                jobNameToGuidDict = new Dictionary<string,Guid>();
            }
            jobNameToGuidDict.Clear();

            object[] comboboxItems = new object[srv.JobServer.Jobs.Count];
            int comboboxIndex=0;

            foreach( Job job in srv.JobServer.Jobs )
            {
                int categoryID = job.CategoryID;
                string key = FormatFullJobName(job.Name, srv.JobServer.JobCategories.ItemById(categoryID).Name);
                jobNameToGuidDict.Add(key,job.JobID);
                comboboxItems[comboboxIndex++] = key;
            }

            // this.comboBoxJob.Items.Clear();
            // this.comboBoxJob.Items.AddRange(comboboxItems);

            if( currentJob == null )
            {
                // could happen if job was just deleted from the server, or no job is selected
                return;
            }
            // this.comboBoxJob.SelectedItem = FormatFullJobName(currentJob.Name, srv.JobServer.JobCategories.ItemById(currentJob.CategoryID).Name);
        }

        /// <summary>
        /// Fills operator list grid
        /// </summary>
        void FillOperatorListGrid()
        {
            bool notifySomebody;
            // if (this.notifyOperators.Checked)
            // {
            //     //if notify others is already checked, maintain the state of the check box
            //     notifySomebody = true;
            // }
            // else
            {
                ///Otherwise turn the notify checkbox off (unless there are notification(see below))
                notifySomebody = false;
            }

            // this.operatorList.DeleteAllRows();

            // GridCellCollection gridCellCollection;
            // GridCell gridCell;
            Alert agentAlert;

            if (this.agentAlertName != null)
                agentAlert = this.DataContainer.Server.JobServer.Alerts[this.agentAlertName];
            else
                agentAlert = null;

            foreach (Microsoft.SqlServer.Management.Smo.Agent.Operator agentOperator in this.DataContainer.Server.JobServer.Operators)
            {
                // gridCellCollection = new GridCellCollection();

                // gridCell = new GridCell(agentOperator.Name);
                // gridCell.Tag = agentOperator;
                // gridCellCollection.Add(gridCell);

                // if (agentAlert != null)
                // {
                //     DataTable notifications;

                //     notifications = agentAlert.EnumNotifications(NotifyMethods.NotifyAll, agentOperator.Name);

                //     GridCheckBoxState notifyEmail = GridCheckBoxState.Unchecked;
                //     GridCheckBoxState notifyPager = GridCheckBoxState.Unchecked;
                    
                //     if (notifications.Rows.Count != 0)
                //     {
                //         notifySomebody = true;

                //         notifyEmail = Convert.ToBoolean(notifications.Rows[0][enumColEmail], System.Globalization.CultureInfo.InvariantCulture) ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;
                //         notifyPager = Convert.ToBoolean(notifications.Rows[0][enumColPager], System.Globalization.CultureInfo.InvariantCulture) ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;                    
                //     }

                //     gridCellCollection.Add(new GridCell(notifyEmail));
                //     gridCellCollection.Add(new GridCell(notifyPager));
                // }
                // else
                // {
                //     // Email column
                //     gridCellCollection.Add(new GridCell(GridCheckBoxState.Unchecked));
                //     // Pager column
                //     gridCellCollection.Add(new GridCell(GridCheckBoxState.Unchecked));
                // }

                // this.operatorList.AddRow(gridCellCollection);
            }

            // this.notifyOperators.Checked = notifySomebody;
            // if (this.operatorList.RowsNumber > 0)
            // {
            //     this.operatorList.SelectedRow = 0;
            // }
        }

        public void UpdateOperators(Alert alert)
        {
            // DlgGridControl grid = this.operatorList;

            // for (int i = 0; i < this.operatorList.RowsNumber; ++i)
            // {
            //     string operatorName = grid.GetCellInfo(i, Convert.ToInt32(OperatorGridColumn.Name, System.Globalization.CultureInfo.InvariantCulture)).CellData.ToString();

            //     bool alertAlreadyExisted = false;
            //     DataTable dt = alert.EnumNotifications(operatorName);
            //     if ((dt != null) && (dt.Rows.Count != 0))
            //     {
            //         alertAlreadyExisted = true;
            //     }

            //     bool notifyEmail = IsEmbeededCheckboxChecked(grid, i, Convert.ToInt32(OperatorGridColumn.Email, System.Globalization.CultureInfo.InvariantCulture));
            //     bool notifyPager = IsEmbeededCheckboxChecked(grid, i, Convert.ToInt32(OperatorGridColumn.Pager, System.Globalization.CultureInfo.InvariantCulture));
                
            //     NotifyMethods notifyMethods = NotifyMethods.None;
            //     if (notifyEmail) notifyMethods |= NotifyMethods.NotifyEmail;
            //     if (notifyPager) notifyMethods |= NotifyMethods.Pager;
                
            //     if ((this.notifyOperators.Checked == true) && (notifyMethods != NotifyMethods.None))
            //     {
            //         if (notifyMethods != NotifyMethods.None)
            //         {
            //             if (alertAlreadyExisted)
            //             {
            //                 alert.UpdateNotification(operatorName, notifyMethods);
            //             }
            //             else
            //             {
            //                 alert.AddNotification(operatorName, notifyMethods);
            //             }
            //         }
            //     }
            //     else
            //     {
            //         if (alertAlreadyExisted == true)
            //         {
            //             alert.RemoveNotification(operatorName);
            //         }
            //     }
            // }
        }

        #endregion
    }
}
