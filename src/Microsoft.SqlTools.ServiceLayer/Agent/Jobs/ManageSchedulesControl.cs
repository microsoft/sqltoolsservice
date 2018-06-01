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
    //public class ManageSchedulesControl : UserControl
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class ManageSchedulesControl : ManagementActionBase
    {
        #region Constants
        private static double[] columnRatios = new double[] {
            0.10,
            0.30,
            0.10,
            0.40,
            0.10
        };

        private const int colScheduleId = 0;
        private const int colScheduleName = 1;
        private const int colScheduleEnabled = 2;
        private const int colScheduleDescription = 3;
        private const int colScheduleNoJobsReferenced = 4;

        /// <summary>
        /// column that stores inside the cell.Tag an SMO JobSchedule object
        /// </summary>
        private const int colTagSmoObject = colScheduleName;
        #endregion

        #region UI Variables
        // private System.Windows.Forms.Panel panelEntireControl;
        // private System.Windows.Forms.Panel panelGridContainer;
        // private System.Windows.Forms.Button buttonDelete;
        // private System.Windows.Forms.Button buttonProperties;
        // private System.Windows.Forms.Label labelAvailableSchedules;
        // /// <summary> 
        // /// Required designer variable.
        // /// </summary>
        // private System.ComponentModel.Container components = null;
        #endregion

        #region Non-UI Variables
        private bool panelInitialized = false;

        private ArrayList m_schedulesMarkedForDelete = null;
        // private System.Windows.Forms.Button buttonNew;

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

        /// <summary>
        /// we cannot set base.ServiceProvider since it is r/o
        /// so we will use an alternate service provider while
        /// we are in 'Pick Schedule Mode' (non-dbCommander)
        /// </summary>
        private IServiceProvider m_serviceProviderInPickScheduleMode = null;
        #endregion

        #region Constructors/Dispose
        public ManageSchedulesControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // CreateGrid();
        }

        /// <summary>
        /// initialization - passing context (coming from OE)
        /// 
        /// display in "Manage Schedules" mode
        /// </summary>
        /// <param name="context"></param>
        public ManageSchedulesControl(CDataContainer context)
        {
            System.Diagnostics.Debug.Assert(context != null);

            InitializeComponent();
            //CreateGrid();

            DataContainer = context;

            // buttonNew.Enabled = true;
            // buttonNew.Visible = true;
            // buttonNew.Click += new EventHandler(OnNewSharedSchedule);

            // IPanelForm ip = this as IPanelForm;

            // ip.OnInitialization();
            // ip.OnSelection(null);
        }


        /// <summary>
        /// initialization - passing context (coming from parent dialog)
        /// 
        /// display in "Pick up a schedule for job" mode
        /// </summary>
        /// <param name="context"></param>
        public ManageSchedulesControl(CDataContainer context, string jobName, IServiceProvider svcProvider)
        {
            System.Diagnostics.Debug.Assert(context != null);

            m_serviceProviderInPickScheduleMode = svcProvider;
            m_jobName = jobName;

            InitializeComponent();
            //CreateGrid();

            DataContainer = context;

            // IPanelForm ip = this as IPanelForm;

            // ip.OnInitialization();
            // ip.OnSelection(null);
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

        #region Public
        // public JobSchedule SelectedSchedule
        // {
        //     get
        //     {
        //         System.Diagnostics.Debug.Assert(m_jobName != null, "property should be used in 'Pickup a schedule for job' mode - not in 'Manage schedule' mode");
        //         System.Diagnostics.Debug.Assert(m_grid != null);

        //         Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;
        //         int iRow = grid.SelectedRow;

        //         if(iRow < 0)
        //         {
        //             return null; // nothing selected
        //         }

        //         JobSchedule schedule = GetScheduleTag(grid, iRow);

        //         System.Diagnostics.Debug.Assert(schedule != null);
        //         return schedule;
        //     }
        // }

        // public string SelectedScheduleDescription
        // {
        //     get
        //     {
        //         return this.GetScheduleDescription(this.SelectedSchedule); 
        //     }
        // }
        #endregion

        #region Implementation LoadData, InitProp
        /// <summary>
        /// reads context - ensures we have
        ///     valid context
        ///     valid server connection
        /// </summary>
        private void LoadData()
        {
            System.Diagnostics.Debug.Assert(DataContainer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server != null);
        }

        /// <summary>
        /// create and initialize ui with real data
        /// </summary>
        private void InitProp()
        {
            // InitializeButtons(); 
            // InitializeGridColumns();
            InitializeExcludedSchedules();
            ResetUIToInitialValues();
        }

        /// <summary>
        /// Get the list of any schedules that should not be shown in the UI.
        /// </summary>
        private void InitializeExcludedSchedules()
        {        
            if(DataContainer == null)
            {
                throw new InvalidOperationException();
            }

            string excludedIdList = String.Empty;
            string removedIdList = String.Empty;

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
        #endregion

        #region ResetUIToInitialValues, SendDataToServer
        /// <summary>
        /// initialize ui with data
        /// </summary>
        private void ResetUIToInitialValues()
        {
            // FillGridWithData();
            // UpdateDialogTitle();
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
        #endregion

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {            
        }
        #endregion

        #region IPanelForm interface

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of Panel property
        /// </summary>
        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {
        //         return this;
        //     }
        // }

        // /// <summary>
        // /// interface IPanelForm
        // /// 
        // /// implementation of OnSelection
        // /// </summary>
        // void IPanelForm.OnSelection(TreeNode node)
        // {
        // }

        // /// <summary>
        // /// interface IPanelForm
        // /// 
        // /// implementation of OnPanelLoseSelection
        // /// </summary>
        // /// <param name="node"></param>
        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {
        // }

        // /// <summary>
        // /// interface IPanelForm
        // /// 
        // /// implementation of OnReset
        // /// </summary>
        // /// <param name="node"></param>
        // public override void OnReset(object sender)
        // {
        //     base.OnReset(sender);
        //     if(this.panelInitialized)
        //     {
        //         ResetUIToInitialValues();
        //     }
        // }

        // /// <summary>
        // /// interface IPanelForm
        // /// 
        // /// implementation of OnInitialization
        // /// </summary>
        // void IPanelForm.OnInitialization()
        // {
        //     if(this.panelInitialized == true)
        //     {
        //         return;
        //     }

        //     this.panelInitialized = true;
        //     try
        //     {
        //         this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + ".ag.job.manageschedules.f1";
        //         LoadData();
        //         InitProp();

        //         IPanelForm panelform = this as IPanelForm;

        //         panelform.Panel.Enabled = true;
        //     }
        //     catch
        //     {
        //         IPanelForm panelform = this as IPanelForm;

        //         panelform.Panel.Enabled = false;
        //         throw;
        //     }
        // }

        // /// <summary>
        // /// interface IPanelForm
        // /// 
        // /// implementation of OnPanelRunNow
        // /// </summary>
        // /// <param name="node"></param>
        // public override void OnRunNow(object sender)
        // {
        //     base.OnRunNow(sender);
        //     if(this.panelInitialized)
        //     {
        //         SendDataToServer();
        //     }
        // }
        #endregion

        #region Grid Operations
        //private SqlManagerUIDlgGrid m_grid = null;

        /// <summary>
        /// dinamically create a grid control
        /// 
        /// normally this would have been done via winforms designer
        /// but due to Whidbey migration we had to made it this way
        /// </summary>
        // private void CreateGrid()
        // {
        //     System.Diagnostics.Debug.Assert(m_grid == null);

        //     m_grid = new SqlManagerUIDlgGrid();
        //     m_grid.Dock = DockStyle.Fill;
        //     m_grid.MouseButtonClicked += new Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventHandler(this.grid_MouseButtonClicked);
        //     m_grid.SelectionChanged += new SelectionChangedEventHandler(this.grid_SelectionChanged);
        //     m_grid.GridSpecialEvent += new GridSpecialEventHandler(this.grid_GridSpecialEvent); //for hyperlink click
        //     m_grid.KeyPressedOnCell += new KeyPressedOnCellEventHandler(this.grid_KeyPressedOnCell); //for hyperlink handling
        //     try
        //     {
        //         this.SuspendLayout();
        //         this.panelGridContainer.SuspendLayout();
        //         this.panelGridContainer.Controls.Clear();
        //         this.panelGridContainer.Controls.Add(m_grid);
        //     }
        //     finally
        //     {
        //         this.panelGridContainer.ResumeLayout();
        //         this.ResumeLayout();
        //     }
        // }

        // /// <summary>
        // /// initialze grid columns
        // /// </summary>
        // private void InitializeGridColumns()
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);

        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

        //     while(grid.ColumnsNumber != 0)
        //     {
        //         grid.DeleteColumn(0);
        //     }

        //     GridColumnInfo colInfo = null;
        //     int i = 0;

        //     foreach(double fColumnRatio in columnRatios)
        //     {
        //         colInfo = new GridColumnInfo();
        //         colInfo.ColumnWidth = (int)((double)grid.Width * fColumnRatio);
        //         colInfo.WidthType = GridColumnWidthType.InPixels;
        //         switch(i)
        //         {
        //             case colScheduleEnabled:
        //                 colInfo.ColumnType = GridColumnType.Checkbox;
        //                 break;

        //             case colScheduleNoJobsReferenced:
        //                 colInfo.ColumnType = GridColumnType.Hyperlink;
        //                 break;

        //             default:
        //                 break;
        //         }
        //         grid.AddColumn(colInfo);
        //         ++i;
        //     }

        //     grid.SetHeaderInfo(colScheduleId, SR.GridHeader_ID, null);
        //     grid.SetHeaderInfo(colScheduleName, SR.GridHeader_ScheduleName, null);
        //     grid.SetHeaderInfo(colScheduleEnabled, SR.GridHeader_ScheduleEnabled, null);
        //     grid.SetHeaderInfo(colScheduleDescription, SR.GridHeader_ScheduleDescription, null);
        //     grid.SetHeaderInfo(colScheduleNoJobsReferenced, SR.GridHeader_ScheduleJobsInSchedule, null);

        //     grid.EnableSortingByColumn(colScheduleId);
        //     grid.EnableSortingByColumn(colScheduleName);
        //     grid.EnableSortingByColumn(colScheduleEnabled);
        //     grid.EnableSortingByColumn(colScheduleDescription);
        //     grid.EnableSortingByColumn(colScheduleNoJobsReferenced);

        //     grid.FirstScrollableColumn = colScheduleEnabled;
        //     grid.SelectionType = GridSelectionType.SingleRow;
        //     grid.UpdateGrid();
        // }

        // /// <summary>
        // /// fills grid with data
        // /// 
        // /// iterates the shared schedules available on agent and creates a row for each one of them
        // /// </summary>
        // private void FillGridWithData()
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);

        //     System.Diagnostics.Debug.Assert(DataContainer != null);
        //     System.Diagnostics.Debug.Assert(DataContainer.Server != null);
        //     System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer != null);
        //     System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer.Jobs != null);
        //     System.Diagnostics.Debug.Assert(DataContainer.Server.JobServer.SharedSchedules != null);

        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

        //     grid.DeleteAllRows();

        //     // For performance, we want the initial query for JobSchedule to return all fields
        //     // because we will use them all here and in the JobScheduleData class. This prevents
        //     // spurious extra sql queries later when we need those properties.
        //     StringCollection originalFields = DataContainer.Server.GetDefaultInitFields(typeof(JobSchedule));

        //     try
        //     {
        //         // Overwrite the default fields to grab all JobSchedule properties
        //         DataContainer.Server.SetDefaultInitFields(typeof(JobSchedule), true);
        //         foreach (JobSchedule schedule in DataContainer.Server.JobServer.SharedSchedules)
        //         {
        //             System.Diagnostics.Debug.Assert(schedule != null);
        //             if (excludedScheduleId.Count == 0 || excludedScheduleId.BinarySearch(schedule.ID) < 0)
        //             {
        //                 AddGridRowForJob(schedule);
        //             }
        //         }
        //     }
        //     finally
        //     {
        //         // Restore the original fields that were in use before we did our lookup
        //         DataContainer.Server.SetDefaultInitFields(typeof(JobSchedule), originalFields);
        //     }

        //     m_schedulesMarkedForDelete = new ArrayList();

        //     // select the first one if it exists
        //     if (grid.RowsNumber > 0)
        //     {
        //         grid.SetSelectedCell(0, 0);
        //         this.buttonDelete.Enabled = true;
        //         this.buttonProperties.Enabled = true;
        //     }
        // }

        // /// <summary>
        // /// adds a new row to the grid
        // /// </summary>
        // private void AddGridRowForJob(JobSchedule schedule)
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);
        //     System.Diagnostics.Debug.Assert(schedule != null);

        //     string scheduleDescription = GetScheduleDescription(schedule); // create a ScheduleData and do .ToString() on it
        //     System.Diagnostics.Debug.Assert(scheduleDescription != null);

        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

        //     GridCellCollection row = new GridCellCollection();
        //     GridCell cell;

        //     cell = new GridCell(Convert.ToString(schedule.ID, System.Globalization.CultureInfo.CurrentCulture)); row.Add(cell); // id
        //     cell = new GridCell(schedule.Name); cell.Tag = schedule; row.Add(cell); // name
        //     cell = new GridCell
        //         (
        //         (m_jobName == null) ? // null if r/w - non-null if r/o
        //         (schedule.IsEnabled ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked) :
        //         (schedule.IsEnabled ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Disabled)
        //         ); row.Add(cell); // enabled
        //     cell = new GridCell(scheduleDescription); row.Add(cell); // description

        //     int jobsInSchedule = schedule.JobCount;
        //     if(this.removedScheduleId.BinarySearch(schedule.ID) >= 0)
        //     {
        //         jobsInSchedule = Math.Max(0, jobsInSchedule - 1);
        //     }
        //     cell = new GridCell(Convert.ToString(jobsInSchedule, System.Globalization.CultureInfo.InvariantCulture)); row.Add(cell); // job count

        //     grid.AddRow(row);
        // }

        // /// <summary>
        // /// flips on/off checkboxes from grid
        // /// </summary>
        // /// <param name="rowsno"></param>
        // /// <param name="colno"></param>
        // void FlipCheckbox(SqlManagerUIDlgGrid grid, int rowno, int colno)
        // {
        //     // get the storage for the cell
        //     GridCell cell = grid.GetCellInfo(rowno, colno);
        //     GridCheckBoxState state = (GridCheckBoxState)cell.CellData;

        //     // explicitly invert the cell state
        //     switch(state)
        //     {
        //         case GridCheckBoxState.Checked:
        //             cell.CellData = GridCheckBoxState.Unchecked;
        //             break;

        //         case GridCheckBoxState.Unchecked:
        //             cell.CellData = GridCheckBoxState.Checked;
        //             break;

        //         case GridCheckBoxState.Indeterminate:
        //             cell.CellData = GridCheckBoxState.Checked;
        //             break;

        //         case GridCheckBoxState.None:
        //             break;

        //         default:
        //             System.Diagnostics.Debug.Assert(false, "unknown checkbox state");
        //             break;
        //     }
        // }

        // /// <summary>
        // /// gets status of checkbox
        // /// </summary>
        // /// <param name="grid"></param>
        // /// <param name="rowno"></param>
        // /// <param name="colno"></param>
        // /// <returns></returns>
        // bool IsEmbeededCheckboxChecked(SqlManagerUIDlgGrid grid, int rowno, int colno)
        // {
        //     // get the storage for the cell
        //     GridCell cell = grid.GetCellInfo(rowno, colno);
        //     GridCheckBoxState state = (GridCheckBoxState)cell.CellData;

        //     return (state == GridCheckBoxState.Checked);
        // }

        // JobSchedule GetScheduleTag(Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid, int iRow)
        // {
        //     System.Diagnostics.Debug.Assert(grid != null);
        //     System.Diagnostics.Debug.Assert(iRow >= 0);
        //     System.Diagnostics.Debug.Assert(iRow < grid.RowsNumber);

        //     GridCell cell = grid.GetCellInfo(iRow, colTagSmoObject);

        //     System.Diagnostics.Debug.Assert(cell != null);
        //     System.Diagnostics.Debug.Assert(cell.Tag != null);

        //     JobSchedule schedule = cell.Tag as JobSchedule;

        //     System.Diagnostics.Debug.Assert(schedule != null);
        //     return schedule;
        // }
        // #endregion

        // #region Grid Events
        // /// <summary>
        // /// user clicked - we flip checkboxes
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="args"></param>
        // private void grid_MouseButtonClicked(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventArgs args)
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);
        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;

        //     if(m_jobName != null)
        //     {
        //         // read only when in 'Pick a schedule' mode so dont flip anything
        //         return;
        //     }

        //     if(args.Button != MouseButtons.Left)
        //     {
        //         return;
        //     }

        //     int rowno = Convert.ToInt32(args.RowIndex);
        //     int colno = Convert.ToInt32(args.ColumnIndex);

        //     switch(colno)
        //     {
        //         case colScheduleEnabled: // flip checkbox
        //             FlipCheckbox(grid, rowno, colno);
        //             break;

        //         default: // else do default action: e.g. edit - open combo - etc ...
        //             // grid.StartCellEdit(rowno,colno);
        //             // grid.OnMouseButtonClicked(rowno, colno, args.CellRect, args.Button);
        //             break;
        //     }
        // }

        // /// <summary>
        // /// selection changed - enable/disable delete button (depending if there are jobs referencing this item or not)
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="args"></param>
        // private void grid_SelectionChanged(object sender, SelectionChangedEventArgs args)
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);

        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;
        //     int iRow = grid.SelectedRow;

        //     if(iRow < 0)
        //     {
        //         this.buttonDelete.Enabled = false;
        //         this.buttonProperties.Enabled = false;

        //         return; // nothing selected
        //     }

        //     JobSchedule schedule = GetScheduleTag(grid, iRow);

        //     this.buttonDelete.Enabled = true;
        //     this.buttonProperties.Enabled = true;
        // }

        // /// <summary>
        // /// called when user followed hyperlink
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // private void grid_GridSpecialEvent(object sender, GridSpecialEventArgs e)
        // {
        //     if((e.ColumnIndex == colScheduleNoJobsReferenced) && (e.EventType == GridSpecialEventArgs.HyperlinkClick))
        //     {
        //         ShowJobsReferencingScheduleDialog(e.RowIndex);
        //     }
        // }

        // /// <summary>
        // /// called when user pressed a key on grid's cell
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // private void grid_KeyPressedOnCell(object sender, KeyPressedOnCellEventArgs e)
        // {
        //     if((e.ColumnIndex == colScheduleNoJobsReferenced) && (e.Key == Keys.Space))
        //     {
        //         ShowJobsReferencingScheduleDialog(e.RowIndex);
        //     }
        // }

        // #endregion

        // #region Helpers - Get Shared Schedule Description, some extra Initialization
        // private string GetScheduleDescription(JobSchedule schedule)
        // {
        //     SimpleJobSchedule sjs = SimpleJobSchedule.FromJobScheduleData
        //                       (
        //                       new JobScheduleData(schedule)
        //                       );
        //     string description = sjs.ToString();

        //     return description != null ? description : String.Empty;
        // }

        // private void UpdateDialogTitle()
        // {
        //     if(m_jobName != null)
        //     {
        //         this.Text = SR.DialogTitle_PickScheduleForJob(m_jobName);
        //     }
        //     else
        //     {
        //         this.Icon = ResourceUtils.LoadIcon("ScheduleJob.ico");
        //         this.Text = SR.DialogTitle_ManageSchedules;
        //     }
        // }

        // private void InitializeButtons()
        // {
        //     this.buttonDelete.Visible = (m_jobName == null); // false if in 'pick schedule' mode
        //     this.buttonDelete.Enabled = false; // initialy nothing selected
        //     this.buttonProperties.Enabled = false; // initailly nothing selected

        //     this.buttonDelete.Click += new EventHandler(this.OnDeleteClicked);
        //     this.buttonProperties.Click += new EventHandler(this.OnPropertiesClicked);
        // }

        // /// <summary>
        // /// display jobs refrencing the schedule for a given row in grid
        // /// </summary>
        // /// <param name="iRow"></param>
        // private void ShowJobsReferencingScheduleDialog(long lRow)
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);

        //     int iRow = Convert.ToInt32(lRow);
        //     JobSchedule schedule = GetScheduleTag(m_grid, iRow);

        //     System.Diagnostics.Debug.Assert(schedule != null);
        //     System.Diagnostics.Debug.Assert(schedule.Name != null);
        //     System.Diagnostics.Debug.Assert(schedule.Name.Length > 0);

        //     JobsReferencingScheduleForm jrsf = new JobsReferencingScheduleForm
        //                                 (
        //                                 this.DataContainer,
        //                                 schedule.ID,
        //                                 schedule.Name,
        //                                 m_jobName != null,
        //                                 (m_serviceProviderInPickScheduleMode != null) ? m_serviceProviderInPickScheduleMode : this.ServiceProvider // maybe null in 'Pick Schedule' mode
        //         );
        //     using(jrsf)
        //     {
        //         DialogResult dr = jrsf.ShowDialog();

        //         if(dr == DialogResult.OK)
        //         {
        //             schedule.Refresh(); // shared schedule was modified by removing/adding jobs

        //             GridCell cell = null;

        //             cell = m_grid.GetCellInfo(iRow, colScheduleNoJobsReferenced);
        //             cell.CellData = jrsf.NoOfSelectedJobs.ToString(System.Globalization.CultureInfo.InvariantCulture);
        //             m_grid.SetCellInfo(iRow, colScheduleNoJobsReferenced, cell);
        //         }
        //     }
        // }
        // #endregion

        // #region Button Events
        // /// <summary>
        // /// on 'Delete' clicked we mark current schedule for deletion
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="args"></param>
        // private void OnDeleteClicked(object sender, EventArgs args)
        // {
        //     System.Diagnostics.Debug.Assert(m_schedulesMarkedForDelete != null);
        //     System.Diagnostics.Debug.Assert(m_grid != null);

        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;
        //     int iRow = grid.SelectedRow;

        //     if(iRow < 0)
        //     {
        //         return; // nothing selected
        //     }

        //     JobSchedule schedule = GetScheduleTag(grid, iRow);

        //     if(schedule.JobCount != 0)
        //     {
        //         string message = SR.SharedSchedules_DeleteScheduleReferencedByJobs_Message;
        //         Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons buttons = Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.YesNo;
        //         DialogResult dr = MessageBoxProvider.ShowMessage(message, SR.SharedSchedules, buttons, Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Warning, this);

        //         if(dr != DialogResult.Yes)
        //         {
        //             return;
        //         }
        //     }

        //     m_schedulesMarkedForDelete.Add(schedule);
        //     grid.DeleteRow(iRow);
        // }

        // /// <summary>
        // /// on properties clicked we display Schedule Properties dialog
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="args"></param>
        // private void OnPropertiesClicked(object sender, EventArgs args)
        // {
        //     System.Diagnostics.Debug.Assert(m_grid != null);

        //     Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = m_grid;
        //     int iRow = grid.SelectedRow;

        //     if(iRow < 0)
        //     {
        //         System.Diagnostics.Debug.Assert(false, "'Properties' button should have been disabled since nothing was selected");
        //         return; // nothing selected
        //     }

        //     JobSchedule schedule = GetScheduleTag(grid, iRow);

        //     ScheduleDialog dlg = new ScheduleDialog(schedule, this.DataContainer); // applyOnClose = true
        //     dlg.SetSite((m_serviceProviderInPickScheduleMode != null) ? m_serviceProviderInPickScheduleMode : this.ServiceProvider);
        //     using(dlg)
        //     {
        //         DialogResult dr = dlg.ShowDialog(this);

        //         if(dr == DialogResult.OK)
        //         {
        //             System.Diagnostics.Debug.Assert(dlg.Schedule != null);
        //             SimpleJobSchedule sjs = SimpleJobSchedule.FromJobScheduleData(dlg.Schedule);

        //             GridCell cell = null;

        //             string name = sjs.Name;

        //             cell = grid.GetCellInfo(iRow, colScheduleName);
        //             cell.CellData = name;
        //             grid.SetCellInfo(iRow, colScheduleName, cell);

        //             string description = sjs.ToString();

        //             cell = grid.GetCellInfo(iRow, colScheduleDescription);
        //             cell.CellData = description;
        //             grid.SetCellInfo(iRow, colScheduleDescription, cell);

        //             bool isEnabled = sjs.IsEnabled;

        //             cell = grid.GetCellInfo(iRow, colScheduleEnabled);
        //             cell.CellData = isEnabled ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;
        //             grid.SetCellInfo(iRow, colScheduleEnabled, cell);
        //         }
        //     }
        // }

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
