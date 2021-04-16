//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// Contains information about an azure subscription identifier
    /// </summary>
    public interface IAzureSubscriptionIdentifier : IEquatable<IAzureSubscriptionIdentifier>
    {
        IAzureUserAccount UserAccount
        {
            get;
        }

        /// <summary>
        /// Service endpoint
        /// </summary>
        Uri ServiceManagementEndpoint
        {
            get;
        }

        /// <summary>
        /// Subscription id
        /// </summary>
        string SubscriptionId
        {
            get;
        }        
    }
}
