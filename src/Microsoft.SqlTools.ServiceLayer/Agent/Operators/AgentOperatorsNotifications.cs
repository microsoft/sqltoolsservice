using Microsoft.SqlServer.Management.Sdk.Sfc;
#region usnig
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Notifications page on Agent Operators Dialog
    /// </summary>
    internal class AgentOperatorsNotifications : AgentConfigurationBase
    {
        #region Internal types

        enum AlertGridColumn
        {
            Name = 0,
            Email,
            Pager,            
        };

        enum JobGridColumn
        {
            Name = 0,
            Email,
            Pager,
        };

        #endregion

        #region Members

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        // private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.Label viewNotificationsLabel;
        // private System.Windows.Forms.Button sendEmail;
        // private System.Windows.Forms.RadioButton alerts;
        // private System.Windows.Forms.RadioButton jobs;
        // private System.Windows.Forms.Label alertsJobsListLabel;

        /// <summary>
        /// Agent operator
        /// </summary>
        private AgentOperatorsData operatorsData = null;
        /// <summary>
        /// true if alerts list was filled or false otherwise
        /// </summary>
        private bool alertsListFilled = false;
        /// <summary>
        /// Alerts list
        /// </summary>
		// private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid alertsList;
        /// <summary>
        /// true if jobs list was filled or false otherwise
        /// </summary>
        private bool jobsListFilled = false;
        /// <summary>
        /// Jobs list
        /// </summary>
		// private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid jobsList;
        #endregion

        #region Constructors

        /// <summary>
        /// Hidden default constructor
        /// </summary>
        private AgentOperatorsNotifications()
        {
            InitializeComponent();
        }

        public AgentOperatorsNotifications(CDataContainer dataContainer, AgentOperatorsData operatorsData)
            : this()
        {
            if(dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }
            if(operatorsData == null)
            {
                throw new ArgumentNullException("operatorsData");
            }
            DataContainer = dataContainer;
            this.operatorsData = operatorsData;

            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.operator.notifications.f1";

            // this.jobs.Enabled = !this.operatorsData.Creating;

            // this.AllUIEnabled = true;

            // this.sendEmail.Visible = false;

            // InitializeAlertsGrid();
            // InitializeJobsGrid();

            // // this.alerts.CheckedChanged += new EventHandler(OnAlertsCheckedChanged);
            // // this.jobs.CheckedChanged += new EventHandler(OnJobsCheckedChanged);
            // this.alertsList.MouseButtonClicked += new MouseButtonClickedEventHandler(OnAlertsListMouseButtonClicked);
        }

        #endregion

        #region IPanelForm

        // void IPanelForm.OnSelection(TreeNode node)
        // {
        // }

        // public override void OnReset(object sender)
        // {
        //     FillAlertsGrid(true);
        //     FillJobsGrid(true);

        //     base.OnReset(sender);
        // }

        // public override void OnGatherUiInformation(RunType action)
        // {
        //     STrace.Assert(this.operatorsData != null);
        //     if(this.operatorsData == null)
        //     {
        //         throw new InvalidOperationException();
        //     }

        //     GridCell gridCell;
        //     AgentAlertNotificationHelper notification;

        //     List<AgentAlertNotificationHelper> notificationList = new List<AgentAlertNotificationHelper>(this.alertsList.RowsNumber);

        //     for(int rowNumber = 0; rowNumber < this.alertsList.RowsNumber; rowNumber++)
        //     {
        //         gridCell = this.alertsList.GetCellInfo(rowNumber, (int)AlertGridColumn.Name);
        //         notification = (AgentAlertNotificationHelper)gridCell.Tag;

        //         gridCell = this.alertsList.GetCellInfo(rowNumber, (int)AlertGridColumn.Email);
        //         notification.NotifyEmail = ((GridCheckBoxState)gridCell.CellData == GridCheckBoxState.Checked);

        //         gridCell = this.alertsList.GetCellInfo(rowNumber, (int)AlertGridColumn.Pager);
        //         notification.NotifyPager = ((GridCheckBoxState)gridCell.CellData == GridCheckBoxState.Checked);
                
        //         notificationList.Add(notification);
        //     }

        //     this.operatorsData.AlertNotifications = notificationList;
        // }

        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {
        // }

        // void IPanelForm.OnInitialization()
        // {
        //     this.alerts.Checked = true;
        // }

        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {
        //         return this;
        //     }
        // }

        #endregion

        #region Overrides

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                // if(components != null)
                // {
                //     components.Dispose();
                // }
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentOperatorsNotifications));
            // this.viewNotificationsLabel = new System.Windows.Forms.Label();
            // this.sendEmail = new System.Windows.Forms.Button();
            // this.alerts = new System.Windows.Forms.RadioButton();
            // this.jobs = new System.Windows.Forms.RadioButton();
            // this.alertsJobsListLabel = new System.Windows.Forms.Label();
            // this.alertsList = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            // this.jobsList = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            // ((System.ComponentModel.ISupportInitialize)(this.alertsList)).BeginInit();
            // ((System.ComponentModel.ISupportInitialize)(this.jobsList)).BeginInit();
            // this.SuspendLayout();
            // this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // viewNotificationsLabel
            // 
            // resources.ApplyResources(this.viewNotificationsLabel, "viewNotificationsLabel");
            // this.viewNotificationsLabel.Name = "viewNotificationsLabel";
            // // 
            // // sendEmail
            // // 
            // resources.ApplyResources(this.sendEmail, "sendEmail");
            // this.sendEmail.Name = "sendEmail";
            // // 
            // // alerts
            // // 
            // resources.ApplyResources(this.alerts, "alerts");
            // this.alerts.Name = "alerts";
            // // 
            // // jobs
            // // 
            // resources.ApplyResources(this.jobs, "jobs");
            // this.jobs.Name = "jobs";
            // // 
            // // alertsJobsListLabel
            // // 
            // resources.ApplyResources(this.alertsJobsListLabel, "alertsJobsListLabel");
            // this.alertsJobsListLabel.Name = "alertsJobsListLabel";
            // // 
            // // alertsList
            // // 
            // resources.ApplyResources(this.alertsList, "alertsList");
            // this.alertsList.BackColor = System.Drawing.SystemColors.Window;
            // this.alertsList.ForceEnabled = false;
            // this.alertsList.Name = "alertsList";
            // // 
            // // jobsList
            // // 
            // resources.ApplyResources(this.jobsList, "jobsList");
            // this.jobsList.BackColor = System.Drawing.SystemColors.Window;
            // this.jobsList.ForceEnabled = false;
            // this.jobsList.Name = "jobsList";
            // // 
            // // AgentOperatorsNotifications
            // // 
            // this.Controls.Add(this.viewNotificationsLabel);
            // this.Controls.Add(this.sendEmail);
            // this.Controls.Add(this.jobs);
            // this.Controls.Add(this.alerts);
            // this.Controls.Add(this.alertsJobsListLabel);
            // this.Controls.Add(this.jobsList);
            // this.Controls.Add(this.alertsList);
            // this.Name = "AgentOperatorsNotifications";
            // resources.ApplyResources(this, "$this");
            // ((System.ComponentModel.ISupportInitialize)(this.alertsList)).EndInit();
            // ((System.ComponentModel.ISupportInitialize)(this.jobsList)).EndInit();
            // this.ResumeLayout(false);

        }
        #endregion

        #region Event handlers

        private void OnAlertsCheckedChanged(object sender, EventArgs e)
        {
            // if(this.alerts.Checked)
            // {
            //     this.alertsJobsListLabel.Text = AgentOperatorsNotificationsSR.AlertList;
            //     FillAlertsGrid(false);
            //     this.jobsList.Visible = false;
            //     this.alertsList.Visible = true;
            //     if (operatorsData.ReadOnly)
            //     {
            //         this.alertsList.Enabled = false;
            //     }
            //     // Select first grid row if nothing is selected
            //     if(this.alertsList.RowsNumber > 0)
            //         this.alertsList.SetSelectedCell(0, 0);
            // }
        }

        private void OnJobsCheckedChanged(object sender, EventArgs e)
        {
            // if(this.jobs.Checked)
            // {
            //     this.alertsJobsListLabel.Text = AgentOperatorsNotificationsSR.JobList;
            //     FillJobsGrid(false);
            //     this.alertsList.Visible = false;
            //     this.jobsList.Visible = true;
            //     if (operatorsData.ReadOnly)
            //     {
            //         this.jobsList.Enabled = false;
            //     }
            //     // Select first grid row if nothing is selected
            //     if(this.alertsList.RowsNumber > 0)
            //         this.alertsList.SetSelectedCell(0, 0);
            // }
        }

        // private void OnAlertsListMouseButtonClicked(object sender, MouseButtonClickedEventArgs args)
        // {
        //     // if(args.Button != MouseButtons.Left)
        //     //     return;

        //     // if(args.ColumnIndex == (int)AlertGridColumn.Email || args.ColumnIndex == (int)AlertGridColumn.Pager )
        //     // {
        //     //     GridCell gridCell = this.alertsList.GetCellInfo((int)args.RowIndex, args.ColumnIndex);
        //     //     GridCheckBoxState state = (GridCheckBoxState)gridCell.CellData;

        //     //     // Invert the cell state
        //     //     if(state == GridCheckBoxState.Checked)
        //     //         gridCell.CellData = GridCheckBoxState.Unchecked;
        //     //     else
        //     //         gridCell.CellData = GridCheckBoxState.Checked;
        //     // }
        // }

        #endregion

        #region Private helpers

        /// <summary>
        /// Fills alertsJobsList grid with alerts from server
        /// </summary>
        void FillAlertsGrid(bool refresh)
        {
            // if(this.alertsListFilled == true && refresh == false)
            //     return; // There is nothing to do here

            // this.alertsList.DeleteAllRows();

            // // Fill grid control with alerts
            // GridCellCollection gridCellCollection;
            // GridCell gridCell;

            // AgentAlertNotificationHelper notification;
            // int numNotifications = this.operatorsData.AlertNotifications.Count;
            // for(int i = 0; i < numNotifications; i++)
            // {
            //     notification = this.operatorsData.AlertNotifications[i];

            //     gridCellCollection = new GridCellCollection();

            //     gridCell = new GridCell(notification.Name);
            //     gridCell.Tag = notification;
            //     gridCellCollection.Add(gridCell);

            //     // Email column
            //     if(notification.NotifyEmail)
            //     {
            //         gridCellCollection.Add(new GridCell(GridCheckBoxState.Checked));
            //     }
            //     else
            //     {
            //         gridCellCollection.Add(new GridCell(GridCheckBoxState.Unchecked));
            //     }
            //     // Pager column
            //     if(notification.NotifyPager)
            //     {
            //         gridCellCollection.Add(new GridCell(GridCheckBoxState.Checked));
            //     }
            //     else
            //     {
            //         gridCellCollection.Add(new GridCell(GridCheckBoxState.Unchecked));
            //     }                

            //     this.alertsList.AddRow(gridCellCollection);
            // }

            // this.alertsListFilled = true;
        }

        /// <summary>
        /// Fills jobsList grid with jobs from server
        /// </summary>
        void FillJobsGrid(bool refresh)
        {
            // if(this.operatorsData.Creating)
            //     return; // Jobs list is disabled if we create new operator

            // if(this.jobsListFilled == true && refresh == false)
            //     return; // We are done

            // this.jobsList.DeleteAllRows();

            // // Fill grid control with alerts
            // GridCellCollection gridCellCollection;

            // AgentJobNotificationHelper notification;
            // int numNotifications = this.operatorsData.JobNotifications.Count;
            // for(int i = 0; i < numNotifications; ++i)
            // {
            //     notification = this.operatorsData.JobNotifications[i];
            //     gridCellCollection = new GridCellCollection();

            //     gridCellCollection.Add(new GridCell(notification.Name));
            //     // Email column
            //     if(notification.NotifyEmail != CompletionAction.Never)
            //     {
            //         gridCellCollection.Add(new GridCell(ConvertToString(notification.NotifyEmail)));
            //     }
            //     else
            //     {
            //         gridCellCollection.Add(new GridCell(String.Empty));
            //     }
            //     // Pager column
            //     if(notification.NotifyPager != CompletionAction.Never)
            //     {
            //         gridCellCollection.Add(new GridCell(ConvertToString(notification.NotifyPager)));
            //     }
            //     else
            //     {
            //         gridCellCollection.Add(new GridCell(String.Empty));
            //     }                

            //     this.jobsList.AddRow(gridCellCollection);
            // }
        }

        /// <summary>
        /// Initialized alerts grid
        /// </summary>
        void InitializeAlertsGrid()
        {
            // GridColumnInfo gridColumnInfo;
            // int alertsListWidth = this.alertsList.ClientRectangle.Width;

            // // Add alert/job name column
            // gridColumnInfo = new GridColumnInfo();
            // gridColumnInfo.ColumnType = GridColumnType.Text;
            // gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
            // gridColumnInfo.ColumnWidth = (int)(Math.Floor(alertsListWidth * 0.58)); // 58%
            // gridColumnInfo.IsUserResizable = true;
            // this.alertsList.AddColumn(gridColumnInfo);
            // this.alertsList.SetHeaderInfo((int)AlertGridColumn.Name, AgentOperatorsNotificationsSR.AlertName, null);
            // gridColumnInfo = new GridColumnInfo();
            // gridColumnInfo.ColumnType = GridColumnType.Checkbox;
            // gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
            // gridColumnInfo.ColumnWidth = (int)(Math.Floor(alertsListWidth * 0.14)); // 14%
            // gridColumnInfo.ColumnAlignment = HorizontalAlignment.Center;
            // gridColumnInfo.HeaderAlignment = HorizontalAlignment.Center;
            // gridColumnInfo.IsUserResizable = true;
            // this.alertsList.AddColumn(gridColumnInfo);
            // this.alertsList.SetHeaderInfo((int)AlertGridColumn.Email, AgentOperatorsNotificationsSR.Email, null);
            // // Add pager column
            // gridColumnInfo = new GridColumnInfo();
            // gridColumnInfo.ColumnType = GridColumnType.Checkbox;
            // gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
            // gridColumnInfo.ColumnWidth = (int)(Math.Floor(alertsListWidth * 0.14)); // 14%
            // gridColumnInfo.ColumnAlignment = HorizontalAlignment.Center;
            // gridColumnInfo.HeaderAlignment = HorizontalAlignment.Center;
            // gridColumnInfo.IsUserResizable = true;            
            // this.alertsList.AddColumn(gridColumnInfo);
            // this.alertsList.SetHeaderInfo((int)AlertGridColumn.Pager, AgentOperatorsNotificationsSR.Pager, null);            
        }

        /// <summary>
        /// Initialized jobs grid
        /// </summary>
        void InitializeJobsGrid()
        {
            // GridColumnInfo gridColumnInfo;
            // int jobsListWidth = this.jobsList.ClientRectangle.Width;

            // // Add job name column
            // gridColumnInfo = new GridColumnInfo();
            // gridColumnInfo.ColumnType = GridColumnType.Text;
            // gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
            // gridColumnInfo.ColumnWidth = (int)(Math.Floor(jobsListWidth * 0.58)); // 58%
            // gridColumnInfo.IsUserResizable = true;
            // this.jobsList.AddColumn(gridColumnInfo);
            // this.jobsList.SetHeaderInfo((int)JobGridColumn.Name, AgentOperatorsNotificationsSR.JobName, null);
            // // Add e-mail column
            // gridColumnInfo = new GridColumnInfo();
            // gridColumnInfo.ColumnType = GridColumnType.Text;
            // gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
            // gridColumnInfo.ColumnWidth = (int)(Math.Floor(jobsListWidth * 0.14)); // 14%
            // gridColumnInfo.IsUserResizable = true;
            // this.jobsList.AddColumn(gridColumnInfo);
            // this.jobsList.SetHeaderInfo((int)JobGridColumn.Email, AgentOperatorsNotificationsSR.Email, null);
            // // Add pager column
            // gridColumnInfo = new GridColumnInfo();
            // gridColumnInfo.ColumnType = GridColumnType.Text;
            // gridColumnInfo.WidthType = GridColumnWidthType.InPixels;
            // gridColumnInfo.ColumnWidth = (int)(Math.Floor(jobsListWidth * 0.14)); // 14%
            // gridColumnInfo.IsUserResizable = true;
            // this.jobsList.AddColumn(gridColumnInfo);
            // this.jobsList.SetHeaderInfo((int)JobGridColumn.Pager, AgentOperatorsNotificationsSR.Pager, null);
            
        }

        string ConvertToString(CompletionAction completionAction)
        {
            switch(completionAction)
            {
                case CompletionAction.Always: return "AgentOperatorsNotificationsSR.Always";
                case CompletionAction.Never: return "AgentOperatorsNotificationsSR.Never";
                case CompletionAction.OnFailure: return "AgentOperatorsNotificationsSR.OnFailure";
                case CompletionAction.OnSuccess: return "AgentOperatorsNotificationsSR.OnSuccess";
                default: return "---";
            }
        }

        #endregion
    }
}
