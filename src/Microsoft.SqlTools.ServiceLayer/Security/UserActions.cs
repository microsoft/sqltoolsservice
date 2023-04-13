//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
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
            public bool IsNewObject { get; set; }

            public string Database { get; set; }

            public UserPrototypeData OriginalUserData { get; set; }

            public UserViewState(bool isNewObject, string database, UserPrototypeData originalUserData)
            {
                this.IsNewObject = isNewObject;
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
            // check input parameters
            if (string.IsNullOrWhiteSpace(parameters.Database))
            {
                throw new ArgumentNullException("parameters.Database");
            }

            if (string.IsNullOrWhiteSpace(parameters.ContextId))
            {
                throw new ArgumentNullException("parameters.ContextId");
            }

            // open a connection for running the user dialog and associated task
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

            // create a default user data context and database object
            CDataContainer dataContainer = CreateUserDataContainer(connInfo, null, ConfigAction.Create, parameters.Database);
            string databaseUrn = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                "Server/Database[@Name='{0}']", Urn.EscapeString(parameters.Database));
            Database? parentDb = dataContainer.Server.GetSmoObject(databaseUrn) as Database;

            var languageOptions = LanguageUtils.GetDefaultLanguageOptions(dataContainer);
            var languageOptionsList = languageOptions.Select(LanguageUtils.FormatLanguageDisplay).ToList();
            languageOptionsList.Insert(0, SR.DefaultLanguagePlaceholder);

            // if viewing an exisitng user then populate some properties
            UserInfo? userInfo = null;
            string? defaultLanguageAlias = null;
            ExhaustiveUserTypes userType = ExhaustiveUserTypes.LoginMappedUser;
            if (!parameters.IsNewObject)
            {
                User existingUser = dataContainer.Server.Databases[parentDb.Name].Users[parameters.Name];
                userType = UserActions.GetCurrentUserTypeForExistingUser(existingUser);
                DatabaseUserType databaseUserType = UserActions.GetDatabaseUserTypeForUserType(userType);
                userInfo = new UserInfo()
                {
                    Type = databaseUserType,
                    Name = parameters.Name,
                    LoginName = existingUser.Login,
                    DefaultSchema = existingUser.DefaultSchema,                    
                };

                // update the authentication type for contained users
                if (databaseUserType == DatabaseUserType.Contained)
                {
                    userInfo.AuthenticationType = ServerAuthenticationType.Sql;
                }

                // Default language is only applicable for users inside a contained database.
                if (LanguageUtils.IsDefaultLanguageSupported(dataContainer.Server)
                    && parentDb.ContainmentType != ContainmentType.None)
                {
                    defaultLanguageAlias = LanguageUtils.GetLanguageAliasFromName(
                        existingUser.Parent.Parent, 
                        existingUser.DefaultLanguage.Name);                   
                }
            }

            // generate a user prototype
            UserPrototype currentUserPrototype = UserPrototypeFactory.GetUserPrototype(dataContainer, userInfo, originalData: null, userType);

            // get the default schema if available
            string? defaultSchema = null;
            IUserPrototypeWithDefaultSchema defaultSchemaPrototype = currentUserPrototype as IUserPrototypeWithDefaultSchema;
            if (defaultSchemaPrototype != null && defaultSchemaPrototype.IsDefaultSchemaSupported)
            {
                defaultSchema = defaultSchemaPrototype.DefaultSchema;
            }

            ServerConnection serverConnection = dataContainer.ServerConnection;
            bool isSqlAzure = serverConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase;
            bool supportsContainedUser = isSqlAzure || UserActions.IsParentDatabaseContained(parentDb);

            // set default alias to <default> if needed
            if (string.IsNullOrEmpty(defaultLanguageAlias) 
                && supportsContainedUser
                && LanguageUtils.IsDefaultLanguageSupported(dataContainer.Server))
            {
                defaultLanguageAlias = SR.DefaultLanguagePlaceholder;
            }

            // set the fake password placeholder when editing an existing user
            string? password = null;
            IUserPrototypeWithPassword userWithPwdPrototype = currentUserPrototype as IUserPrototypeWithPassword;
            if (userWithPwdPrototype != null && !parameters.IsNewObject)
            {
                userWithPwdPrototype.Password = DatabaseUtils.GetReadOnlySecureString(LoginPrototype.fakePassword);
                userWithPwdPrototype.PasswordConfirm = DatabaseUtils.GetReadOnlySecureString(LoginPrototype.fakePassword);
                password = LoginPrototype.fakePassword;             
            }

            // get the login name if it exists
            string? loginName = null;
            IUserPrototypeWithMappedLogin mappedLoginPrototype = currentUserPrototype as IUserPrototypeWithMappedLogin;
            if (mappedLoginPrototype != null)
            {
                loginName = mappedLoginPrototype.LoginName;
            }

            // populate user's role assignments
            List<string> databaseRoles = new List<string>();
            foreach (string role in currentUserPrototype.DatabaseRoleNames)
            {
                if (currentUserPrototype.IsRoleMember(role))
                {
                    databaseRoles.Add(role);
                }
            }

            // populate user's schema ownerships
            List<string> schemaNames = new List<string>();
            foreach (string schema in currentUserPrototype.SchemaNames)
            {
                if (currentUserPrototype.IsSchemaOwner(schema))
                {
                    schemaNames.Add(schema);
                }
            }
         
            UserViewInfo userViewInfo = new UserViewInfo()
            {
                ObjectInfo = new UserInfo()
                {
                    Type = userInfo?.Type ?? DatabaseUserType.WithLogin,
                    AuthenticationType = userInfo?.AuthenticationType ?? ServerAuthenticationType.Sql,
                    Name = currentUserPrototype.Name,
                    LoginName = loginName,
                    Password = password,
                    DefaultSchema = defaultSchema,
                    OwnedSchemas = schemaNames.ToArray(),
                    DatabaseRoles = databaseRoles.ToArray(),
                    DefaultLanguage = LanguageUtils.FormatLanguageDisplay(
                        languageOptions.FirstOrDefault(o => o?.Language.Name == defaultLanguageAlias || o?.Language.Alias == defaultLanguageAlias, null)),
                },
                SupportContainedUser = supportsContainedUser,
                SupportWindowsAuthentication = false,
                SupportAADAuthentication = false,
                SupportSQLAuthentication = true,
                Languages = languageOptionsList.ToArray(),
                Schemas = currentUserPrototype.SchemaNames.ToArray(),
                Logins = DatabaseUtils.LoadSqlLogins(serverConnection),
                DatabaseRoles = currentUserPrototype.DatabaseRoleNames.ToArray()
            };

            this.contextIdToViewState.Add(
                parameters.ContextId,
                new UserViewState(parameters.IsNewObject, parameters.Database, currentUserPrototype.CurrentState));

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

            ConfigureUser(
                parameters.ContextId,
                parameters.User,
                ConfigAction.Create,
                RunType.RunNow,
                viewState.Database,
                viewState.OriginalUserData);

            await requestContext.SendResult(new CreateUserResult()
            {
                User = parameters.User,
                Success = true,
                ErrorMessage = string.Empty
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

            ConfigureUser(
                parameters.ContextId,
                parameters.User,
                ConfigAction.Update,
                RunType.RunNow,
                viewState.Database,
                viewState.OriginalUserData);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = true,
                ErrorMessage = string.Empty
            });
        }

        /// <summary>
        /// Handle request to update a user
        /// </summary>
        internal async Task HandleScriptUserRequest(ScriptUserParams parameters, RequestContext<string> requestContext)
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

            // todo: check if it's an existing user

            string sqlScript = ConfigureUser(
                parameters.ContextId,
                parameters.User,
                viewState.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.ScriptToWindow,
                viewState.Database,
                viewState.OriginalUserData);

            await requestContext.SendResult(sqlScript);
        }

        internal async Task HandleDisposeUserViewRequest(DisposeUserViewRequestParams parameters, RequestContext<ResultStatus> requestContext)
        {
            this.ConnectionServiceInstance.Disconnect(new DisconnectParams()
            {
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

            string urn = (configAction == ConfigAction.Update && user != null)
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

        internal string ConfigureUser(
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

            string sqlScript = string.Empty;
            CDataContainer dataContainer = CreateUserDataContainer(connInfo, user, configAction, databaseName);
            using (var actions = new UserActions(dataContainer, configAction, user, originalData))
            {
                var executionHandler = new ExecutonHandler(actions);
                executionHandler.RunNow(runType, this);
                if (executionHandler.ExecutionResult == ExecutionMode.Failure)
                {
                    throw executionHandler.ExecutionFailureException;
                }

                if (runType == RunType.ScriptToWindow)
                {
                    sqlScript = executionHandler.ScriptTextFromLastRun;
                }
            }

            return sqlScript;
        }
    }

    internal class UserActions : ManagementActionBase
    {
        #region Variables
        private UserPrototype userPrototype;
        private ConfigAction configAction;
        #endregion

        #region Constructors / Dispose
        /// <summary>
        /// Handle user create and update actions
        /// </summary>        
        public UserActions(
            CDataContainer dataContainer,
            ConfigAction configAction,
            UserInfo? user,
            UserPrototypeData? originalData)
        {
            this.DataContainer = dataContainer;
            this.configAction = configAction;

            ExhaustiveUserTypes currentUserType;
            if (dataContainer.IsNewObject)
            {
                currentUserType = UserActions.GetUserTypeForUserInfo(user);
            }
            else
            {
                currentUserType = UserActions.GetCurrentUserTypeForExistingUser(
                    dataContainer.Server.GetSmoObject(dataContainer.ObjectUrn) as User);
            }

            this.userPrototype = UserPrototypeFactory.GetUserPrototype(dataContainer, user, originalData, currentUserType);
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
        /// called by the management actions framework to execute the action
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            if (this.configAction != ConfigAction.Drop)
            {
                this.userPrototype.ApplyChanges();
            }
        }

        internal static ExhaustiveUserTypes GetUserTypeForUserInfo(UserInfo user)
        {
            ExhaustiveUserTypes userType = ExhaustiveUserTypes.LoginMappedUser;
            switch (user.Type)
            {
                case DatabaseUserType.WithLogin:
                    userType = ExhaustiveUserTypes.LoginMappedUser;
                    break;
                case DatabaseUserType.WithWindowsGroupLogin:
                    userType = ExhaustiveUserTypes.WindowsUser;
                    break;
                case DatabaseUserType.Contained:
                    userType = ExhaustiveUserTypes.SqlUserWithPassword;
                    break;
                case DatabaseUserType.NoConnectAccess:
                    userType = ExhaustiveUserTypes.SqlUserWithoutLogin;
                    break;
            }
            return userType;
        }

        internal static DatabaseUserType GetDatabaseUserTypeForUserType(ExhaustiveUserTypes userType)
        {
            DatabaseUserType databaseUserType = DatabaseUserType.WithLogin;
            switch (userType)
            {
                case ExhaustiveUserTypes.LoginMappedUser:
                    databaseUserType = DatabaseUserType.WithLogin;
                    break;
                case ExhaustiveUserTypes.WindowsUser:
                    databaseUserType = DatabaseUserType.WithWindowsGroupLogin;
                    break;
                case ExhaustiveUserTypes.SqlUserWithPassword:
                    databaseUserType = DatabaseUserType.Contained;
                    break;
                case ExhaustiveUserTypes.SqlUserWithoutLogin:
                    databaseUserType = DatabaseUserType.NoConnectAccess;
                    break;
            }
            return databaseUserType;
        }        

        internal static ExhaustiveUserTypes GetCurrentUserTypeForExistingUser(User? user)
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

        internal static bool IsParentDatabaseContained(Urn parentDbUrn, Server server)
        {
            string parentDbName = parentDbUrn.GetNameForType("Database");
            return IsParentDatabaseContained(server.Databases[parentDbName]);
        }

        internal static bool IsParentDatabaseContained(Database parentDatabase)
        {
            return parentDatabase.IsSupportedProperty("ContainmentType") 
                && parentDatabase.ContainmentType == ContainmentType.Partial;
        }
    }
}
