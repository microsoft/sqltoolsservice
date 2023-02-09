//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class AgentProxyAccountActions : ManagementActionBase
    {
        #region Constants
        internal const string ProxyAccountPropertyName = "proxyaccount";
        internal const string ProxyAccountSubsystem = "SubSystem";
        internal const string ProxyAccountMode = "Mode";
        internal const string ProxyAccountDuplicateMode = "Duplicate";
        internal const int SysnameLength = 256;
        internal const int DescriptionLength = 512;
        #endregion

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

        private AgentProxyInfo proxyInfo;

        // Flag indicating that proxy account should be duplicated
        private bool duplicate;

        private ConfigAction configAction;

        private bool readOnly = false;

        /// <summary>
        /// Main constructor. Creates all pages and adds them 
        /// to the tree control.
        /// </summary>
        public AgentProxyAccountActions(CDataContainer dataContainer, AgentProxyInfo proxyInfo, ConfigAction configAction)
        {
            this.DataContainer = dataContainer;
            this.proxyInfo = proxyInfo;
            this.configAction = configAction;

            if (configAction != ConfigAction.Drop)
            {
                // Create data structures
                int length = Enum.GetValues(typeof(ProxyPrincipalType)).Length;
                this.principals = new ArrayList[length];
                for (int i = 0; i < length; ++i)
                {
                    this.principals[i] = new ArrayList();
                }
                
                if (configAction == ConfigAction.Update)
                {
                    RefreshData();
                }
            }

            // Find out if we are creating a new proxy account or
            // modifying an existing one.
            GetProxyAccountName(dataContainer, ref this.proxyAccountName, ref this.duplicate);
        }

        public static string SysadminAccount
        {
            get { return SR.SysadminAccount; }
        }

        /// <summary>
        /// Main execution method. Creates or Alters a proxyAccount name.
        /// </summary>
        /// <returns>Always returns false</returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {        
           base.DoPreProcessExecution(runType, out executionResult);

           if (this.configAction == ConfigAction.Create)
            {
                return Create();
            }
            else if (this.configAction == ConfigAction.Update)
            {
                return Update();
            }
            else if (this.configAction == ConfigAction.Drop)
            {
                return Drop();
            }

            // Always return false to stop framework from calling OnRunNow
            return false;
        }

        /// <summary>
        /// It creates a new ProxyAccount or gets an existing
        /// one from JobServer and updates all properties.
        /// </summary>
        private bool CreateOrUpdateProxyAccount(AgentProxyInfo proxyInfo)
        {
            ProxyAccount proxyAccount = null;
            if (this.configAction == ConfigAction.Create)        
            {
                proxyAccount = new ProxyAccount(this.DataContainer.Server.JobServer, 
                                                proxyInfo.AccountName,
                                                proxyInfo.CredentialName,
                                                proxyInfo.IsEnabled,
                                                proxyInfo.Description);

                UpdateProxyAccount(proxyAccount);
                proxyAccount.Create();
            }
            else if (this.DataContainer.Server.JobServer.ProxyAccounts.Contains(this.proxyAccountName))
            {
                // Try refresh and check again
                this.DataContainer.Server.JobServer.ProxyAccounts.Refresh();
                if (this.DataContainer.Server.JobServer.ProxyAccounts.Contains(this.proxyAccountName))
                {              
                    proxyAccount = AgentProxyAccountActions.GetProxyAccount(this.proxyAccountName, this.DataContainer.Server.JobServer);    
                    // Set the other properties
                    proxyAccount.CredentialName = proxyInfo.CredentialName;
                    proxyAccount.Description = proxyInfo.Description;

                    UpdateProxyAccount(proxyAccount);
                    proxyAccount.Alter();

                    // Rename the proxy if needed
                    // This has to be done after Alter, in order to 
                    // work correcly when scripting this action.
                    if (this.proxyAccountName != proxyInfo.AccountName)
                    {
                        proxyAccount.Rename(proxyInfo.AccountName);
                    }
                }             
            }
            else
            {
                return false;
            }

            return true;

#if false  // @TODO - reenable subsystem code below

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
        
        public bool Create()
        {
            CreateOrUpdateProxyAccount(this.proxyInfo);
            return true;
        }

        public bool Update()
        {
            CreateOrUpdateProxyAccount(this.proxyInfo);
            return true;
        }

        public bool Drop()
        {
            if (this.DataContainer.Server.JobServer.ProxyAccounts.Contains(this.proxyAccountName))
            {
                // Try refresh and check again
                this.DataContainer.Server.JobServer.ProxyAccounts.Refresh();
                if (this.DataContainer.Server.JobServer.ProxyAccounts.Contains(this.proxyAccountName))
                {
                    ProxyAccount proxyAccount = AgentProxyAccountActions.GetProxyAccount(this.proxyAccountName, this.DataContainer.Server.JobServer);    
                    proxyAccount.DropIfExists();
                }
            }
        
            return false;
        }

        /// <summary>
        /// Called to update the proxy object
        /// </summary>
        public void UpdateProxyAccount(ProxyAccount proxyAccount)
        {
            if (proxyAccount == null)
            {
                throw new ArgumentNullException("proxyAccount");
            }

            ArrayList principalsToAdd    = new ArrayList();
            ArrayList principalsToRemove = new ArrayList();
            
            // Process Sql Logins 
            if (ExtractPermissionsToAddAndRemove(
                this.configAction == ConfigAction.Update ? proxyAccount.EnumLogins() : null, 
                this.principals[(int) ProxyPrincipalType.SqlLogin], 
                principalsToAdd, 
                principalsToRemove))
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
            if (ExtractPermissionsToAddAndRemove(
                this.configAction == ConfigAction.Update ? proxyAccount.EnumServerRoles() : null, 
                this.principals[(int) ProxyPrincipalType.ServerRole], 
                principalsToAdd, 
                principalsToRemove))
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
            if (ExtractPermissionsToAddAndRemove(
                this.configAction == ConfigAction.Update ? proxyAccount.EnumMsdbRoles() : null, 
                this.principals[(int) ProxyPrincipalType.MsdbRole], 
                principalsToAdd, 
                principalsToRemove))
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
           // Reset all principal collections
            for (int i = 0; i < this.principals.Length; ++i)
            {
                this.principals[i].Clear();
            }

            // Add new data from proxy account
            if (this.proxyAccountName != null)
            {
                ProxyAccount proxyAccount = GetProxyAccount(this.proxyAccountName, this.DataContainer.Server.JobServer);

                // Get all the logins associated with this proxy
                DataTable dt = proxyAccount.EnumLogins();
                foreach (DataRow row in dt.Rows)
                {
                    this.principals[(int)ProxyPrincipalType.SqlLogin].Add(row["Name"]);
                }

                // Get all the Server roles associated with this proxy
                dt = proxyAccount.EnumServerRoles();
                foreach (DataRow row in dt.Rows)
                {
                    this.principals[(int)ProxyPrincipalType.ServerRole].Add(row["Name"]);
                }

                // Get all the MSDB roles associated with this account
                dt = proxyAccount.EnumMsdbRoles();
                foreach (DataRow row in dt.Rows)
                {
                    this.principals[(int)ProxyPrincipalType.MsdbRole].Add(row["Name"]);
                }

                // only sa can modify
                this.readOnly = !this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);
            }
        }

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
            {
                throw new ArgumentException("proxyAccountName");
            }
            
            if (jobServer == null) 
            {
                throw new ArgumentNullException("jobServer");
            }

            ProxyAccount proxyAccount = jobServer.ProxyAccounts[proxyAccountName];
            if (proxyAccount == null)
            {
                // proxy not found. Try refreshing the collection
                jobServer.ProxyAccounts.Refresh();
                proxyAccount = jobServer.ProxyAccounts[proxyAccountName];

                // if still cannot get the proxy throw an exception
                if (proxyAccount == null)
                {
                    throw new ApplicationException(SR.ProxyAccountNotFound(proxyAccountName));
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
            parameters.GetParam(AgentProxyAccountActions.ProxyAccountPropertyName, ref proxyAccountName);
            if (proxyAccountName != null && proxyAccountName.Length == 0)
            {
                // Reset empty name back to null
                proxyAccountName = null;
            }

            // Get duplicate flag
            string mode = string.Empty;
            if (parameters.GetParam(AgentProxyAccountActions.ProxyAccountMode, ref mode) && 
                0 == string.Compare(mode, AgentProxyAccountActions.ProxyAccountDuplicateMode, StringComparison.Ordinal))
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
        internal static string[] ListProxyAccountsForSubsystem(
            ServerConnection serverConnection, 
            string subsystemName, 
            bool includeSysadmin)
        {
            ArrayList proxyAccounts = new ArrayList();

            // This method is valid only on Yukon and later
            if (serverConnection.ServerVersion.Major >= 9)
            {
                if (includeSysadmin)
                {
                    proxyAccounts.Add(AgentProxyAccountActions.SysadminAccount);
                }

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
    }
}
