using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace Microsoft.SqlServer.Management.SqlManagerUI
{
    /// <summary>
    /// Summary description for JobsReferencingScheduleControl.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class JobsReferencingScheduleControl : ManagementActionBase
    {
#region Constants
        private static double[] columnRatios = new double[] {
            0.10,
            0.35,
            0.10,
            0.35,
            0.90
        };

        private const int colJobSelected = 0;
        private const int colJobName = 1;
        private const int colJobEnabled = 2;
        private const int colJobCategory = 3;
        private const int colJobDescription = 4;

        /// <summary>
        /// column that stores inside the cell.Tag and SMO Job object
        /// </summary>
        private const int colTagSmoObject = colJobName;
#endregion

#region UI Variables
        private System.Windows.Forms.Panel panelEntireUserInterface;
        private System.Windows.Forms.Label labelStaticSchedule;
        private System.Windows.Forms.TextBox textBoxSchedule;
        private System.Windows.Forms.Label labelStaticSelectJobs;
        private System.Windows.Forms.Panel panelGridContainer;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
#endregion

#region Non-UI Variables
        private bool panelInitialized = false;
        private int m_scheduleId = -1; // used to unique identify the schedule (since duplicate schedule names are allowed)
        private string m_scheduleName = null;
        private bool m_readOnlyMode = false;
        private Urn jobUrn = null;
#endregion

#region Public - ApplyChanges() , NoOfSelectedJobs
        private int m_noOfSelectedJobs = -1;
        public int NoOfSelectedJobs
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_grid != null);
                // System.Diagnostics.Debug.Assert(m_noOfSelectedJobs != -1, "ask for this property only after changes were applied");
                return m_noOfSelectedJobs;
            }
        }

        /// <summary>
        /// called by form container - tells control to apply user changes
        /// </summary>
        public void ApplyChanges()
        {
            SendDataToServer();
        }
#endregion

#region Constructors/Dispose
        /// <summary>
        /// constructor used so Win Forms designer can work
        /// </summary>
        public JobsReferencingScheduleControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            CreateGrid();
            InitializeGridColumns();
        }


        /// <summary>
        /// initialization - passing context (server and actual schedule to be displayed)
        /// 
        /// urn from context points to the actual schedule being managed
        /// </summary>
        /// <param name="context"></param>
        public JobsReferencingScheduleControl(CDataContainer context)
        {
            System.Diagnostics.Debug.Assert(context != null);

            m_scheduleName = context.ObjectName;
            m_readOnlyMode = false;

            DataContainer = context;

            InitializeComponent();
            CreateGrid();

            IPanelForm ip = this as IPanelForm;

            ip.OnInitialization();
            ip.OnSelection(null);
        }

        /// <summary>
        /// initialization - passing context (server and actual schedule to be displayed)
        /// 
        /// urn from context points to some generic context from which a parent dialog was launched (Manage Schedules/Schedule Properties/New Job/Job Properties)
        /// string urnSchedule points to actual schedule 
        /// </summary>
        /// <param name="context">context as it came from object explorer</param>
        /// <param name="scheduleId">unique schedule id (since name cannot be used for identification - schedules can have duplicate names - e.g. the one used by Replication team)</param>
        /// <param name="scheduleName">friendly name of schedule (used for display purposes)</param>
        /// <param name="readOnlyMode">true if dialog is diplayed in read/only mode</param>
        public JobsReferencingScheduleControl(CDataContainer context, int scheduleId, string scheduleName, bool readOnlyMode)
        {
            System.Diagnostics.Debug.Assert(context != null);
            System.Diagnostics.Debug.Assert(scheduleName != null);
            System.Diagnostics.Debug.Assert(scheduleName.Length > 0);

            m_scheduleId = scheduleId;
            m_scheduleName = scheduleName;
            m_readOnlyMode = readOnlyMode;

            InitializeComponent();
            CreateGrid();

            DataContainer = context;

            IPanelForm ip = this as IPanelForm;

            ip.OnInitialization();
            ip.OnSelection(null);

            this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + ".ag.jobsreferencingaschedule.f1";
        }

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

