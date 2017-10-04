//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// Contains information about an Azure user account subscription
    /// </summary>
    public interface IAzureUserAccountSubscriptionContext :
        IAzureSubscriptionContext, IEquatable<IAzureUserAccountSubscriptionContext>
    {
        /// <summary>
        /// User Account
        /// </summary>
        IAzureUserAccount UserAccount
        {
            get;
        }        
    }
}
