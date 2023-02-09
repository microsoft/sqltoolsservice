//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal class UserActions : ManagementActionBase
    {
#region Variables
        //private UserPrototypeData userData;
        private UserPrototype userPrototype;
        private UserInfo user;
        private ConfigAction configAction;
#endregion

#region Constructors / Dispose
        /// <summary>
        /// required when loading from Object Explorer context
        /// </summary>
        /// <param name="context"></param>
        public UserActions(
            CDataContainer context,
            UserInfo user,
            ConfigAction configAction)
        {
            this.DataContainer = context;
            this.user = user;
            this.configAction = configAction;

            this.userPrototype = InitUserNew(context, user);
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
        
        private UserPrototype InitUserNew(CDataContainer dataContainer, UserInfo user)
        {
            ExhaustiveUserTypes currentUserType;
            UserPrototypeFactory userPrototypeFactory = UserPrototypeFactory.GetInstance(dataContainer, user);

            if (dataContainer.IsNewObject)
            {
                if (IsParentDatabaseContained(dataContainer.ParentUrn, dataContainer))
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

        private ExhaustiveUserTypes GetCurrentUserTypeForExistingUser(User user)
        {
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

        private bool IsParentDatabaseContained(Urn parentDbUrn, CDataContainer dataContainer)
        {
            string parentDbName = parentDbUrn.GetNameForType("Database");
            Database parentDatabase = dataContainer.Server.Databases[parentDbName];

            if (parentDatabase.IsSupportedProperty("ContainmentType")
                && parentDatabase.ContainmentType == ContainmentType.Partial)
            {
                return true;
            }

            return false;
        }
    }
}
