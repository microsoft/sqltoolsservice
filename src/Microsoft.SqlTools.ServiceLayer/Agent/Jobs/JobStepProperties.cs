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
using Microsoft.SqlServer.Management.Diagnostics;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepProperties.
    /// </summary>
    internal class JobStepProperties : ManagementActionBase
    {
        #region private memebers
        private JobStepSubSystems subSystems;
        private JobPropertiesAdvanced advanced = null;
        private JobStepDefinition jobStepDefinition = null;
        private JobStepSubSystem selectedSubSystem = null;
         private bool needToUpdate = false;
        private const int jobIdLowerBound= 1;
        private int currentStepID = jobIdLowerBound;
        private int stepsCount = jobIdLowerBound;
        private IJobStepPropertiesControl activeControl = null;



        #endregion

        public JobStepProperties()
        {
        }

        private JobStepData data;
        // used to persist state between job step types
        private JobStepData runtimeData;
        private IServiceProvider serviceProvider;

        internal JobStepProperties(CDataContainer dataContainer, JobStepData context, JobPropertiesAdvanced advancedPanel, IServiceProvider service)
        {           
            // this.advanced = new JobPropertiesAdvanced();

            this.DataContainer = dataContainer;
            this.data = context;
            this.runtimeData = new JobStepData(this.data);
            currentStepID = this.data.ID;
            stepsCount = this.data.StepCount;
            this.advanced = advancedPanel;
            this.serviceProvider = service;

            STParameters parameters = new STParameters(this.DataContainer.Document);
            string acceptedits = String.Empty;
            parameters.GetParam("acceptedits", ref acceptedits);
            bool commitEditsToJobStep = (acceptedits == "true") ? true : false;


            // If not Edit/Single Step Job then disable Next and Previous Buttons
            // if ((string.IsNullOrEmpty(data.Name) == true) || stepsCount == 1 || commitEditsToJobStep)
            // {
            //     this.prevButton.Enabled = false;
            //     this.nextButton.Enabled = false;
            //     return;
            // }
            // // Findout if it is Edit mode
            // if (currentStepID == stepsCount)
            // {
            //     this.nextButton.Enabled = false;
            //     return;
            // }
            // if (currentStepID == 1 && stepsCount > 1)
            // {
            //     this.prevButton.Enabled = false;
            // }
            
        }

        // public override void OnInitialization()
        // {
        //     base.OnInitialization();
        //     if (this.data != null)
        //     {
        //         InitializeData();
        //         UpdateControlStatus();
        //     }
        // }

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
            // System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobStepProperties));
            // this.stepNameLabel = new System.Windows.Forms.Label();
            // this.stepName = new System.Windows.Forms.TextBox();
            // this.typeLabel = new System.Windows.Forms.Label();
            // this.typeList = new System.Windows.Forms.ComboBox();
            // this.runasLabel = new System.Windows.Forms.Label();
            // this.runAs = new System.Windows.Forms.ComboBox();
            // this.jobStepDefinition = new Microsoft.SqlServer.Management.SqlManagerUI.JobStepDefinition();
            // this.prevButton = new System.Windows.Forms.Button();
            // this.nextButton = new System.Windows.Forms.Button();
            // this.SuspendLayout();
            // this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // // 
            // // stepNameLabel
            // // 
            // resources.ApplyResources(this.stepNameLabel, "stepNameLabel");
            // this.stepNameLabel.Name = "stepNameLabel";
            // // 
            // // stepName
            // // 
            // resources.ApplyResources(this.stepName, "stepName");
            // this.stepName.Name = "stepName";
            // this.stepName.TextChanged += new System.EventHandler(this.stepName_TextChanged);
            // // 
            // // typeLabel
            // // 
            // resources.ApplyResources(this.typeLabel, "typeLabel");
            // this.typeLabel.Name = "typeLabel";
            // // 
            // // typeList
            // // 
            // resources.ApplyResources(this.typeList, "typeList");
            // this.typeList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            // this.typeList.FormattingEnabled = true;
            // this.typeList.Name = "typeList";
            // this.typeList.Sorted = true;
            // this.typeList.SelectedIndexChanged += new System.EventHandler(this.typeList_SelectedIndexChanged);
            // // 
            // // runasLabel
            // // 
            // resources.ApplyResources(this.runasLabel, "runasLabel");
            // this.runasLabel.Name = "runasLabel";
            // // 
            // // runAs
            // // 
            // resources.ApplyResources(this.runAs, "runAs");
            // this.runAs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            // this.runAs.FormattingEnabled = true;
            // this.runAs.Name = "runAs";
            // this.runAs.SelectedValueChanged += new System.EventHandler(this.runAs_SelectedValueChanged);
            // // 
            // // jobStepDefinition
            // // 
            // resources.ApplyResources(this.jobStepDefinition, "jobStepDefinition");
            // this.jobStepDefinition.Name = "jobStepDefinition";
            // // 
            // // prevButton
            // // 
            // resources.ApplyResources(this.prevButton, "prevButton");
            // this.prevButton.Name = "prevButton";
            // this.prevButton.UseVisualStyleBackColor = true;
            // this.prevButton.Click += new System.EventHandler(this.previous_ButtonClicked);
            // // 
            // // nextButton
            // // 
            // resources.ApplyResources(this.nextButton, "nextButton");
            // this.nextButton.Name = "nextButton";
            // this.nextButton.UseVisualStyleBackColor = true;
            // this.nextButton.Click += new System.EventHandler(this.next_ButtonClicked);
            // // 
            // // JobStepProperties
            // // 
            // this.Controls.Add(this.nextButton);
            // this.Controls.Add(this.prevButton);
            // this.Controls.Add(this.jobStepDefinition);
            // this.Controls.Add(this.runAs);
            // this.Controls.Add(this.runasLabel);
            // this.Controls.Add(this.typeList);
            // this.Controls.Add(this.typeLabel);
            // this.Controls.Add(this.stepName);
            // this.Controls.Add(this.stepNameLabel);
            // this.Name = "JobStepProperties";
            // resources.ApplyResources(this, "$this");
            // this.ResumeLayout(false);
            // this.PerformLayout();

        }
        #endregion

        #region ui stuff
        private void InitializeData()
        {
            // if (this.data == null)
            // {
            //     return;
            // }
            // this.stepName.Text = data.Name;
            // InitializeStepCombo();
        }
        // private void InitializeStepCombo()
        // {
        //     this.typeList.Items.AddRange(FilterStepCombo(this.ServerConnection.DatabaseEngineEdition).ToArray());
        //     this.typeList.SelectedItem = this.SubSystems.Lookup(this.data.SubSystem);
        // }

        private List<JobStepSubSystem> FilterStepCombo(DatabaseEngineEdition engineEdition)
        {
            // Currently, only Managed Instances have limitations in terms of supported subsystems.
            // The following are not supported:
            // ANALYSISCOMMAND and ANALYSISQUERY.
            //
            if (engineEdition != DatabaseEngineEdition.SqlManagedInstance)
            {
                return new List<JobStepSubSystem>(this.SubSystems.AvailableSubSystems);
            }
            else
            {
                List<JobStepSubSystem> supportedSubSystems = new List<JobStepSubSystem>();

                const string analysisCommand = "analysiscommand";
                const string analysisQuery = "analysisquery";

                foreach (JobStepSubSystem subSystem in this.SubSystems.AvailableSubSystems)
                {
                    if (!subSystem.Name.ToLowerInvariant().Contains(analysisCommand) &&
                        !subSystem.Name.ToLowerInvariant().Contains(analysisQuery))
                    {
                        supportedSubSystems.Add(subSystem);
                    }
                }

                return supportedSubSystems;
            }
        }

        private void UpdateControlStatus()
        {
            // SetDialogFieldsReadOnly(this.data.Parent.IsReadOnly, new Control[] {
            //       this.stepName,
            //       this.typeList,
            //       this.runAs}
            // );
        }
        #endregion

        private JobStepSubSystems SubSystems
        {
            get
            {
                if (this.subSystems == null)
                {
                    this.subSystems = new JobStepSubSystems(this.DataContainer, this.data, this.serviceProvider);
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

            //     if
            //         (
            //         (this.DataContainer.ServerConnection.ServerVersion.Major >= 9) &&
            //         (subSystem.Key != AgentSubSystem.TransactSql) // $FUTURE - apred 6/11/2004 - per Rob - we do a partial fix to 285666 for Beta2, TSQL jobs dont have proxies, in Beta 3 we will replace this with a combo for users, but till then we just disable this (Beta3 change is tracked by 286861)
            //         )
            //     {
            //         bool isSysAdmin = this.DataContainer.ServerConnection.IsInFixedServerRole(FixedServerRoles.SysAdmin);

            //         // Refresh ProxyAccounts combo
            //         // Try to select back the same proxy that was selected before changing the subsystem.
            //         this.runAs.Items.Clear();
            //         this.runAs.Items.AddRange(AgentProxyAccount.ListProxyAccountsForSubsystem(this.DataContainer.ServerConnection, subSystem.Name, isSysAdmin));
            //         if (this.runAs.FindStringExact(this.data.ProxyName) >= 0)
            //             this.runAs.SelectedItem = this.data.ProxyName;
            //         else
            //             this.runAs.SelectedItem = string.Empty;

            //         this.runAs.Enabled = (this.runAs.Items.Count > 0);
            //     }
            //     else
            //     {
            //         // Disable 'Run As' combo for legacy servers // $FUTURE - apred 6/11/2004 - and for Beta 2 for TSQL steps also - see bugs 285666 & 286861
            //         this.runAs.Items.Clear();
            //         this.runAs.SelectedIndex = -1;
            //         this.runAs.Enabled = false;
            //     }

            //     this.selectedSubSystem = subSystem;
            //     needToUpdate = false;

            // }
        }


        private void typeList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            UpdateJobStep();
        }

        // do nothing
        // public override void OnRunNow(object sender)
        // {
        // }

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


        // private void stepName_TextChanged(System.Object sender, System.EventArgs e)
        // {
        //     UpdateCommandUI();
        // }

        // public override bool IsRunTypeEnabled(RunType runType)
        // {
        //     switch (runType)
        //     {
        //         case RunType.ScriptToJob:
        //         case RunType.ScriptToClipboard:
        //         case RunType.ScriptToFile:
        //         case RunType.ScriptToWindow:
        //         case RunType.RunNow:
        //             return false;
        //         case RunType.RunNowAndExit:
        //             return this.stepName.Text.Length > 0;
        //     }
        //     return base.IsRunTypeEnabled(runType);
        // }

        // private void runAs_SelectedValueChanged(object sender, EventArgs e)
        // {
        //     string proxyName = this.runAs.SelectedItem as string;
        //     if (proxyName != null)
        //         this.data.ProxyName = proxyName;
        //     else
        //         this.data.ProxyName = string.Empty;
        // }

        // private void next_ButtonClicked(object sender, EventArgs e)
        // {
        //     this.currentStepID++;
        //     if (this.currentStepID > this.stepsCount)
        //     {
        //         return;
        //     }

        //     try
        //     {
        //         UpdateStepDetails(this.currentStepID);
        //         if (this.currentStepID == this.stepsCount)
        //         {
        //             //disable Next Button and make sure Previous button is Enabled.
        //             this.nextButton.Enabled = false;
        //         }
        //         this.prevButton.Enabled = true; //Enable Previous Button
        //     }
        //     catch (Exception em)
        //     {
        //         MessageBoxProvider.ShowMessage(em, null, Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK, Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Error, null);
        //         this.currentStepID--;
        //     }
        // }

        // private void previous_ButtonClicked(object sender, EventArgs e)
        // {
        //     this.currentStepID--;
        //     if (this.currentStepID < jobIdLowerBound)
        //     {
        //         return;
        //     }

        //     try
        //     {
        //         UpdateStepDetails(this.currentStepID);
        //         if (this.currentStepID == jobIdLowerBound)
        //         {
        //             this.prevButton.Enabled = false;
        //         }
        //         this.nextButton.Enabled = true;
        //     }
        //     catch (Exception em)
        //     {
        //         MessageBoxProvider.ShowMessage(em, null, Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK, Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Error, null);
        //         this.currentStepID++;
        //     }
        
        // }
    }
}

      







