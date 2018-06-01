//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.ServiceProcess;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesGeneral.
    /// </summary>
    internal class SqlServerAgentPropertiesGeneral : ManagementActionBase
    {

        #region Implementation members
        private int ServerVersion = 0;
        const int Version_90 = 9;
        private System.ComponentModel.IContainer components;
        #endregion

        #region UI controls members
        /// <summary>
        /// Required designer variable.
        /// </summary>
        // private System.Windows.Forms.Label labelServiceState;
        // private System.Windows.Forms.Label textServiceState;
        // private System.Windows.Forms.CheckBox checkAutoSql;
        // private System.Windows.Forms.CheckBox checkAutoAgent;
        // private System.Windows.Forms.Label labelFileName;
        // private System.Windows.Forms.TextBox textFileName;
        // private System.Windows.Forms.Button buttonBrowse;
        // private System.Windows.Forms.CheckBox checkIncludeTrace;
        // private System.Windows.Forms.CheckBox checkWriteOem;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorService;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorLog;
        // private System.Windows.Forms.ToolTip toolTip1;
        #endregion

        #region Trace support
        public const string m_strComponentName = "SqlServerAgentPropGeneral";
        private string ComponentName
        {
            get
            {
                return m_strComponentName;
            }
        }
        #endregion

        #region ctors

        public SqlServerAgentPropertiesGeneral()
        {
            ServerVersion = 9; // default to yukon
            //InitializeComponent();
        }

        public SqlServerAgentPropertiesGeneral(CDataContainer dataContainer)
        {
            //InitializeComponent();
            DataContainer = dataContainer;
            ServerVersion = DataContainer.Server.Information.Version.Major;
            // this.AllUIEnabled = true;
            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.agent.general.f1";
        }

        #endregion

        #region Implementation


        // private void ApplyChanges()
        // {
        //     this.ExecutionMode = ExecutionMode.Success;

        //     JobServer agent = DataContainer.Server.JobServer;

        //     bool AlterValues = false;


        //     try
        //     {
        //         if (this.checkAutoAgent.Checked != agent.SqlAgentRestart)
        //         {
        //             AlterValues = true;
        //             agent.SqlAgentRestart = this.checkAutoAgent.Checked;
        //         }

        //         if (this.checkAutoSql.Checked != agent.SqlServerRestart)
        //         {
        //             AlterValues = true;
        //             agent.SqlServerRestart = this.checkAutoSql.Checked;
        //         }

        //         if (this.textFileName.Text != agent.ErrorLogFile)
        //         {
        //             AlterValues = true;
        //             agent.ErrorLogFile = this.textFileName.Text;
        //         }

        //         if ((this.checkIncludeTrace.Checked == true && agent.AgentLogLevel != AgentLogLevels.All) || (this.checkIncludeTrace.Checked == false && agent.AgentLogLevel == AgentLogLevels.All))
        //         {
        //             AlterValues = true;
        //             agent.AgentLogLevel = (this.checkIncludeTrace.Checked == true) ? AgentLogLevels.All : AgentLogLevels.Errors;
        //         }
        //         if (this.checkWriteOem.Checked != agent.WriteOemErrorLog)
        //         {
        //             AlterValues = true;
        //             agent.WriteOemErrorLog = this.checkWriteOem.Checked;
        //         }
                
        //         if (true == AlterValues)
        //         {
        //             agent.Alter();
        //         }
        //     }
        //     catch (SMO.SmoException smoex)
        //     {
        //         DisplayExceptionMessage(smoex);
        //         this.ExecutionMode = ExecutionMode.Failure;
        //     }

        // }

        private void InitProperties()
        {

            SMO.Server smoServer = DataContainer.Server;
            string SqlServerName = smoServer.Information.NetName;
            bool instanceSupported = smoServer.Information.Version >= new Version(8, 0);
            string InstanceName = instanceSupported ? smoServer.InstanceName : String.Empty;
            string machineName = CUtils.GetMachineName(SqlServerName);

            bool IsDefaultInstance = true;            
            bool agentStopped = false;

            /// Determine if we work with the default instance
            if (0 != InstanceName.Length)
            {
                /// we work with a named instance                 
                IsDefaultInstance = false;
            }

            string AgentServiceName = string.Empty;

            if (false == IsDefaultInstance)
            {
                AgentServiceName = "SQLAgent$" + InstanceName;
            }
            else
            {
                AgentServiceName = "sqlserveragent";
            }

            // try
            // {
            //     using (ServiceController agentService = new ServiceController(AgentServiceName, machineName))
            //     {                    
            //         if (agentService.Status == ServiceControllerStatus.Stopped)
            //         {
            //             agentStopped = true;
            //         }
            //         this.textServiceState.Text = GetServiceState(agentService.Status);
            //     }
            // }
            // catch (Exception)
            // {
            //     this.textServiceState.Text = string.Empty;
            // }

            // this.textFileName.ReadOnly = true;
            // this.buttonBrowse.Enabled = agentStopped;
            // this.checkWriteOem.Enabled = agentStopped;

            /// Get the job server (agent) object
            JobServer agent = smoServer.JobServer;
            
            // try
            // {
            //     this.checkAutoAgent.Checked = agent.SqlAgentRestart;
            // }
            // catch (SMO.SmoException)
            // {
            //     this.checkAutoAgent.Enabled = false;
            // }

            // try
            // {
            //     this.checkAutoSql.Checked = agent.SqlServerRestart;
            // }
            // catch (SMO.SmoException)
            // {
            //     this.checkAutoSql.Enabled = false;
            // }

            // Managed Instances (CloudLifter) do not allow
            // changing setting related to service restarts.
            // Just disable the UI elements altogether.
            //
            // this.checkAutoAgent.Enabled = smoServer.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance;
            // this.checkAutoSql.Enabled = smoServer.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance;

            // /// Error log file name
            // this.textFileName.Text = agent.ErrorLogFile;
            // this.toolTip1.SetToolTip(this.textFileName, agent.ErrorLogFile);

            // /// Log level - All == Include trace messages 
            // this.checkIncludeTrace.Checked = (agent.AgentLogLevel == AgentLogLevels.All);

            // /// Write OEM file
            // this.checkWriteOem.Checked = agent.WriteOemErrorLog;
            
        }


        // private string GetServiceState(ServiceControllerStatus serviceState)
        // {
        //     if (serviceState == ServiceControllerStatus.Running)
        //     {
        //         return SqlServerAgentSR.ServiceState_Running;
        //     }
        //     else
        //     {
        //         if (serviceState == ServiceControllerStatus.Stopped)
        //         {
        //             return SqlServerAgentSR.ServiceState_Stopped;
        //         }
        //         else
        //         {
        //             if (serviceState == ServiceControllerStatus.Paused)
        //             {
        //                 return SqlServerAgentSR.ServiceState_Paused;
        //             }
        //             else
        //             {
        //                 if (serviceState == ServiceControllerStatus.ContinuePending)
        //                 {
        //                     return SqlServerAgentSR.ServiceState_ContinuePending;
        //                 }
        //                 else
        //                 {
        //                     if (serviceState == ServiceControllerStatus.StartPending)
        //                     {
        //                         return SqlServerAgentSR.ServiceState_StartPending;
        //                     }
        //                     else
        //                     {
        //                         if (serviceState == ServiceControllerStatus.PausePending)
        //                         {
        //                             return SqlServerAgentSR.ServiceState_PausePending;
        //                         }
        //                         else
        //                         {
        //                             if (serviceState == ServiceControllerStatus.StopPending)
        //                             {
        //                                 return SqlServerAgentSR.ServiceState_StopPending;
        //                             }
        //                             else
        //                             {
        //                                 return SqlServerAgentSR.Unknown;
        //                             }
        //                         }
        //                     }
        //                 }

        //             }
        //         }
        //     }
        // }

        #endregion

        #region IPanenForm Implementation

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


        // public override void OnRunNow(object sender)
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



        #endregion

        #region Dispose

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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     this.components = new System.ComponentModel.Container();
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerAgentPropertiesGeneral));
        //     this.separatorService = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.labelServiceState = new System.Windows.Forms.Label();
        //     this.textServiceState = new System.Windows.Forms.Label();
        //     this.checkAutoSql = new System.Windows.Forms.CheckBox();
        //     this.checkAutoAgent = new System.Windows.Forms.CheckBox();
        //     this.separatorLog = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.labelFileName = new System.Windows.Forms.Label();
        //     this.textFileName = new System.Windows.Forms.TextBox();
        //     this.buttonBrowse = new System.Windows.Forms.Button();
        //     this.checkIncludeTrace = new System.Windows.Forms.CheckBox();
        //     this.checkWriteOem = new System.Windows.Forms.CheckBox();
        //     this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
        //     this.SuspendLayout();
        //     // 
        //     // separatorService
        //     // 
        //     resources.ApplyResources(this.separatorService, "separatorService");
        //     this.separatorService.Name = "separatorService";
        //     // 
        //     // labelServiceState
        //     // 
        //     resources.ApplyResources(this.labelServiceState, "labelServiceState");
        //     this.labelServiceState.Name = "labelServiceState";
        //     // 
        //     // textServiceState
        //     // 
        //     resources.ApplyResources(this.textServiceState, "textServiceState");
        //     this.textServiceState.Name = "textServiceState";
        //     // 
        //     // checkAutoSql
        //     // 
        //     resources.ApplyResources(this.checkAutoSql, "checkAutoSql");
        //     this.checkAutoSql.Name = "checkAutoSql";
        //     // 
        //     // checkAutoAgent
        //     // 
        //     resources.ApplyResources(this.checkAutoAgent, "checkAutoAgent");
        //     this.checkAutoAgent.Name = "checkAutoAgent";
        //     // 
        //     // separatorLog
        //     // 
        //     resources.ApplyResources(this.separatorLog, "separatorLog");
        //     this.separatorLog.Name = "separatorLog";
        //     // 
        //     // labelFileName
        //     // 
        //     resources.ApplyResources(this.labelFileName, "labelFileName");
        //     this.labelFileName.Name = "labelFileName";
        //     // 
        //     // textFileName
        //     // 
        //     resources.ApplyResources(this.textFileName, "textFileName");
        //     this.textFileName.Name = "textFileName";
        //     // 
        //     // buttonBrowse
        //     // 
        //     resources.ApplyResources(this.buttonBrowse, "buttonBrowse");
        //     this.buttonBrowse.Name = "buttonBrowse";
        //     this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
        //     // 
        //     // checkIncludeTrace
        //     // 
        //     resources.ApplyResources(this.checkIncludeTrace, "checkIncludeTrace");
        //     this.checkIncludeTrace.Name = "checkIncludeTrace";
        //     // 
        //     // checkWriteOem
        //     // 
        //     resources.ApplyResources(this.checkWriteOem, "checkWriteOem");
        //     this.checkWriteOem.Name = "checkWriteOem";
        //     // 
        //     // SqlServerAgentPropertiesGeneral
        //     // 
        //     resources.ApplyResources(this, "$this");
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     this.Controls.Add(this.checkWriteOem);
        //     this.Controls.Add(this.checkIncludeTrace);
        //     this.Controls.Add(this.buttonBrowse);
        //     this.Controls.Add(this.textFileName);
        //     this.Controls.Add(this.labelFileName);
        //     this.Controls.Add(this.separatorLog);
        //     this.Controls.Add(this.checkAutoAgent);
        //     this.Controls.Add(this.checkAutoSql);
        //     this.Controls.Add(this.textServiceState);
        //     this.Controls.Add(this.labelServiceState);
        //     this.Controls.Add(this.separatorService);
        //     this.Name = "SqlServerAgentPropertiesGeneral";
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region UI controls event handlers

        // private void buttonBrowse_Click(object sender, System.EventArgs e)
        // {
        //     using (BrowseFolder browse = new BrowseFolder(this.DataContainer.ServerConnection, true, (IMessageBoxProvider)this.ServiceProvider.GetService(typeof(IMessageBoxProvider))))
        //     {
        //         if (!string.IsNullOrEmpty(this.textFileName.Text))
        //         {                    
        //             browse.StartPath = SMO.PathWrapper.GetDirectoryName(this.textFileName.Text);
        //         }
        //         DialogResult res = browse.ShowDialog(this);
        //         {
        //             if (res == DialogResult.OK)
        //             {
        //                 this.textFileName.Text = browse.SelectedFullFileName;
        //             }
        //         }
        //     }
        // }
        #endregion
    }
}








