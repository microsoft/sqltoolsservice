//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepProperties.
    /// </summary>
    internal class JobStepProperties : ManagementActionBase
    {
        private JobStepSubSystems subSystems;
        private JobPropertiesAdvanced advanced = null;
        private JobStepDefinition jobStepDefinition = null;
        private JobStepSubSystem selectedSubSystem = null;
        private bool needToUpdate = false;
        private const int jobIdLowerBound= 1;
        private int currentStepID = jobIdLowerBound;
        private int stepsCount = jobIdLowerBound;
        private IJobStepPropertiesControl activeControl = null;
        private JobStepData data;
        // used to persist state between job step types
        private JobStepData runtimeData;

        internal JobStepProperties(CDataContainer dataContainer, JobStepData context, JobPropertiesAdvanced advancedPanel)
        {           
            this.DataContainer = dataContainer;
            this.data = context;
            this.runtimeData = new JobStepData(this.data);
            currentStepID = this.data.ID;
            stepsCount = this.data.StepCount;
            this.advanced = advancedPanel;        
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

        private JobStepSubSystems SubSystems
        {
            get
            {
                if (this.subSystems == null)
                {
                    this.subSystems = new JobStepSubSystems(this.DataContainer, this.data);
                }
                return this.subSystems;
            }
        }           

        private void UpdateStepDetails(int stepID)
        {
            // Persist the information first before updating the job details
            // this.data.SubSystem = ((JobStepSubSystem)this.typeList.SelectedItem).Key;
            // this.data.Name = "this.stepName.Text";
            // this.activeControl = ((JobStepSubSystem)this.typeList.SelectedItem).Definition as IJobStepPropertiesControl;
            // for (int stepIndex = 0; stepIndex < stepsCount; stepIndex++)
            // {
            //     // don't compare if the id's are the same.
            //     if (this.data.ID != ((JobStepData)this.data.Steps[stepIndex]).ID && this.data.Name == ((JobStepData)this.data.Steps[stepIndex]).Name)
            //     {
            //         //Throw an error if the job step name already exists
            //         throw new ApplicationException(JobSR.JobStepNameAlreadyExists(this.data.Name));
            //     }
            // }

            // if (this.activeControl != null)
            // {
            //     this.activeControl.Save(this.data, true);
            // }
                       
            // // Get the subSystem which is selected at present 
            // JobStepSubSystem subSystemToPersist = this.typeList.SelectedItem as JobStepSubSystem;
            // if(subSystemToPersist!=null)
            // {
            //     if (subSystemToPersist.Advanced != null && this.advanced != null)
            //     {
            //         this.advanced.SetActionToPersist();
            //     }
            // }
           
            // // We have already taken care of index range before accessing JobSteps.Steps[]
            // JobStepData stepData = this.data.Steps[stepID - 1] as JobStepData;
            // this.data = this.runtimeData = stepData;
            // this.stepName.Text = this.data.Name;
            // JobStepSubSystem subSystem = this.SubSystems.Lookup(stepData.SubSystem);
            // needToUpdate = true;
            // JobStepSubSystem subSystemCurrent = this.typeList.SelectedItem as JobStepSubSystem;
            // if (0 == string.Compare(subSystem.Name, subSystemCurrent.Name, StringComparison.OrdinalIgnoreCase))
            // {
            //     // If subsystem type is same then typeList_SelectedIndexChanged event will *NOT* be fired and calling UpdateJobStep() is mandatory
            //     UpdateJobStep();
            // }
            // else
            // {
            //     // an event will be fired when subSystem type is changed which calls UpdateJobStep() internally
            //     this.typeList.SelectedItem = subSystem;
            // }
            // needToUpdate = false;
            // this.Title = JobSR.EditJobStep(this.data.Name);
        
        }

        private void UpdateJobStep()
        {
            // JobStepSubSystem subSystem = this.typeList.SelectedItem as JobStepSubSystem;
            // if (subSystem != null)
            // {
            //     // used for saving/loading runtime state
            //     IJobStepPropertiesControl control = null;
            //     if (false == needToUpdate)
            //     {
            //         if (this.selectedSubSystem != null)
            //         {
            //             // save the old state
            //             control = selectedSubSystem.Definition as IJobStepPropertiesControl;
            //             if (control != null)
            //             {
            //                 control.Save(this.runtimeData, true);
            //             }
            //             // save the old state
            //             control = selectedSubSystem.Advanced as IJobStepPropertiesControl;
            //             if (control != null)
            //             {
            //                 control.Save(this.runtimeData, true);
            //             }
            //         }
            //     }
            //     // load state into the newly selected control
            //     control = subSystem.Definition as IJobStepPropertiesControl;
            //     if (control != null)
            //     {
            //         control.Load(this.runtimeData);
            //     }
            //     control = subSystem.Advanced as IJobStepPropertiesControl;
            //     if (control != null)
            //     {
            //         control.Load(this.runtimeData);
            //     }
            //     this.jobStepDefinition.Controls.Clear();
            //     subSystem.Definition.Dock = DockStyle.Fill;
            //     this.jobStepDefinition.Controls.Add(subSystem.Definition);

            //     if (subSystem.Advanced != null && this.advanced != null)
            //     {
            //         if (true == needToUpdate)
            //         {
            //             this.advanced.SetSpecificActionControl(subSystem.Advanced, subSystem.FriendlyName, this.data);
            //         }
            //         else
            //         {
            //             this.advanced.SetSpecificActions(subSystem.Advanced, subSystem.FriendlyName);
            //         }
            //     }

        }

        // /// <summary>
        // /// Get the values needed to commit changes from the UI controls
        // /// </summary>
        // /// <param name="runType"></param>
        // public override void OnGatherUiInformation(RunType runType)
        // {
        //     base.OnGatherUiInformation(runType);

        //     this.data.SubSystem = ((JobStepSubSystem)this.typeList.SelectedItem).Key;
        //     this.data.Name = this.stepName.Text;
        //     this.activeControl = ((JobStepSubSystem)this.typeList.SelectedItem).Definition as IJobStepPropertiesControl;

        //     if (this.activeControl != null)
        //     {
        //         this.activeControl.Save(this.data, false);
        //     }

        //     STrace.Assert(this.selectedSubSystem != null, "this.selectedSubSystem is null!");
        // }
    }
}

      