#region Implementation LoadData, InitProp
        /// <summary>
        /// reads context - ensures we have
        ///        valid context
        ///        valid server connection
        ///        valid schedule name
        /// </summary>
        private void LoadData()
        {
            System.Diagnostics.Debug.Assert(DataContainer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server != null);
            System.Diagnostics.Debug.Assert(m_scheduleName != null);
            System.Diagnostics.Debug.Assert(m_scheduleId != -1);

            string urnString = String.Empty;

            STParameters param = new STParameters();
            param.SetDocument(DataContainer.Document);

            param.GetParam("joburn", ref urnString);

            if (urnString != null)
            {
                this.jobUrn = new Urn(urnString);
            }
        }

        /// <summary>
        /// create and initialize ui with real data
        /// </summary>
        private void InitProp()
        {
            InitializeGridColumns();
            ResetUIToInitialValues();
        }
#endregion

#region ResetUIToInitialValues, SendDataToServer
        /// <summary>
        /// initialize ui with data
        /// </summary>
        private void ResetUIToInitialValues()
        {
            FillGridWithData();
            UpdateDialogTitle();
        }

        /// <summary>
        /// applies changes
        /// </summary>
        private void SendDataToServer()
        {
            System.Diagnostics.Debug.Assert(m_grid != null);
            System.Diagnostics.Debug.Assert(DataContainer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer.SharedSchedules != null);
            System.Diagnostics.Debug.Assert(m_scheduleName != null);
            System.Diagnostics.Debug.Assert(m_scheduleId != -1);

            if (m_readOnlyMode == true)
            {
                return;
            }

            JobSchedule schedule = DataContainer.Server.JobServer.SharedSchedules.ItemById(m_scheduleId);
            System.Diagnostics.Debug.Assert(schedule != null);
            if (schedule == null)
            {
                return; // schedule deleted meanwhile
            }

            m_noOfSelectedJobs = 0;
            for (int iRow = 0; iRow < m_grid.RowsNumber; ++iRow)
            {
                bool isSelected = IsEmbeededCheckboxChecked(m_grid, iRow, colJobSelected);
                bool isEnabled = IsEmbeededCheckboxChecked(m_grid, iRow, colJobEnabled);

                if (isSelected)
                {
                    m_noOfSelectedJobs++;
                }

                Job job = GetJobTag(m_grid, iRow);
                bool wasSelected = GetWasSelectedTag(m_grid, iRow);

                bool alterRequired = false;

                if (isEnabled != job.IsEnabled)
                {
                    job.IsEnabled = isEnabled;
                    alterRequired = true;
                }

                if (wasSelected && !isSelected)
                {
                    // deselect
                    // Dont remove unused schedules automatically. Let user explicitly delete it from UI if needed
                    job.RemoveSharedSchedule(schedule.ID, true);
                    alterRequired = true;
                }

                if (!wasSelected && isSelected)
                {
                    // select
                    job.AddSharedSchedule(schedule.ID);
                    alterRequired = true;
                }

                if (alterRequired == true)
                {
                    job.Alter();
                }
            }
        }
#endregion

