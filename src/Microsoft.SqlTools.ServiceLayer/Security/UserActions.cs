//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal class UserServiceHandlerImpl
    {
        private class UserViewState
        {
            public string Database { get; set; }

            public UserPrototypeData OriginalUserData { get; set; }

            public UserViewState(string database, UserPrototypeData originalUserData)
            {
                this.Database = database;
                this.OriginalUserData = originalUserData;
            }
        }

        private ConnectionService? connectionService;

        private Dictionary<string, UserViewState> contextIdToViewState = new Dictionary<string, UserViewState>();
        
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
        /// Handle request to initialize user view
        /// </summary>
        internal async Task HandleInitializeUserViewRequest(InitializeUserViewParams parameters, RequestContext<UserViewInfo> requestContext)
        {
            if (string.IsNullOrWhiteSpace(parameters.Database))
            {
                throw new ArgumentNullException("parameters.Database");
            }

            if (string.IsNullOrWhiteSpace(parameters.ContextId))
            {
                throw new ArgumentNullException("parameters.ContextId");
            }

            ConnectionInfo originalConnInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.ConnectionUri, out originalConnInfo);
            if (originalConnInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", parameters.ConnectionUri);
            }

            string originalDatabaseName = originalConnInfo.ConnectionDetails.DatabaseName;
            try
            {
                originalConnInfo.ConnectionDetails.DatabaseName = parameters.Database;
                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = parameters.ContextId,
                    Connection = originalConnInfo.ConnectionDetails,
                    Type = Connection.ConnectionType.Default
                };
                await this.ConnectionServiceInstance.Connect(connectParams);
            }
            finally
            {
                originalConnInfo.ConnectionDetails.DatabaseName = originalDatabaseName;
            }

            ConnectionInfo connInfo;
            this.ConnectionServiceInstance.TryFindConnection(parameters.ContextId, out connInfo);
            CDataContainer dataContainer = CreateUserDataContainer(connInfo, null, ConfigAction.Create, parameters.Database);

            UserInfo? userInfo = null;
            if (!parameters.IsNewObject)
            {
                User? existingUser = null;
                string databaseUrn = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(parameters.Database));
                Database? parentDb = dataContainer.Server.GetSmoObject(databaseUrn) as Database;
                existingUser = dataContainer.Server.Databases[parentDb.Name].Users[parameters.Name];

                if (string.IsNullOrWhiteSpace(existingUser.Login))
                {
                    throw new ApplicationException("Only 'User with Login' user type supported");
                }

                userInfo = new UserInfo()
                {
                    Name = parameters.Name,
                    LoginName = existingUser.Login,
                    DefaultSchema = existingUser.DefaultSchema
                };
            }

            UserPrototypeFactory userPrototypeFactory = UserPrototypeFactory.GetInstance(dataContainer, userInfo, originalData: null);
            UserPrototype currentUserPrototype = userPrototypeFactory.GetUserPrototype(ExhaustiveUserTypes.LoginMappedUser);        
            
            IUserPrototypeWithDefaultLanguage defaultLanguagePrototype = currentUserPrototype as IUserPrototypeWithDefaultLanguage;
            string? defaultLanguageAlias = null;
            if (defaultLanguagePrototype != null && defaultLanguagePrototype.IsDefaultLanguageSupported)
            {
                string dbUrn = "Server/Database[@Name='" + Urn.EscapeString(parameters.Database) + "']";
                defaultLanguageAlias = defaultLanguagePrototype.DefaultLanguageAlias;
                //If engine returns default language as empty or null, that means the default language of  
                //database will be used.
                //Default language is not applicable for users inside an uncontained authentication.
                if (string.IsNullOrEmpty(defaultLanguageAlias)
                    && (dataContainer.Server.GetSmoObject(dbUrn) as Database).ContainmentType != ContainmentType.None)
                {
                    defaultLanguageAlias = SR.DefaultLanguagePlaceholder;
                }
            }

            string? defaultSchema = null;
            IUserPrototypeWithDefaultSchema defaultSchemaPrototype = currentUserPrototype as IUserPrototypeWithDefaultSchema;
            if (defaultSchemaPrototype != null && defaultSchemaPrototype.IsDefaultSchemaSupported)
            {
                defaultSchema = defaultSchemaPrototype.DefaultSchema;
            }
            // IUserPrototypeWithPassword userWithPwdPrototype = currentUserPrototype as IUserPrototypeWithPassword;
            // if (userWithPwdPrototype != null && !this.DataContainer.IsNewObject)
            // {
            //     this.passwordTextBox.Text = FAKE_PASSWORD;
            //     this.confirmPwdTextBox.Text = FAKE_PASSWORD;                
            // }

            string? loginName = null;
            IUserPrototypeWithMappedLogin mappedLoginPrototype = currentUserPrototype as IUserPrototypeWithMappedLogin;
            if (mappedLoginPrototype != null)
            {
                loginName = mappedLoginPrototype.LoginName;
            }

            List<string> databaseRoles = new List<string>();
            foreach (string role in currentUserPrototype.DatabaseRoleNames)
            {
                if (currentUserPrototype.IsRoleMember(role))
                {
                    databaseRoles.Add(role);
                }
            }

            List<string> schemaNames = new List<string>();
            foreach (string schema in currentUserPrototype.SchemaNames)
            {
                if (currentUserPrototype.IsSchemaOwner(schema))
                {
                    schemaNames.Add(schema);
                }
            }

            // default to dbo schema, if there isn't already a default
            if (string.IsNullOrWhiteSpace(defaultSchema) && currentUserPrototype.SchemaNames.Contains("dbo"))
            {
                defaultSchema = "dbo";
            }

            ServerConnection serverConnection = dataContainer.ServerConnection;
            UserViewInfo userViewInfo = new UserViewInfo()
            {
                ObjectInfo = new UserInfo()
                {
                    Type = DatabaseUserType.WithLogin,
                    Name = currentUserPrototype.Name,
                    LoginName = loginName,
                    Password = string.Empty,
                    DefaultSchema = defaultSchema,
                    OwnedSchemas = schemaNames.ToArray(),
                    DatabaseRoles = databaseRoles.ToArray(),
                    DefaultLanguage = defaultLanguageAlias
                },
                SupportContainedUser = false,  // support for these will be added later
                SupportWindowsAuthentication = false,
                SupportAADAuthentication = false,
                SupportSQLAuthentication = true,
                Languages = new string[] { },
                Schemas = currentUserPrototype.SchemaNames.ToArray(),
                Logins = DatabaseUtils.LoadSqlLogins(serverConnection),
                DatabaseRoles = currentUserPrototype.DatabaseRoleNames.ToArray()
            };

            this.contextIdToViewState.Add(
                parameters.ContextId, 
                new UserViewState(parameters.Database, currentUserPrototype.CurrentState));

            await requestContext.SendResult(userViewInfo);
        }

        /// <summary>
        /// Handle request to create a user
        /// </summary>
        internal async Task HandleCreateUserRequest(CreateUserParams parameters, RequestContext<CreateUserResult> requestContext)
        {
            if (parameters.ContextId == null)
            {
                throw new ArgumentException("Invalid context ID");
            }

            UserViewState viewState;
            this.contextIdToViewState.TryGetValue(parameters.ContextId, out viewState);

            if (viewState == null)
            {
                throw new ArgumentException("Invalid context ID view state");
            }

            Tuple<bool, string> result = ConfigureUser(
                parameters.ContextId,
                parameters.User,
                ConfigAction.Create,
                RunType.RunNow,
                viewState.Database,
                viewState.OriginalUserData);

            await requestContext.SendResult(new CreateUserResult()
            {
                User = parameters.User,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to update a user
        /// </summary>
        internal async Task HandleUpdateUserRequest(UpdateUserParams parameters, RequestContext<ResultStatus> requestContext)
        {
            if (parameters.ContextId == null)
            {
                throw new ArgumentException("Invalid context ID");
            }

            UserViewState viewState;
            this.contextIdToViewState.TryGetValue(parameters.ContextId, out viewState);

            if (viewState == null)
            {
                throw new ArgumentException("Invalid context ID view state");
            }

            Tuple<bool, string> result = ConfigureUser(
                parameters.ContextId,
                parameters.User,
                ConfigAction.Update,
                RunType.RunNow,
                viewState.Database,
                viewState.OriginalUserData);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to delete a user
        /// </summary>
        internal async Task HandleDeleteUserRequest(DeleteUserParams parameters, RequestContext<ResultStatus> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            if (string.IsNullOrWhiteSpace(parameters.Name) || string.IsNullOrWhiteSpace(parameters.Database))
            {
                throw new ArgumentException("Invalid null parameter");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            string dbUrn = "Server/Database[@Name='" + Urn.EscapeString(parameters.Database) + "']";
            Database? parent = dataContainer.Server.GetSmoObject(new Urn(dbUrn)) as Database;
            User user = parent.Users[parameters.Name];
            dataContainer.SqlDialogSubject = user;

            CheckForSchemaOwnerships(parent, user);
            DatabaseUtils.DoDropObject(dataContainer);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = true,
                ErrorMessage = string.Empty
            });
        }

        internal async Task HandleDisposeUserViewRequest(DisposeUserViewRequestParams parameters, RequestContext<ResultStatus> requestContext)
        {
            this.ConnectionServiceInstance.Disconnect(new DisconnectParams(){
                OwnerUri = parameters.ContextId,
                Type = null
            });

            if (parameters.ContextId != null)
            {
                this.contextIdToViewState.Remove(parameters.ContextId);
            }

            await requestContext.SendResult(new ResultStatus()
            {
                Success = true,
                ErrorMessage = string.Empty
            });
        }

        internal CDataContainer CreateUserDataContainer(
            ConnectionInfo connInfo, 
            UserInfo? user, 
            ConfigAction configAction,
            string databaseName)
        {
            var serverConnection = ConnectionService.OpenServerConnection(connInfo, "DataContainer");
            var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
            connectionInfoWithConnection.ServerConnection = serverConnection;

            string urn =  (configAction == ConfigAction.Update && user != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']/User[@Name='{1}']",
                    Urn.EscapeString(databaseName),
                    Urn.EscapeString(user.Name))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(databaseName));

            ActionContext context = new ActionContext(serverConnection, "User", urn);
            DataContainerXmlGenerator containerXml = new DataContainerXmlGenerator(context);

            if (configAction == ConfigAction.Create)
            {
                containerXml.AddProperty("itemtype", "User");
            }

            XmlDocument xmlDoc = containerXml.GenerateXmlDocument();                    
            return CDataContainer.CreateDataContainer(connectionInfoWithConnection, xmlDoc);
        }

        internal Tuple<bool, string> ConfigureUser(
            string? ownerUri,
            UserInfo? user,
            ConfigAction configAction,
            RunType runType,
            string databaseName,
            UserPrototypeData? originalData)
        {
            ConnectionInfo connInfo;
            this.ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", ownerUri);
            }

            CDataContainer dataContainer = CreateUserDataContainer(connInfo, user, configAction, databaseName);
            using (var actions = new UserActions(dataContainer, user, configAction, originalData))
            {
                var executionHandler = new ExecutonHandler(actions);
                executionHandler.RunNow(runType, this);
                if (executionHandler.ExecutionResult == ExecutionMode.Failure)
                {
                    throw executionHandler.ExecutionFailureException;
                }
            }

            return new Tuple<bool, string>(true, string.Empty);
        }

        private void CheckForSchemaOwnerships(Database parentDb, User existingUser)
        {
            foreach (Schema sch in parentDb.Schemas)
            {
                var comparer = parentDb.GetStringComparer();
                if (comparer.Compare(sch.Owner, existingUser.Name) == 0)
                {
                    throw new ApplicationException("Cannot drop user since it owns a schema");
                }
            }
        }
    }

    internal class UserActions : ManagementActionBase
    {
#region Variables
        //private UserPrototypeData userData;
        private UserPrototype userPrototype;
        private UserInfo? user;
        private ConfigAction configAction;
#endregion

#region Constructors / Dispose
        /// <summary>
        /// required when loading from Object Explorer context
        /// </summary>
        /// <param name="context"></param>
        public UserActions(
            CDataContainer context,
            UserInfo? user,
            ConfigAction configAction,
            UserPrototypeData? originalData)
        {
            this.DataContainer = context;
            this.user = user;
            this.configAction = configAction;

            this.userPrototype = InitUserPrototype(context, user, originalData);
        }

        // /// <summary> 
        // /// Clean up any resources being used.
        // /// </summary>
        // protected override void Dispose(bool disposing)
        // {
        //     base.Dispose(disposing);
        // }

#endregion

        /// <summary>
        /// called on background thread by the framework to execute the action
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            if (this.configAction == ConfigAction.Drop)
            {
                // if (this.credentialData.Credential != null)
                // {
                //     this.credentialData.Credential.DropIfExists();
                // }
            }
            else
            {
                this.userPrototype.ApplyChanges();
            }
        }
        
        private UserPrototype InitUserPrototype(CDataContainer dataContainer, UserInfo user, UserPrototypeData? originalData)
        {
            ExhaustiveUserTypes currentUserType;
            UserPrototypeFactory userPrototypeFactory = UserPrototypeFactory.GetInstance(dataContainer, user, originalData);

            if (dataContainer.IsNewObject)
            {
                if (dataContainer.Server != null && IsParentDatabaseContained(dataContainer.ParentUrn, dataContainer.Server))
                {
                    currentUserType = ExhaustiveUserTypes.SqlUserWithPassword;
                }
                else
                {
                    currentUserType = ExhaustiveUserTypes.LoginMappedUser;
                }
            }
            else
            {
                currentUserType = this.GetCurrentUserTypeForExistingUser(
                    dataContainer.Server.GetSmoObject(dataContainer.ObjectUrn) as User);
            }

           UserPrototype currentUserPrototype = userPrototypeFactory.GetUserPrototype(currentUserType);
           return currentUserPrototype;
        }

        private ExhaustiveUserTypes GetCurrentUserTypeForExistingUser(User? user)
        {
            if (user == null)
            {
                return ExhaustiveUserTypes.Unknown;
            }

            switch (user.UserType)
            {
                case UserType.SqlUser:
                    if (user.IsSupportedProperty("AuthenticationType"))
                    {
                        if (user.AuthenticationType == AuthenticationType.Windows)
                        {
                            return ExhaustiveUserTypes.WindowsUser;                            
                        }
                        else if (user.AuthenticationType == AuthenticationType.Database)
                        {
                            return ExhaustiveUserTypes.SqlUserWithPassword;
                        }
                    }

                    return ExhaustiveUserTypes.LoginMappedUser;
                    
                case UserType.NoLogin:
                    return ExhaustiveUserTypes.SqlUserWithoutLogin;
                    
                case UserType.Certificate:
                    return ExhaustiveUserTypes.CertificateMappedUser;
                    
                case UserType.AsymmetricKey:
                    return ExhaustiveUserTypes.AsymmetricKeyMappedUser;
                    
                default:
                    return ExhaustiveUserTypes.Unknown;
            }
        }

        private static bool IsParentDatabaseContained(Urn parentDbUrn, Server server)
        {
            string parentDbName = parentDbUrn.GetNameForType("Database");
            Database parentDatabase = server.Databases[parentDbName];

            if (parentDatabase.IsSupportedProperty("ContainmentType")
                && parentDatabase.ContainmentType == ContainmentType.Partial)
            {
                return true;
            }

            return false;
        }
    }
}
