//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SqlTools.Azure.Core.Authentication
{
    /// <summary>
    /// Contains information about an Azure user account
    /// </summary>
    public  interface IAzureUserAccount : IEquatable<IAzureUserAccount>, IUserAccount
    {
        /// <summary>
        /// User Account Display Info
        /// </summary>
        IAzureUserAccountDisplayInfo DisplayInfo
        {
            get;
        }

        /// <summary>
        /// Tenant Id
        /// </summary>
        string TenantId
        {
            get;
        }         
    }
}
