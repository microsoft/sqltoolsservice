//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// An Azure Sql server resource
    /// </summary>
    public interface IAzureSqlServerResource : IAzureResource
    {
        /// <summary>
        /// Fully qualified domain name
        /// </summary>        
        string FullyQualifiedDomainName
        {
            get;
        }

        /// <summary>
        /// Administrator Login
        /// </summary>
        string AdministratorLogin
        {
            get;
        }
    }
}
