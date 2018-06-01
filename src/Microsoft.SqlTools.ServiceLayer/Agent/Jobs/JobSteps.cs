//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.SqlManagerUI;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobSteps.
    /// </summary>
    internal class JobSteps : ManagementActionBase
    {
        // private System.Windows.Forms.Label jobStepListLabel;
        // private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid jobStepList;
        // private System.Windows.Forms.Button moveStepUp;
        // private System.Windows.Forms.Button moveStepDown;
        // private System.Windows.Forms.Label moveStepLabel;
        // private System.Windows.Forms.Label startStepLabel;
        // private System.Windows.Forms.ComboBox startStep;
        // private System.Windows.Forms.Button newJob;
        // private System.Windows.Forms.Button insertJob;
        // private System.Windows.Forms.Button editJob;
        // private System.Windows.Forms.Button deleteJob;

        private bool validated = true;
        
        /// <summary>
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;

        public JobSteps()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            InitializeGrid();
            data = null;
        }
        private JobData data;
        public JobSteps(CDataContainer dataContainer, JobData data)
        {
            this.DataContainer = dataContainer;

            // InitializeComponent();
            // InitializeGrid();

            this.data = data;
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobSteps));
            // this.jobStepListLabel = new System.Windows.Forms.Label();
            // this.jobStepList = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            // this.moveStepUp = new System.Windows.Forms.Button();
            // this.moveStepDown = new System.Windows.Forms.Button();
            // this.moveStepLabel = new System.Windows.Forms.Label();
            // this.startStepLabel = new System.Windows.Forms.Label();
            // this.startStep = new System.Windows.Forms.ComboBox();
            // this.newJob = new System.Windows.Forms.Button();
            // this.insertJob = new System.Windows.Forms.Button();
            // this.editJob = new System.Windows.Forms.Button();
            // this.deleteJob = new System.Windows.Forms.Button();
            // ((System.ComponentModel.ISupportInitialize)(this.jobStepList)).BeginInit();
            // this.SuspendLayout();
            // this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // // 
            // // jobStepListLabel
            // // 
            // resources.ApplyResources(this.jobStepListLabel, "jobStepListLabel");
            // this.jobStepListLabel.Name = "jobStepListLabel";
            // // 
            // // jobStepList
            // // 
            // resources.ApplyResources(this.jobStepList, "jobStepList");
            // this.jobStepList.BackColor = System.Drawing.SystemColors.Window;
            // this.jobStepList.ForceEnabled = false;
            // this.jobStepList.Name = "jobStepList";
            // this.jobStepList.SelectionType = Microsoft.SqlServer.Management.UI.Grid.GridSelectionType.RowBlocks;
            // this.jobStepList.SelectionChanged += new Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventHandler(this.GridSelectionChanged);
            // this.jobStepList.MouseButtonDoubleClicked += new Microsoft.SqlServer.Management.UI.Grid.MouseButtonDoubleClickedEventHandler(this.OnDoubleClick);
            // // 
            // // moveStepUp
            // // 
            // resources.ApplyResources(this.moveStepUp, "moveStepUp");
            // this.moveStepUp.Name = "moveStepUp";
            // this.moveStepUp.Click += new System.EventHandler(this.MoveJobStepUp);
            // // 
            // // moveStepDown
            // // 
            // resources.ApplyResources(this.moveStepDown, "moveStepDown");
            // this.moveStepDown.Name = "moveStepDown";
            // this.moveStepDown.Click += new System.EventHandler(this.MoveJobStepDown);
            // // 
            // // moveStepLabel
            // // 
            // resources.ApplyResources(this.moveStepLabel, "moveStepLabel");
            // this.moveStepLabel.Name = "moveStepLabel";
            // // 
            // // startStepLabel
            // // 
            // resources.ApplyResources(this.startStepLabel, "startStepLabel");
            // this.startStepLabel.Name = "startStepLabel";
            // // 
            // // startStep
            // // 
            // resources.ApplyResources(this.startStep, "startStep");
            // this.startStep.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            // this.startStep.FormattingEnabled = true;
            // this.startStep.Name = "startStep";
            // this.startStep.SelectedIndexChanged += new System.EventHandler(this.startStep_SelectedIndexChanged);
            // // 
            // // newJob
            // // 
            // resources.ApplyResources(this.newJob, "newJob");
            // this.newJob.Name = "newJob";
            // this.newJob.Click += new System.EventHandler(this.newJobStep_Click);
            // // 
            // // insertJob
            // // 
            // resources.ApplyResources(this.insertJob, "insertJob");
            // this.insertJob.Name = "insertJob";
            // this.insertJob.Click += new System.EventHandler(this.insertJobStep_Click);
            // // 
            // // editJob
            // // 
            // resources.ApplyResources(this.editJob, "editJob");
            // this.editJob.Name = "editJob";
            // this.editJob.Click += new System.EventHandler(this.editJobStep_Click);
            // // 
            // // deleteJob
            // // 
            // resources.ApplyResources(this.deleteJob, "deleteJob");
            // this.deleteJob.Name = "deleteJob";
            // this.deleteJob.Click += new System.EventHandler(this.deleteJobStep_Click);
            // // 
            // // JobSteps
            // // 
            // this.Controls.Add(this.deleteJob);
            // this.Controls.Add(this.editJob);
            // this.Controls.Add(this.insertJob);
            // this.Controls.Add(this.newJob);
            // this.Controls.Add(this.startStep);
            // this.Controls.Add(this.startStepLabel);
            // this.Controls.Add(this.moveStepLabel);
            // this.Controls.Add(this.moveStepDown);
            // this.Controls.Add(this.moveStepUp);
            // this.Controls.Add(this.jobStepList);
            // this.Controls.Add(this.jobStepListLabel);
            // this.Name = "JobSteps";
            // resources.ApplyResources(this, "$this");
            // ((System.ComponentModel.ISupportInitialize)(this.jobStepList)).EndInit();
            // this.ResumeLayout(false);

        }
        #endregion

        #region IPanelForm implementation
        // void IPanelForm.OnInitialization()
        // {
        //     if (this.data == null)
        //     {
        //         return;
        //     }

        //     // load the grid
        //     PopulateGrid(this.data.JobSteps);

        //     // get events that the step order has changed.
        //     this.data.JobSteps.StepOrderChanged += new EventHandler(StepOrderChanged);

        //     // load the start step combo
        //     PopulateStartStepCombo(this.data.JobSteps);

        //     UpdateControlStatus();
        // }
        #endregion

        #region ISupportValidation
        /// <summary>
        /// Validate the Job step dialog.
        /// This is limited to two checks
        /// 1. See if all steps are reachable
        /// 2. If we are editing rather than creating check to see if the last steps completion
        ///    action will change from GoToNext to QuitWithSuccess.
        /// </summary>
        /// <returns>true if the checks passed, or the user has ok the warning</returns>
        // bool ISupportValidation.Validate()
        // {
        //     // Only validate once after the user has made changes. Validate() is called when the
        //     // user navigates away from the JobSteps page, and if they navigate back and then out
        //     // again they'd be prompted twice. This annoys our users.
        //     if (this.validated)
        //     {
        //         return true;
        //     }
            
        //     bool valid = true;
        //     // Get the unreachable steps
        //     List<JobStepData> unreachableSteps = this.data.JobSteps.FindUnreachableJobSteps();
        //     // see if the last step success completion action will change
        //     bool lastStepWillChange = this.data.JobSteps.CheckIfLastStepCompletionActionWillChange();

        //     // warning message
        //     StringBuilder warningMessage = new StringBuilder();

        //     // if there are unreachable steps, add the warning and each problematic step
        //     if (unreachableSteps.Count > 0)
        //     {
        //         warningMessage.AppendLine(JobSR.UnreachableStepHeader);
        //         foreach (JobStepData jobStep in unreachableSteps)
        //         {
        //             warningMessage.AppendLine(JobSR.UnreachableStepFormat(jobStep.ID, jobStep.Name));
        //         }
        //         warningMessage.AppendLine(String.Empty);
        //     }

        //     // add a warning if the last step will change
        //     if (lastStepWillChange)
        //     {
        //         warningMessage.AppendLine(JobSR.LastStepSuccessWillChange);
        //         warningMessage.AppendLine(String.Empty);
        //     }

        //     // if anything was wrong tell the user and see if they are ok with it
        //     if (warningMessage.Length > 0)
        //     {
        //         warningMessage.Append(JobSR.AreYouSure);

        //         valid = (ShowMessage(warningMessage.ToString(), SRError.SQLWorkbench, ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Warning) != DialogResult.No);
        //     }

        //     this.validated = valid;
        //     return valid;
        // }
        #endregion


        #region ui setup
        // private void PopulateGrid(JobStepsData steps)
        // {
        //     for (int i = 0; i < steps.Steps.Count; i++)
        //     {
        //         JobStepData step = steps.Steps[i] as JobStepData;
        //         if (step != null)
        //         {
        //             // add rows to the grid
        //             GridCellCollection row = new GridCellCollection();
        //             GridCell cell;

        //             // id
        //             cell = new GridCell(step.ID.ToString(CultureInfo.InvariantCulture));
        //             row.Add(cell);
        //             // step name
        //             cell = new GridCell(step.Name);
        //             row.Add(cell);
        //             // subsystem
        //             cell = new GridCell(JobStepSubSystems.LookupFriendlyName(step.SubSystem));
        //             row.Add(cell);
        //             // on success
        //             cell = new GridCell(GetFriendlyNameForAction(step.SuccessAction, step.SuccessStep));
        //             row.Add(cell);
        //             // on failure 
        //             cell = new GridCell(GetFriendlyNameForAction(step.FailureAction, step.FailStep));
        //             row.Add(cell);

        //             this.jobStepList.AddRow(row);
        //         }
        //     }
        // }
        /// <summary>
        /// Convert an action and it's target step to a localizable user friendly name
        /// </summary>
        /// <param name="action"></param>
        /// <param name="targetStep"></param>
        /// <returns></returns>
        private static string GetFriendlyNameForAction(StepCompletionAction action, JobStepData targetStep)
        {
            String friendlyName = String.Empty;
            // switch (action)
            // {
            //     case StepCompletionAction.GoToNextStep:
            //         friendlyName = JobSR.GotoNextStep;
            //         break;
            //     case StepCompletionAction.QuitWithFailure:
            //         friendlyName = JobSR.QuitWithFailure;
            //         break;
            //     case StepCompletionAction.QuitWithSuccess:
            //         friendlyName = JobSR.QuitWithSuccess;
            //         break;
            //     case StepCompletionAction.GoToStep:
            //         STrace.Assert(targetStep != null, "Action type is goto step, but the target step is null");
            //         if (targetStep != null)
            //         {
            //             friendlyName = JobSR.GotoStep(targetStep.ID, targetStep.Name);
            //         }
            //         break;
            //     default:
            //         STrace.Assert(false, "Unknown jobstep completion action");
            //         break;
            // }
            return friendlyName;
        }
        private void PopulateStartStepCombo(JobStepsData steps)
        {
            // clear any existing items
            // this.startStep.Items.Clear();
            // // selected step
            // // add new ones
            // for (int i = 0; i < steps.Steps.Count; i++)
            // {
            //     JobStepData step = steps.Steps[i] as JobStepData;
            //     if (step != null)
            //     {
            //         this.startStep.Items.Add(step);
            //     }
            // }
            // // set the start step
            // if (this.data.JobSteps.StartStep != null)
            // {
            //     this.startStep.SelectedItem = this.data.JobSteps.StartStep;
            // }
        }
        /// <summary>
        /// set up the grid headers etc.
        /// </summary>
        private void InitializeGrid()
        {
            // GridColumnInfo ci = new GridColumnInfo();
            // // setup the Step column
            // ci.ColumnWidth = 35;
            // ci.WidthType = GridColumnWidthType.InPixels;
            // this.jobStepList.AddColumn(ci);
            // this.jobStepList.SetHeaderInfo(0, JobSR.Step, null);
            // // setup the Name column
            // ci.ColumnWidth = 266;
            // ci.WidthType = GridColumnWidthType.InPixels;
            // this.jobStepList.AddColumn(ci);
            // this.jobStepList.SetHeaderInfo(1, JobSR.Name, null);
            // // setup the type column
            // ci.ColumnWidth = 80;
            // ci.WidthType = GridColumnWidthType.InPixels;
            // this.jobStepList.AddColumn(ci);
            // this.jobStepList.SetHeaderInfo(2, JobSR.Type, null);
            // // setup the on success column
            // ci.ColumnWidth = 80;
            // ci.WidthType = GridColumnWidthType.InPixels;
            // this.jobStepList.AddColumn(ci);
            // this.jobStepList.SetHeaderInfo(3, JobSR.OnSuccess, null);
            // // setup the on failure column
            // ci.ColumnWidth = 80;
            // ci.WidthType = GridColumnWidthType.InPixels;
            // this.jobStepList.AddColumn(ci);
            // this.jobStepList.SetHeaderInfo(4, JobSR.OnFailure, null);
        }
        private void UpdateControlStatus()
        {
            // check that the selected row is valid
            // if (this.jobStepList.SelectedCells.Count == 1)
            // {
            //     if (this.jobStepList.SelectedCells[0].Y > this.jobStepList.RowsNumber - 1 && this.jobStepList.RowsNumber > 0)
            //     {
            //         BlockOfCells[] blocks = new BlockOfCells[1];
            //         blocks[0] = new BlockOfCells(this.jobStepList.RowsNumber - 1, 0);
            //         BlockOfCellsCollection cells = new BlockOfCellsCollection(blocks);
            //         this.jobStepList.SelectedCells = cells;
            //         this.jobStepList.UpdateGrid();
            //     }
            // }



            // // ensure that if there are rows in the grid one is always selected
            // if (this.jobStepList.RowsNumber > 0 && this.jobStepList.SelectedCells.Count == 0)
            // {
            //     BlockOfCells[] blocks = new BlockOfCells[1];
            //     blocks[0] = new BlockOfCells(0, 0);
            //     BlockOfCellsCollection cells = new BlockOfCellsCollection(blocks);
            //     this.jobStepList.SelectedCells = cells;
            //     this.jobStepList.UpdateGrid();
            // }

            // // enabled if there is more than one row in the grid, and only one row
            // // is selected.
            // this.moveStepUp.Enabled = ((this.jobStepList.RowsNumber > 1
            //                           && this.jobStepList.SelectedCells.Count == 1
            //                           && this.jobStepList.SelectedCells[0].Y > 0))
            //                           && !this.data.IsReadOnly;
            // this.moveStepDown.Enabled = (this.jobStepList.RowsNumber > 1
            //                           && this.jobStepList.SelectedCells.Count == 1
            //                           && this.jobStepList.SelectedCells[0].Y < (this.jobStepList.RowsNumber - 1))
            //                           && !this.data.IsReadOnly;


            // // start step is enabled if there is at least one step.
            // this.startStep.Enabled = (this.jobStepList.RowsNumber > 0 && !this.data.IsReadOnly);

            // // new is always enabled. (Unless this a MSX job on a TSX server)
            // if (this.data.IsLocalJob) this.newJob.Enabled = true;


            // // insert is enabled if a row is selected.
            // this.insertJob.Enabled = ((this.jobStepList.RowsNumber > 0
            //                           && this.jobStepList.SelectedCells.Count == 1))
            //                           && !this.data.IsReadOnly;

            // // edit is enabled if there is one row selected.
            // this.editJob.Enabled = ((this.jobStepList.RowsNumber > 0
            //                         && this.jobStepList.SelectedCells.Count == 1));

            // // delete is enabled if there are rows selected.
            // this.deleteJob.Enabled = ((this.jobStepList.RowsNumber > 0
            //                            && this.jobStepList.SelectedCells.Count == 1))
            //                            && !this.data.IsReadOnly;

            // ///If this jobSteps are readonly, then we'll need to make the controls on this form read-only.
            // ///We will also need to change the editJob button text to read "View"
            // if (this.data.IsReadOnly)
            // {

            //     SetDialogFieldsReadOnly(this.data.IsReadOnly,
            //        new Control[] { this.moveStepUp,
            //      this.moveStepDown,
            //      this.startStep,
            //      this.newJob,
            //      this.insertJob,
            //      this.deleteJob });
            //     this.editJob.Text = JobSR.JobStepView;
            // }

        }


        #endregion

        // private void GridSelectionChanged(object sender, SelectionChangedEventArgs args)
        // {
        //     UpdateControlStatus();
        // }

        // private void newJobStep_Click(object sender, System.EventArgs e)
        // {
        //     JobStepData data = new JobStepData(this.data.JobSteps);
        //     JobStepPropertySheet jsProp = new JobStepPropertySheet(this.DataContainer, data, this.ServiceProvider);

        //     using (LaunchForm lf = new LaunchForm(jsProp, this.ServiceProvider))
        //     {
        //         lf.ShowDialog();
        //         if (jsProp.DialogResult == DialogResult.OK)
        //         {
        //             this.data.JobSteps.AddStep(data);

        //             this.jobStepList.DeleteAllRows();
        //             PopulateGrid(this.data.JobSteps);
        //             UpdateControlStatus();
        //             this.validated = false;
        //         }
        //     }
        // }

        // private void insertJobStep_Click(object sender, System.EventArgs e)
        // {
        //     JobStepData data = new JobStepData(this.data.JobSteps);
        //     JobStepPropertySheet jsProp = new JobStepPropertySheet(this.DataContainer, data, this.ServiceProvider);

        //     using (LaunchForm lf = new LaunchForm(jsProp, ServiceProvider))
        //     {
        //         lf.ShowDialog();

        //         if (jsProp.DialogResult == DialogResult.OK)
        //         {
        //             int row = (int)this.jobStepList.SelectedCells[0].Y;
        //             this.data.JobSteps.InsertStep(row, data);

        //             this.jobStepList.DeleteAllRows();
        //             PopulateGrid(this.data.JobSteps);
        //             UpdateControlStatus();
        //             this.validated = false;
        //         }
        //     }
        // }

        // private void editJobStep_Click(object sender, System.EventArgs e)
        // {
        //     int row = (int)this.jobStepList.SelectedCells[0].Y;
        //     JobStepData stepData = this.data.JobSteps.Steps[row] as JobStepData;
        //     if (stepData != null)
        //     {
        //         //carry over the IsReadOnly status from JobData
        //         //this.stepData.isReadOnly = this.data.IsReadOnly;
        //         JobStepPropertySheet jsProp = new JobStepPropertySheet(this.DataContainer, stepData, this.ServiceProvider);

        //         using (LaunchForm lf = new LaunchForm(jsProp, ServiceProvider))
        //         {
        //             lf.ShowDialog();

        //             if (jsProp.DialogResult == DialogResult.OK)
        //             {
        //                 this.jobStepList.DeleteAllRows();
        //                 PopulateGrid(this.data.JobSteps);
        //                 UpdateControlStatus();
        //                 this.validated = false;
        //             }
        //         }
        //     }
        // }

        // private void deleteJobStep_Click(object sender, System.EventArgs e)
        // {
        //     int row = (int)this.jobStepList.SelectedCells[0].Y;
        //     JobStepData data = this.data.JobSteps.Steps[row] as JobStepData;
        //     if (data != null)
        //     {
        //         this.data.JobSteps.DeleteStep(data);

        //         this.jobStepList.DeleteAllRows();
        //         PopulateGrid(this.data.JobSteps);
        //         this.validated = false;
        //     }
        //     UpdateControlStatus();
        // }

        // private void startStep_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     JobStepData newStartStep = this.startStep.SelectedItem as JobStepData;
        //     this.data.JobSteps.StartStep = newStartStep;
        //     this.validated = false;
        // }

        // private void MoveJobStepUp(object sender, System.EventArgs e)
        // {
        //     int row = (int)this.jobStepList.SelectedCells[0].Y;
        //     STrace.Assert(row != 0, "Should be able to move the first step up");

        //     // switch the rows
        //     object temp = this.data.JobSteps.Steps[row];
        //     this.data.JobSteps.Steps[row] = this.data.JobSteps.Steps[row - 1];
        //     this.data.JobSteps.Steps[row - 1] = temp;

        //     this.data.JobSteps.RecalculateStepIds();

        //     // update the grid
        //     this.jobStepList.DeleteAllRows();
        //     PopulateGrid(this.data.JobSteps);

        //     // move the currently selected row
        //     long newRow = Math.Max(this.jobStepList.SelectedCells[0].Y - 1L, 0L);
        //     BlockOfCells[] blocks = new BlockOfCells[1];
        //     blocks[0] = new BlockOfCells(newRow, 0);
        //     BlockOfCellsCollection cells = new BlockOfCellsCollection(blocks);
        //     this.jobStepList.SelectedCells = cells;

        //     // ensure it's visible
        //     this.jobStepList.EnsureCellIsVisible(newRow, 0);
        //     // update the ui
        //     this.jobStepList.UpdateGrid();
        //     UpdateControlStatus();
        //     this.validated = false;
        // }
        
        // private void MoveJobStepDown(object sender, System.EventArgs e)
        // {
        //     int row = (int)this.jobStepList.SelectedCells[0].Y;
        //     STrace.Assert(row != (int)this.jobStepList.RowsNumber - 1, "Should be able to move the last step down");

        //     // switch the rows
        //     object temp = this.data.JobSteps.Steps[row];
        //     this.data.JobSteps.Steps[row] = this.data.JobSteps.Steps[row + 1];
        //     this.data.JobSteps.Steps[row + 1] = temp;

        //     this.data.JobSteps.RecalculateStepIds();

        //     // update the grid
        //     this.jobStepList.DeleteAllRows();
        //     PopulateGrid(this.data.JobSteps);

        //     // move the currently selected row
        //     long newRow = Math.Min(this.jobStepList.SelectedCells[0].Y + 1L, this.jobStepList.RowsNumber - 1);
        //     BlockOfCells[] blocks = new BlockOfCells[1];
        //     blocks[0] = new BlockOfCells(newRow, 0);
        //     BlockOfCellsCollection cells = new BlockOfCellsCollection(blocks);
        //     this.jobStepList.SelectedCells = cells;

        //     // ensure it's visible
        //     this.jobStepList.EnsureCellIsVisible(newRow, 0);
        //     // update the ui
        //     this.jobStepList.UpdateGrid();
        //     UpdateControlStatus();
        //     this.validated = false;
        // }
        // #endregion

        // private void StepOrderChanged(object sender, EventArgs e)
        // {
        //     // repopulate the combo.
        //     PopulateStartStepCombo(this.data.JobSteps);
        //     this.validated = false;
        // }

        // private void OnDoubleClick(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonDoubleClickedEventArgs args)
        // {
        //     // sanity check, may be outside of the visible range
        //     if (args.RowIndex < 0 || args.RowIndex > this.jobStepList.RowsNumber)
        //     {
        //         return;
        //     }

        //     int row = (int)args.RowIndex;
        //     JobStepData data = this.data.JobSteps.Steps[row] as JobStepData;
        //     if (data != null)
        //     {
        //         JobStepPropertySheet jsProp = new JobStepPropertySheet(this.DataContainer, data, this.ServiceProvider);

        //         using (LaunchForm lf = new LaunchForm(jsProp, ServiceProvider))
        //         {
        //             lf.ShowDialog();

        //             if (jsProp.DialogResult == DialogResult.OK)
        //             {
        //                 this.jobStepList.DeleteAllRows();
        //                 PopulateGrid(this.data.JobSteps);
        //                 UpdateControlStatus();
        //             }
        //         }
        //     }
        // }
    }
}








