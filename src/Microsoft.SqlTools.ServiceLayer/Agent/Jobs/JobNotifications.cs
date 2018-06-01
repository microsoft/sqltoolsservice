//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobNotifications.
    /// </summary>
    internal class JobNotifications : ManagementActionBase
    {
        // private System.Windows.Forms.CheckBox emailCheck;
        // private System.Windows.Forms.Label label1;
        // private System.Windows.Forms.ComboBox emailOperator;
        // private System.Windows.Forms.ComboBox emailWhenJob;
        // private System.Windows.Forms.ComboBox pageWhenJob;
        // private System.Windows.Forms.ComboBox pageOperator;
        // private System.Windows.Forms.CheckBox pageCheck;
        // private System.Windows.Forms.CheckBox eventLogCheck;
        // private System.Windows.Forms.ComboBox eventLogWhenJob;
        // private System.Windows.Forms.ComboBox deleteWhenJob;
        // private System.Windows.Forms.CheckBox deleteCheck;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;

        private JobData data;
        //private TableLayoutPanel tableLayoutPanel1;
        private bool loading = false;

        public JobNotifications()
        {
        }

        public JobNotifications(CDataContainer dataContainer, JobData data)
        {
            this.DataContainer = dataContainer;
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
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobNotifications));
        //     this.emailCheck = new System.Windows.Forms.CheckBox();
        //     this.label1 = new System.Windows.Forms.Label();
        //     this.emailOperator = new System.Windows.Forms.ComboBox();
        //     this.emailWhenJob = new System.Windows.Forms.ComboBox();
        //     this.pageWhenJob = new System.Windows.Forms.ComboBox();
        //     this.pageOperator = new System.Windows.Forms.ComboBox();
        //     this.pageCheck = new System.Windows.Forms.CheckBox();
        //     this.eventLogCheck = new System.Windows.Forms.CheckBox();
        //     this.eventLogWhenJob = new System.Windows.Forms.ComboBox();
        //     this.deleteWhenJob = new System.Windows.Forms.ComboBox();
        //     this.deleteCheck = new System.Windows.Forms.CheckBox();
        //     this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
        //     this.tableLayoutPanel1.SuspendLayout();
        //     this.SuspendLayout();
        //     // 
        //     // emailCheck
        //     // 
        //     resources.ApplyResources(this.emailCheck, "emailCheck");
        //     this.emailCheck.Name = "emailCheck";
        //     this.emailCheck.CheckedChanged += new System.EventHandler(this.emailCheck_CheckedChanged);
        //     // 
        //     // label1
        //     // 
        //     this.tableLayoutPanel1.SetColumnSpan(this.label1, 3);
        //     resources.ApplyResources(this.label1, "label1");
        //     this.label1.Name = "label1";
        //     // 
        //     // emailOperator
        //     // 
        //     resources.ApplyResources(this.emailOperator, "emailOperator");
        //     this.emailOperator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.emailOperator.FormattingEnabled = true;
        //     this.emailOperator.Name = "emailOperator";
        //     this.emailOperator.SelectedIndexChanged += new System.EventHandler(this.emailOperator_SelectedIndexChanged);
        //     // 
        //     // emailWhenJob
        //     // 
        //     resources.ApplyResources(this.emailWhenJob, "emailWhenJob");
        //     this.emailWhenJob.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.emailWhenJob.FormattingEnabled = true;
        //     this.emailWhenJob.Name = "emailWhenJob";
        //     this.emailWhenJob.SelectedIndexChanged += new System.EventHandler(this.emailWhenJob_SelectedIndexChanged);
        //     // 
        //     // pageWhenJob
        //     // 
        //     resources.ApplyResources(this.pageWhenJob, "pageWhenJob");
        //     this.pageWhenJob.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.pageWhenJob.FormattingEnabled = true;
        //     this.pageWhenJob.Name = "pageWhenJob";
        //     this.pageWhenJob.SelectedIndexChanged += new System.EventHandler(this.pageWhenJob_SelectedIndexChanged);
        //     // 
        //     // pageOperator
        //     // 
        //     resources.ApplyResources(this.pageOperator, "pageOperator");
        //     this.pageOperator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.pageOperator.FormattingEnabled = true;
        //     this.pageOperator.Name = "pageOperator";
        //     this.pageOperator.SelectedIndexChanged += new System.EventHandler(this.pageOperator_SelectedIndexChanged);
        //     // 
        //     // pageCheck
        //     // 
        //     resources.ApplyResources(this.pageCheck, "pageCheck");
        //     this.pageCheck.Name = "pageCheck";
        //     this.pageCheck.CheckedChanged += new System.EventHandler(this.pageCheck_CheckedChanged);
        //     // 
        //     // eventLogCheck
        //     // 
        //     resources.ApplyResources(this.eventLogCheck, "eventLogCheck");
        //     this.tableLayoutPanel1.SetColumnSpan(this.eventLogCheck, 2);
        //     this.eventLogCheck.Name = "eventLogCheck";
        //     this.eventLogCheck.CheckedChanged += new System.EventHandler(this.eventLogCheck_CheckedChanged);
        //     // 
        //     // eventLogWhenJob
        //     // 
        //     resources.ApplyResources(this.eventLogWhenJob, "eventLogWhenJob");
        //     this.eventLogWhenJob.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.eventLogWhenJob.FormattingEnabled = true;
        //     this.eventLogWhenJob.Name = "eventLogWhenJob";
        //     this.eventLogWhenJob.SelectedIndexChanged += new System.EventHandler(this.eventLogWhenJob_SelectedIndexChanged);
        //     // 
        //     // deleteWhenJob
        //     // 
        //     resources.ApplyResources(this.deleteWhenJob, "deleteWhenJob");
        //     this.deleteWhenJob.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.deleteWhenJob.FormattingEnabled = true;
        //     this.deleteWhenJob.Name = "deleteWhenJob";
        //     this.deleteWhenJob.SelectedIndexChanged += new System.EventHandler(this.deleteWhenJob_SelectedIndexChanged);
        //     // 
        //     // deleteCheck
        //     // 
        //     resources.ApplyResources(this.deleteCheck, "deleteCheck");
        //     this.tableLayoutPanel1.SetColumnSpan(this.deleteCheck, 2);
        //     this.deleteCheck.Name = "deleteCheck";
        //     this.deleteCheck.CheckedChanged += new System.EventHandler(this.deleteCheck_CheckedChanged);
        //     // 
        //     // tableLayoutPanel1
        //     // 
        //     resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
        //     this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
        //     this.tableLayoutPanel1.Controls.Add(this.deleteWhenJob, 2, 4);
        //     this.tableLayoutPanel1.Controls.Add(this.deleteCheck, 0, 4);
        //     this.tableLayoutPanel1.Controls.Add(this.emailCheck, 0, 1);
        //     this.tableLayoutPanel1.Controls.Add(this.emailOperator, 1, 1);
        //     this.tableLayoutPanel1.Controls.Add(this.eventLogWhenJob, 2, 3);
        //     this.tableLayoutPanel1.Controls.Add(this.emailWhenJob, 2, 1);
        //     this.tableLayoutPanel1.Controls.Add(this.eventLogCheck, 0, 3);
        //     this.tableLayoutPanel1.Controls.Add(this.pageCheck, 0, 2);
        //     this.tableLayoutPanel1.Controls.Add(this.pageWhenJob, 2, 2);
        //     this.tableLayoutPanel1.Controls.Add(this.pageOperator, 1, 2);
        //     this.tableLayoutPanel1.Name = "tableLayoutPanel1";
        //     // 
        //     // JobNotifications
        //     // 
        //     resources.ApplyResources(this, "$this");
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     this.Controls.Add(this.tableLayoutPanel1);
        //     this.Name = "JobNotifications";
        //     this.tableLayoutPanel1.ResumeLayout(false);
        //     this.tableLayoutPanel1.PerformLayout();
        //     this.ResumeLayout(false);

        // }
        #endregion

        #region ui initialization
        // private void UpdateControlStatus()
        // {
        //     if (this.data == null)
        //     {
        //         return;
        //     }

        //     // disable controls if the user does not have rights to make changes
        //     if (this.data.Version.Major < 9 && ((this.data.UserRole & UserRoles.SysAdmin) == 0))
        //     {
        //         this.emailCheck.Enabled = false;
        //         this.pageCheck.Enabled = false;
        //     }

        //     if (DataContainer.Server.HostPlatform != Microsoft.SqlServer.Management.Common.HostPlatformNames.Windows)
        //     {
        //         this.pageCheck.Enabled = false;
        //     }

        //     // disable controls to the right of the checkbox if it isn't checked
        //     this.emailOperator.Enabled = this.emailCheck.Checked;
        //     this.emailWhenJob.Enabled = this.emailCheck.Checked;

        //     this.pageOperator.Enabled = this.pageCheck.Checked;
        //     this.pageWhenJob.Enabled = this.pageCheck.Checked;
            
        //     this.eventLogWhenJob.Enabled = this.eventLogCheck.Checked;

        //     this.deleteWhenJob.Enabled = this.deleteCheck.Checked;

        //     ///If the jobs data object is readonly then this panel should be read only too.
        //     ///Disable the entire container...
        //     if (this.data.IsReadOnly)
        //     {
        //         ///Set the state of the controls based on the job step read-only value
        //         SetDialogFieldsReadOnly(data.IsReadOnly, new Control[] {
        //               emailCheck,emailOperator,emailWhenJob,              
        //               pageWhenJob,pageOperator,pageCheck,
        //               eventLogCheck,eventLogWhenJob,deleteWhenJob,deleteCheck });
        //     }

        // }
        #endregion

        #region IPanelForm implementation
        // void IPanelForm.OnInitialization()
        // {
        //     if (this.data == null)
        //     {
        //         return;
        //     }

        //     // Indicate that the form is loading and events should
        //     // not be processed yet
        //     this.loading = true;

        //     // populate the operator combo boxes
        //     this.emailOperator.Items.AddRange(this.data.Operators);
        //     this.pageOperator.Items.AddRange(this.data.Operators);
            
        //     // populate the Level combo boxes
        //     this.emailWhenJob.Items.AddRange(this.CompletionActions);
        //     this.emailWhenJob.DropDownWidth = Math.Max(1, this.GetMaxWidthOfComboItems(emailWhenJob));

        //     this.pageWhenJob.Items.AddRange(this.CompletionActions);
        //     this.pageWhenJob.DropDownWidth = Math.Max(1, this.GetMaxWidthOfComboItems(this.pageWhenJob));

            
        //     this.eventLogWhenJob.Items.AddRange(this.CompletionActions);
        //     this.eventLogWhenJob.DropDownWidth = Math.Max(1, GetMaxWidthOfComboItems(this.eventLogWhenJob));

        //     this.deleteWhenJob.Items.AddRange(this.CompletionActions);
        //     this.deleteWhenJob.DropDownWidth = Math.Max(1, GetMaxWidthOfComboItems(this.deleteWhenJob));

        //     // email
        //     this.emailCheck.Checked = this.data.OperatorToEmail.Length > 0;
            
        //     this.emailOperator.Text = this.data.OperatorToEmail;
        //     this.emailWhenJob.SelectedIndex = CompletionActionToComboIndex(this.data.EmailLevel);

        //     // Pager
        //     // Not supported for Managed Instances
        //     //
        //     if (IsManagedInstance())
        //     {
        //         this.pageCheck.Enabled = false;
        //         this.pageCheck.Checked = false;
        //     }
        //     else
        //     {
        //         this.pageCheck.Checked = this.data.OperatorToPage.Length > 0;
        //         if (!this.pageOperator.Items.Contains(this.data.OperatorToPage))
        //         {
        //             this.pageOperator.Items.Add(this.data.OperatorToPage);
        //         }
        //         this.pageOperator.Text = this.data.OperatorToPage;
        //         this.pageWhenJob.SelectedIndex = CompletionActionToComboIndex(this.data.PageLevel);
        //     }

        //     // Event log
        //     // Not supported for Managed Instances
        //     //
        //     if (IsManagedInstance())
        //     {
        //         this.eventLogCheck.Checked = false;
        //         this.eventLogCheck.Enabled = false;
        //     }
        //     else
        //     {
        //         this.eventLogCheck.Checked = (this.data.EventLogLevel != CompletionAction.Never);
        //         this.eventLogWhenJob.SelectedIndex = CompletionActionToComboIndex(this.data.EventLogLevel);
        //     }
        //     // delete
        //     this.deleteCheck.Checked = (this.data.DeleteLevel != CompletionAction.Never);
        //     this.deleteWhenJob.SelectedIndex = CompletionActionToComboIndex(this.data.DeleteLevel, 0);

        //     this.loading = false;

        //     UpdateControlStatus();
        // }
        #endregion

        #region event handlers
        // private void emailCheck_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         UpdateControlStatus();
        //         emailWhenJob_SelectedIndexChanged(sender, e);
        //     }
        // }

        // private void pageCheck_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         UpdateControlStatus();
        //         pageWhenJob_SelectedIndexChanged(sender, e);
        //     }
        // }
        

        // private void eventLogCheck_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         UpdateControlStatus();
        //         eventLogWhenJob_SelectedIndexChanged(sender, e);
        //     }
        // }

        // private void deleteCheck_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         UpdateControlStatus();
        //         deleteWhenJob_SelectedIndexChanged(sender, e);
        //     }
        // }

        // private void emailOperator_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         this.data.OperatorToEmail = this.emailOperator.SelectedItem.ToString();
        //     }
        // }

        // private void emailWhenJob_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         if (this.emailCheck.Checked)
        //         {
        //             this.data.EmailLevel = ComboIndexToCompletionAction(this.emailWhenJob.SelectedIndex);
        //         }
        //         else
        //         {
        //             this.data.EmailLevel = CompletionAction.Never;
        //         }
        //     }
        // }

        // private void pageOperator_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         this.data.OperatorToPage = this.pageOperator.SelectedItem.ToString();
        //     }
        // }

        // private void pageWhenJob_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         if (this.pageCheck.Checked)
        //         {
        //             this.data.PageLevel = ComboIndexToCompletionAction(this.pageWhenJob.SelectedIndex);
        //         }
        //         else
        //         {
        //             this.data.PageLevel = CompletionAction.Never;
        //         }
        //     }
        // }

        // private void eventLogWhenJob_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if (!loading)
        //     {
        //         if (this.eventLogCheck.Checked)
        //         {
        //             this.data.EventLogLevel = ComboIndexToCompletionAction(this.eventLogWhenJob.SelectedIndex);
        //         }
        //         else
        //         {
        //             this.data.EventLogLevel = CompletionAction.Never;
        //         }
        //     }
        // }

        // private void deleteWhenJob_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if (this.deleteCheck.Checked)
        //     {
        //         this.data.DeleteLevel = ComboIndexToCompletionAction(this.deleteWhenJob.SelectedIndex);
        //     }
        //     else
        //     {
        //         this.data.DeleteLevel = CompletionAction.Never;
        //     }
        // }
        #endregion

        // private string[] comlpetionActions = {
        //                                        JobSR.JobSucceeds, 
        //                                        JobSR.JobFails, 
        //                                        JobSR.JobCompletes };
        // private string[] CompletionActions
        // {
        //     get
        //     {
        //         return comlpetionActions;
        //     }
        // }
        // private CompletionAction[] completionActions = { CompletionAction.OnSuccess
        //                                                  ,CompletionAction.OnFailure
        //                                                  ,CompletionAction.Always };
        // private CompletionAction ComboIndexToCompletionAction(int index)
        // {
        //     if (index < 0)
        //     {
        //         return CompletionAction.Never;
        //     }
        //     else
        //     {
        //         return completionActions[index];
        //     }
        // }
        // private int CompletionActionToComboIndex(CompletionAction action)
        // {
        //     return CompletionActionToComboIndex(action, 1);
        // }
        // private int CompletionActionToComboIndex(CompletionAction action, int defaultValue)
        // {
        //     // default to failure
        //     int rv = defaultValue;
        //     switch (action)
        //     {
        //         case CompletionAction.OnSuccess:
        //             rv = 0;
        //             break;
        //         case CompletionAction.OnFailure:
        //             rv = 1;
        //             break;
        //         case CompletionAction.Always:
        //             rv = 2;
        //             break;
        //         case CompletionAction.Never:
        //             rv = defaultValue;
        //             break;
        //     }
        //     return rv;
        // }

        // /// <summary>
        // /// Determine the largest size of a particular combobox.
        // /// </summary>
        // /// <param name="comboBox"></param>
        // /// <returns></returns>
        // private int GetMaxWidthOfComboItems(ComboBox comboBox)
        // {
        //     SizeF maxSize = new SizeF();
        //     SizeF tempSize = new SizeF();
        //     using (System.Drawing.Graphics g = comboBox.CreateGraphics())
        //     {
        //         for (int index = 0; index < comboBox.Items.Count - 1; index++)
        //         {
        //             tempSize =
        //                 g.MeasureString(comboBox.Items[index].ToString().Trim(), comboBox.Font);

        //             if (tempSize.Width > maxSize.Width)
        //             {
        //                 maxSize.Width = tempSize.Width;
        //             }

        //         }
        //     }
        //     return (int)maxSize.Width;

        // }

        private bool IsManagedInstance()
        {
            return (this.ServerConnection != null &&
                    this.ServerConnection.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance);
        }
    }
}
