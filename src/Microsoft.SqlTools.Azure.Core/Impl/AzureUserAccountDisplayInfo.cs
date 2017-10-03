//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.Azure.Core.Authentication;
using Microsoft.VisualStudio.WindowsAzure.Authentication;
#if (VS11 || VS12)
using Microsoft.VisualStudio.WindowsAzure.CommonAzureTools.KeychainMarshaller;
using Microsoft.VisualStudio.WindowsAzure.CommonAzureTools.KeychainShellDependencies;
using Microsoft.VisualStudio.WindowsAzure.CommonAzureTools.KeychainShellDependencies.Interfaces;
#else
using Microsoft.VisualStudio.Services.Client.AccountManagement;
#endif


namespace Microsoft.SqlTools.Azure.Core.Impl
{
    /// <summary>
    /// Implementation for <see cref="IAzureUserAccountDisplayInfo" /> using VS services
    /// Contains information about an Azure account display info
    /// </summary>
    internal class AzureUserAccountDisplayInfo : IAzureUserAccountDisplayInfo
    {
        private readonly IAzureUserAccountDisplayInfo _azureUserAccountDisplayInfo;
        private readonly AccountDisplayInfo _accountDisplayInfo;

        /// <summary>
        /// Creating the instance using <see cref="IAzureUserAccountDisplayInfo" />
        /// </summary>
        public AzureUserAccountDisplayInfo(IAzureUserAccountDisplayInfo azureUserAccountDisplayInfo)
        {
            _azureUserAccountDisplayInfo = azureUserAccountDisplayInfo;
        }

        /// <summary>
        /// Creating the instance using <see cref="AccountDisplayInfo" />
        /// </summary>
        public AzureUserAccountDisplayInfo(AccountDisplayInfo accountDisplayInfo)
        {
            _accountDisplayInfo = accountDisplayInfo;
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
                if (_accountDisplayInfo != null)
                {
                    return _accountDisplayInfo.AccountDisplayName;
                }
                return _azureUserAccountDisplayInfo != null ? _azureUserAccountDisplayInfo.AccountDisplayName : string.Empty;
            }
        }

        /// <summary>
        /// Account lego
        /// </summary>
        public byte[] AccountLogo
        {
            get
            {
                if (_accountDisplayInfo != null)
                {
                    return _accountDisplayInfo.AccountLogo;
                }
                return _azureUserAccountDisplayInfo != null ? _azureUserAccountDisplayInfo.AccountLogo : null;
            }
        }

        /// <summary>
        /// Provider display name
        /// </summary>
        public string ProviderDisplayName
        {
            get
            {
                if (_accountDisplayInfo != null)
                {
                    return _accountDisplayInfo.ProviderDisplayName;
                }
                return _azureUserAccountDisplayInfo != null ? _azureUserAccountDisplayInfo.ProviderDisplayName : string.Empty;
            }
        }

        /// <summary>
        /// Provider lego
        /// </summary>
        public byte[] ProviderLogo
        {
            get
            {
                if (_accountDisplayInfo != null)
                {
                    return _accountDisplayInfo.ProviderLogo;
                }
                return _azureUserAccountDisplayInfo != null ? _azureUserAccountDisplayInfo.ProviderLogo : null;
            }
        }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName
        {
            get
            {
                if (_accountDisplayInfo != null)
                {
                    return _accountDisplayInfo.UserName;
                }
                return _azureUserAccountDisplayInfo != null ? _azureUserAccountDisplayInfo.UserName : string.Empty;
            }
        }
    }
}
