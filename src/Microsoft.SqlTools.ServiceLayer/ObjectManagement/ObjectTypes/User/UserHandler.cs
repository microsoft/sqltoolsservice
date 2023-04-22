//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// User object type handler
    /// </summary>
    public class UserHandler : ObjectTypeHandler<UserInfo, UserViewContext>
    {
        public UserHandler(ConnectionService connectionService) : base(connectionService)
        {
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.User;
        }

        public override async Task<InitializeViewResult> InitializeObjectView(Contracts.InitializeViewRequestParams parameters)
        {
            // check input parameters
            if (string.IsNullOrWhiteSpace(parameters.Database))
            {
                throw new ArgumentNullException("parameters.Database");
            }

            // open a connection for running the user dialog and associated task
            ConnectionInfo originalConnInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out originalConnInfo);
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
                await this.ConnectionService.Connect(connectParams);
            }
            finally
            {
                originalConnInfo.ConnectionDetails.DatabaseName = originalDatabaseName;
            }
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(parameters.ContextId, out connInfo);

            // create a default user data context and database object
            CDataContainer dataContainer = CreateUserDataContainer(connInfo, null, ConfigAction.Create, parameters.Database);
            string databaseUrn = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                "Server/Database[@Name='{0}']", Urn.EscapeString(parameters.Database));
            Database parentDb = dataContainer.Server.GetSmoObject(databaseUrn) as Database;

            var languageOptions = LanguageUtils.GetDefaultLanguageOptions(dataContainer);
            var languageOptionsList = languageOptions.Select(LanguageUtils.FormatLanguageDisplay).ToList();
            languageOptionsList.Insert(0, SR.DefaultLanguagePlaceholder);

            // if viewing an exisitng user then populate some properties
            UserInfo userInfo = null;
            string defaultLanguageAlias = null;
            ExhaustiveUserTypes userType = ExhaustiveUserTypes.LoginMappedUser;
            if (!parameters.IsNewObject)
            {
                User existingUser = dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as User;
                userType = UserActions.GetCurrentUserTypeForExistingUser(existingUser);
                DatabaseUserType databaseUserType = UserActions.GetDatabaseUserTypeForUserType(userType);

                // if contained user determine if SQL or AAD auth type
                ServerAuthenticationType authenticationType =
                    (databaseUserType == DatabaseUserType.Contained && userType == ExhaustiveUserTypes.ExternalUser)
                        ? ServerAuthenticationType.AzureActiveDirectory : ServerAuthenticationType.Sql;

                userInfo = new UserInfo()
                {
                    Type = databaseUserType,
                    AuthenticationType = authenticationType,
                    Name = existingUser.Name,
                    LoginName = existingUser.Login,
                    DefaultSchema = existingUser.DefaultSchema,
                };

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
            string defaultSchema = null;
            IUserPrototypeWithDefaultSchema defaultSchemaPrototype = currentUserPrototype as IUserPrototypeWithDefaultSchema;
            if (defaultSchemaPrototype != null && defaultSchemaPrototype.IsDefaultSchemaSupported)
            {
                defaultSchema = defaultSchemaPrototype.DefaultSchema;
            }

            ServerConnection serverConnection = dataContainer.ServerConnection;
            bool isSqlAzure = serverConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase;
            bool supportsContainedUser = isSqlAzure || UserActions.IsParentDatabaseContained(parentDb);

            // set the fake password placeholder when editing an existing user
            string password = null;
            IUserPrototypeWithPassword userWithPwdPrototype = currentUserPrototype as IUserPrototypeWithPassword;
            if (userWithPwdPrototype != null && !parameters.IsNewObject)
            {
                userWithPwdPrototype.Password = DatabaseUtils.GetReadOnlySecureString(LoginPrototype.fakePassword);
                userWithPwdPrototype.PasswordConfirm = DatabaseUtils.GetReadOnlySecureString(LoginPrototype.fakePassword);
                password = LoginPrototype.fakePassword;
            }

            // get the login name if it exists
            string loginName = null;
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

            string defaultLanguage = null;
            if (!parameters.IsNewObject)
            {
                defaultLanguage = LanguageUtils.FormatLanguageDisplay(
                    languageOptions.FirstOrDefault(
                        o => o?.Language.Name == defaultLanguageAlias || o?.Language.Alias == defaultLanguageAlias, null));
            }
            if (string.IsNullOrEmpty(defaultLanguage))
            {
                defaultLanguage = SR.DefaultLanguagePlaceholder;
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
                    DefaultLanguage = defaultLanguage
                },
                SupportContainedUser = supportsContainedUser,
                SupportWindowsAuthentication = false,
                SupportAADAuthentication = currentUserPrototype.AADAuthSupported,
                SupportSQLAuthentication = true,
                Languages = languageOptionsList.ToArray(),
                Schemas = currentUserPrototype.SchemaNames.ToArray(),
                Logins = DatabaseUtils.LoadSqlLogins(serverConnection),
                DatabaseRoles = currentUserPrototype.DatabaseRoleNames.ToArray()
            };
            var context = new UserViewContext(parameters, serverConnection, currentUserPrototype.CurrentState);
            return new InitializeViewResult { ViewInfo = userViewInfo, Context = context };
        }

        public override Task Save(UserViewContext context, UserInfo obj)
        {
            ConfigureUser(
                context.Parameters.ContextId,
                obj,
                context.Parameters.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.RunNow,
                context.Parameters.Database,
                context.OriginalUserData);
            return Task.CompletedTask;
        }

        public override Task<string> Script(UserViewContext context, UserInfo obj)
        {
            var script = ConfigureUser(
                context.Parameters.ContextId,
                obj,
                context.Parameters.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.ScriptToWindow,
                context.Parameters.Database,
                context.OriginalUserData);
            return Task.FromResult(script);
        }

        internal CDataContainer CreateUserDataContainer(ConnectionInfo connInfo, UserInfo user, ConfigAction configAction, string databaseName)
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

        internal string ConfigureUser(string ownerUri, UserInfo user, ConfigAction configAction, RunType runType, string databaseName, UserPrototypeData originalData)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(ownerUri, out connInfo);
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
}