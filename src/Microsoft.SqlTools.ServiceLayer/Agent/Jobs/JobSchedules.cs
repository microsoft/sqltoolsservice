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
        #region consts - grid columns
        private const int colScheduleId = 0;
        private const int colScheduleName = 1;
        private const int colScheduleEnabled = 2;
        private const int colScheduleDescription = 3;
        private const int colScheduleJobInSchedule = 4;
        #endregion

        #region members
        private bool sharedSchedulesSupported = false;
        private JobData data;

        // private System.Windows.Forms.Label scheduleLabel;
        // private System.Windows.Forms.Button addSchedule;
        // private System.Windows.Forms.Button editSchedule;
        // private System.Windows.Forms.Button deleteSchedule;
        // private SqlManagerUIDlgGrid scheduleList;
        // private System.Windows.Forms.Button newSchedule;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        #endregion

        #region construction
        public JobSchedules()
        {
            //
            // Required for Windows Form Designer support
            //
            // InitializeComponent();
            // InitializeGrid();
            data = null;
        }

        public JobSchedules(CDataContainer dataContainer, JobData data)
        {
            //
            // Required for Windows Form Designer support
            //
            //InitializeComponent();

            this.DataContainer = dataContainer; // initialize this before grid so grid knows if we have Yukon or not
            this.data = data;
        }
        #endregion

        #region cleanup
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobSchedules));
        //     this.scheduleLabel = new System.Windows.Forms.Label();
        //     this.scheduleList = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
        //     this.addSchedule = new System.Windows.Forms.Button();
        //     this.editSchedule = new System.Windows.Forms.Button();
        //     this.deleteSchedule = new System.Windows.Forms.Button();
        //     this.newSchedule = new System.Windows.Forms.Button();
        //     ((System.ComponentModel.ISupportInitialize)(this.scheduleList)).BeginInit();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // scheduleLabel
        //     // 
        //     resources.ApplyResources(this.scheduleLabel, "scheduleLabel");
        //     this.scheduleLabel.Name = "scheduleLabel";
        //     // 
        //     // scheduleList
        //     // 
        //     resources.ApplyResources(this.scheduleList, "scheduleList");
        //     this.scheduleList.BackColor = System.Drawing.SystemColors.Window;
        //     this.scheduleList.ForceEnabled = false;
        //     this.scheduleList.Name = "scheduleList";
        //     this.scheduleList.SelectionType = Microsoft.SqlServer.Management.UI.Grid.GridSelectionType.RowBlocks;
        //     this.scheduleList.SelectionChanged += new Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventHandler(this.GridSelectionChanged);
        //     this.scheduleList.MouseButtonDoubleClicked += new Microsoft.SqlServer.Management.UI.Grid.MouseButtonDoubleClickedEventHandler(this.OnDoubleClicked);
        //     // 
        //     // addSchedule
        //     // 
        //     resources.ApplyResources(this.addSchedule, "addSchedule");
        //     this.addSchedule.Name = "addSchedule";
        //     this.addSchedule.Click += new System.EventHandler(this.addSchedule_Click);
        //     // 
        //     // editSchedule
        //     // 
        //     resources.ApplyResources(this.editSchedule, "editSchedule");
        //     this.editSchedule.Name = "editSchedule";
        //     this.editSchedule.Click += new System.EventHandler(this.editSchedule_Click);
        //     // 
        //     // deleteSchedule
        //     // 
        //     resources.ApplyResources(this.deleteSchedule, "deleteSchedule");
        //     this.deleteSchedule.Name = "deleteSchedule";
        //     this.deleteSchedule.Click += new System.EventHandler(this.deleteSchedule_Click);
        //     // 
        //     // newSchedule
        //     // 
        //     resources.ApplyResources(this.newSchedule, "newSchedule");
        //     this.newSchedule.Name = "newSchedule";
        //     this.newSchedule.Click += new System.EventHandler(this.newSchedule_Click);
        //     // 
        //     // JobSchedules
        //     // 
        //     this.Controls.Add(this.newSchedule);
        //     this.Controls.Add(this.deleteSchedule);
        //     this.Controls.Add(this.editSchedule);
        //     this.Controls.Add(this.addSchedule);
        //     this.Controls.Add(this.scheduleList);
        //     this.Controls.Add(this.scheduleLabel);
        //     this.Name = "JobSchedules";
        //     resources.ApplyResources(this, "$this");
        //     ((System.ComponentModel.ISupportInitialize)(this.scheduleList)).EndInit();
        //     this.ResumeLayout(false);

        // }
        #endregion

        #region IPanelForm implementation
        // void IPanelForm.OnInitialization()
        // {
        //     System.Diagnostics.Debug.Assert(this.DataContainer != null);
        //     System.Diagnostics.Debug.Assert(this.DataContainer.Server != null);
        //     if (this.DataContainer.Server.Information.Version.Major >= 9)
        //     {
        //         this.sharedSchedulesSupported = true;
        //         this.deleteSchedule.Text = JobSR.ButtonAsRemove;
        //     }
        //     else
        //     {
        //         this.sharedSchedulesSupported = false;
        //         this.deleteSchedule.Text = JobSR.ButtonAsDelete;
        //     }

        //     this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.job.schedules.f1";

        //     InitializeGrid();
        //     InitializeData();
        //     UpdateControlStatus();
        // }
        #endregion

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
        /// <summary>
        /// set up the grid headers etc.
        /// </summary>
        // private void InitializeGrid()
        // {
        //     GridColumnInfo ci = new GridColumnInfo();
        //     // setup th id column
        //     ci.ColumnWidth = 60;
        //     ci.WidthType = GridColumnWidthType.InPixels;
        //     this.scheduleList.AddColumn(ci);
        //     this.scheduleList.SetHeaderInfo(colScheduleId, "ID", null); // $CONSIDER localize the string after Beta 2 string freeze is lifted - apred June 1, 2004
        //     // setup the name column
        //     ci.ColumnWidth = 180;
        //     ci.WidthType = GridColumnWidthType.InPixels;
        //     this.scheduleList.AddColumn(ci);
        //     this.scheduleList.SetHeaderInfo(colScheduleName, JobSR.Name, null);
        //     // setup the enabled column
        //     ci.ColumnWidth = 60;
        //     ci.WidthType = GridColumnWidthType.InPixels;
        //     this.scheduleList.AddColumn(ci);
        //     this.scheduleList.SetHeaderInfo(colScheduleEnabled, JobSR.Enabled, null);
        //     // setup the description column
        //     ci.ColumnWidth = 350;
        //     ci.WidthType = GridColumnWidthType.InPixels;
        //     this.scheduleList.AddColumn(ci);
        //     this.scheduleList.SetHeaderInfo(colScheduleDescription, JobSR.Description, null);
        //     // setup 'jobs in schedule' column (optional)
        //     if (this.sharedSchedulesSupported == true)
        //     {
        //         ci.ColumnWidth = 30;
        //         ci.WidthType = GridColumnWidthType.InPixels;
        //         ci.ColumnType = GridColumnType.Hyperlink;
        //         this.scheduleList.AddColumn(ci);
        //         this.scheduleList.SetHeaderInfo(colScheduleJobInSchedule, JobSR.GridHeader_ScheduleJobsInSchedule, null);
        //     }

        //     this.scheduleList.GridSpecialEvent += new GridSpecialEventHandler(this.grid_GridSpecialEvent); //for hyperlink click
        //     this.scheduleList.KeyPressedOnCell += new KeyPressedOnCellEventHandler(this.grid_KeyPressedOnCell); //for hyperlink handling
        // }
        // private void UpdateControlStatus()
        // {
        //     // check that the selected row is valid
        //     if (this.scheduleList.SelectedCells.Count == 1)
        //     {
        //         if (this.scheduleList.SelectedCells[0].Y > this.scheduleList.RowsNumber - 1 && this.scheduleList.RowsNumber > 0)
        //         {
        //             BlockOfCells[] blocks = new BlockOfCells[1];
        //             blocks[0] = new BlockOfCells(this.scheduleList.RowsNumber - 1, 0);
        //             BlockOfCellsCollection cells = new BlockOfCellsCollection(blocks);
        //             this.scheduleList.SelectedCells = cells;
        //             this.scheduleList.UpdateGrid();
        //         }
        //     }
        //     // ensure that if there are rows in the grid one is always selected
        //     if (this.scheduleList.RowsNumber > 0 && this.scheduleList.SelectedCells.Count == 0)
        //     {
        //         BlockOfCells[] blocks = new BlockOfCells[1];
        //         blocks[0] = new BlockOfCells(0, 0);
        //         BlockOfCellsCollection cells = new BlockOfCellsCollection(blocks);
        //         this.scheduleList.SelectedCells = cells;
        //         this.scheduleList.UpdateGrid();
        //     }

        //     // add enabled only if shared schedules are supported
        //     this.addSchedule.Enabled = (this.sharedSchedulesSupported && !this.data.IsReadOnly);

        //     this.newSchedule.Enabled = (true && !this.data.IsReadOnly);

        //     // edit enabled if one row selected
        //     this.editSchedule.Enabled = (this.scheduleList.RowsNumber > 0
        //                                  && this.scheduleList.SelectedCells.Count == 1);
        //     if (this.data.IsReadOnly)
        //     {
        //         this.editSchedule.Text = JobSR.ScheduleListView;
        //     }

        //     // delete enabled if at least one row selected
        //     this.deleteSchedule.Enabled = (this.scheduleList.RowsNumber > 0
        //                                    && this.scheduleList.SelectedCells.Count > 0) && !this.data.IsReadOnly;
        // }
        #endregion

        #region ui event handlers
        // private void GridSelectionChanged(object sender, SelectionChangedEventArgs args)
        // {
        //     UpdateControlStatus();
        // }

        // private void newSchedule_Click(object sender, System.EventArgs e)
        // {
        //     JobScheduleData scheduleData = new JobScheduleData();


        //     using (ScheduleDialog jobSchedule = new ScheduleDialog(scheduleData, this.data.JobSchedules.Schedules, this.DataContainer, this.ServiceProvider))
        //     {
        //         jobSchedule.Text = JobSR.NewSchedule;
        //         jobSchedule.SetSite(this.ServiceProvider);

        //         if (DialogResult.OK == jobSchedule.ShowDialog())
        //         {
        //             this.data.JobSchedules.AddSchedule(jobSchedule.Schedule);
        //             this.scheduleList.DeleteAllRows();
        //             PopulateGrid(this.data.JobSchedules);
        //             UpdateControlStatus();
        //         }
        //     }
        // }

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

        // private void OnDoubleClicked(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonDoubleClickedEventArgs args)
        // {
        //     // sanity check, may be outside of the visible range
        //     if (args.RowIndex < 0 || args.RowIndex > this.scheduleList.RowsNumber)
        //     {
        //         return;
        //     }

        //     EditSelectedSchedule();
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

        // /// <summary>
        // /// called when user followed hyperlink
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // private void grid_GridSpecialEvent(object sender, GridSpecialEventArgs e)
        // {
        //     if ((e.ColumnIndex == colScheduleJobInSchedule) && (e.EventType == GridSpecialEventArgs.HyperlinkClick))
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
        //     if ((e.ColumnIndex == colScheduleJobInSchedule) && (e.Key == Keys.Space))
        //     {
        //         ShowJobsReferencingScheduleDialog(e.RowIndex);
        //     }
        // }
        #endregion

        #region helper functions
        /// <summary>
        /// display jobs refrencing the schedule for a given row in grid
        /// </summary>
        /// <param name="iRow"></param>
        // private void ShowJobsReferencingScheduleDialog(long lRow)
        // {
        //     System.Diagnostics.Debug.Assert(scheduleList != null);

        //     int iRow = Convert.ToInt32(lRow);
        //     string scheduleName = GetScheduleName(this.scheduleList, iRow);
        //     int scheduleId = GetScheduleId(this.scheduleList, iRow);

        //     // can't do anything if the schedule has not yet been created.
        //     if (scheduleId < 0)
        //     {
        //         return;
        //     }

        //     System.Diagnostics.Debug.Assert(scheduleName != null);
        //     System.Diagnostics.Debug.Assert(scheduleName.Length != 0);
        //     System.Diagnostics.Debug.Assert(scheduleId != -1);

        //     JobsReferencingScheduleForm jrsf = new JobsReferencingScheduleForm
        //                                        (
        //                                        this.DataContainer,
        //                                        scheduleId,
        //                                        scheduleName,
        //                                        true,
        //                                        this.ServiceProvider
        //                                        );
        //     using (jrsf)
        //     {
        //         DialogResult dr = jrsf.ShowDialog();
        //     }
        // }

        /// <summary>
        /// returns name of schedule for a given grid row
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        // private string GetScheduleName(SqlManagerUIDlgGrid grid, int iRow)
        // {
        //     String scheduleName = String.Empty;

        //     System.Diagnostics.Debug.Assert(grid != null);
        //     System.Diagnostics.Debug.Assert(iRow >= 0);
        //     System.Diagnostics.Debug.Assert(iRow < grid.RowsNumber);

        //     GridCell cell = grid.GetCellInfo(iRow, colScheduleName);

        //     System.Diagnostics.Debug.Assert(cell != null);
        //     System.Diagnostics.Debug.Assert(cell.CellData != null);

        //     scheduleName = cell.CellData as String;

        //     System.Diagnostics.Debug.Assert(scheduleName != null);

        //     return scheduleName;
        // }

        /// <summary>
        /// returns id of schedule for a given grid row
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="iRow"></param>
        /// <returns></returns>
        // private int GetScheduleId(SqlManagerUIDlgGrid grid, int iRow)
        // {
        //     int scheduleId = -1;

        //     System.Diagnostics.Debug.Assert(grid != null);
        //     System.Diagnostics.Debug.Assert(iRow >= 0);
        //     System.Diagnostics.Debug.Assert(iRow < grid.RowsNumber);

        //     GridCell cell = grid.GetCellInfo(iRow, colScheduleId);

        //     System.Diagnostics.Debug.Assert(cell != null);
        //     System.Diagnostics.Debug.Assert(cell.CellData != null);

        //     scheduleId = Convert.ToInt32(cell.CellData as String, System.Globalization.CultureInfo.CurrentCulture);

        //     System.Diagnostics.Debug.Assert(scheduleId != -1);

        //     return scheduleId;
        // }

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
        #endregion


    }
}
