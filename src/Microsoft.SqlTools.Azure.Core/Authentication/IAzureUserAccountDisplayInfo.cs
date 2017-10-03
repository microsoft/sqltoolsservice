//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SqlTools.Azure.Core.Authentication
{
    /// <summary>
    /// Contains information about an Azure user display
    /// </summary>
    public interface IAzureUserAccountDisplayInfo : IEquatable<IAzureUserAccountDisplayInfo>
    {
        /// <summary>
        /// Account Display Name
        /// </summary>
        string AccountDisplayName
        {
            get;
        }

        /// <summary>
        /// Account Logo
        /// </summary>
        byte[] AccountLogo
        {
            get;
        }

        /// <summary>
        /// Provider Dislay Name
        /// </summary>
        string ProviderDisplayName
        {
            get;
        }

        /// <summary>
        /// Provider Logo
        /// </summary>
        byte[] ProviderLogo
        {
            get;
        }

        /// <summary>
        /// User Name
        /// </summary>
        string UserName
        {
            get;
        }
    }
}