#region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(JobsReferencingScheduleControl));
            this.panelEntireUserInterface = new System.Windows.Forms.Panel();
            this.labelStaticSchedule = new System.Windows.Forms.Label();
            this.textBoxSchedule = new System.Windows.Forms.TextBox();
            this.labelStaticSelectJobs = new System.Windows.Forms.Label();
            this.panelGridContainer = new System.Windows.Forms.Panel();
            this.panelEntireUserInterface.SuspendLayout();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // panelEntireUserInterface
            // 
            this.panelEntireUserInterface.AccessibleDescription = resources.GetString("panelEntireUserInterface.AccessibleDescription");
            this.panelEntireUserInterface.AccessibleName = resources.GetString("panelEntireUserInterface.AccessibleName");
            this.panelEntireUserInterface.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("panelEntireUserInterface.Anchor")));
            this.panelEntireUserInterface.AutoScroll = ((bool)(resources.GetObject("panelEntireUserInterface.AutoScroll")));
            this.panelEntireUserInterface.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("panelEntireUserInterface.AutoScrollMargin")));
            this.panelEntireUserInterface.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("panelEntireUserInterface.AutoScrollMinSize")));
            this.panelEntireUserInterface.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelEntireUserInterface.BackgroundImage")));
            this.panelEntireUserInterface.Controls.Add(this.panelGridContainer);
            this.panelEntireUserInterface.Controls.Add(this.labelStaticSelectJobs);
            this.panelEntireUserInterface.Controls.Add(this.textBoxSchedule);
            this.panelEntireUserInterface.Controls.Add(this.labelStaticSchedule);
            this.panelEntireUserInterface.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("panelEntireUserInterface.Dock")));
            this.panelEntireUserInterface.Enabled = ((bool)(resources.GetObject("panelEntireUserInterface.Enabled")));
            this.panelEntireUserInterface.Font = ((System.Drawing.Font)(resources.GetObject("panelEntireUserInterface.Font")));
            this.panelEntireUserInterface.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("panelEntireUserInterface.ImeMode")));
            this.panelEntireUserInterface.Location = ((System.Drawing.Point)(resources.GetObject("panelEntireUserInterface.Location")));
            this.panelEntireUserInterface.Name = "panelEntireUserInterface";
            this.panelEntireUserInterface.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("panelEntireUserInterface.RightToLeft")));
            this.panelEntireUserInterface.Size = ((System.Drawing.Size)(resources.GetObject("panelEntireUserInterface.Size")));
            this.panelEntireUserInterface.TabIndex = ((int)(resources.GetObject("panelEntireUserInterface.TabIndex")));
            this.panelEntireUserInterface.Text = resources.GetString("panelEntireUserInterface.Text");
            this.panelEntireUserInterface.Visible = ((bool)(resources.GetObject("panelEntireUserInterface.Visible")));
            // 
            // labelStaticSchedule
            // 
            this.labelStaticSchedule.AccessibleDescription = resources.GetString("labelStaticSchedule.AccessibleDescription");
            this.labelStaticSchedule.AccessibleName = resources.GetString("labelStaticSchedule.AccessibleName");
            this.labelStaticSchedule.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("labelStaticSchedule.Anchor")));
            this.labelStaticSchedule.AutoSize = ((bool)(resources.GetObject("labelStaticSchedule.AutoSize")));
            this.labelStaticSchedule.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("labelStaticSchedule.Dock")));
            this.labelStaticSchedule.Enabled = ((bool)(resources.GetObject("labelStaticSchedule.Enabled")));
            this.labelStaticSchedule.Font = ((System.Drawing.Font)(resources.GetObject("labelStaticSchedule.Font")));
            this.labelStaticSchedule.Image = ((System.Drawing.Image)(resources.GetObject("labelStaticSchedule.Image")));
            this.labelStaticSchedule.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("labelStaticSchedule.ImageAlign")));
            this.labelStaticSchedule.ImageIndex = ((int)(resources.GetObject("labelStaticSchedule.ImageIndex")));
            this.labelStaticSchedule.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("labelStaticSchedule.ImeMode")));
            this.labelStaticSchedule.Location = ((System.Drawing.Point)(resources.GetObject("labelStaticSchedule.Location")));
            this.labelStaticSchedule.Name = "labelStaticSchedule";
            this.labelStaticSchedule.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("labelStaticSchedule.RightToLeft")));
            this.labelStaticSchedule.Size = ((System.Drawing.Size)(resources.GetObject("labelStaticSchedule.Size")));
            this.labelStaticSchedule.TabIndex = ((int)(resources.GetObject("labelStaticSchedule.TabIndex")));
            this.labelStaticSchedule.Text = resources.GetString("labelStaticSchedule.Text");
            this.labelStaticSchedule.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("labelStaticSchedule.TextAlign")));
            this.labelStaticSchedule.Visible = ((bool)(resources.GetObject("labelStaticSchedule.Visible")));
            // 
            // textBoxSchedule
            // 
            this.textBoxSchedule.AccessibleDescription = resources.GetString("textBoxSchedule.AccessibleDescription");
            this.textBoxSchedule.AccessibleName = resources.GetString("textBoxSchedule.AccessibleName");
            this.textBoxSchedule.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("textBoxSchedule.Anchor")));
            this.textBoxSchedule.AutoSize = ((bool)(resources.GetObject("textBoxSchedule.AutoSize")));
            this.textBoxSchedule.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("textBoxSchedule.BackgroundImage")));
            this.textBoxSchedule.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("textBoxSchedule.Dock")));
            this.textBoxSchedule.Enabled = ((bool)(resources.GetObject("textBoxSchedule.Enabled")));
            this.textBoxSchedule.Font = ((System.Drawing.Font)(resources.GetObject("textBoxSchedule.Font")));
            this.textBoxSchedule.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("textBoxSchedule.ImeMode")));
            this.textBoxSchedule.Location = ((System.Drawing.Point)(resources.GetObject("textBoxSchedule.Location")));
            this.textBoxSchedule.MaxLength = ((int)(resources.GetObject("textBoxSchedule.MaxLength")));
            this.textBoxSchedule.Multiline = ((bool)(resources.GetObject("textBoxSchedule.Multiline")));
            this.textBoxSchedule.Name = "textBoxSchedule";
            this.textBoxSchedule.PasswordChar = ((char)(resources.GetObject("textBoxSchedule.PasswordChar")));
            this.textBoxSchedule.ReadOnly = true;
            this.textBoxSchedule.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("textBoxSchedule.RightToLeft")));
            this.textBoxSchedule.ScrollBars = ((System.Windows.Forms.ScrollBars)(resources.GetObject("textBoxSchedule.ScrollBars")));
            this.textBoxSchedule.Size = ((System.Drawing.Size)(resources.GetObject("textBoxSchedule.Size")));
            this.textBoxSchedule.TabIndex = ((int)(resources.GetObject("textBoxSchedule.TabIndex")));
            this.textBoxSchedule.Text = resources.GetString("textBoxSchedule.Text");
            this.textBoxSchedule.TextAlign = ((System.Windows.Forms.HorizontalAlignment)(resources.GetObject("textBoxSchedule.TextAlign")));
            this.textBoxSchedule.Visible = ((bool)(resources.GetObject("textBoxSchedule.Visible")));
            this.textBoxSchedule.WordWrap = ((bool)(resources.GetObject("textBoxSchedule.WordWrap")));
            // 
            // labelStaticSelectJobs
            // 
            this.labelStaticSelectJobs.AccessibleDescription = resources.GetString("labelStaticSelectJobs.AccessibleDescription");
            this.labelStaticSelectJobs.AccessibleName = resources.GetString("labelStaticSelectJobs.AccessibleName");
            this.labelStaticSelectJobs.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("labelStaticSelectJobs.Anchor")));
            this.labelStaticSelectJobs.AutoSize = ((bool)(resources.GetObject("labelStaticSelectJobs.AutoSize")));
            this.labelStaticSelectJobs.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("labelStaticSelectJobs.Dock")));
            this.labelStaticSelectJobs.Enabled = ((bool)(resources.GetObject("labelStaticSelectJobs.Enabled")));
            this.labelStaticSelectJobs.Font = ((System.Drawing.Font)(resources.GetObject("labelStaticSelectJobs.Font")));
            this.labelStaticSelectJobs.Image = ((System.Drawing.Image)(resources.GetObject("labelStaticSelectJobs.Image")));
            this.labelStaticSelectJobs.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("labelStaticSelectJobs.ImageAlign")));
            this.labelStaticSelectJobs.ImageIndex = ((int)(resources.GetObject("labelStaticSelectJobs.ImageIndex")));
            this.labelStaticSelectJobs.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("labelStaticSelectJobs.ImeMode")));
            this.labelStaticSelectJobs.Location = ((System.Drawing.Point)(resources.GetObject("labelStaticSelectJobs.Location")));
            this.labelStaticSelectJobs.Name = "labelStaticSelectJobs";
            this.labelStaticSelectJobs.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("labelStaticSelectJobs.RightToLeft")));
            this.labelStaticSelectJobs.Size = ((System.Drawing.Size)(resources.GetObject("labelStaticSelectJobs.Size")));
            this.labelStaticSelectJobs.TabIndex = ((int)(resources.GetObject("labelStaticSelectJobs.TabIndex")));
            this.labelStaticSelectJobs.Text = resources.GetString("labelStaticSelectJobs.Text");
            this.labelStaticSelectJobs.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("labelStaticSelectJobs.TextAlign")));
            this.labelStaticSelectJobs.Visible = ((bool)(resources.GetObject("labelStaticSelectJobs.Visible")));
            // 
            // panelGridContainer
            // 
            this.panelGridContainer.AccessibleDescription = resources.GetString("panelGridContainer.AccessibleDescription");
            this.panelGridContainer.AccessibleName = resources.GetString("panelGridContainer.AccessibleName");
            this.panelGridContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("panelGridContainer.Anchor")));
            this.panelGridContainer.AutoScroll = ((bool)(resources.GetObject("panelGridContainer.AutoScroll")));
            this.panelGridContainer.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("panelGridContainer.AutoScrollMargin")));
            this.panelGridContainer.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("panelGridContainer.AutoScrollMinSize")));
            this.panelGridContainer.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelGridContainer.BackgroundImage")));
            this.panelGridContainer.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("panelGridContainer.Dock")));
            this.panelGridContainer.Enabled = ((bool)(resources.GetObject("panelGridContainer.Enabled")));
            this.panelGridContainer.Font = ((System.Drawing.Font)(resources.GetObject("panelGridContainer.Font")));
            this.panelGridContainer.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("panelGridContainer.ImeMode")));
            this.panelGridContainer.Location = ((System.Drawing.Point)(resources.GetObject("panelGridContainer.Location")));
            this.panelGridContainer.Name = "panelGridContainer";
            this.panelGridContainer.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("panelGridContainer.RightToLeft")));
            this.panelGridContainer.Size = ((System.Drawing.Size)(resources.GetObject("panelGridContainer.Size")));
            this.panelGridContainer.TabIndex = ((int)(resources.GetObject("panelGridContainer.TabIndex")));
            this.panelGridContainer.Text = resources.GetString("panelGridContainer.Text");
            this.panelGridContainer.Visible = ((bool)(resources.GetObject("panelGridContainer.Visible")));
            // 
            // JobsReferencingScheduleControl
            // 
            this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
            this.AccessibleName = resources.GetString("$this.AccessibleName");
            this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
            this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
            this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.Controls.Add(this.panelEntireUserInterface);
            this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
            this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
            this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
            this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
            this.Name = "JobsReferencingScheduleControl";
            this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
            this.Size = ((System.Drawing.Size)(resources.GetObject("$this.Size")));
            this.panelEntireUserInterface.ResumeLayout(false);
            this.ResumeLayout(false);

        }
