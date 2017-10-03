//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.ResourceProvider.Core;
using Models = Microsoft.Azure.Management.Sql.Models;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Implementation for <see cref="IAzureSqlServerResource" /> using VS services
    /// Provides information about an Azure Sql Server resource
    /// </summary>
    internal class SqlAzureResource : AzureResourceWrapper, IAzureSqlServerResource
    {
        private readonly Models.Server _azureSqlServerResource;

        /// <summary>
        /// Initializes the resource 
        /// </summary>
        public SqlAzureResource(Models.Server azureResource) : base(azureResource)
        {
            CommonUtil.CheckForNull(azureResource, nameof(azureResource));
            _azureSqlServerResource = azureResource;
        }

        /// <summary>
        /// Fully qualified domain name
        /// </summary>
        public string FullyQualifiedDomainName
        {
            get
            {
                return _azureSqlServerResource != null ? _azureSqlServerResource.FullyQualifiedDomainName : string.Empty;
            }
        }

        /// <summary>
        /// Administrator User
        /// </summary>
        public string AdministratorLogin
        {
            get
            {
                return _azureSqlServerResource != null ? _azureSqlServerResource.AdministratorLogin : string.Empty;
            }
        }
    }
}
