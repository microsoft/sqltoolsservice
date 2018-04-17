using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Admin;

#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Agent alert options page
    /// </summary>
    internal class AgentAlertOptions : AgentControlBase
    {
        #region Members

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.Label includeAlertErrorTextLabel;
        // private System.Windows.Forms.CheckBox email;
        // private System.Windows.Forms.CheckBox pager;
        // private System.Windows.Forms.Label additionNotificationMessageLabel;
        // private System.Windows.Forms.TextBox additionNotificationMessage;
        // private System.Windows.Forms.Label delayBetweenResponsesLabel;
        // private System.Windows.Forms.Label minutesLabel;
        // private System.Windows.Forms.NumericUpDown minutes;
        // private System.Windows.Forms.Label secondsLabel;
        // private System.Windows.Forms.NumericUpDown seconds;
        /// <summary>
        /// true if controls have been initialized
        /// </summary>
        private bool controlsInitialized = false;
        /// <summary>
        /// Agent alert being edited
        /// </summary>
        private string agentAlertName = null;

        private bool readOnly = false;

        #endregion

        #region Constructors

        private AgentAlertOptions()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.alert.options.f1";
        }

        public AgentAlertOptions(CDataContainer dataContainer, string agentAlertName)
            : this()
        {
            if (dataContainer == null)
                throw new ArgumentNullException("dataContainer");

            DataContainer = dataContainer;
            //this.AllUIEnabled = true;
            this.agentAlertName = agentAlertName;

            // this.minutes.Minimum = 0;
            // this.minutes.Maximum = 720;	// According to spec.
            // this.seconds.Minimum = 0;
            // this.seconds.Maximum = 59;	// According to spec.

            // InitializeControls(false);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Updates alert fields with data from this page
        /// </summary>
        /// <param name="alert"></param>
        public void UpdateAlert(Alert alert)
        {
            if (alert == null)
                throw new ArgumentNullException("alert");

            if (this.agentAlertName != null)
                this.agentAlertName = alert.Name;

            NotifyMethods notifyMethods = NotifyMethods.None;

            // if (this.email.Checked)
            //     notifyMethods |= NotifyMethods.NotifyEmail;
            // if (this.pager.Checked)
            //     notifyMethods |= NotifyMethods.Pager;
            
            alert.IncludeEventDescription = notifyMethods;
            //alert.NotificationMessage = this.additionNotificationMessage.Text;
            //alert.DelayBetweenResponses = (int)(this.minutes.Value * 60 + this.seconds.Value);
        }

        #endregion

        #region IPanelForm

        // void IPanelForm.OnSelection(TreeNode node)
        // {
        // }

        // public override void OnReset(object sender)
        // {
        //     InitializeControls(true);
        //     base.OnReset(sender);
        // }

        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {
        // }

        // void IPanelForm.OnInitialization()
        // {
        //     if (this.readOnly)
        //     {
        //         SetDialogFieldsReadOnly(true,
        //             new Control[] {
        //                 email,
        //                 pager,
        //                 additionNotificationMessageLabel,
        //                 additionNotificationMessage,
        //                 delayBetweenResponsesLabel,
        //                 minutesLabel,
        //                 minutes,
        //                 secondsLabel,
        //                 seconds
        //             });
        //     }
        // }

        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {
        //         return this;
        //     }
        // }

        #endregion

        #region Overrides

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

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentAlertOptions));
        //     this.includeAlertErrorTextLabel = new System.Windows.Forms.Label();
        //     this.email = new System.Windows.Forms.CheckBox();
        //     this.pager = new System.Windows.Forms.CheckBox();
        //     this.additionNotificationMessageLabel = new System.Windows.Forms.Label();
        //     this.additionNotificationMessage = new System.Windows.Forms.TextBox();
        //     this.delayBetweenResponsesLabel = new System.Windows.Forms.Label();
        //     this.minutes = new System.Windows.Forms.NumericUpDown();
        //     this.seconds = new System.Windows.Forms.NumericUpDown();
        //     this.minutesLabel = new System.Windows.Forms.Label();
        //     this.secondsLabel = new System.Windows.Forms.Label();
        //     ((System.ComponentModel.ISupportInitialize)(this.minutes)).BeginInit();
        //     ((System.ComponentModel.ISupportInitialize)(this.seconds)).BeginInit();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // includeAlertErrorTextLabel
        //     // 
        //     resources.ApplyResources(this.includeAlertErrorTextLabel, "includeAlertErrorTextLabel");
        //     this.includeAlertErrorTextLabel.Name = "includeAlertErrorTextLabel";
        //     // 
        //     // email
        //     // 
        //     resources.ApplyResources(this.email, "email");
        //     this.email.Name = "email";
        //     // 
        //     // pager
        //     // 
        //     resources.ApplyResources(this.pager, "pager");
        //     this.pager.Name = "pager";
        //     // 
        //     // additionNotificationMessageLabel
        //     // 
        //     resources.ApplyResources(this.additionNotificationMessageLabel, "additionNotificationMessageLabel");
        //     this.additionNotificationMessageLabel.Name = "additionNotificationMessageLabel";
        //     // 
        //     // additionNotificationMessage
        //     // 
        //     resources.ApplyResources(this.additionNotificationMessage, "additionNotificationMessage");
        //     this.additionNotificationMessage.Name = "additionNotificationMessage";
        //     // 
        //     // delayBetweenResponsesLabel
        //     // 
        //     resources.ApplyResources(this.delayBetweenResponsesLabel, "delayBetweenResponsesLabel");
        //     this.delayBetweenResponsesLabel.Name = "delayBetweenResponsesLabel";
        //     // 
        //     // minutes
        //     // 
        //     resources.ApplyResources(this.minutes, "minutes");
        //     this.minutes.Name = "minutes";
        //     // 
        //     // seconds
        //     // 
        //     resources.ApplyResources(this.seconds, "seconds");
        //     this.seconds.Name = "seconds";
        //     // 
        //     // minutesLabel
        //     // 
        //     resources.ApplyResources(this.minutesLabel, "minutesLabel");
        //     this.minutesLabel.Name = "minutesLabel";
        //     // 
        //     // secondsLabel
        //     // 
        //     resources.ApplyResources(this.secondsLabel, "secondsLabel");
        //     this.secondsLabel.Name = "secondsLabel";
        //     // 
        //     // AgentAlertOptions
        //     // 
        //     this.Controls.Add(this.includeAlertErrorTextLabel);
        //     this.Controls.Add(this.email);
        //     this.Controls.Add(this.pager);
        //     this.Controls.Add(this.additionNotificationMessageLabel);
        //     this.Controls.Add(this.additionNotificationMessage);
        //     this.Controls.Add(this.delayBetweenResponsesLabel);
        //     this.Controls.Add(this.minutesLabel);
        //     this.Controls.Add(this.minutes);
        //     this.Controls.Add(this.secondsLabel);
        //     this.Controls.Add(this.seconds);
        //     this.Name = "AgentAlertOptions";
        //     resources.ApplyResources(this, "$this");
        //     ((System.ComponentModel.ISupportInitialize)(this.minutes)).EndInit();
        //     ((System.ComponentModel.ISupportInitialize)(this.seconds)).EndInit();
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region Private helpers
        /// <summary>
        /// Initializes controls with data from alert
        /// </summary>
        /// <param name="refresh">true if child controls need to be refreshed</param>
        // void InitializeControls(bool refresh)
        // {
        //     if (refresh == false && this.controlsInitialized)
        //         return;

        //     if (this.agentAlertName == null)
        //     {
        //         this.email.Checked = false;
        //         this.pager.Checked = false;
        //         this.additionNotificationMessage.Text = "";
        //         this.minutes.Value = 0;
        //         this.seconds.Value = 0;
        //     }
        //     else
        //     {
        //         Alert agentAlert = this.DataContainer.Server.JobServer.Alerts[this.agentAlertName];

        //         this.readOnly = !this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);

        //         this.email.Checked = (agentAlert.IncludeEventDescription & NotifyMethods.NotifyEmail) != 0;
        //         this.pager.Checked = (agentAlert.IncludeEventDescription & NotifyMethods.Pager) != 0;
                
        //         this.additionNotificationMessage.Text = agentAlert.NotificationMessage;
        //         // Whidbey bug: Value sometimes is not set
        //         long minutes, seconds;
        //         minutes = Math.DivRem(agentAlert.DelayBetweenResponses, 60, out seconds);
        //         this.minutes.Value = minutes; //AgentOperatorsGeneral.ConvertAgentTime(agentAlert.DelayBetweenResponses).Minute;
        //         this.seconds.Value = seconds; //AgentOperatorsGeneral.ConvertAgentTime(agentAlert.DelayBetweenResponses).Second;
        //         // Whidbey bug: We need to get values to refresh them
        //         minutes = (long)this.minutes.Value;
        //         seconds = (long)this.seconds.Value;
        //     }
        //     this.pager.Enabled = DataContainer.Server.HostPlatform == Microsoft.SqlServer.Management.Common.HostPlatformNames.Windows;
        //     this.controlsInitialized = true;
        // }

        #endregion
    }
}
