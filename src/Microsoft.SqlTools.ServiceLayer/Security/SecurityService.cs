//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
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

        private ConnectionService? connectionService;

        private UserServiceHandlerImpl userServiceHandler;

        private static readonly Lazy<SecurityService> instance = new Lazy<SecurityService>(() => new SecurityService());

        private Dictionary<string, string> contextIdToConnectionUriMap = new Dictionary<string, string>();

        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public SecurityService()
        {
            userServiceHandler = new UserServiceHandlerImpl();
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
        internal IProtocolEndpoint? ServiceHost
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
            this.ServiceHost.SetRequestHandler(UpdateLoginRequest.Type, HandleUpdateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(DeleteLoginRequest.Type, HandleDeleteLoginRequest, true);
            this.ServiceHost.SetRequestHandler(InitializeLoginViewRequest.Type, HandleInitializeLoginViewRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeLoginViewRequest.Type, HandleDisposeLoginViewRequest, true);

            // User request handlers
            this.ServiceHost.SetRequestHandler(InitializeUserViewRequest.Type, this.userServiceHandler.HandleInitializeUserViewRequest, true);
            this.ServiceHost.SetRequestHandler(CreateUserRequest.Type, this.userServiceHandler.HandleCreateUserRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateUserRequest.Type, this.userServiceHandler.HandleUpdateUserRequest, true);
            this.ServiceHost.SetRequestHandler(DeleteUserRequest.Type, this.userServiceHandler.HandleDeleteUserRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeUserViewRequest.Type, this.userServiceHandler.HandleDisposeUserViewRequest, true);
        }


        #region "Login Handlers"        

        /// <summary>
        /// Handle request to create a login
        /// </summary>
        internal async Task HandleCreateLoginRequest(CreateLoginParams parameters, RequestContext<object> requestContext)
        {
            ConnectionInfo connInfo;
            string ownerUri;
            contextIdToConnectionUriMap.TryGetValue(parameters.ContextId, out ownerUri);
            ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);

            if (connInfo == null) 
            {
                    // raise error here
            }

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
                if (0 != string.Compare(prototype.SqlPassword, prototype.SqlPasswordConfirm, StringComparison.Ordinal))
                {
                    // raise error here
                }
            }

            prototype.ApplyGeneralChanges(dataContainer.Server);

            // TODO move this to LoginData
            // TODO support role assignment for Azure
            LoginPrototype newPrototype = new LoginPrototype(dataContainer.Server, dataContainer.Server.Logins[parameters.Login.Name]);
            var _ =newPrototype.ServerRoles.ServerRoleNames;

            foreach (string role in parameters.Login.ServerRoles ?? Enumerable.Empty<string>())
            {
                newPrototype.ServerRoles.SetMember(role, true);
            }

            newPrototype.ApplyServerRoleChanges(dataContainer.Server);
            await requestContext.SendResult(new object());
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteLoginRequest(DeleteLoginParams parameters, RequestContext<object> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.ConnectionUri, out connInfo);
            // if (connInfo == null) 
            // {
            //     // raise an error
            // }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            Login login = dataContainer.Server?.Logins[parameters.Name];
     
            dataContainer.SqlDialogSubject = login;
            DatabaseUtils.DoDropObject(dataContainer);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = true,
                ErrorMessage = string.Empty
            });
        }

        internal async Task HandleUpdateLoginRequest(UpdateLoginParams parameters, RequestContext<object> requestContext)
        {
            ConnectionInfo connInfo;
            string ownerUri;
            contextIdToConnectionUriMap.TryGetValue(parameters.ContextId, out ownerUri);
            ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
            if (connInfo == null) 
            {
                    // raise error here
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginPrototype prototype = new LoginPrototype(dataContainer.Server, dataContainer.Server.Logins[parameters.Login.Name]);

            var login = parameters.Login;
            prototype.SqlPassword = login.Password;
            if (0 != String.Compare(login.DefaultLanguage, SR.DefaultLanguagePlaceholder, StringComparison.Ordinal))
            {
                string[] arr = login.DefaultLanguage?.Split(" - ");
                if (arr != null && arr.Length > 1)
                {
                    prototype.DefaultLanguage = arr[1];
                }
            }
            prototype.DefaultDatabase = login.DefaultDatabase;
            prototype.EnforcePolicy = login.EnforcePasswordPolicy;
            prototype.EnforceExpiration = login.EnforcePasswordPolicy ? login.EnforcePasswordExpiration : false;
            prototype.IsLockedOut = login.IsLockedOut;
            prototype.IsDisabled = !login.IsEnabled;
            prototype.MustChange = login.EnforcePasswordPolicy ? login.MustChangePassword : false;
            prototype.WindowsGrantAccess = login.ConnectPermission;

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
                if (0 != string.Compare(prototype.SqlPassword, prototype.SqlPasswordConfirm, StringComparison.Ordinal))
                {
                    // raise error here
                }
            }

            var _ = prototype.ServerRoles.ServerRoleNames;
            foreach (string role in login.ServerRoles)
            {
                prototype.ServerRoles.SetMember(role, true);
            }

            prototype.ApplyGeneralChanges(dataContainer.Server);
            prototype.ApplyServerRoleChanges(dataContainer.Server);
            prototype.ApplyDatabaseRoleChanges(dataContainer.Server);
            await requestContext.SendResult(new object());
        }

        internal async Task HandleInitializeLoginViewRequest(InitializeLoginViewRequestParams parameters, RequestContext<LoginViewInfo> requestContext)
        {
            contextIdToConnectionUriMap.Add(parameters.ContextId, parameters.ConnectionUri);
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null) 
            {
                // raise an error
            }
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginViewInfo loginViewInfo = new LoginViewInfo();

            // TODO cache databases and languages
            string[] databases = new string[dataContainer.Server.Databases.Count];
            for (int i = 0; i < dataContainer.Server.Databases.Count; i++)
            {
                databases[i] = dataContainer.Server.Databases[i].Name;
            }

            var languageOptions = GetDefaultLanguageOptions(dataContainer);
            var languageOptionsList = languageOptions.Select(FormatLanguageDisplay).ToList();
            if (parameters.IsNewObject)
            {
                languageOptionsList.Insert(0, SR.DefaultLanguagePlaceholder);
            }
            string[] languages = languageOptionsList.ToArray();
            LoginPrototype prototype = parameters.IsNewObject 
            ? new LoginPrototype(dataContainer.Server) 
            : new LoginPrototype(dataContainer.Server, dataContainer.Server.Logins[parameters.Name]);

            List<string> loginServerRoles = new List<string>();
            foreach(string role in prototype.ServerRoles.ServerRoleNames)
            {
                if (prototype.ServerRoles.IsMember(role))
                {
                    loginServerRoles.Add(role);
                }
            }

            LoginInfo loginInfo = new LoginInfo()
            {
                Name = prototype.LoginName,
                Password = prototype.SqlPassword,
                OldPassword = prototype.OldPassword,
                AuthenticationType = LoginTypeToAuthenticationType(prototype.LoginType),
                EnforcePasswordExpiration = prototype.EnforceExpiration,
                EnforcePasswordPolicy = prototype.EnforcePolicy,
                MustChangePassword = prototype.MustChange,
                DefaultDatabase = prototype.DefaultDatabase,
                DefaultLanguage = FormatLanguageDisplay(languageOptions.FirstOrDefault(o => o?.Language.Name == prototype.DefaultLanguage || o?.Language.Alias == prototype.DefaultLanguage, null)),
                ServerRoles = loginServerRoles.ToArray(),
                ConnectPermission = prototype.WindowsGrantAccess,
                IsEnabled = !prototype.IsDisabled,
                IsLockedOut = prototype.IsLockedOut,
                UserMapping = new ServerLoginDatabaseUserMapping[0]
            };

            await requestContext.SendResult(new LoginViewInfo()
            {
                ObjectInfo = loginInfo,
                SupportWindowsAuthentication = prototype.WindowsAuthSupported,
                SupportAADAuthentication = prototype.AADAuthSupported,
                SupportSQLAuthentication = true, // SQL Auth support for login, not necessarily mean SQL Auth support for CONNECT etc.
                CanEditLockedOutState = true,
                Databases = databases,
                Languages = languages,
                ServerRoles = prototype.ServerRoles.ServerRoleNames,
                SupportAdvancedPasswordOptions = dataContainer.Server.DatabaseEngineType == DatabaseEngineType.Standalone || dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse,
                SupportAdvancedOptions = dataContainer.Server.DatabaseEngineType == DatabaseEngineType.Standalone || dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance
            });
        }

        private LoginAuthenticationType LoginTypeToAuthenticationType(LoginType loginType)
        {
            switch (loginType)
            {
                case LoginType.WindowsUser:
                case LoginType.WindowsGroup:
                    return LoginAuthenticationType.Windows;
                case LoginType.SqlLogin:
                    return LoginAuthenticationType.Sql;
                case LoginType.ExternalUser:
                case LoginType.ExternalGroup:
                    return LoginAuthenticationType.AAD;
                default:
                    return LoginAuthenticationType.Others;
            }
        }

        internal async Task HandleDisposeLoginViewRequest(DisposeLoginViewRequestParams parameters, RequestContext<object> requestContext)
        {
            await requestContext.SendResult(new object());
        }

        private string FormatLanguageDisplay(LanguageDisplay? l)
        {
            if (l == null) return null;
             return string.Format("{0} - {1}", l.Language.Alias, l.Language.Name);
        }

        private IList<LanguageDisplay> GetDefaultLanguageOptions(CDataContainer dataContainer)
        {
            // sort the languages alphabetically by alias
            SortedList sortedLanguages = new SortedList(Comparer.Default);

            LanguageUtils.SetLanguageDefaultInitFieldsForDefaultLanguages(dataContainer.Server);
            if (dataContainer.Server != null && dataContainer.Server.Languages != null)
            {
                foreach (Language language in dataContainer.Server.Languages)
                {
                    LanguageDisplay listValue = new LanguageDisplay(language);
                    sortedLanguages.Add(language.Alias, listValue);
                }
            }

            IList<LanguageDisplay> res = new List<LanguageDisplay>();
            foreach (LanguageDisplay ld in sortedLanguages.Values)
            {
                res.Add(ld);
            }

            return res;
        }

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

                var credentials = dataContainer.Server?.Credentials;
                int credentialsCount = credentials != null ? credentials.Count : 0;
                CredentialInfo[] credentialsInfos = new CredentialInfo[credentialsCount];
                if (credentials != null)
                {
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

        // private void DbRole_LoadMembership(string databaseName, string dbroleName, ServerConnection serverConnection)
        // {
        //     var roleMembers = new HybridDictionary();
        //     bool isPropertiesMode = false;
        //     if (isPropertiesMode)
        //     {
        //         Enumerator  enumerator  = new Enumerator();
        //         Urn         urn         = String.Format(System.Globalization.CultureInfo.InvariantCulture,
        //                                                 "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member",
        //                                                 Urn.EscapeString(databaseName),
        //                                                 Urn.EscapeString(dbroleName));
        //         string[]    fields      = new string[] { "Name" };
        //         OrderBy[]   orderBy     = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc)};
        //         Request     request     = new Request(urn, fields, orderBy);
        //         DataTable   dt          = enumerator.Process(serverConnection, request);

        //         foreach (DataRow dr in dt.Rows)
        //         {
        //             string memberName = dr["Name"].ToString();
        //             if (memberName != null)
        //             {
        //                 roleMembers[memberName] = new RoleMembership(true);
        //             }
        //         }
        //     }
        // }

        // /// <summary>
        // /// sends to server user changes related to membership
        // /// </summary>
        // private void DbRole_SendToServerMembershipChanges(Database db, DatabaseRole dbrole)
        // {
        //     var roleMembers = new HybridDictionary();
        //     IDictionaryEnumerator enumerator = roleMembers.GetEnumerator();
        //     enumerator.Reset();

        //     while (enumerator.MoveNext())
        //     {
        //         DictionaryEntry entry       = enumerator.Entry;
        //         string          memberName  = entry.Key.ToString();
        //         RoleMembership  membership  = (RoleMembership) entry.Value;
        //         if (membership != null)
        //         {
        //             if (!membership.initiallyAMember && membership.currentlyAMember)
        //             {
        //                 dbrole.AddMember(memberName);
        //             }
        //             else if (membership.initiallyAMember && !membership.currentlyAMember)
        //             {
        //                 dbrole.DropMember(memberName);
        //             }
        //         }
        //     }
        // }

        // private void InitProp(ServerConnection serverConnection, string serverName, string databaseName, 
        //     string dbroleName, string dbroleUrn, bool isPropertiesMode)
        // {            
        //     System.Diagnostics.Debug.Assert(serverName!=null);
        //     System.Diagnostics.Debug.Assert((databaseName!=null) && (databaseName.Trim().Length!=0));

        //     // LoadSchemas();
        //     // LoadMembership();

        //     if (isPropertiesMode == true)
        //     {
        //         // initialize from enumerator in properties mode
        //         System.Diagnostics.Debug.Assert(dbroleName!=null);
        //         System.Diagnostics.Debug.Assert(dbroleName.Trim().Length !=0);
        //         System.Diagnostics.Debug.Assert(dbroleUrn!=null);
        //         System.Diagnostics.Debug.Assert(dbroleUrn.Trim().Length != 0);

        //         Enumerator en = new Enumerator();
        //         Request req = new Request();
        //         req.Fields = new String [] { "Owner" };

        //         if ((dbroleUrn!=null) && (dbroleUrn.Trim().Length != 0))
        //         {
        //             req.Urn = dbroleUrn;
        //         }
        //         else
        //         {
        //             req.Urn = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']/Role[@Name='" + Urn.EscapeString(dbroleName) + "]";
        //         }

        //         DataTable dt = en.Process(serverConnection, req);
        //         System.Diagnostics.Debug.Assert(dt!=null);
        //         System.Diagnostics.Debug.Assert(dt.Rows.Count==1);

        //         if (dt.Rows.Count==0)
        //         {
        //             throw new Exception("DatabaseRoleSR.ErrorDbRoleNotFound");
        //         }

        //         // DataRow dr = dt.Rows[0];
        //         // this.initialOwner = Convert.ToString(dr[DatabaseRoleGeneral.ownerField],System.Globalization.CultureInfo.InvariantCulture);
        //         // this.textBoxOwner.Text = this.initialOwner;
        //     }
        // }

        // private void DbRole_SendDataToServer(CDataContainer dataContainer, string databaseName, 
        //     string dbroleName, string ownerName, string initialOwner, string roleName, bool isPropertiesMode)
        // {
        //     System.Diagnostics.Debug.Assert(databaseName != null && databaseName.Trim().Length != 0, "database name is empty");
        //     System.Diagnostics.Debug.Assert(dataContainer.Server != null, "server is null");

        //     Database database = dataContainer.Server.Databases[databaseName];
        //     System.Diagnostics.Debug.Assert(database!= null, "database is null");

        //     DatabaseRole role;

        //     if (isPropertiesMode == true) // in properties mode -> alter role
        //     {
        //         System.Diagnostics.Debug.Assert(dbroleName != null && dbroleName.Trim().Length != 0, "role name is empty");

        //         role = database.Roles[dbroleName];
        //         System.Diagnostics.Debug.Assert(role != null, "role is null");

        //         if (0 != String.Compare(ownerName, initialOwner, StringComparison.Ordinal))
        //         {
        //             role.Owner = ownerName;
        //             role.Alter();
        //         }
        //     }
        //     else // not in properties mode -> create role
        //     {
        //         role = new DatabaseRole(database, roleName);
        //         if (ownerName.Length != 0)
        //         {
        //             role.Owner = ownerName;
        //         }

        //         role.Create();
        //     }

        //     // SendToServerSchemaOwnershipChanges(database, role);
        //     // SendToServerMembershipChanges(database, role);
        // }


        // /// <summary>
        // /// sends to server changes related to schema ownership
        // /// </summary>
        // private void DbRole_SendToServerSchemaOwnershipChanges(CDataContainer dataContainer, Database db, DatabaseRole dbrole)
        // {
        //     if (dataContainer.Server == null)
        //     {
        //         return;
        //     }

        //     HybridDictionary schemaOwnership = new HybridDictionary();
        //     if (9 <= dataContainer.Server.Information.Version.Major)
        //     {
        //         IDictionaryEnumerator enumerator = schemaOwnership.GetEnumerator();
        //         enumerator.Reset();
        //         while (enumerator.MoveNext())
        //         {
        //             DictionaryEntry de          = enumerator.Entry;
        //             string          schemaName  = de.Key.ToString();
        //             SchemaOwnership ownership   = (SchemaOwnership)de.Value;

        //             // If we are creating a new role, then no schema will have been initially owned by this role.
        //             // If we are modifying an existing role, we can only take ownership of roles.  (Ownership can't
        //             // be renounced, it can only be positively assigned to a principal.)
        //             if (ownership != null && (ownership.currentlyOwned && !ownership.initiallyOwned))
        //             {
        //                 Schema schema = db.Schemas[schemaName];
        //                 schema.Owner = dbrole.Name;
        //                 schema.Alter();
        //             }
        //         }
        //     }
        // }

        #endregion

    }
}
