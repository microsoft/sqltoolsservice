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
        public JobPropertiesAdvanced(CDataContainer dataContainer, JobStepData context)
        {
            this.DataContainer = dataContainer;
            this.data = context;
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
       
        #endregion
    }
}
