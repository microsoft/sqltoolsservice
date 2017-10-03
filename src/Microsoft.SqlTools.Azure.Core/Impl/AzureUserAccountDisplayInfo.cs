//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Azure.Core.Authentication;


namespace Microsoft.SqlTools.Azure.Core.Impl
{
    /// <summary>
    /// Implementation for <see cref="IAzureUserAccountDisplayInfo" /> using VS services
    /// Contains information about an Azure account display info
    /// </summary>
    internal class AzureUserAccountDisplayInfo : IAzureUserAccountDisplayInfo
    {
        private string userName;
        private string accountDisplayName;

        /// <summary>
        /// Creating the instance using <see cref="IAzureUserAccountDisplayInfo" />
        /// </summary>
        public AzureUserAccountDisplayInfo(IAzureUserAccountDisplayInfo azureUserAccountDisplayInfo)
        {
            CopyFrom(azureUserAccountDisplayInfo);
        }

        /// <summary>
        /// Creating empty instance
        /// </summary>
        public AzureUserAccountDisplayInfo()
        {
        }

        private void CopyFrom(IAzureUserAccountDisplayInfo azureUserAccountDisplayInfo)
        {
            this.AccountDisplayName = azureUserAccountDisplayInfo.AccountDisplayName;
            this.AccountLogo = azureUserAccountDisplayInfo.AccountLogo;
            this.ProviderDisplayName = azureUserAccountDisplayInfo.ProviderDisplayName;
            this.ProviderLogo = azureUserAccountDisplayInfo.ProviderLogo;
            this.UserName = azureUserAccountDisplayInfo.UserName;
        }

        /// <summary>
        /// Returns true if given user account equals this class
        /// </summary>
        public bool Equals(IAzureUserAccountDisplayInfo other)
        {
            return other != null && 
                ((other.AccountDisplayName == null && AccountDisplayName == null ) || (other.AccountDisplayName != null && other.AccountDisplayName.Equals(AccountDisplayName))) && 
                ((other.UserName == null && UserName == null ) || (other.UserName != null && other.UserName.Equals(UserName)));
        }

        /// <summary>
        /// Account display name
        /// </summary>
        public string AccountDisplayName
        {
            get
            {
                return accountDisplayName != null ? accountDisplayName : string.Empty;
            }
            set
            {
                accountDisplayName = value;
            }
        }

        /// <summary>
        /// Account lego
        /// </summary>
        public byte[] AccountLogo
        {
            get;
            set;
        }

        /// <summary>
        /// Provider display name
        /// </summary>
        public string ProviderDisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// Provider lego
        /// </summary>
        public byte[] ProviderLogo
        {
            get;
            set;
        }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName
        {
            get
            {
                return userName != null ? userName : string.Empty;
            }
            set
            {
                userName = value;
            }
        }
    }
}
