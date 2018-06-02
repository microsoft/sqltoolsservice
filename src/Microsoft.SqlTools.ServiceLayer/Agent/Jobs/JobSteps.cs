//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobSteps.
    /// </summary>
    internal class JobSteps : ManagementActionBase
    {
        private bool validated = true;
        
        private JobData data;

        public JobSteps(CDataContainer dataContainer, JobData data)
        {
            this.DataContainer = dataContainer;
            this.data = data;

           // PopulateGrid(this.data.JobSteps);
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

        #region ISupportValidation
        /// <summary>
        /// Validate the Job step data.
        /// This is limited to two checks
        /// 1. See if all steps are reachable
        /// 2. If we are editing rather than creating check to see if the last steps completion
        ///    action will change from GoToNext to QuitWithSuccess.
        /// </summary>
        /// <returns>true if the checks passed, or the user has ok the warning</returns>
        public bool Validate()
        {
            // Only validate once after the user has made changes. Validate() is called when the
            // user navigates away from the JobSteps page, and if they navigate back and then out
            // again they'd be prompted twice. This annoys our users.
            if (this.validated)
            {
                return true;
            }
            
            bool valid = true;
            // Get the unreachable steps
            List<JobStepData> unreachableSteps = this.data.JobSteps.FindUnreachableJobSteps();
            // see if the last step success completion action will change
            bool lastStepWillChange = this.data.JobSteps.CheckIfLastStepCompletionActionWillChange();

            // warning message
            StringBuilder warningMessage = new StringBuilder();

            // if there are unreachable steps, add the warning and each problematic step
            if (unreachableSteps.Count > 0)
            {
                warningMessage.AppendLine("JobSR.UnreachableStepHeader");
                foreach (JobStepData jobStep in unreachableSteps)
                {
                    warningMessage.AppendLine("JobSR.UnreachableStepFormat(jobStep.ID, jobStep.Name)");
                }
                warningMessage.AppendLine(string.Empty);
            }

            // add a warning if the last step will change
            if (lastStepWillChange)
            {
                warningMessage.AppendLine("JobSR.LastStepSuccessWillChange");
                warningMessage.AppendLine(string.Empty);
            }

            // if anything was wrong tell the user and see if they are ok with it
            if (warningMessage.Length > 0)
            {
                warningMessage.Append("JobSR.AreYouSure");

                //valid = (ShowMessage(warningMessage.ToString(), SRError.SQLWorkbench, ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Warning) != DialogResult.No);
            }

            this.validated = valid;
            return valid;
        }
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

        #endregion

        public void CreateJobStep()
        {
            JobStepData data = new JobStepData(this.data.JobSteps);
            JobStepPropertySheet jsProp = new JobStepPropertySheet(this.DataContainer, data);

            jsProp.Init();
            jsProp.Create();
        }


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
