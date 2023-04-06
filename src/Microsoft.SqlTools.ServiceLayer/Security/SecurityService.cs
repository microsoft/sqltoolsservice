//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

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

        private LoginServiceHandlerImpl loginServiceHandler;

        private static readonly Lazy<SecurityService> instance = new Lazy<SecurityService>(() => new SecurityService());


        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public SecurityService()
        {
            userServiceHandler = new UserServiceHandlerImpl();
            loginServiceHandler = new LoginServiceHandlerImpl();
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
            this.ServiceHost.SetRequestHandler(GetCredentialsRequest.Type, HandleGetCredentialsRequest, true);

            // Login request handlers
            this.ServiceHost.SetRequestHandler(CreateLoginRequest.Type, this.loginServiceHandler.HandleCreateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateLoginRequest.Type, this.loginServiceHandler.HandleUpdateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(InitializeLoginViewRequest.Type, this.loginServiceHandler.HandleInitializeLoginViewRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeLoginViewRequest.Type, this.loginServiceHandler.HandleDisposeLoginViewRequest, true);

            // User request handlers
            this.ServiceHost.SetRequestHandler(InitializeUserViewRequest.Type, this.userServiceHandler.HandleInitializeUserViewRequest, true);
            this.ServiceHost.SetRequestHandler(CreateUserRequest.Type, this.userServiceHandler.HandleCreateUserRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateUserRequest.Type, this.userServiceHandler.HandleUpdateUserRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeUserViewRequest.Type, this.userServiceHandler.HandleDisposeUserViewRequest, true);
        }

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
