using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Agent operators history page
    /// </summary>
    internal class AgentOperatorsHistory : AgentControlBase
    {
        #region Members

        // private System.Windows.Forms.Label mostRecentNotificationAttemptsLabel;
        // private System.Windows.Forms.Label byEmailLabel;
        // private System.Windows.Forms.TextBox byEmail;
        // private System.Windows.Forms.Label byPagerLabel;
        // private System.Windows.Forms.TextBox byPager;
        // /// <summary> 
        // /// Required designer variable.
        // /// </summary>
        // private System.ComponentModel.Container components = null;
        /// <summary>
        /// Agent operator object
        /// </summary>
        private AgentOperatorsData operatorsData = null;
        /// <summary>
        /// true if controls were initialized or false otherwise
        /// </summary>
        private bool controlsInitialized = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor is hidden
        /// </summary>
        private AgentOperatorsHistory()
        {
            // This call is required by the Windows.Forms Form Designer.
            //InitializeComponent();

            //Specify the help text for this feature.
            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.operator.history.f1";
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="agentOperatorName"></param>
        public AgentOperatorsHistory(CDataContainer dataContainer, AgentOperatorsData operatorsData)
            : this()
        {
            if(dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }
            if(operatorsData == null)
            {
                throw new ArgumentNullException("operatorsData");
            }
            DataContainer = dataContainer;
            this.operatorsData = operatorsData;

            //this.AllUIEnabled = true;

            InitializeControls(false);
        }

        #endregion

        #region Overrides

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                // if(components != null)
                // {
                //     components.Dispose();
                // }
            }
            base.Dispose(disposing);
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
        // }

        // UserControl IPanelForm.Panel
        // {
        //     get { return this; }
        // }

        #endregion

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentOperatorsHistory));
        //     this.mostRecentNotificationAttemptsLabel = new System.Windows.Forms.Label();
        //     this.byEmailLabel = new System.Windows.Forms.Label();
        //     this.byPagerLabel = new System.Windows.Forms.Label();
        //     this.byEmail = new System.Windows.Forms.TextBox();
        //     this.byPager = new System.Windows.Forms.TextBox();
        //     this.SuspendLayout();
        //     // 
        //     // mostRecentNotificationAttemptsLabel
        //     // 
        //     resources.ApplyResources(this.mostRecentNotificationAttemptsLabel, "mostRecentNotificationAttemptsLabel");
        //     this.mostRecentNotificationAttemptsLabel.Name = "mostRecentNotificationAttemptsLabel";
        //     // 
        //     // byEmailLabel
        //     // 
        //     resources.ApplyResources(this.byEmailLabel, "byEmailLabel");
        //     this.byEmailLabel.Name = "byEmailLabel";
        //     // 
        //     // byPagerLabel
        //     // 
        //     resources.ApplyResources(this.byPagerLabel, "byPagerLabel");
        //     this.byPagerLabel.Name = "byPagerLabel";
        //     // 
        //     // byEmail
        //     // 
        //     resources.ApplyResources(this.byEmail, "byEmail");
        //     this.byEmail.BackColor = System.Drawing.SystemColors.Control;
        //     this.byEmail.BorderStyle = System.Windows.Forms.BorderStyle.None;
        //     this.byEmail.Name = "byEmail";
        //     // 
        //     // byPager
        //     // 
        //     resources.ApplyResources(this.byPager, "byPager");
        //     this.byPager.BackColor = System.Drawing.SystemColors.Control;
        //     this.byPager.BorderStyle = System.Windows.Forms.BorderStyle.None;
        //     this.byPager.Name = "byPager";
        //     // 
        //     // AgentOperatorsHistory
        //     // 
        //     resources.ApplyResources(this, "$this");
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     this.Controls.Add(this.byPager);
        //     this.Controls.Add(this.byEmail);
        //     this.Controls.Add(this.byPagerLabel);
        //     this.Controls.Add(this.byEmailLabel);
        //     this.Controls.Add(this.mostRecentNotificationAttemptsLabel);
        //     this.Name = "AgentOperatorsHistory";
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region Private helpers

        /// <summary>
        /// Initializes controls with data from server
        /// </summary>
        /// <param name="refresh">true if refresh is needed</param>
        void InitializeControls(bool refresh)
        {
            if(this.controlsInitialized == true && refresh == false)
                return; // There is nothing to do here

            if(this.operatorsData == null)
            {
                throw new InvalidOperationException();
            }

            // if(operatorsData.LastEmailDate == DateTime.MinValue)
            // {
            //     this.byEmail.Text = AgentOperatorsHistorySR.NeverEmailed;
            // }
            // else
            // {
            //     this.byEmail.Text = operatorsData.LastEmailDate.ToString("F", CultureInfo.CurrentCulture);
            // }

            // if(operatorsData.LastPagerDate == DateTime.MinValue)
            // {
            //     this.byPager.Text = AgentOperatorsHistorySR.NeverPaged;
            // }
            // else
            // {
            //     this.byPager.Text = operatorsData.LastPagerDate.ToString("F", CultureInfo.CurrentCulture);
            // }            

            this.controlsInitialized = true;
        }

        #endregion
    }
}

