//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    internal class JobStepsActions : ManagementActionBase
    {
        private bool validated = true;

        private JobData data;

        public JobStepsActions(CDataContainer dataContainer, JobData data)
        {
            this.DataContainer = dataContainer;
            this.data = data;
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
            }

            this.validated = valid;
            return valid;
        }
        #endregion


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
        // private static string GetFriendlyNameForAction(StepCompletionAction action, JobStepData targetStep)
        // {
        //     String friendlyName = String.Empty;
        //     // switch (action)
        //     // {
        //     //     case StepCompletionAction.GoToNextStep:
        //     //         friendlyName = JobSR.GotoNextStep;
        //     //         break;
        //     //     case StepCompletionAction.QuitWithFailure:
        //     //         friendlyName = JobSR.QuitWithFailure;
        //     //         break;
        //     //     case StepCompletionAction.QuitWithSuccess:
        //     //         friendlyName = JobSR.QuitWithSuccess;
        //     //         break;
        //     //     case StepCompletionAction.GoToStep:
        //     //         STrace.Assert(targetStep != null, "Action type is goto step, but the target step is null");
        //     //         if (targetStep != null)
        //     //         {
        //     //             friendlyName = JobSR.GotoStep(targetStep.ID, targetStep.Name);
        //     //         }
        //     //         break;
        //     //     default:
        //     //         STrace.Assert(false, "Unknown jobstep completion action");
        //     //         break;
        //     // }
        //     return friendlyName;
        // }
      

        public void CreateJobStep()
        {
            //JobStepData data = new JobStepData(this.data.JobSteps);
            JobStepData data = this.data.JobSteps.Steps[0] as JobStepData;
            JobStepPropertySheet jsProp = new JobStepPropertySheet(this.DataContainer, data);

            jsProp.Init();
            jsProp.Create();
        }
    }
}
