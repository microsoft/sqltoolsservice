//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropSheet.
    /// </summary>
    internal sealed class SqlServerAgentPropSheet : ManagementActionBase
    {
        public SqlServerAgentPropSheet()
        {            
        }

        public SqlServerAgentPropSheet(CDataContainer dataContainer)
        {                        
            DataContainer = dataContainer;
            Initialize(dataContainer);
        }
        
        public void Initialize(CDataContainer dataContainer)
        {                        
            // PanelTreeNode    node;
            // PanelTreeNode    auxNode;
            // CUtils            util        = new CUtils();            
            // STParameters    param;                        

            // param        = new STParameters();

            // param.SetDocument(dataContainer.Document);            
            
            // UserControl AgentPropertiesGeneral = new SqlServerAgentPropertiesGeneral(dataContainer);            
            // UserControl AgentPropertiesAdvanced = new SqlServerAgentPropertiesAdvanced(dataContainer);                        
            // UserControl AgentPropertiesJobSystem = new SqlServerAgentPropertiesJobSystem(dataContainer);            
            // UserControl AgentPropertiesHistory = new SqlServerAgentPropertiesHistory(dataContainer);            
            // UserControl AgentPropertiesConnection = new SqlServerAgentPropertiesConnection(dataContainer);            
            // UserControl AgentPropertiesAlertSystem = new SqlServerAgentPropertiesAlertSystem(dataContainer);            
            
            // AddView(AgentPropertiesGeneral);
            // AddView(AgentPropertiesAdvanced);
            // AddView(AgentPropertiesAlertSystem);
            // AddView(AgentPropertiesJobSystem);
            // AddView(AgentPropertiesConnection);
            // AddView(AgentPropertiesHistory);            
            
            // this.Icon = util.LoadIcon("server.ico");            

            // node        = new PanelTreeNode();
            // node.Text    = SqlServerAgentSR.AgentPropertiesNode;
            // node.Type    = eNodeType.Folder;
            // node.Tag    = 1;

            // auxNode    = new PanelTreeNode();
            // auxNode.Text    = SqlServerAgentSR.GeneralNode;
            // auxNode.Tag        = 1;
            // auxNode.Type    = eNodeType.Item;
            // node.Nodes.Add(auxNode);

            // SelectNode(auxNode);            

            
            // auxNode    = new PanelTreeNode();
            // auxNode.Text    = SqlServerAgentSR.AdvancedNode;
            // auxNode.Tag        = 2;
            // auxNode.Type    = eNodeType.Item;
            // node.Nodes.Add(auxNode);

            // auxNode    = new PanelTreeNode();
            // auxNode.Text    = SqlServerAgentSR.AlertSystemNode;
            // auxNode.Tag        = 3;
            // auxNode.Type    = eNodeType.Item;
            // node.Nodes.Add(auxNode);

            // auxNode    = new PanelTreeNode();
            // auxNode.Text    = SqlServerAgentSR.JobSystemNode;
            // auxNode.Tag        = 4;
            // auxNode.Type    = eNodeType.Item;
            // node.Nodes.Add(auxNode);

            // auxNode    = new PanelTreeNode();
            // auxNode.Text    = SqlServerAgentSR.ConnectionNode;
            // auxNode.Tag        = 5;
            // auxNode.Type    = eNodeType.Item;
            // node.Nodes.Add(auxNode);

            // auxNode    = new PanelTreeNode();
            // auxNode.Text    = SqlServerAgentSR.HistoryNode;
            // auxNode.Tag        = 6;
            // auxNode.Type    = eNodeType.Item;
            // node.Nodes.Add(auxNode);
            

            // AddNode(node);            

            // Text = SqlServerAgentSR.AgentPropertiesTitle(dataContainer.Server.Name);
        }
    }
    /// <summary>
    /// Summary description for LogonUser.
    /// </summary>
    internal sealed class Impersonate
    {
        
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId="0", Justification="Temporary suppression: Revisit in SQL 11")]
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId="1", Justification="Temporary suppression: Revisit in SQL 11")]
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId="2", Justification="Temporary suppression: Revisit in SQL 11")]
        [DllImport("advapi32", SetLastError=true)]
        internal static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword, 
                                              int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("Kernel32")]
        internal static extern int GetLastError();

        [DllImport("advapi32")]
        internal static extern bool CloseHandle(IntPtr phToken);
    
        internal static string LogonUserManaged(string userName, string password, ref IntPtr token)
        {            
            String [] pair = userName.Split("\\".ToCharArray());    
            int LogonMethod = 3;        // logon using blah

            if (pair.Length > 2)
            {
                return SRError.InvalidUsername(userName);
            }

            bool retval = false;
            
            if (pair.Length == 2)
            {                
                retval = Impersonate.LogonUser(pair[1], pair[0], password, LogonMethod, 0, out token);
            }
            else
            {
                retval = Impersonate.LogonUser(pair[0], ".", password, LogonMethod, 0, out token);
            }
            if (true == retval)
            {
                return string.Empty;
            }
            else
            {
                return "SRError.LogonFailed(userName)";
            }
        }
    }    
}








