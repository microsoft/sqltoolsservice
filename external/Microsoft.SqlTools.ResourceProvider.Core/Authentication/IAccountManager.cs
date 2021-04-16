//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// An account manager has the information of currently logged in user and can authenticate the user   
    /// Implementing classes must add a <see cref="ExportableAttribute" />
    /// to the class in order to be found by the extension manager,
    /// and to define the type and category supported
    /// </summary>  
    public interface IAccountManager : IExportable
    {
        /// <summary>
        /// Returns true is user needs reauthentication
        /// </summary>
        Task<bool> GetUserNeedsReauthenticationAsync();

        /// <summary>
        /// Authenticates the user
        /// </summary>
        Task<IUserAccount> AuthenticateAsync();

        /// <summary>
        /// Prompt the login dialog to login to a new use that has not been cached
        /// </summary>
        Task<IUserAccount> AddUserAccountAsync();

        /// <summary>
        /// Set the current loaged in user in the cache
        /// </summary>
        Task<IUserAccount> SetCurrentAccountAsync(object account);

        /// <summary>
        /// Returns the current logged in user
        /// </summary>
        Task<IUserAccount> GetCurrentAccountAsync();

        /// <summary>
        /// Returns true if the API supports a login control
        /// </summary>
        bool HasLoginDialog
        {
            get; 
        }

        /// <summary>
        /// Event to raise when the current logged in user changed
        /// </summary>
        event EventHandler CurrentAccountChanged;
    }
}
