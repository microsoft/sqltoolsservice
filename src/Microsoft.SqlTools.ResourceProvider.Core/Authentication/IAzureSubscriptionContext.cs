//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// Contains information about an Azure subscription
    public interface IAzureSubscriptionContext : IEquatable<IAzureSubscriptionContext>
    {
        /// <summary>
        /// Subscription Identifier
        /// </summary>
        IAzureSubscriptionIdentifier Subscription
        {
            get;
        }

        /// <summary>
        /// Subscription name
        /// </summary>
        string SubscriptionName
        {
            get;
        }
    }
}
