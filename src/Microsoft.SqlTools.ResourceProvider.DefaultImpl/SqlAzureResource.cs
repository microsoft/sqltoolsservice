//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Azure.ResourceManager.Sql;
using Microsoft.SqlTools.ResourceProvider.Core;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Implementation for <see cref="IAzureSqlServerResource" /> using VS services
    /// Provides information about an Azure Sql Server resource
    /// </summary>
    public class SqlAzureResource : AzureResourceWrapper, IAzureSqlServerResource
    {
        private readonly SqlServerData _azureSqlServerResource;

        /// <summary>
        /// Initializes the resource
        /// </summary>
        public SqlAzureResource(SqlServerData azureResource) : base(ToResourceInfo(azureResource))
        {
            CommonUtil.CheckForNull(azureResource, nameof(azureResource));
            _azureSqlServerResource = azureResource;
        }

        private static AzureResourceInfo ToResourceInfo(SqlServerData data)
        {
            CommonUtil.CheckForNull(data, nameof(data));
            return new AzureResourceInfo(
                data.Name,
                data.Id?.ToString(),
                data.ResourceType.ToString(),
                data.Location.ToString());
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
