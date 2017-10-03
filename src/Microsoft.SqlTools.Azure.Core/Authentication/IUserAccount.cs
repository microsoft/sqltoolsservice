//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.Azure.Core.Authentication
{
    /// <summary>
    /// User account authentication information to be used by <see cref="IAccountManager" />
    /// </summary>
    public interface IUserAccount
    {
        /// <summary>
        /// The unique Id for the user
        /// </summary>
        string UserId
        {
            get;
        }

        /// <summary>
        /// The user name for the user
        /// </summary>
        string UserName
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

        /// <summary>
        /// The actual account object which is wrapped by this class 
        /// </summary>
        object Account { get; }
    }
}
