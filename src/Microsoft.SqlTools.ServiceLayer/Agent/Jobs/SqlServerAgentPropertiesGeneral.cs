#if false
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        #endregion

        #region ctors

        public SqlServerAgentPropertiesGeneral(CDataContainer dataContainer)
        {
            DataContainer = dataContainer;
            ServerVersion = DataContainer.Server.Information.Version.Major;
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

        #region Dispose

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
        #endregion
    }
}

#endif