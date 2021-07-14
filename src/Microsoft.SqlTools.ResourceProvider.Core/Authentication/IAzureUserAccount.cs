//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// Contains information about an Azure user account
    /// </summary>
    public interface IAzureUserAccount : IEquatable<IAzureUserAccount>, IUserAccount
    {
        Account UnderlyingAccount
        {
            get;
            set;
        }
        /// <summary>
        /// User Account Display Info
        /// </summary>
        IAzureUserAccountDisplayInfo DisplayInfo
        {
            get;
        }

        /// <summary>
        /// Primary Tenant Id
        /// </summary>
        string TenantId
        {
            get;
        }

        /// <summary>
        /// All tenant IDs
        /// </summary>
        IList<IAzureTenant> AllTenants
        {
            get;
        }
    }
}
