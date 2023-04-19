//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Login object type handler
    /// </summary>
    public class LoginHandler : ObjectTypeHandler<LoginInfo, LoginViewContext>
    {
        public LoginHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.ServerLevelLogin;
        }

        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams parameters)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginViewInfo loginViewInfo = new LoginViewInfo();

            // TODO cache databases and languages
            string[] databases = new string[dataContainer.Server.Databases.Count];
            for (int i = 0; i < dataContainer.Server.Databases.Count; i++)
            {
                databases[i] = dataContainer.Server.Databases[i].Name;
            }

            var languageOptions = LanguageUtils.GetDefaultLanguageOptions(dataContainer);
            var languageOptionsList = languageOptions.Select(LanguageUtils.FormatLanguageDisplay).ToList();
            if (parameters.IsNewObject)
            {
                languageOptionsList.Insert(0, SR.DefaultLanguagePlaceholder);
            }
            string[] languages = languageOptionsList.ToArray();
            LoginPrototype prototype = parameters.IsNewObject
            ? new LoginPrototype(dataContainer.Server)
            : new LoginPrototype(dataContainer.Server, dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as Login);

            List<string> loginServerRoles = new List<string>();
            foreach (string role in prototype.ServerRoles.ServerRoleNames)
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
                DefaultLanguage = parameters.IsNewObject ? SR.DefaultLanguagePlaceholder : LanguageUtils.FormatLanguageDisplay(languageOptions.FirstOrDefault(o => o?.Language.Name == prototype.DefaultLanguage || o?.Language.Alias == prototype.DefaultLanguage, null)),
                ServerRoles = loginServerRoles.ToArray(),
                ConnectPermission = prototype.WindowsGrantAccess,
                IsEnabled = !prototype.IsDisabled,
                IsLockedOut = prototype.IsLockedOut,
                UserMapping = new ServerLoginDatabaseUserMapping[0]
            };

            var viewInfo = new LoginViewInfo()
            {
                ObjectInfo = loginInfo,
                SupportWindowsAuthentication = prototype.WindowsAuthSupported,
                SupportAADAuthentication = prototype.AADAuthSupported,
                SupportSQLAuthentication = true, // SQL Auth support for login, not necessarily mean SQL Auth support for CONNECT etc.
                CanEditLockedOutState = !parameters.IsNewObject && prototype.IsLockedOut,
                Databases = databases,
                Languages = languages,
                ServerRoles = prototype.ServerRoles.ServerRoleNames,
                SupportAdvancedPasswordOptions = dataContainer.Server.DatabaseEngineType == DatabaseEngineType.Standalone || dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse,
                SupportAdvancedOptions = dataContainer.Server.DatabaseEngineType == DatabaseEngineType.Standalone || dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance
            };
            var context = new LoginViewContext(parameters);
            return Task.FromResult(new InitializeViewResult()
            {
                ViewInfo = viewInfo,
                Context = context
            });
        }

        public override Task Save(LoginViewContext context, LoginInfo obj)
        {
            if (context.Parameters.IsNewObject)
            {
                this.DoHandleCreateLoginRequest(context, obj, RunType.RunNow);
            }
            else
            {
                this.DoHandleUpdateLoginRequest(context, obj, RunType.RunNow);
            }
            return Task.CompletedTask;
        }

        public override Task<string> Script(LoginViewContext context, LoginInfo obj)
        {
            string script;
            if (context.Parameters.IsNewObject)
            {
                script = this.DoHandleCreateLoginRequest(context, obj, RunType.ScriptToWindow);
            }
            else
            {
                script = this.DoHandleUpdateLoginRequest(context, obj, RunType.ScriptToWindow);
            }
            return Task.FromResult(script);
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

        private string ConfigureLogin(CDataContainer dataContainer, ConfigAction configAction, RunType runType, LoginPrototype prototype)
        {
            string sqlScript = string.Empty;
            using (var actions = new LoginActions(dataContainer, configAction, prototype))
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

        private string DoHandleUpdateLoginRequest(LoginViewContext context, LoginInfo login, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginPrototype prototype = new LoginPrototype(dataContainer.Server, dataContainer.Server.Logins[login.Name]);

            prototype.SqlPassword = login.Password;
            if (0 != string.Compare(login.DefaultLanguage, SR.DefaultLanguagePlaceholder, StringComparison.Ordinal))
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

            foreach (string role in prototype.ServerRoles.ServerRoleNames)
            {
                prototype.ServerRoles.SetMember(role, false);
            }

            foreach (string role in login.ServerRoles)
            {
                prototype.ServerRoles.SetMember(role, true);
            }

            return ConfigureLogin(
                dataContainer,
                ConfigAction.Update,
                runType,
                prototype);
        }

        private string DoHandleCreateLoginRequest(LoginViewContext context, LoginInfo login, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);

            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginPrototype prototype = new LoginPrototype(dataContainer.Server, login);

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

            // TODO move this to LoginData
            // TODO support role assignment for Azure            
            foreach (string role in login.ServerRoles ?? Enumerable.Empty<string>())
            {
                prototype.ServerRoles.SetMember(role, true);
            }

            return ConfigureLogin(dataContainer, ConfigAction.Create, runType, prototype);
        }

    }
}