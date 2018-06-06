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
using System.Security;
using System.Xml;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// Summary description for SqlServerAgentPropertiesJobSystem.
	/// </summary>
    internal class SqlServerAgentPropertiesJobSystem : ManagementActionBase
	{
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

        #endregion
       

        #region ctors
        
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
	}
}








