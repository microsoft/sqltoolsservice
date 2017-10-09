//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    /// <summary>
    /// An object, usable in <see cref="CreateFirewallRuleRequest"/>s and other messages
    /// </summary>
    public class Account
    {
        /// <summary>
        /// The key that identifies the account
        /// </summary>
        public AccountKey Key { get; set; }

        /// <summary>
        /// Display information for the account
        /// </summary>
        public AccountDisplayInfo DisplayInfo { get; set; }

        /// <summary>
        /// Customizable properties, which will include the access token or similar authentication support
        /// </summary>
        public AccountProperties Properties { get; set; }

        /// <summary>
        /// Indicates if the account needs refreshing
        /// </summary>
        public bool IsStale { get; set; }
  
    }

    /// <summary>
    /// Azure-specific properties. Note that ideally with would reuse GeneralRequestDetails but
    /// this isn't feasible right now as that is specific to having an Options property to hang it off
    /// </summary>
    public class AccountProperties
    {

        /// <summary>
        /// Is this a Microsoft account, such as live.com, or not?
        /// </summary>
        internal bool IsMsAccount
        {
            get;
            set;
        }

        /// <summary>
        /// Tenants for each object
        /// </summary>
        public IEnumerable<Tenant> Tenants
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Represents a key that identifies an account.
    /// </summary>
    public class AccountKey
    {
        /// <summary>
        /// Identifier of the provider
        /// </summary>
        public string ProviderId { get; set; }

        // Note: ignoring ProviderArgs as it's not relevant

        /// <summary>
        /// Identifier for the account, unique to the provider
        /// </summary>
        public string AccountId { get; set; }
    }

    /// <summary>
    /// Represents display information for an account.
    /// </summary>
    public class AccountDisplayInfo
    {
        /// <summary>
        /// A display name that offers context for the account, such as "Contoso".
        /// </summary>
            
        public string ContextualDisplayName { get; set; }

        // Note: ignoring ContextualLogo as it's not needed

        /// <summary>
        /// A display name that identifies the account, such as "user@contoso.com".
        /// </summary
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Represents a tenant (an Azure Active Directory instance) to which a user has access
    /// </summary>
    public class Tenant
    {
        /// <summary>
        /// Globally unique identifier of the tenant
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Display name of the tenant
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Identifier of the user in the tenant
        /// </summary>
        public string UserId { get; set; }
    }
}
