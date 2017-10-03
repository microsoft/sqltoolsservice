//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Azure.Core
{
    internal class AzureServerDatabaseDiscoveryProvider
    {
        private readonly IAzureResourceManagementSession _session;
        /// <summary>
        /// the event to raise when a database is found
        /// </summary>
        public event EventHandler<DatabaseInfoEventArgs> DatabaseFound;

        public AzureServerDatabaseDiscoveryProvider(IAzureResourceManager azureResourceManager, IAzureResourceManagementSession session, ServerDefinition serverDefinition)
        {
            CommonUtil.CheckForNull(session, "session");
            CommonUtil.CheckForNull(azureResourceManager, "azureResourceManager");
            _session = session;
            AzureResourceManager = azureResourceManager;

            ServerDefinition = serverDefinition ?? ServerDefinition.Default;
        }

        /// <summary>
        /// Returns the resource manager that has same metadata as this class
        /// </summary>
        internal IAzureResourceManager AzureResourceManager { get; set; }

        private ServerDefinition ServerDefinition { get; set; }

        public async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabasesForServers(IList<IAzureSqlServerResource> serverResources, CancellationToken cancellationToken)
        {
            ServiceResponse < DatabaseInstanceInfo >  result = new ServiceResponse<DatabaseInstanceInfo>();
            if (serverResources != null)
            {
                result = await AzureUtil.ExecuteGetAzureResourceAsParallel(_session, serverResources, null, cancellationToken, GetDatabasesForServerFromService);
            }
            return result;
        }        

        private async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabasesForServerFromService(
           IAzureResourceManagementSession session,
           IAzureSqlServerResource azureSqlServer,
           string serverName,
           CancellationToken cancellationToken,
           CancellationToken internalCancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ServiceResponse<DatabaseInstanceInfo>();
                }
                ServerInstanceInfo serverInstanceInfo = new ServerInstanceInfo(ServerDefinition)
                {
                    Name = azureSqlServer.Name,
                    FullyQualifiedDomainName = azureSqlServer.FullyQualifiedDomainName,
                    AdministratorLogin = azureSqlServer.AdministratorLogin
                };
                OnDatabaseFound(new DatabaseInstanceInfo(serverInstanceInfo));
                IEnumerable<IAzureResource> databases = await AzureResourceManager.GetAzureDatabasesAsync(
                    session,
                    azureSqlServer.ResourceGroupName,
                    azureSqlServer.Name);
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ServiceResponse<DatabaseInstanceInfo>();
                }
                else
                {
                    IEnumerable<DatabaseInstanceInfo> data = databases.Select(x => ConvertToModel(serverInstanceInfo, x));
                    ServiceResponse<DatabaseInstanceInfo> result = new ServiceResponse<DatabaseInstanceInfo>(data);
                    foreach (var databaseInstance in result.Data)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        OnDatabaseFound(databaseInstance);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse<DatabaseInstanceInfo>(ex);
            }
        }

        /// <summary>
        /// Raises DatabaseFound event with the given databases info
        /// </summary>
        private void OnDatabaseFound(DatabaseInstanceInfo databaseInfo)
        {
            if (DatabaseFound != null)
            {
                DatabaseFound(this, new DatabaseInfoEventArgs() { Database = databaseInfo });
            }
        }

        /// <summary>
        /// Converts the resource to DatabaseInstanceInfo
        /// </summary>       
        private DatabaseInstanceInfo ConvertToModel(ServerInstanceInfo serverInstanceInfo, IAzureResource azureResource)
        {
            DatabaseInstanceInfo databaseInstance = new DatabaseInstanceInfo(serverInstanceInfo)
            {
                Name = azureResource.Name.Replace(serverInstanceInfo.Name + "/", "")
            };

            return databaseInstance;
        }

    }
}
