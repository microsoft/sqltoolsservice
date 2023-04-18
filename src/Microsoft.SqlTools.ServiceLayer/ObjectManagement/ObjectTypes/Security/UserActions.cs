//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
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
            UserInfo user,
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
                currentUserType = UserActions.GetCurrentUserTypeForExistingUser(dataContainer.Server.GetSmoObject(dataContainer.ObjectUrn) as User);
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
                    if (user.AuthenticationType == ServerAuthenticationType.AzureActiveDirectory)
                    {
                        userType = ExhaustiveUserTypes.ExternalUser;
                    }
                    else
                    {
                        userType = ExhaustiveUserTypes.SqlUserWithPassword;
                    }
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
                case ExhaustiveUserTypes.ExternalUser:
                    databaseUserType = DatabaseUserType.Contained;
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
                case UserType.External:
                    return ExhaustiveUserTypes.ExternalUser;
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
