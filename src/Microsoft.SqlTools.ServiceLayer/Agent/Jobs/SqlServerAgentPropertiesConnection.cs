//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesConnection.
    /// </summary>	
    internal class SqlServerAgentPropertiesConnection : ManagementActionBase
    {        
        bool SqlPasswordChanged = false;


        #region Implementation

        // private void ApplyChanges()
        // {
        //     this.ExecutionMode = ExecutionMode.Success;
        //     JobServer agent = DataContainer.Server.JobServer;
        //     string OriginalLogin = agent.HostLoginName;
        //     string CurrentLogin = "";
        //     bool AlterValues = false;
        //     try
        //     {
        //         if (true == this.radioSQLAuth.Checked)
        //         {
        //             CurrentLogin = (this.comboLogin.SelectedItem).ToString();
        //         }
        //         if (String.Compare(CurrentLogin, OriginalLogin, StringComparison.OrdinalIgnoreCase) != 0 || true == SqlPasswordChanged)
        //         {
        //             if (CurrentLogin.Length > 0)
        //             {
        //                 agent.SetHostLoginAccount(CurrentLogin, this.textPassword.Text);
        //                 VerifyLogin();
        //             }
        //             else
        //             {
        //                 agent.ClearHostLoginAccount();
        //             }
        //         }

        //         string SelectedAlias = this.comboAliases.Text;                

        //         if (String.Compare(SelectedAlias, agent.LocalHostAlias, StringComparison.OrdinalIgnoreCase) != 0)
        //         {
        //             AlterValues = true;

        //             agent.LocalHostAlias = SelectedAlias;

        //         }
        //         if (true == AlterValues)
        //         {
        //             agent.Alter();
        //         }
        //     }
        //     catch (SmoException smoex)
        //     {
        //         DisplayExceptionMessage(smoex);
        //         this.ExecutionMode = ExecutionMode.Failure;
        //     }

        // }

        // private void InitProperties()
        // {            
        //     try
        //     {                
        //         JobServer agent = DataContainer.Server.JobServer;                

        //         if (this.DataContainer.Server.Information.Version.Major < 9)
        //         {

        //             PopulateLoginCombo();

        //             bool IsWinAuth = (agent.HostLoginName.Length == 0);
        //             this.radioWinAuth.Checked = IsWinAuth;
        //             this.radioSQLAuth.Checked = !IsWinAuth;
        //             if (false == IsWinAuth)
        //             {
        //                 string SqlLogin = agent.HostLoginName;
        //                 if (!this.comboLogin.Items.Contains(SqlLogin))
        //                 {
        //                     this.comboLogin.Items.Add(SqlLogin);
        //                 }
        //                 this.comboLogin.SelectedItem = SqlLogin;
        //                 this.textPassword.Text = "**********";
        //                 SqlPasswordChanged = false;
        //             }
        //         }
        //         else
        //         {
        //             this.radioWinAuth.Checked = true;
        //             this.radioWinAuth.Enabled = this.radioSQLAuth.Enabled = this.comboLogin.Enabled = false;
        //             this.textPassword.Enabled = this.labelLogin.Enabled = this.labelPasswd.Enabled = false;
        //         }

        //         string ServerAliasHost = agent.LocalHostAlias;
        //         this.comboAliases.Text = ServerAliasHost;                

        //         // Managed Instances do not allow changing
        //         // "alias local host server"
        //         //
        //         this.comboAliases.Enabled = DataContainer.Server.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance;
        //     }
        //     catch (Exception)
        //     {                
        //     }
        // }

        #endregion


        #region ctors

        public SqlServerAgentPropertiesConnection(CDataContainer dataContainer)
        {
            DataContainer = dataContainer;
            //this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.agent.connection.f1";
        }

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