#endregion

#region IPanelForm interface

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of Panel property
        /// </summary>
        UserControl IPanelForm.Panel
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnSelection
        /// </summary>
        void IPanelForm.OnSelection(TreeNode node)
        {
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnPanelLoseSelection
        /// </summary>
        /// <param name="node"></param>
        void IPanelForm.OnPanelLoseSelection(TreeNode node)
        {
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnReset
        /// </summary>
        /// <param name="node"></param>
        public override void OnReset(object sender)
        {
            base.OnReset(sender);
            if (this.panelInitialized)
            {
                ResetUIToInitialValues();
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnInitialization
        /// </summary>
        void IPanelForm.OnInitialization()
        {
            if (this.panelInitialized == true)
            {
                return;
            }

            this.panelInitialized = true;
            try
            {
                this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + ".ag.jobsreferencingaschedule.f1";

                LoadData();
                InitProp();

                IPanelForm panelform = this as IPanelForm;
                panelform.Panel.Enabled = true;
            }
            catch
            {
                IPanelForm panelform = this as IPanelForm;
                panelform.Panel.Enabled = false;

                throw;
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            base.OnRunNow(sender);
            if (this.panelInitialized)
            {
                SendDataToServer();
            }
        }
#endregion

#region Grid Operations
        private SqlManagerUIDlgGrid m_grid = null;

        /// <summary>
        /// dinamically create a grid control
        /// 
        /// normally this would have been done via winforms designer
        /// but due to Whidbey migration we had to made it this way
        /// </summary>
        private void CreateGrid()
        {
            System.Diagnostics.Debug.Assert(m_grid == null);

            m_grid = new SqlManagerUIDlgGrid();
            m_grid.Dock = DockStyle.Fill;
            m_grid.MouseButtonClicked += new Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventHandler(this.grid_MouseButtonClicked);

            try
            {
                this.SuspendLayout();
                this.panelGridContainer.SuspendLayout();
                this.panelGridContainer.Controls.Clear();
                this.panelGridContainer.Controls.Add(m_grid);
            }
            finally
            {
                this.panelGridContainer.ResumeLayout();
                this.ResumeLayout();
            }
        }

        /// <summary>
        /// initialze grid columns
        /// </summary>
        private void InitializeGridColumns()
        {
            System.Diagnostics.Debug.Assert(m_grid != null);
            Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

            while (grid.ColumnsNumber != 0)
            {
                grid.DeleteColumn(0);
            }

            GridColumnInfo colInfo = null;
            int i = 0;

            foreach (double fColumnRatio in columnRatios)
            {
                colInfo = new GridColumnInfo();
                colInfo.ColumnWidth = (int)((double)grid.Width * fColumnRatio);
                colInfo.WidthType = GridColumnWidthType.InPixels;
                switch (i)
                {
                    case colJobSelected:
                        colInfo.ColumnType = GridColumnType.Checkbox;
                        break;

                    case colJobEnabled:
                        colInfo.ColumnType = GridColumnType.Checkbox;
                        break;

                    default:
                        break;
                }
                grid.AddColumn(colInfo);
                ++i;
            }

            grid.SetHeaderInfo(colJobSelected, SR.GridHeader_JobSelected, null);
            grid.SetHeaderInfo(colJobName, SR.GridHeader_JobName, null);
            grid.SetHeaderInfo(colJobEnabled, SR.GridHeader_JobEnabled, null);
            grid.SetHeaderInfo(colJobCategory, SR.GridHeader_JobCategory, null);
            grid.SetHeaderInfo(colJobDescription, SR.GridHeader_JobDescription, null);

            grid.EnableSortingByColumn(colJobSelected);
            grid.EnableSortingByColumn(colJobName);
            grid.EnableSortingByColumn(colJobEnabled);
            grid.EnableSortingByColumn(colJobCategory);
            grid.EnableSortingByColumn(colJobDescription);

            grid.FirstScrollableColumn = colJobEnabled;
            grid.SelectionType = GridSelectionType.SingleRow;
            grid.UpdateGrid();
        }

        /// <summary>
        /// fills grid with data
        /// 
        /// iterates the available jobs and if they reference the schedule adds them to grid
        /// </summary>
        private void FillGridWithData()
        {
            System.Diagnostics.Debug.Assert(m_grid != null);
            System.Diagnostics.Debug.Assert(m_scheduleName != null);
            System.Diagnostics.Debug.Assert(m_scheduleName.Length > 0);
            System.Diagnostics.Debug.Assert(m_scheduleId != -1);

            System.Diagnostics.Debug.Assert(DataContainer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer.Jobs != null);

            Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

            grid.DeleteAllRows();

            Dictionary<Job, bool> jobs = new Dictionary<Job, bool>(DataContainer.Server.JobServer.Jobs.Count);

            JobSchedule schedule = DataContainer.Server.JobServer.SharedSchedules.ItemById(m_scheduleId);

            // if schedule object returned was null, it is possible that schedule specified in scheduledata 
            // is not yet created on server
            if (null == schedule)
            {
                return;
            }

            Guid[] jobGuids = schedule.EnumJobReferences(); // Note that this call is expensive
                                                            // since it uses a cursor

            foreach (Guid guid in jobGuids)
            {
                Job job = DataContainer.Server.JobServer.Jobs.ItemById(guid);            
                System.Diagnostics.Debug.Assert(job != null);
                jobs.Add(job, true);
            }

            if (!m_readOnlyMode)
            {
                // If we're not in readonly mode, we need to list all jobs.
                // ensure we have latest info - what are the available jobs
                DataContainer.Server.JobServer.Jobs.Refresh();
                foreach (Job job in DataContainer.Server.JobServer.Jobs)
                {
                    // If this job wasn't already in our dictionary, then we know it doesn't use
                    // this schedule. Add it as a new, unselected entry.
                    if (!jobs.ContainsKey(job))
                    {
                        jobs.Add(job, false);
                    }
                }
            }

            foreach (KeyValuePair<Job, bool> jobInfo in jobs)
            {
                AddGridRowForJob(jobInfo.Key, jobInfo.Value);
            }

            m_grid.SortByColumn(colJobName, SortingColumnState.Ascending);
            
            if ((grid.SelectedRow < 0) && (grid.RowsNumber > 0))
            {
                grid.SelectedRow = 0;
            }
        }

        /// <summary>
        /// adds a new row to the grid
        /// </summary>
        private void AddGridRowForJob(Job job, bool selected)
        {
            System.Diagnostics.Debug.Assert(m_grid != null);
            System.Diagnostics.Debug.Assert(job != null);

            if (selected == false && m_readOnlyMode == true)
            {
                // in read-only mode we display only jobs that
                // are referencing schedule dialog
                return;
            }

            Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

            GridCellCollection row = new GridCellCollection();
            GridCell cell;

            if (job.Urn == this.jobUrn)
            {
                return;
            }

            cell = new GridCell
                (
                 (m_readOnlyMode == true) ?
                 (selected ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Disabled) :
                 (selected ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked)
                 ); 
            cell.Tag = selected ? job : null; row.Add(cell); // selected (all by default) - TAGGED with initial selected value
            cell = new GridCell(job.Name); cell.Tag = job; row.Add(cell); // name - TAGGED with Job object
            cell = new GridCell
                (
                 (m_readOnlyMode == true) ?
                 (job.IsEnabled ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Disabled) :
                 (job.IsEnabled ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked)
                 );
            row.Add(cell); // enabled

            LocalizableCategory catLocalized = new LocalizableCategory(job.CategoryID, job.Category);
            cell = new GridCell(catLocalized.Name); row.Add(cell); // category
            cell = new GridCell(job.Description); row.Add(cell); // description

            grid.AddRow(row);
        }

        /// <summary>
        /// flips on/off checkboxes from grid
        /// </summary>
        /// <param name="rowsno"></param>
        /// <param name="colno"></param>
        void FlipCheckbox(SqlManagerUIDlgGrid grid, int rowno, int colno)
        {
            // get the storage for the cell
            GridCell cell = grid.GetCellInfo(rowno, colno);
            GridCheckBoxState state = (GridCheckBoxState)cell.CellData;

            // explicitly invert the cell state
            switch (state)
            {
                case GridCheckBoxState.Checked:
                    cell.CellData = GridCheckBoxState.Unchecked;
                    break;

                case GridCheckBoxState.Unchecked:
                    cell.CellData = GridCheckBoxState.Checked;
                    break;

                case GridCheckBoxState.Indeterminate:
                    cell.CellData = GridCheckBoxState.Checked;
                    break;

                case GridCheckBoxState.None:
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false, "unknown checkbox state");
                    break;
            }
        }

        /// <summary>
        /// gets status of checkbox
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="rowno"></param>
        /// <param name="colno"></param>
        /// <returns></returns>
        bool IsEmbeededCheckboxChecked(SqlManagerUIDlgGrid grid, int rowno, int colno)
        {
            // get the storage for the cell
            GridCell cell = grid.GetCellInfo(rowno, colno);
            GridCheckBoxState state = (GridCheckBoxState)cell.CellData;

            return (state == GridCheckBoxState.Checked);
        }

        Job GetJobTag(Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid, int iRow)
        {
            System.Diagnostics.Debug.Assert(grid != null);
            System.Diagnostics.Debug.Assert(iRow >= 0);
            System.Diagnostics.Debug.Assert(iRow < grid.RowsNumber);

            GridCell cell = grid.GetCellInfo(iRow, colTagSmoObject);

            System.Diagnostics.Debug.Assert(cell != null);
            System.Diagnostics.Debug.Assert(cell.Tag != null);

            Job job = cell.Tag as Job;

            System.Diagnostics.Debug.Assert(job != null);
            return job;
        }

        bool GetWasSelectedTag(Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid, int iRow)
        {
            System.Diagnostics.Debug.Assert(grid != null);
            System.Diagnostics.Debug.Assert(iRow >= 0);
            System.Diagnostics.Debug.Assert(iRow < grid.RowsNumber);

            GridCell cell = grid.GetCellInfo(iRow, colJobSelected);

            System.Diagnostics.Debug.Assert(cell != null);

            object o = cell.Tag;
            return o != null;
        }
#endregion

#region Grid Events
        /// <summary>
        /// user clicked - we flip checkboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void grid_MouseButtonClicked(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventArgs args)
        {
            System.Diagnostics.Debug.Assert(m_grid != null);
            Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            if (m_readOnlyMode == true)
            {
                return;
            }

            int rowno = Convert.ToInt32(args.RowIndex);
            int colno = Convert.ToInt32(args.ColumnIndex);

            switch (colno)
            {
                case colJobSelected: // flip checkbox
                    FlipCheckbox(grid, rowno, colno);
                    break;

                case colJobEnabled: // flip checkbox
                    FlipCheckbox(grid, rowno, colno);
                    break;

                default: // else do default action: e.g. edit - open combo - etc ...
                    // grid.StartCellEdit(rowno,colno);
                    // grid.OnMouseButtonClicked(rowno, colno, args.CellRect, args.Button);
                    break;
            }
        }
#endregion

#region Helpers
        private void UpdateDialogTitle()
        {
            System.Diagnostics.Debug.Assert(m_scheduleName != null);
            System.Diagnostics.Debug.Assert(m_scheduleId != -1);

            this.Text = SR.DialogTitle_JobsReferencingSchedule(m_scheduleName);
            this.textBoxSchedule.Text = m_scheduleName;
        }
#endregion
    }
}
