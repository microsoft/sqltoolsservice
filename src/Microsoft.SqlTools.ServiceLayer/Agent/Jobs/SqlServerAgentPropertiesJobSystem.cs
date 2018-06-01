//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using System.Xml;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;
using System.Security;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// Summary description for SqlServerAgentPropertiesJobSystem.
	/// </summary>
    internal class SqlServerAgentPropertiesJobSystem : ManagementActionBase
	{
        #region UI controls members
        /// <summary>
		/// Required designer variable.
		/// </summary>
		// private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.Label labelShutdown;
        // private System.Windows.Forms.NumericUpDown numShutdown;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorJobStep;
        // private System.Windows.Forms.CheckBox checkProxy;
        // private System.Windows.Forms.Label labelProxySettings;
        // private System.Windows.Forms.Label labelUserName;
        // private System.Windows.Forms.TextBox textUserName;
        // private System.Windows.Forms.Label labelPassword;
		// private System.Windows.Forms.Label labelDomain;
		// private System.Windows.Forms.TextBox textDomain;
        // private System.Windows.Forms.TextBox textPassword;
        #endregion

        #region Private members

        private int shutDownWaitTime;
        private bool sysAdminOnly;
        private string domainUser = string.Empty;
        private string userName = string.Empty;
        private string passwdMask = new string('*', 16);
        private SecureString securePasswd = new SecureString();

        #endregion

        #region Implementation
        
        private void ApplyChanges()
        {
            // this.ExecutionMode	= ExecutionMode.Success;
            // bool    AlterValues = false;
            // bool    AlterProxyValues = false;

            // JobServer   agent   = DataContainer.Server.JobServer;            
            
            // try
            // {
            //     if (this.shutDownWaitTime != agent.AgentShutdownWaitTime)
            //     {
            //         agent.AgentShutdownWaitTime = this.shutDownWaitTime;
            //         AlterValues                 = true;
            //     }

            //     if (this.DataContainer.Server.Information.Version.Major < 9)
            //     {                    
            //         if (this.domainUser.Length != 0)
            //         {
            //             this.domainUser = this.domainUser + "\\" + this.userName;
            //         }
            //         else
            //         {
            //             this.domainUser = this.userName;
            //         }

            //         if (this.sysAdminOnly != agent.SysAdminOnly)
            //         {
            //             AlterProxyValues = true;
            //             if (true == this.sysAdminOnly)
            //             {
            //                 DataContainer.Server.ProxyAccount.IsEnabled = false;                            
            //             }
            //             else
            //             {
            //                 DataContainer.Server.ProxyAccount.IsEnabled = true;
            //                 DataContainer.Server.ProxyAccount.SetAccount(domainUser, this.securePasswd.ToString());                            
            //             }
            //         }
            //         else
            //         {
            //             if (this.sysAdminOnly == false)
            //             {                            
            //                 if (domainUser != DataContainer.Server.ProxyAccount.WindowsAccount)
            //                 {
            //                     AlterProxyValues = true;
            //                     DataContainer.Server.ProxyAccount.SetAccount(domainUser, this.securePasswd.ToString());
            //                 }
            //                 else
            //                 {
            //                     if (passwdMask != this.securePasswd.ToString())
            //                     {
            //                         AlterProxyValues = true;
            //                         DataContainer.Server.ProxyAccount.SetPassword(this.securePasswd.ToString());                                    
            //                     }
            //                 }
            //             }
            //         }
            //     }

            //     if (true == AlterProxyValues)
            //     {
            //         DataContainer.Server.ProxyAccount.Alter();
            //     }
            //     if(true == AlterValues)
            //     {
            //         agent.Alter();
            //     }
            // }
            // catch(SmoException smoex)
            // {
            //     DisplayExceptionMessage(smoex);
            //     this.ExecutionMode	= ExecutionMode.Failure;
            // }
        
        }

        
        private void InitProperties()
        {
            // JobServer agent = DataContainer.Server.JobServer;
            // this.numShutdown.Value  = agent.AgentShutdownWaitTime;

			// bool IsChecked;

			// if (this.DataContainer.Server.Information.Version.Major < 9)
			// {
            //     IsChecked = this.checkProxy.Checked = DataContainer.Server.ProxyAccount.IsEnabled;
			// 	this.textUserName.Enabled = IsChecked;
			// 	this.textPassword.Enabled = IsChecked;
			// 	this.textDomain.Enabled = IsChecked;
            //     if (true == IsChecked)
			// 	{
			// 		/// REVIEW-2003/12/06-macies Update this code to work with Yukon proxy accounts
			// 		/// The code below assumes that there can be only one proxy account on the server
			// 		/// as it was in Shiloh
            //         string agentHostLogin = DataContainer.Server.ProxyAccount.WindowsAccount;
            //         string[] domainUser = agentHostLogin.Split('\\');
            //         if (domainUser[0] != null)
            //         {
            //             this.textDomain.Text = domainUser[0];
            //         }
            //         if (domainUser.Rank > 0 && domainUser[1] != null)
            //         {
            //             this.textUserName.Text = domainUser[1];
            //         }
            //         this.textPassword.Text = passwdMask;
			// 	}
			// }
			// else
			// {
			// 	this.checkProxy.Checked = this.checkProxy.Enabled = false;
			// 	this.labelUserName.Enabled = this.textUserName.Enabled = false;
			// 	this.labelPassword.Enabled = this.textPassword.Enabled = false;
			// 	this.labelDomain.Enabled = this.textDomain.Enabled = false;
			// 	this.labelProxySettings.Enabled = false;
			// }
			

            
        }

        #endregion
        

        #region Trace support
        public const string m_strComponentName = "SqlServerAgentPropAdvanced";
        private string ComponentName
        {
            get
            {
                return m_strComponentName;
            }
        }
		#endregion

        #region IPanenForm Implementation


        // public override void OnGatherUiInformation(RunType action)
        // {
        //     this.shutDownWaitTime   = (int)this.numShutdown.Value;                                                
        //     this.sysAdminOnly       = !this.checkProxy.Checked;
        //     this.domainUser         = this.textDomain.Text.Trim();
        //     this.userName           = this.textUserName.Text.Trim();
        //     if (this.textPassword.Text.Length > 0)
        //     {
        //         this.securePasswd = new SqlSecureString(this.textPassword.Text);
        //     }
        //     else
        //     {
        //         this.securePasswd = new SqlSecureString(this.passwdMask);
        //     }
        //     this.ExecutionMode = ExecutionMode.Success;
        // }
        
        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {                
        //         return this;
        //     }
        // }


        /// <summary>
        /// IPanelForm.OnInitialization
        /// 
        /// TODO - in order to reduce IPanelForm container load time
        /// and to improve performance, IPanelForm-s should be able
        /// to lazy-initialize themself when IPanelForm.OnInitialization
        /// is called (a continer like TreePanelForm calls the
        /// OnInitialization() method before first OnSelection())
        /// </summary>
        // void IPanelForm.OnInitialization()
        // {            
        //     InitProperties();
        // }


        // public override void OnRunNow (object sender)
        // {
        //     base.OnRunNow(sender);
        //     ApplyChanges();
        // }


        // public override void OnReset(object sender)
        // {
        //     base.OnReset(sender);

        //     this.DataContainer.Server.JobServer.Refresh();
        //     this.DataContainer.Server.JobServer.AlertSystem.Refresh();
        //     InitProperties();        
        // }


        // void IPanelForm.OnSelection(TreeNode node)
        // {
        // }


        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {         
        // }        

		// bool ISupportValidation.Validate()
		// {			
		// 	if (false == CUtils.ValidateNumeric(this.numShutdown,SRError.InvalidNumericalValue(SRError.ControlShoutdownInterval,this.numShutdown.Minimum,this.numShutdown.Maximum),true))
		// 	{
		// 		return false;
		// 	}
		// 	return base.Validate();
		// }

        #endregion

        #region ctors
        
        public SqlServerAgentPropertiesJobSystem()
		{			
			//InitializeComponent();
		}


        public SqlServerAgentPropertiesJobSystem(CDataContainer dataContainer)
        {			
            //InitializeComponent();
            DataContainer       = dataContainer;                        
            //this.HelpF1Keyword	= AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.agent.job.f1";
        }
        
        
        #endregion

        #region Dispose
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
			}
			base.Dispose( disposing );
		}

        #endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerAgentPropertiesJobSystem));
            // this.labelShutdown = new System.Windows.Forms.Label();
            // this.numShutdown = new System.Windows.Forms.NumericUpDown();
            // this.separatorJobStep = new Microsoft.SqlServer.Management.Controls.Separator();
            // this.checkProxy = new System.Windows.Forms.CheckBox();
            // this.labelProxySettings = new System.Windows.Forms.Label();
            // this.labelUserName = new System.Windows.Forms.Label();
            // this.textUserName = new System.Windows.Forms.TextBox();
            // this.labelPassword = new System.Windows.Forms.Label();
            // this.textPassword = new System.Windows.Forms.TextBox();
            // this.labelDomain = new System.Windows.Forms.Label();
            // this.textDomain = new System.Windows.Forms.TextBox();
            // ((System.ComponentModel.ISupportInitialize)(this.numShutdown)).BeginInit();
            // this.SuspendLayout();
            // this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // // 
            // // labelShutdown
            // // 
            // resources.ApplyResources(this.labelShutdown, "labelShutdown");
            // this.labelShutdown.Name = "labelShutdown";
            // // 
            // // numShutdown
            // // 
            // resources.ApplyResources(this.numShutdown, "numShutdown");
            // this.numShutdown.Maximum = new decimal(new int[] {
            // 600,
            // 0,
            // 0,
            // 0});
            // this.numShutdown.Minimum = new decimal(new int[] {
            // 5,
            // 0,
            // 0,
            // 0});
            // this.numShutdown.Name = "numShutdown";
            // this.numShutdown.Value = new decimal(new int[] {
            // 15,
            // 0,
            // 0,
            // 0});
            // this.numShutdown.Enter += new System.EventHandler(this.numShutdown_Enter);
            // // 
            // // separatorJobStep
            // // 
            // resources.ApplyResources(this.separatorJobStep, "separatorJobStep");
            // this.separatorJobStep.Name = "separatorJobStep";
            // // 
            // // checkProxy
            // // 
            // resources.ApplyResources(this.checkProxy, "checkProxy");
            // this.checkProxy.Name = "checkProxy";
            // this.checkProxy.CheckedChanged += new System.EventHandler(this.checkProxy_CheckedChanged);
            // // 
            // // labelProxySettings
            // // 
            // resources.ApplyResources(this.labelProxySettings, "labelProxySettings");
            // this.labelProxySettings.Name = "labelProxySettings";
            // // 
            // // labelUserName
            // // 
            // resources.ApplyResources(this.labelUserName, "labelUserName");
            // this.labelUserName.Name = "labelUserName";
            // // 
            // // textUserName
            // // 
            // resources.ApplyResources(this.textUserName, "textUserName");
            // this.textUserName.Name = "textUserName";
            // // 
            // // labelPassword
            // // 
            // resources.ApplyResources(this.labelPassword, "labelPassword");
            // this.labelPassword.Name = "labelPassword";
            // // 
            // // textPassword
            // // 
            // resources.ApplyResources(this.textPassword, "textPassword");
            // this.textPassword.Name = "textPassword";
            // // 
            // // labelDomain
            // // 
            // resources.ApplyResources(this.labelDomain, "labelDomain");
            // this.labelDomain.Name = "labelDomain";
            // // 
            // // textDomain
            // // 
            // resources.ApplyResources(this.textDomain, "textDomain");
            // this.textDomain.Name = "textDomain";
            // // 
            // // SqlServerAgentPropertiesJobSystem
            // // 
            // this.Controls.Add(this.textDomain);
            // this.Controls.Add(this.labelDomain);
            // this.Controls.Add(this.textPassword);
            // this.Controls.Add(this.labelPassword);
            // this.Controls.Add(this.textUserName);
            // this.Controls.Add(this.labelUserName);
            // this.Controls.Add(this.labelProxySettings);
            // this.Controls.Add(this.checkProxy);
            // this.Controls.Add(this.separatorJobStep);
            // this.Controls.Add(this.numShutdown);
            // this.Controls.Add(this.labelShutdown);
            // this.Name = "SqlServerAgentPropertiesJobSystem";
            // resources.ApplyResources(this, "$this");
            // ((System.ComponentModel.ISupportInitialize)(this.numShutdown)).EndInit();
            // this.ResumeLayout(false);
            // this.PerformLayout();

        }
		#endregion

        #region UI controls Events

        // private void checkProxy_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked              = this.checkProxy.Checked;
        //     this.textUserName.Enabled   = IsChecked;
        //     this.textPassword.Enabled   = IsChecked;
		// 	this.textDomain.Enabled		= IsChecked;
        // }
        // private void numShutdown_Enter(object sender, System.EventArgs e)
        // {
        //     this.numShutdown.Tag	= this.numShutdown.Value;		
        // }
        #endregion        

	}
}








