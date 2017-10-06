//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// User account authentication information to be used by <see cref="IAccountManager" />
    /// </summary>
    public interface IUserAccount
    {
        /// <summary>
        /// The unique Id for the user
        /// </summary>
        string UniqueId
        {
            get;
        }
        
        /// <summary>
        /// Returns true if user needs reauthentication
        /// </summary>
        bool NeedsReauthentication
        {
            get;
        }
        
    }
}
