//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.Azure.Core.Authentication;

namespace Microsoft.SqlTools.Azure.Core.Impl
{
    /// <summary>
    /// Implementation for <see cref="IAzureUserAccount" /> using VS services
    /// Contains information about an Azure account
    /// </summary>
    internal class AzureUserAccount : IAzureUserAccount
    {
        private readonly IAzureUserAccount _azureUserAccount;
        private readonly Account _account;

        /// <summary>
        /// Default constructor to initializes user session
        /// </summary>
        public AzureUserAccount(Account account)
        {
            _account = account;
        }

        /// <summary>
        /// Default constructor to initializes user session
        /// </summary>
        public AzureUserAccount(IAzureUserAccount azureUserAccount)
        {
            _azureUserAccount = azureUserAccount;
        }

        /// <summary>
        /// Returns true if given user account equals this class
        /// </summary>
        public bool Equals(IAzureUserAccount other)
        {
            return other != null &&
                   CommonUtil.SameString(other.UserId, UserId) &&
                   CommonUtil.SameString(other.TenantId, TenantId) &&
                   CommonUtil.SameString(other.UserName, UserName);
        }

        

        /// <summary>
        /// User Id
        /// </summary>
        public string UserId
        {
            get
            {
                if (_account != null)
                {
                    return _account.UniqueId;
                }

                return _azureUserAccount == null ? string.Empty :_azureUserAccount.UniqueId;
            }
        }

        /// <summary>
        /// User Name
        /// </summary>
        public string UserName
        {
            get
            {
                if (_account != null && _account.DisplayInfo != null)
                {
                    return _account.DisplayInfo.UserName;
                }
                return _azureUserAccount != null ? _azureUserAccount.DisplayInfo.UserName : string.Empty;
            }
        }

        /// <summary>
        /// Returns true if user needs reauthentication
        /// </summary>
        public bool NeedsReauthentication
        {
            get
            {
                if (_account != null)
                {
                    return _account.NeedsReauthentication;
                }
                return _azureUserAccount != null ? _azureUserAccount.NeedsReauthentication : true;
            }
        }

        /// <summary>
        /// The actual user account object which is wrapped by this class
        /// </summary>
        public object Account
        {
            get
            {
                return _account;
                
            }
        }

        /// <summary>
        /// User display info
        /// </summary>
        public IAzureUserAccountDisplayInfo DisplayInfo
        {
            get
            {
                if (_account != null && _account.DisplayInfo != null)
                {
                    return new AzureUserAccountDisplayInfo(_account.DisplayInfo);
                }
                return _azureUserAccount != null ? new AzureUserAccountDisplayInfo(_azureUserAccount.DisplayInfo): null;
            }
        }

        /// <summary>
        /// Tenant Id
        /// </summary>
        public string TenantId
        {
            get
            {
                return _azureUserAccount != null ? _azureUserAccount.TenantId : string.Empty;
            }
        } 
    }
}
