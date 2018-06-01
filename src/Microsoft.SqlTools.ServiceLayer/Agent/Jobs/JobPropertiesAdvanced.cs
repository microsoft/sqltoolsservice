//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.SqlManagerUI;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobPropertiesAdvanced.
    /// </summary>
    internal class JobPropertiesAdvanced : ManagementActionBase
    {
        // private System.Windows.Forms.Label onSuccessActionLabel;
        // private System.Windows.Forms.ComboBox onSuccessAction;
        // private System.Windows.Forms.Label retryAttemptsLabel;
        // private System.Windows.Forms.Label retryIntervalLabel;
        // private System.Windows.Forms.ComboBox onFailureAction;
        // private System.Windows.Forms.Label onFailureActionLabel;
        // private Microsoft.SqlServer.Management.Controls.Separator stepSecificActions;
        // private System.Windows.Forms.Panel specificActionsPanel;
        // private System.Windows.Forms.NumericUpDown retryAttempts;
        // private System.Windows.Forms.NumericUpDown retryInterval;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;

        public JobPropertiesAdvanced()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // TODO: Add any initialization after the InitForm call
            data = null;
        }
        private JobStepData data;
        private IServiceProvider serviceProvider;
        public JobPropertiesAdvanced(CDataContainer dataContainer, JobStepData context, IServiceProvider service)
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // this.retryAttempts.Maximum = Int32.MaxValue;
            // this.retryInterval.Maximum = Int32.MaxValue;

            // this.DataContainer = dataContainer;
            // this.data = context;
            // this.serviceProvider = service;
            // InitializeData();
            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.job.stepadvanced.f1";
            // UpdateControlStatus();
        }

        // public void SetSpecificActions(Control control, string actionsText)
        // {
        //     this.specificActionsPanel.Controls.Clear();
        //     this.specificActionsPanel.Controls.Add(control);
        //     control.Dock = DockStyle.Fill;

        //     this.stepSecificActions.Text = actionsText;

        //     IJobStepPropertiesControl stepControl = this.specificActionsPanel.Controls[0] as IJobStepPropertiesControl;
        //     if (stepControl != null)
        //     {
        //         stepControl.Load(this.data);
        //     }

        // }

        // public void SetSpecificActionControl(Control control, string actionsText,JobStepData stepData)
        // {
        //     this.data = stepData;
        //     this.onSuccessAction.Items.Clear();
        //     this.onFailureAction.Items.Clear();
        //     InitializeData();
        //     this.specificActionsPanel.Controls.Clear();
        //     this.specificActionsPanel.Controls.Add(control);
        //     control.Dock = DockStyle.Fill;


        //     this.stepSecificActions.Text = actionsText;

        //     IJobStepPropertiesControl stepControl = this.specificActionsPanel.Controls[0] as IJobStepPropertiesControl;
        //     if (stepControl != null)
        //     {
        //         stepControl.Load(this.data);
        //     }

        // }

        public void SetActionToPersist()
        {
            // Save the data
            //OnGatherUiInformation(RunType.RunNow);

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

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobPropertiesAdvanced));
        //     this.onSuccessActionLabel = new System.Windows.Forms.Label();
        //     this.onSuccessAction = new System.Windows.Forms.ComboBox();
        //     this.retryAttemptsLabel = new System.Windows.Forms.Label();
        //     this.retryIntervalLabel = new System.Windows.Forms.Label();
        //     this.retryAttempts = new System.Windows.Forms.NumericUpDown();
        //     this.retryInterval = new System.Windows.Forms.NumericUpDown();
        //     this.onFailureAction = new System.Windows.Forms.ComboBox();
        //     this.onFailureActionLabel = new System.Windows.Forms.Label();
        //     this.stepSecificActions = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.specificActionsPanel = new System.Windows.Forms.Panel();
        //     ((System.ComponentModel.ISupportInitialize)(this.retryAttempts)).BeginInit();
        //     ((System.ComponentModel.ISupportInitialize)(this.retryInterval)).BeginInit();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // onSuccessActionLabel
        //     // 
        //     resources.ApplyResources(this.onSuccessActionLabel, "onSuccessActionLabel");
        //     this.onSuccessActionLabel.Name = "onSuccessActionLabel";
        //     // 
        //     // onSuccessAction
        //     // 
        //     resources.ApplyResources(this.onSuccessAction, "onSuccessAction");
        //     this.onSuccessAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.onSuccessAction.FormattingEnabled = true;
        //     this.onSuccessAction.Name = "onSuccessAction";
        //     // 
        //     // retryAttemptsLabel
        //     // 
        //     resources.ApplyResources(this.retryAttemptsLabel, "retryAttemptsLabel");
        //     this.retryAttemptsLabel.Name = "retryAttemptsLabel";
        //     // 
        //     // retryIntervalLabel
        //     // 
        //     resources.ApplyResources(this.retryIntervalLabel, "retryIntervalLabel");
        //     this.retryIntervalLabel.Name = "retryIntervalLabel";
        //     // 
        //     // retryAttempts
        //     // 
        //     resources.ApplyResources(this.retryAttempts, "retryAttempts");
        //     this.retryAttempts.Name = "retryAttempts";
        //     // 
        //     // retryInterval
        //     // 
        //     resources.ApplyResources(this.retryInterval, "retryInterval");
        //     this.retryInterval.Name = "retryInterval";
        //     // 
        //     // onFailureAction
        //     // 
        //     resources.ApplyResources(this.onFailureAction, "onFailureAction");
        //     this.onFailureAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.onFailureAction.FormattingEnabled = true;
        //     this.onFailureAction.Name = "onFailureAction";
        //     // 
        //     // onFailureActionLabel
        //     // 
        //     resources.ApplyResources(this.onFailureActionLabel, "onFailureActionLabel");
        //     this.onFailureActionLabel.Name = "onFailureActionLabel";
        //     // 
        //     // stepSecificActions
        //     // 
        //     resources.ApplyResources(this.stepSecificActions, "stepSecificActions");
        //     this.stepSecificActions.Name = "stepSecificActions";
        //     // 
        //     // specificActionsPanel
        //     // 
        //     resources.ApplyResources(this.specificActionsPanel, "specificActionsPanel");
        //     this.specificActionsPanel.Name = "specificActionsPanel";
        //     // 
        //     // JobPropertiesAdvanced
        //     // 
        //     this.Controls.Add(this.specificActionsPanel);
        //     this.Controls.Add(this.stepSecificActions);
        //     this.Controls.Add(this.onFailureAction);
        //     this.Controls.Add(this.onFailureActionLabel);
        //     this.Controls.Add(this.retryInterval);
        //     this.Controls.Add(this.retryAttempts);
        //     this.Controls.Add(this.retryIntervalLabel);
        //     this.Controls.Add(this.retryAttemptsLabel);
        //     this.Controls.Add(this.onSuccessAction);
        //     this.Controls.Add(this.onSuccessActionLabel);
        //     this.Name = "JobPropertiesAdvanced";
        //     resources.ApplyResources(this, "$this");
        //     ((System.ComponentModel.ISupportInitialize)(this.retryAttempts)).EndInit();
        //     ((System.ComponentModel.ISupportInitialize)(this.retryInterval)).EndInit();
        //     this.ResumeLayout(false);

        // }
        #endregion

        #region IPanelForm overrides
        // public override void OnRunNow(object sender)
        // {
        //     base.OnRunNow(sender);
        // }

        // public override void OnGatherUiInformation(RunType runType)
        // {
        //     // must have internal state
        //     STrace.Assert(this.data != null, "this.data is null!");
        //     if (this.data == null)
        //     {
        //         return;
        //     }
        //     this.data.RetryAttempts = (int)this.retryAttempts.Value;
        //     this.data.RetryInterval = (int)this.retryInterval.Value;

        //     NextActionComboItem actionItem = this.onSuccessAction.SelectedItem as NextActionComboItem;
        //     if (actionItem != null)
        //     {
        //         this.data.StepSuccessAction(actionItem.Action, actionItem.Data);
        //     }

        //     actionItem = this.onFailureAction.SelectedItem as NextActionComboItem; ;
        //     if (actionItem != null)
        //     {
        //         this.data.StepFailureAction(actionItem.Action, actionItem.Data);
        //     }

        //     if (0 < this.specificActionsPanel.Controls.Count)
        //     {
        //         IJobStepPropertiesControl activeControl = this.specificActionsPanel.Controls[0] as IJobStepPropertiesControl;
        //         if (activeControl != null)
        //         {
        //             activeControl.Save(this.data, false);
        //         }
        //     }

        // }
        #endregion

        #region ui stuff
        // private void InitializeData()
        // {
        //     if (this.data == null)
        //     {
        //         return;
        //     }
        //     LoadComboData();

        //     this.retryAttempts.Value = this.data.RetryAttempts;
        //     this.retryInterval.Value = this.data.RetryInterval;
        // }
        // private void LoadComboData()
        // {
        //     PopulateActionList(this.onSuccessAction, onSuccessActionList);
        //     SetSelectedAction(this.onSuccessAction, data.SuccessAction, data.SuccessStep);
        //     PopulateActionList(this.onFailureAction, onFailureActionList);
        //     SetSelectedAction(this.onFailureAction, data.FailureAction, data.FailStep);
        // }
        // private void UpdateControlStatus()
        // {
        //     // must have internal state
        //     STrace.Assert(this.data != null);
        //     if (this.data == null)
        //     {
        //         return;
        //     }

        //     SetDialogFieldsReadOnly(this.data.Parent.Parent.IsReadOnly,
        //         new Control[] { onSuccessAction, onFailureAction, retryAttempts, retryInterval, specificActionsPanel });
        // }
        #endregion

        #region helpers
        // private void PopulateActionList(ComboBox control, NextAction[] actionList)
        // {
        //     foreach (NextAction action in actionList)
        //     {
        //         control.Items.Add(
        //            new NextActionComboItem(
        //               action.Description
        //               , action.Action
        //               , null));
        //     }

        //     if (this.data.Parent != null)
        //     {
        //         foreach (JobStepData jobStep in this.data.Parent.Steps)
        //         {
        //             if (jobStep == this.data)
        //             {
        //                 continue;
        //             }
        //             control.Items.Add(
        //                new NextActionComboItem(
        //                   JobSR.GotoStep(jobStep.ID, jobStep.Name)
        //                   , StepCompletionAction.GoToStep
        //                   , jobStep));
        //         }
        //     }
        // }
        // private void SetSelectedAction(ComboBox control, StepCompletionAction action, JobStepData step)
        // {
        //     foreach (NextActionComboItem comboItem in control.Items)
        //     {
        //         if (comboItem != null && comboItem.Action == action && comboItem.Data == step)
        //         {
        //             control.SelectedItem = comboItem;
        //             break;
        //         }
        //     }
        // }
        // private struct NextAction
        // {
        //     public NextAction(StepCompletionAction action, String description)
        //     {
        //         Action = action;
        //         Description = description;
        //     }
        //     public StepCompletionAction Action;
        //     public String Description;
        // }
        // private static NextAction[] onSuccessActionList = {   
        //  new NextAction(StepCompletionAction.GoToNextStep, JobSR.GotoNextStep)
        // ,new NextAction(StepCompletionAction.QuitWithSuccess, JobSR.QuitWithSuccess)
        // ,new NextAction(StepCompletionAction.QuitWithFailure, JobSR.QuitWithFailure) };
        // private static NextAction[] onFailureActionList = {
        //  new NextAction(StepCompletionAction.QuitWithFailure, JobSR.QuitWithFailure)
        // ,new NextAction(StepCompletionAction.GoToNextStep, JobSR.GotoNextStep)
        // ,new NextAction(StepCompletionAction.QuitWithSuccess, JobSR.QuitWithSuccess) };

        // private class NextActionComboItem
        // {
        //     private string description;
        //     private StepCompletionAction action;
        //     private JobStepData data;
        //     public NextActionComboItem(string description, StepCompletionAction action, JobStepData data)
        //     {
        //         this.description = description;
        //         this.action = action;
        //         this.data = data;
        //     }
        //     public override string ToString()
        //     {
        //         return this.description;
        //     }
        //     public StepCompletionAction Action { get { return this.action; } }
        //     public JobStepData Data { get { return this.data; } }
        // }
        #endregion
    }
}








