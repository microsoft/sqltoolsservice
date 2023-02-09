//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Main class for Security Service functionality
    /// </summary>
    public sealed class SecurityService : IDisposable
    {
        private bool disposed;

        private ConnectionService connectionService;

        private static readonly Lazy<SecurityService> instance = new Lazy<SecurityService>(() => new SecurityService());

        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public SecurityService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SecurityService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                connectionService ??= ConnectionService.Instance;
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the Security Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Credential request handlers
            this.ServiceHost.SetRequestHandler(CreateCredentialRequest.Type, HandleCreateCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateCredentialRequest.Type, HandleUpdateCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(DeleteCredentialRequest.Type, HandleDeleteCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(GetCredentialsRequest.Type, HandleGetCredentialsRequest, true);

            // Login request handlers
            this.ServiceHost.SetRequestHandler(CreateLoginRequest.Type, HandleCreateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(DeleteLoginRequest.Type, HandleDeleteLoginRequest, true);

            // User request handlers
            this.ServiceHost.SetRequestHandler(CreateUserRequest.Type, HandleCreateUserRequest, true);
        }


#region "Login Handlers"        

        /// <summary>
        /// Handle request to create a login
        /// </summary>
        internal async Task HandleCreateLoginRequest(CreateLoginParams parameters, RequestContext<CreateLoginResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
            // if (connInfo == null) 
            // {
            //     // raise an error
            // }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginPrototype prototype = new LoginPrototype(dataContainer.Server, parameters.Login);

            if (prototype.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin)
            {
                // check that there is a password
                // this check is made if policy enforcement is off
                // with policy turned on we do not display this message, instead we let server
                // return the error associated with null password (coming from policy) - see bug 124377
                if (prototype.SqlPassword.Length == 0 && prototype.EnforcePolicy == false)
                {
                    // raise error here                                                   
                }

                // check that password and confirm password controls' text matches
                if (0 != String.Compare(prototype.SqlPassword, prototype.SqlPasswordConfirm, StringComparison.Ordinal))
                {
                    // raise error here
                }                 
            }

            prototype.ApplyGeneralChanges(dataContainer.Server);

            await requestContext.SendResult(new CreateLoginResult()
            {
                Login = parameters.Login,
                Success = true,
                ErrorMessage = string.Empty
            });
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteLoginRequest(DeleteLoginParams parameters, RequestContext<ResultStatus> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
            // if (connInfo == null) 
            // {
            //     // raise an error
            // }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            Login login = dataContainer.Server.Logins[parameters.LoginName];
     
            dataContainer.SqlDialogSubject = login;
            DoDropObject(dataContainer);
           
            await requestContext.SendResult(new ResultStatus()
            {
                Success = true,
                ErrorMessage = string.Empty
            });
        }

#endregion

#region "User Handlers"

        internal Task<Tuple<bool, string>> ConfigureUser(
            string ownerUri,
            UserInfo user,
            ConfigAction configAction,
            RunType runType)
        {
            return Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    if (connInfo == null)
                    {
                        throw new ArgumentException("Invalid connection URI '{0}'", ownerUri);
                    }

                    var serverConnection = ConnectionService.OpenServerConnection(connInfo, "DataContainer");
                    var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
                    connectionInfoWithConnection.ServerConnection = serverConnection;

                    string urn  = string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                        "Server/Database[@Name='{0}']", 
                        Urn.EscapeString(serverConnection.DatabaseName));

                    ActionContext context = new ActionContext(serverConnection, "new_user", urn);
                    DataContainerXmlGenerator containerXml = new DataContainerXmlGenerator(context);
                    containerXml.AddProperty("itemtype", "User");

                    XmlDocument xmlDoc = containerXml.GenerateXmlDocument();
                    bool objectExists = configAction != ConfigAction.Create;
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfoWithConnection, xmlDoc);

                    using (var actions = new UserActions(dataContainer, user, configAction))
                    {
                        var executionHandler = new ExecutonHandler(actions);
                        executionHandler.RunNow(runType, this);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        /// <summary>
        /// Handle request to create a user
        /// </summary>
        internal async Task HandleCreateUserRequest(CreateUserParams parameters, RequestContext<CreateUserResult> requestContext)
        {
             var result = await ConfigureUser(parameters.OwnerUri,
                parameters.User,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CreateUserResult()
            {
                User = parameters.User,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });            
        }

        
        private void GetDefaultLanguageOptions(CDataContainer dataContainer)
        {
            // this.defaultLanguageComboBox.Items.Clear();            
            // this.defaultLanguageComboBox.Items.Add(defaultLanguagePlaceholder);            

            // sort the languages alphabetically by alias
            SortedList sortedLanguages = new SortedList(Comparer.Default);

            LanguageUtils.SetLanguageDefaultInitFieldsForDefaultLanguages(dataContainer.Server);
            foreach (Language language in dataContainer.Server.Languages)
            {
                LanguageDisplay listValue = new LanguageDisplay(language);
                sortedLanguages.Add(language.Alias, listValue);
            }

            // add the language display objects to the combo box
            foreach (LanguageDisplay languageDisplay in sortedLanguages.Values)
            {
                //this.defaultLanguageComboBox.Items.Add(languageDisplay);
            }
        }

        // code needs to be ported into the useraction class
        // public void UserMemberships_OnRunNow(object sender, CDataContainer dataContainer)
        // {
        //     UserPrototype currentPrototype = UserPrototypeFactory.GetInstance(dataContainer).CurrentPrototype;

        //     //In case the UserGeneral/OwnedSchemas pages are loaded,
        //     //those will takes care of applying membership changes also.
        //     //Hence, we only need to apply changes in this method when those are not loaded.
        //     if (!currentPrototype.IsRoleMembershipChangesApplied)
        //     {
        //         //base.OnRunNow(sender);

        //         User user = currentPrototype.ApplyChanges();

        //         //this.ExecutionMode = ExecutionMode.Success;
        //         dataContainer.ObjectName = currentPrototype.Name;
        //         dataContainer.SqlDialogSubject = user;
        //     }

        //     //setting back to original after changes are applied
        //     currentPrototype.IsRoleMembershipChangesApplied = false;
        // }

        // /// <summary>
        // /// implementation of OnPanelRunNow
        // /// </summary>
        // /// <param name="node"></param>
        // public void UserOwnedSchemas_OnRunNow(object sender, CDataContainer dataContainer)
        // {
        //     UserPrototype currentPrototype = UserPrototypeFactory.GetInstance(dataContainer).CurrentPrototype;

        //     //In case the UserGeneral/Membership pages are loaded,
        //     //those will takes care of applying schema ownership changes also.
        //     //Hence, we only need to apply changes in this method when those are not loaded.
        //     if (!currentPrototype.IsSchemaOwnershipChangesApplied)
        //     {
        //         //base.OnRunNow(sender);

        //         User user = currentPrototype.ApplyChanges();

        //         //this.ExecutionMode = ExecutionMode.Success;
        //         dataContainer.ObjectName = currentPrototype.Name;
        //         dataContainer.SqlDialogSubject = user;                
        //     }

        //     //setting back to original after changes are applied
        //     currentPrototype.IsSchemaOwnershipChangesApplied = false;
        // }

        // how to populate defaults from prototype, will delete once refactored
        // private void InitializeValuesInUiControls()
        // {
        //     this.userNameTextBox.Text = this.currentUserPrototype.Name;

        //     if(this.currentUserPrototype.UserType == UserType.Certificate)
        //     {
        //         this.mappedObjTextbox.Text = this.currentUserPrototype.CertificateName;
        //     }
        //     if (this.currentUserPrototype.UserType == UserType.AsymmetricKey)
        //     {
        //         this.mappedObjTextbox.Text = this.currentUserPrototype.AsymmetricKeyName;
        //     }
        //     IUserPrototypeWithMappedLogin mappedLoginPrototype = this.currentUserPrototype
        //                                                                     as IUserPrototypeWithMappedLogin;
        //     if (mappedLoginPrototype != null)
        //     {
        //         this.mappedObjTextbox.Text = mappedLoginPrototype.LoginName;
        //     }

        //     IUserPrototypeWithDefaultLanguage defaultLanguagePrototype = this.currentUserPrototype
        //                                                                             as IUserPrototypeWithDefaultLanguage;
        //     if (defaultLanguagePrototype != null
        //         && defaultLanguagePrototype.IsDefaultLanguageSupported)
        //     {
        //         string defaultLanguageAlias = defaultLanguagePrototype.DefaultLanguageAlias;

        //         //If engine returns default language as empty or null, that means the default language of  
        //         //database will be used.
        //         //Default language is not applicable for users inside an uncontained authentication.
        //         if (string.IsNullOrEmpty(defaultLanguageAlias)
        //             && (this.DataContainer.Server.GetSmoObject(this.parentDbUrn) as Database).ContainmentType != ContainmentType.None)
        //         {
        //             defaultLanguageAlias = this.defaultLanguagePlaceholder;
        //         }
        //         this.defaultLanguageComboBox.Text = defaultLanguageAlias;
        //     }

        //     IUserPrototypeWithDefaultSchema defaultSchemaPrototype = this.currentUserPrototype
        //                                                                         as IUserPrototypeWithDefaultSchema;
        //     if (defaultSchemaPrototype != null
        //         && defaultSchemaPrototype.IsDefaultSchemaSupported)
        //     {
        //         this.defaultSchemaTextBox.Text = defaultSchemaPrototype.DefaultSchema;
        //     }

        //     IUserPrototypeWithPassword userWithPwdPrototype = this.currentUserPrototype
        //                                                                 as IUserPrototypeWithPassword;
        //     if (userWithPwdPrototype != null
        //         && !this.DataContainer.IsNewObject)
        //     {
        //         this.passwordTextBox.Text = FAKE_PASSWORD;
        //         this.confirmPwdTextBox.Text = FAKE_PASSWORD;                
        //     }
        // }
        // private void UpdateUiControlsOnLoad()
        // {
        //     if (!this.DataContainer.IsNewObject)
        //     {
        //         this.userNameTextBox.ReadOnly = true; //Rename is not allowed from the dialog.
        //         this.userSearchButton.Enabled = false;
        //         this.mappedObjTextbox.ReadOnly = true; //Changing mapped login, certificate and asymmetric key is not allowed
        //         this.mappedObjSearchButton.Enabled = false;
        //         //from SMO also.
        //         this.userTypeComboBox.Enabled = false;
        //         this.oldPasswordTextBox.ReadOnly = true;
        //     }
        //     else
        //     {
        //         //Old password is only useful for changing the password.
        //         this.specifyOldPwdCheckBox.Enabled = false;
        //         this.oldPasswordLabel.Enabled = false;
        //         this.oldPasswordTextBox.Enabled = false;
        //     }
        // }


#endregion

#region "Credential Handlers"

        /// <summary>
        /// Handle request to create a credential
        /// </summary>
        internal async Task HandleCreateCredentialRequest(CreateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CredentialResult()
            {
                Credential = parameters.Credential,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to update a credential
        /// </summary>
        internal async Task HandleUpdateCredentialRequest(UpdateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new CredentialResult()
            {
                Credential = parameters.Credential,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteCredentialRequest(DeleteCredentialParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }


        /// <summary>
        /// Handle request to get all credentials
        /// </summary>
        internal async Task HandleGetCredentialsRequest(GetCredentialsParams parameters, RequestContext<GetCredentialsResult> requestContext)
        {
            var result = new GetCredentialsResult();
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                var credentials = dataContainer.Server.Credentials;
                int credentialsCount = credentials.Count;
                CredentialInfo[] credentialsInfos = new CredentialInfo[credentialsCount];
                for (int i = 0; i < credentialsCount; ++i)
                {
                    credentialsInfos[i] = new CredentialInfo();
                    credentialsInfos[i].Name = credentials[i].Name;
                    credentialsInfos[i].Identity = credentials[i].Identity;
                    credentialsInfos[i].Id = credentials[i].ID;
                    credentialsInfos[i].DateLastModified = credentials[i].DateLastModified;
                    credentialsInfos[i].CreateDate = credentials[i].CreateDate;
                    credentialsInfos[i].ProviderName = credentials[i].ProviderName;
                }
                result.Credentials = credentialsInfos;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.ToString();
            }

            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

#endregion        

#region "Helpers"

        internal Task<Tuple<bool, string>> ConfigureCredential(
            string ownerUri,
            CredentialInfo credential,
            ConfigAction configAction,
            RunType runType)
        {
            return Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    using (CredentialActions actions = new CredentialActions(dataContainer, credential, configAction))
                    {
                        var executionHandler = new ExecutonHandler(actions);
                        executionHandler.RunNow(runType, this);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        /// <summary>
        /// this is the main method that is called by DropAllObjects for every object
        /// in the grid
        /// </summary>
        /// <param name="objectRowNumber"></param>
        private void DoDropObject(CDataContainer dataContainer)
        {            
            var executionMode = dataContainer.Server.ConnectionContext.SqlExecutionModes;
            var subjectExecutionMode = executionMode;

            //For Azure the ExecutionManager is different depending on which ExecutionManager
            //used - one at the Server level and one at the Database level. So to ensure we
            //don't use the wrong execution mode we need to set the mode for both (for on-prem
            //this will essentially be a no-op)
            SqlSmoObject sqlDialogSubject = null;
            try
            {
                sqlDialogSubject = dataContainer.SqlDialogSubject;
            }
            catch (System.Exception)
            {
                //We may not have a valid dialog subject here (such as if the object hasn't been created yet)
                //so in that case we'll just ignore it as that's a normal scenario. 
            }
            if (sqlDialogSubject != null)
            {
                subjectExecutionMode =
                    sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes;
            }

            Urn objUrn = sqlDialogSubject.Urn;
            System.Diagnostics.Debug.Assert(objUrn != null);

            SfcObjectQuery objectQuery = new SfcObjectQuery(dataContainer.Server);
           
            IDroppable droppableObj = null;
            string[] fields = null;

            foreach( object obj in objectQuery.ExecuteIterator( new SfcQueryExpression( objUrn.ToString() ), fields, null ) )
            {
                System.Diagnostics.Debug.Assert(droppableObj == null, "there is only one object");
                droppableObj = obj as IDroppable;
            }

            // For Azure databases, the SfcObjectQuery executions above may have overwritten our desired execution mode, so restore it
            dataContainer.Server.ConnectionContext.SqlExecutionModes = executionMode;
            if (sqlDialogSubject != null)
            {
                sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes = subjectExecutionMode;
            }

            if (droppableObj == null)
            {
                string objectName = objUrn.GetAttribute("Name");
                objectName ??= string.Empty;
                throw new Microsoft.SqlServer.Management.Smo.MissingObjectException("DropObjectsSR.ObjectDoesNotExist(objUrn.Type, objectName)");
            }

            //special case database drop - see if we need to delete backup and restore history
            SpecialPreDropActionsForObject(dataContainer, droppableObj, 
                deleteBackupRestoreOrDisableAuditSpecOrDisableAudit: false,
                dropOpenConnections: false);

            droppableObj.Drop();

            //special case Resource Governor reconfigure - for pool, external pool, group  Drop(), we need to issue
            SpecialPostDropActionsForObject(dataContainer, droppableObj);

        }
        
        private void SpecialPreDropActionsForObject(CDataContainer dataContainer, IDroppable droppableObj, 
            bool deleteBackupRestoreOrDisableAuditSpecOrDisableAudit, bool dropOpenConnections)
        {
            Database db = droppableObj as Database;

            if (deleteBackupRestoreOrDisableAuditSpecOrDisableAudit)
            {
                if (db != null)
                {
                    dataContainer.Server.DeleteBackupHistory(db.Name);
                }
                else
                {
                    // else droppable object should be a server or database audit specification
                    ServerAuditSpecification sas = droppableObj as ServerAuditSpecification;
                    if (sas != null)
                    {
                        sas.Disable();
                    }
                    else
                    {
                        DatabaseAuditSpecification das = droppableObj as DatabaseAuditSpecification;
                        if (das != null)
                        {
                            das.Disable();
                        }
                        else
                        {
                            Audit aud = droppableObj as Audit;
                            if (aud != null)
                            {
                                aud.Disable();
                            }
                        }
                    }
                }
            }

            // special case database drop - drop existing connections to the database other than this one
            if (dropOpenConnections)
            {
                if (db.ActiveConnections > 0)
                {
                    // force the database to be single user
                    db.DatabaseOptions.UserAccess = DatabaseUserAccess.Single;
                    db.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
            }
        }

        private void SpecialPostDropActionsForObject(CDataContainer dataContainer, IDroppable droppableObj)
        {
            if (droppableObj is Policy)
            {
                Policy policyToDrop = (Policy)droppableObj;
                if (!string.IsNullOrEmpty(policyToDrop.ObjectSet))
                {
                    ObjectSet objectSet = policyToDrop.Parent.ObjectSets[policyToDrop.ObjectSet];
                    objectSet.Drop();
                }
            }

            ResourcePool rp = droppableObj as ResourcePool;
            ExternalResourcePool erp = droppableObj as ExternalResourcePool;
            WorkloadGroup wg = droppableObj as WorkloadGroup;

            if (null != rp || null != erp || null != wg)
            {
                // Alter() Resource Governor to reconfigure
                dataContainer.Server.ResourceGovernor.Alter();
            }
        }

#endregion // "Helpers"

// some potentially useful code for working with server & db roles to be refactored later
#region "Roles" 
        private class SchemaOwnership
        {
            public bool initiallyOwned;
            public bool currentlyOwned;

            public SchemaOwnership(bool initiallyOwned)
            {
                this.initiallyOwned = initiallyOwned;
                this.currentlyOwned = initiallyOwned;
            }
        }

        private class RoleMembership
        {
            public bool initiallyAMember;
            public bool currentlyAMember;

            public RoleMembership(bool initiallyAMember)
            {
                this.initiallyAMember = initiallyAMember;
                this.currentlyAMember = initiallyAMember;
            }

            public RoleMembership(bool initiallyAMember, bool currentlyAMember)
            {
                this.initiallyAMember = initiallyAMember;
                this.currentlyAMember = currentlyAMember;
            }
        }

        private void DbRole_LoadMembership(string databaseName, string dbroleName, ServerConnection serverConnection)
        {
            var roleMembers = new HybridDictionary();
            bool isPropertiesMode = false;
            if (isPropertiesMode)
            {
                Enumerator  enumerator  = new Enumerator();
                Urn         urn         = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                        "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member",
                                                        Urn.EscapeString(databaseName),
                                                        Urn.EscapeString(dbroleName));
                string[]    fields      = new string[] { "Name" };
                OrderBy[]   orderBy     = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc)};
                Request     request     = new Request(urn, fields, orderBy);
                DataTable   dt          = enumerator.Process(serverConnection, request);

                foreach (DataRow dr in dt.Rows)
                {
                    string memberName = dr["Name"].ToString();
                    roleMembers[memberName] = new RoleMembership(true);
                }
            }
        }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void DbRole_SendToServerMembershipChanges(Database db, DatabaseRole dbrole)
        {
            var roleMembers = new HybridDictionary();
            IDictionaryEnumerator enumerator = roleMembers.GetEnumerator();
            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                DictionaryEntry entry       = enumerator.Entry;
                string          memberName  = entry.Key.ToString();
                RoleMembership  membership  = (RoleMembership) entry.Value;

                if (!membership.initiallyAMember && membership.currentlyAMember)
                {
                    dbrole.AddMember(memberName);
                }
                else if (membership.initiallyAMember && !membership.currentlyAMember)
                {
                    dbrole.DropMember(memberName);
                }
            }
        }

        private void InitProp(ServerConnection serverConnection, string serverName, string databaseName, 
            string dbroleName, string dbroleUrn, bool isPropertiesMode)
        {            
            System.Diagnostics.Debug.Assert(serverName!=null);
            System.Diagnostics.Debug.Assert((databaseName!=null) && (databaseName.Trim().Length!=0));

            // LoadSchemas();
            // LoadMembership();

            if (isPropertiesMode == true)
            {
                // initialize from enumerator in properties mode
                System.Diagnostics.Debug.Assert(dbroleName!=null);
                System.Diagnostics.Debug.Assert(dbroleName.Trim().Length !=0);
                System.Diagnostics.Debug.Assert(dbroleUrn!=null);
                System.Diagnostics.Debug.Assert(dbroleUrn.Trim().Length != 0);

                Enumerator en = new Enumerator();
                Request req = new Request();
                req.Fields = new String [] { "Owner" };

                if ((dbroleUrn!=null) && (dbroleUrn.Trim().Length != 0))
                {
                    req.Urn = dbroleUrn;
                }
                else
                {
                    req.Urn = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']/Role[@Name='" + Urn.EscapeString(dbroleName) + "]";
                }

                DataTable dt = en.Process(serverConnection, req);
                System.Diagnostics.Debug.Assert(dt!=null);
                System.Diagnostics.Debug.Assert(dt.Rows.Count==1);

                if (dt.Rows.Count==0)
                {
                    throw new Exception("DatabaseRoleSR.ErrorDbRoleNotFound");
                }

                // DataRow dr = dt.Rows[0];
                // this.initialOwner = Convert.ToString(dr[DatabaseRoleGeneral.ownerField],System.Globalization.CultureInfo.InvariantCulture);
                // this.textBoxOwner.Text = this.initialOwner;
            }
        }

        private void DbRole_SendDataToServer(CDataContainer dataContainer, string databaseName, 
            string dbroleName, string ownerName, string initialOwner, string roleName, bool isPropertiesMode)
        {
            System.Diagnostics.Debug.Assert(databaseName != null && databaseName.Trim().Length != 0, "database name is empty");
            System.Diagnostics.Debug.Assert(dataContainer.Server != null, "server is null");

            Database database = dataContainer.Server.Databases[databaseName];
            System.Diagnostics.Debug.Assert(database!= null, "database is null");

            DatabaseRole role;

            if (isPropertiesMode == true) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(dbroleName != null && dbroleName.Trim().Length != 0, "role name is empty");

                role = database.Roles[dbroleName];
                System.Diagnostics.Debug.Assert(role != null, "role is null");

                if (0 != String.Compare(ownerName, initialOwner, StringComparison.Ordinal))
                {
                    role.Owner = ownerName;
                    role.Alter();
                }
            }
            else // not in properties mode -> create role
            {
                role = new DatabaseRole(database, roleName);
                if (ownerName.Length != 0)
                {
                    role.Owner = ownerName;
                }

                role.Create();
            }

            // SendToServerSchemaOwnershipChanges(database, role);
            // SendToServerMembershipChanges(database, role);
        }

        private void DbRole_LoadSchemas(string databaseName, string  dbroleName, ServerConnection serverConnection)
        {
            bool isPropertiesMode = false;
            HybridDictionary schemaOwnership;
            schemaOwnership = new HybridDictionary();

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String [] { "Name", "Owner" };
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']/Schema";

            DataTable dt = en.Process(serverConnection, req);
            System.Diagnostics.Debug.Assert((dt != null) && (0 < dt.Rows.Count), "enumerator did not return schemas");
            System.Diagnostics.Debug.Assert(!isPropertiesMode || (dbroleName.Length != 0), "role name is not known");

            foreach (DataRow dr in dt.Rows)
            {
                string  schemaName      = Convert.ToString(dr["Name"],System.Globalization.CultureInfo.InvariantCulture);
                string  schemaOwner     = Convert.ToString(dr["Owner"],System.Globalization.CultureInfo.InvariantCulture);
                bool    roleOwnsSchema  = 
                    isPropertiesMode &&
                    (0 == String.Compare(dbroleName, schemaOwner, StringComparison.Ordinal));

                schemaOwnership[schemaName] = new SchemaOwnership(roleOwnsSchema);
            }
        }

        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void DbRole_SendToServerSchemaOwnershipChanges(CDataContainer dataContainer, Database db, DatabaseRole dbrole)
        {
            HybridDictionary schemaOwnership = null;
            if (9 <= dataContainer.Server.Information.Version.Major)
            {
                IDictionaryEnumerator enumerator = schemaOwnership.GetEnumerator();
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    DictionaryEntry de          = enumerator.Entry;
                    string          schemaName  = de.Key.ToString();
                    SchemaOwnership ownership   = (SchemaOwnership)de.Value;

                    // If we are creating a new role, then no schema will have been initially owned by this role.
                    // If we are modifying an existing role, we can only take ownership of roles.  (Ownership can't
                    // be renounced, it can only be positively assigned to a principal.)
                    if (ownership.currentlyOwned && !ownership.initiallyOwned)
                    {
                        Schema schema = db.Schemas[schemaName];
                        schema.Owner = dbrole.Name;
                        schema.Alter();
                    }
                }
            }
        }

#endregion

    }

    
}
