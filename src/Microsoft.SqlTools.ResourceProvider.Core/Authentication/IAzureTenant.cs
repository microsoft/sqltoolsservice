//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// User account authentication information to be used by <see cref="IAccountManager" />
    /// </summary>
    public interface IAzureTenant
    {
        /// <summary>
        /// The unique Id for the tenant
        /// </summary>
        string TenantId
        {
            get;
        }
        
        /// <summary>
        /// Display ID
        /// </summary>
        string AccountDisplayableId
        {
            get;
        }
        
    }
}
