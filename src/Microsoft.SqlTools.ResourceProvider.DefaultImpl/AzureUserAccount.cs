//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.Core.Impl
{
    /// <summary>
    /// Implementation for <see cref="IAzureUserAccount" /> using VS services
    /// Contains information about an Azure account
    /// </summary>
    internal class AzureUserAccount : IAzureUserAccount
    {
        private string uniqueId;

        /// <summary>
        /// Default constructor to initializes user session
        /// </summary>
        public AzureUserAccount()
        {
        }

        /// <summary>
        /// Default constructor to initializes user session
        /// </summary>
        public AzureUserAccount(IAzureUserAccount azureUserAccount)
        {
            CopyFrom(azureUserAccount);
        }

        private void CopyFrom(IAzureUserAccount azureUserAccount)
        {
            this.DisplayInfo = new AzureUserAccountDisplayInfo(azureUserAccount.DisplayInfo);
            this.NeedsReauthentication = azureUserAccount.NeedsReauthentication;
            this.TenantId = azureUserAccount.TenantId;
            this.UniqueId = azureUserAccount.UniqueId;
        }
        /// <summary>
        /// Returns true if given user account equals this class
        /// </summary>
        public bool Equals(IAzureUserAccount other)
        {
            return other != null &&
                   CommonUtil.SameString(other.UniqueId, UniqueId) &&
                   CommonUtil.SameString(other.TenantId, TenantId);
        }
        
        /// <summary>
        /// Unique Id
        /// </summary>
        public string UniqueId
        {
            get
            {
                return uniqueId == null ? string.Empty : uniqueId;
            }
            set
            {
                this.uniqueId = value;
            }
        }
        
        /// <summary>
        /// Returns true if user needs reauthentication
        /// </summary>
        public bool NeedsReauthentication
        {
            get;
            set;
        }
        
        /// <summary>
        /// User display info
        /// </summary>
        public IAzureUserAccountDisplayInfo DisplayInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Tenant Id
        /// </summary>
        public string TenantId
        {
            get;
            set;
        } 
    }
}
