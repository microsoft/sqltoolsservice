//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class AgentProxyAccount : AgentConfigurationBase
    {
        internal enum ProxyPrincipalType
        {
            SqlLogin,
            MsdbRole,
            ServerRole
        }

        // Collections of principals for logins/server roles/msdb roles
        private ArrayList[] principals;

        // Name of the proxy account we work with
        private string proxyAccountName;

        // Flag indicating that proxy account should be duplicated
        private bool duplicate;

        public static string SysadminAccount
        {
            get { return "AgentProxyAccountSR.SysadminAccount"; }
        }

        /// <summary>
        /// Main constructor. Creates all pages and adds them 
        /// to the tree control.
        /// </summary>
        public AgentProxyAccount(CDataContainer dataContainer)
        {
            this.DataContainer = dataContainer;

            // Find out if we are creating a new proxy account or
            // modifying an existing one.
            GetProxyAccountName(dataContainer, ref this.proxyAccountName, ref this.duplicate);
        }

        /// <summary>
        /// It creates a new ProxyAccount or gets an existing
        /// one from JobServer and updates all properties.
        /// </summary>
        /// <param name="proxyAccount">A ProxyAccount to return</param>
        private void CreateProxyAccount(AgentProxyInfo proxyInfo, out ProxyAccount proxyAccount)
        {
            // check if there is already a proxy account with this name
            bool isUpdate = false;
            proxyAccount = null;

            if (this.DataContainer.Server.JobServer.ProxyAccounts.Contains(this.proxyAccountName))
            {
                // Try refresh and check again
                this.DataContainer.Server.JobServer.ProxyAccounts.Refresh();
                if (this.DataContainer.Server.JobServer.ProxyAccounts.Contains(this.proxyAccountName))
                {
                    isUpdate = true;
                    proxyAccount = AgentProxyAccount.GetProxyAccount(this.proxyAccountName, this.DataContainer.Server.JobServer);    
                    // Set the other properties
                    proxyAccount.CredentialName = proxyInfo.CredentialName;
                    proxyAccount.Description = proxyInfo.Description;

                    proxyAccount.Alter();

                    // Rename the proxy if needed
                    // This has to be done after Alter, in order to 
                    // work correcly when scripting this action.
                    if (this.proxyAccountName != proxyAccount.Name)
                    {
                        proxyAccount.Rename(proxyAccountName);
                    }
                }
            }

            if (!isUpdate)        
            {
                proxyAccount = new ProxyAccount(this.DataContainer.Server.JobServer, 
                                                proxyInfo.AccountName,
                                                proxyInfo.CredentialName,
                                                proxyInfo.IsEnabled,
                                                proxyInfo.Description);

                proxyAccount.Create();
            }

#if false
            // Update the subsystems
            foreach (AgentSubSystem subsystem in this.addSubSystems)
            {
                proxyAccount.AddSubSystem(subsystem);
            }

            foreach (AgentSubSystem subsystem in this.removeSubSystems)
            {
                proxyAccount.RemoveSubSystem(subsystem);

                // Update jobsteps that use this proxy accunt
                // when some subsystems are removed from it
                string reassignToProxyName = this.reassignToProxyNames[(int)subsystem];

                if (reassignToProxyName != null)
                {
                    // if version is sql 11 and above call SMO API  to reassign proxy account
                    if (Utils.IsSql11OrLater(this.DataContainer.Server.ServerVersion))
                    {
                        proxyAccount.Reassign(reassignToProxyName);
                    }
                    else
                    {
                        // legacy code
                        // Get a list of all job step objects that use this proxy and this subsystem
                        Request req = new Request();
                        req.Urn = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                "Server/JobServer/Job/Step[@ProxyName=\'{0}\' and @SubSystem={1}]",
                                                Urn.EscapeString(proxyAccount.Name),
                                                (int)subsystem);
                        req.Fields = new string[] { "Name" };
                        req.ParentPropertiesRequests = new PropertiesRequest[1] { new PropertiesRequest() };
                        req.ParentPropertiesRequests[0].Fields = new string[] { "Name" };

                        Enumerator en = new Enumerator();
                        DataTable table = en.Process(this.DataContainer.ServerConnection, req);
                        foreach (DataRow row in table.Rows)
                        {
                            // Get the actual job step object using urn
                            string urnString = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Server/JobServer/Job[@Name=\"{0}\"/Step[@Name=\"{1}\"", row["Job_Name"], row["Name"]);
                            Urn urn = new Urn(urnString);
                            JobStep jobStep = (JobStep)this.DataContainer.Server.GetSmoObject(urn);

                            jobStep.ProxyName = reassignToProxyName;
                            jobStep.Alter();
                        }
                    }
                }
            }
#endif 
        }

        public bool CreateOrUpdate(AgentProxyInfo proxyInfo)
        {
            bool isUpdate = !string.Equals(proxyInfo.AccountName, this.proxyAccountName);

            // Proceed with execution
            try
            {
                ProxyAccount proxyAccount;
                CreateProxyAccount(proxyInfo, out proxyAccount);
                UpdateProxyAccount(proxyAccount);

                //executionResult = ExecutionMode.Success;
            }
            catch(Exception)
            {
                // this.DisplayExceptionMessage(exception);
                // executionResult = ExecutionMode.Failure;
            }

            return true;
        }

        /// <summary>
        /// Called to update the proxy object with properties
        /// from this page.
        /// </summary>
        public void UpdateProxyAccount(ProxyAccount proxyAccount)
        {
            if (proxyAccount == null)
                throw new ArgumentNullException("proxyAccount");

            // Check if page has been initialized
            //if (this.IsHandleCreated)
            {
                ArrayList principalsToAdd    = new ArrayList();
                ArrayList principalsToRemove = new ArrayList();

                // Process Sql Logins 
                if (ExtractPermissionsToAddAndRemove(this.proxyAccountName != null? proxyAccount.EnumLogins() : null, this.principals[(int) ProxyPrincipalType.SqlLogin], principalsToAdd, principalsToRemove))
                {
                    foreach (string principal in principalsToRemove)
                    {
                        proxyAccount.RemoveLogin(principal);
                    }

                    foreach (string principal in principalsToAdd)
                    {
                        proxyAccount.AddLogin(principal);
                    }
                }

                // Process Server Roles
                if (ExtractPermissionsToAddAndRemove(this.proxyAccountName != null? proxyAccount.EnumServerRoles() : null, this.principals[(int) ProxyPrincipalType.ServerRole], principalsToAdd, principalsToRemove))
                {
                    foreach (string principal in principalsToRemove)
                    {
                        proxyAccount.RemoveServerRole(principal);
                    }

                    foreach (string principal in principalsToAdd)
                    {
                        proxyAccount.AddServerRole(principal);
                    }
                }

                // Process Msdb Roles
                if (ExtractPermissionsToAddAndRemove(this.proxyAccountName != null? proxyAccount.EnumMsdbRoles() : null, this.principals[(int) ProxyPrincipalType.MsdbRole], principalsToAdd, principalsToRemove))
                {
                    foreach (string principal in principalsToRemove)
                    {
                        proxyAccount.RemoveMsdbRole(principal);
                    }

                    foreach (string principal in principalsToAdd)
                    {
                        proxyAccount.AddMsdbRole(principal);
                    }
                }
            }
        }


        /// <summary>
        /// This method scans two list of principals - an existing one extracted from ProxyAccount object
        /// and a new one obtained from this panel and then it creates a two differential lists: one of
        /// principals to add and the other of principals to remove.
        /// </summary>
        /// <returns>true if there are any changes between existingPermissions and newPermissions lists</returns>
        private bool ExtractPermissionsToAddAndRemove(DataTable existingPermissions, ArrayList newPermissions, ArrayList principalsToAdd, ArrayList principalsToRemove)
        {
            // Reset both output lists
            principalsToAdd.Clear();
            principalsToRemove.Clear();

            // Sort both input lists
            DataRow[] existingRows = existingPermissions != null? existingPermissions.Select(string.Empty, "Name DESC") : new DataRow[] {};
            newPermissions.Sort();

            // Go through both lists at the same time and find differences
            int existingPos = 0;
            int newPos = 0;

            while (newPos < newPermissions.Count && existingPos < existingRows.Length)
            {
                int result = string.Compare(existingRows[existingPos]["Name"] as string, newPermissions[newPos] as string,StringComparison.Ordinal);

                if (result < 0)
                {
                    // element in existingRows is lower then element in newPermissions
                    // mark element in existingRows for removal
                    principalsToRemove.Add(existingRows[existingPos]["Name"]);
                    ++existingPos;
                }
                else if (result > 0)
                {
                    // element in existingRows is greater then element in newPermissions
                    // mark element in newPermissions for adding
                    principalsToAdd.Add(newPermissions[newPos]);
                    ++newPos;
                }
                else
                {
                    // Both elements are equal.
                    // Advance to the next element
                    ++existingPos;
                    ++newPos;
                }
            }

            while (newPos < newPermissions.Count)
            {
                // Some elements are still left
                // Copy them all to Add collection
                principalsToAdd.Add(newPermissions[newPos++]);
            }

            while (existingPos < existingRows.Length)
            {
                // Some elements are still left
                // Copy them all to Remove collection
                principalsToRemove.Add(existingRows[existingPos++]["Name"]);
            }

            return(principalsToAdd.Count > 0 || principalsToRemove.Count > 0);
        }

        private void RefreshData()
        {
            // List all the jobsteps that use current 
            // proxy account
            Request req = new Request();
            req.Urn = string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                                    "Server/JobServer/Job/Step[@ProxyName=\'{0}\']", 
                                    Urn.EscapeString(this.proxyAccountName));
            req.ResultType = ResultType.IDataReader;
            req.Fields = new string[] {"Name", "SubSystem"};
            req.ParentPropertiesRequests = new PropertiesRequest[1];
            req.ParentPropertiesRequests[0] = new PropertiesRequest(new string[] {"Name"});

            Enumerator en = new Enumerator();
            using (IDataReader reader = en.Process(this.DataContainer.ServerConnection, req).Data as IDataReader)
            {
                while (reader.Read())
                {
                    // this.referencesGrid.AddRow(new GridCellCollection(
                    //     new GridCell[] {
                    //                       new GridCell(reader.GetString(0)),   // Job Name (parent property is first)   
                    //                       new GridCell(reader.GetString(1)),   // JobStep Name
                    //                       new GridCell(JobStepSubSystems.LookupFriendlyName((AgentSubSystem) reader.GetInt32(2)))    // JobStep SubSystem
                    //                    }
                    //     ));
                }
            }

            // Set the total references number
           // this.totalReferences.Text = AgentProxyAccountSR.TotalReferences(this.referencesGrid.RowsNumber);
        }




        #region Implementation
        /// <summary>
        /// Adds panel to the tree control.
        /// </summary>
        /// <param name="panel">Panel control</param>
        /// <param name="panelName">Name of the panel in the tree</param>
        /// <param name="panelID">ID of the panel</param>
        // private void AddPanel(UserControl panel, string panelName, int panelID)
        // {
        //     PanelTreeNode itemNode = new PanelTreeNode();
        //     itemNode.Text = panelName;
        //     itemNode.Tag  = panelID;
        //     itemNode.Type = eNodeType.Item;

        //     AddNode(itemNode);
        //     AddView(panel);
        // }

        #region Constants
        internal const string ProxyAccountPropertyName = "proxyaccount";
        internal const string ProxyAccountSubsystem = "SubSystem";
        internal const string ProxyAccountMode = "Mode";
        internal const string ProxyAccountDuplicateMode = "Duplicate";
        internal const int SysnameLength = 256;
        internal const int DescriptionLength = 512;
        #endregion

        #region Static methods
        /// <summary>
        /// Retrieves an instance of ProxyAccount from job server using name provided.
        /// If proxy does not exist it throws an exception.
        /// </summary>
        /// <param name="proxyAccountName">Name of the proxy to get</param>
        /// <param name="jobServer">Job server to get the proxy from</param>
        /// <returns>A valid proxy account.</returns>
        internal static ProxyAccount GetProxyAccount(string proxyAccountName, JobServer jobServer)
        {
            if (proxyAccountName == null || proxyAccountName.Length == 0)
                throw new ArgumentException("proxyAccountName");
            if (jobServer == null)
                throw new ArgumentNullException("jobServer");

            ProxyAccount proxyAccount = jobServer.ProxyAccounts[proxyAccountName];
            if (proxyAccount == null)
            {
                // proxy not found. Try refreshing the collection
                jobServer.ProxyAccounts.Refresh();
                proxyAccount = jobServer.ProxyAccounts[proxyAccountName];

                // if still cannot get the proxy throw an exception
                if (proxyAccount == null)
                {
                    throw new ApplicationException("SRError.ProxyAccountNotFound(proxyAccountName)");
                }
            }
            return proxyAccount;
        }

        /// <summary>
        /// Retrieves a proxy account name from shared data containter.
        /// </summary>
        internal static void GetProxyAccountName(CDataContainer dataContainer, ref string proxyAccountName, ref bool duplicate)
        {
            STParameters parameters = new STParameters();
            parameters.SetDocument(dataContainer.Document);

            // Get proxy name
            parameters.GetParam(AgentProxyAccount.ProxyAccountPropertyName, ref proxyAccountName);
            if (proxyAccountName != null && proxyAccountName.Length == 0)
            {
                // Reset empty name back to null
                proxyAccountName = null;
            }

            // Get duplicate flag
            string mode = string.Empty;
            if (parameters.GetParam(AgentProxyAccount.ProxyAccountMode, ref mode) && 
                0 == string.Compare(mode, AgentProxyAccount.ProxyAccountDuplicateMode, StringComparison.Ordinal))
            {
                duplicate = true;
            }
        }

        /// <summary>
        /// Uses enumerator to list names of all proxy accounts that use specified Agent SubSystem.
        /// </summary>
        /// <param name="serverConnection">Connection to use.</param>
        /// <param name="subsystemName">Requested SubSystem name</param>
        /// <param name="includeSysadmin">If set to true, 'sysadmin' account is added as a first entry in
        /// the list of proxy accounts.</param>
        /// <returns>An array containing names of proxy accounts</returns>
        internal static string[] ListProxyAccountsForSubsystem(ServerConnection serverConnection, string subsystemName, bool includeSysadmin)
        {
            ArrayList proxyAccounts = new ArrayList();

            // This method is valid only on Yukon and later
            if (serverConnection.ServerVersion.Major >= 9)
            {
                if (includeSysadmin)
                    proxyAccounts.Add(AgentProxyAccount.SysadminAccount);

                // Get the list of proxy accounts
                Request req = new Request();

                req.Urn = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "Server/JobServer/ProxyAccount/AgentSubSystem[@Name = \"{0}\"]", 
                                        Urn.EscapeString(subsystemName));
                req.ResultType = ResultType.IDataReader;
                req.Fields = new string[] { "Name" };
                req.ParentPropertiesRequests = new PropertiesRequest[1] { new PropertiesRequest() };
                req.ParentPropertiesRequests[0].Fields = new string[] { "Name" };

                Enumerator en = new Enumerator();

                using (IDataReader reader = en.Process(serverConnection, req).Data as IDataReader)
                {
                    while (reader.Read())
                    {
                        proxyAccounts.Add(reader.GetString(0));
                    }
                }
            }

            return (string[]) proxyAccounts.ToArray(typeof(string));
        }
        #endregion

        #endregion
    }
}
