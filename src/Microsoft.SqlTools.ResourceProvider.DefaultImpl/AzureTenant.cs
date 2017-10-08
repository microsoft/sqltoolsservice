//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Implementation for <see cref="IAzureTenant" /> using VS services
    /// Contains information about an Azure account
    /// </summary>
    public class AzureTenant : IAzureTenant
    {
        public string TenantId
        {
            get;
            set;
        }

        public string AccountDisplayableId
        {
            get;
            set;
        }

        /// <summary>
        /// Access token for use in login scenarios. Note that we could consider implementing this better in the 
        /// </summary>
        public string AccessToken
        {
            get;
            set;
        }

        /// <summary>
        /// Optional token type defining whether this is a Bearer token or other type of token 
        /// </summary>
        public string TokenType
        {
            get;
            set;
        }
    }
}
