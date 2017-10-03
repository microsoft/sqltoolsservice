//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.Rest;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    /// <summary>
    /// A container to create test data and mock classes to test azure services and providers 
    /// </summary>
    internal class AzureTestContext
    {
        public AzureTestContext(Dictionary<string, List<string>> subscriptionToDatabaseMap)
        {
            AzureAccountManagerMock = new Mock<IAzureAuthenticationManager>();
            List<IAzureUserAccountSubscriptionContext> accountSubscriptions = new List
                <IAzureUserAccountSubscriptionContext>();
            AzureResourceManagerMock = new Mock<IAzureResourceManager>();

            foreach (string subscriptionName in subscriptionToDatabaseMap.Keys)
            {
                var azureAccount = new AzureUserAccount();
                AzureSubscriptionIdentifier subId = new AzureSubscriptionIdentifier(azureAccount, subscriptionName, null);
                var subscription = new AzureUserAccountSubscriptionContext(subId, new TokenCredentials("dummy"));
                accountSubscriptions.Add(subscription);

                var sessionMock = new Mock<IAzureResourceManagementSession>();
                IAzureResourceManagementSession session = sessionMock.Object;
                sessionMock.Setup(x => x.SubscriptionContext).Returns(subscription);
                AzureResourceManagerMock.Setup(x => x.CreateSessionAsync(subscription)).Returns(Task.FromResult(session));
                MockServersAndDatabases(subscriptionToDatabaseMap[subscriptionName], session);
            }
            AzureAccountManagerMock.Setup(x => x.GetSelectedSubscriptionsAsync()).Returns
                (Task.FromResult(accountSubscriptions as IEnumerable<IAzureUserAccountSubscriptionContext>));
        }

        private void MockServersAndDatabases(List<string> resourceNames, IAzureResourceManagementSession session)
        {
            IEnumerable<IAzureResource> azureResources = resourceNames.Select(
                x => new AzureResourceWrapper(new TrackedResource(Guid.NewGuid().ToString(), "id", x, "type")) { ResourceGroupName = Guid.NewGuid().ToString()}
            ).ToList();

            List<IAzureSqlServerResource> servers = new List<IAzureSqlServerResource>();
            foreach (var azureResourceWrapper in azureResources.ToList())
            {
                var serverName = GetServerName(azureResourceWrapper.Name);
                if (string.IsNullOrEmpty(serverName) || servers.Any(x => x.Name == serverName))
                {
                    continue;
                }

                var databases = azureResources.Where(x => x.Name.StartsWith(serverName + "/"));
                if (serverName.Equals("error", StringComparison.OrdinalIgnoreCase))
                {
                    AzureResourceManagerMock.Setup(x => x.GetAzureDatabasesAsync(session, azureResourceWrapper.ResourceGroupName, serverName))
                                            .Throws(new ApplicationException(serverName));
                }
                else
                {
                    AzureResourceManagerMock.Setup(x => x.GetAzureDatabasesAsync(session, azureResourceWrapper.ResourceGroupName, serverName))
                                            .Returns(Task.FromResult(databases));
                }

                Server azureSqlServer = new Server(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), serverName, null, null, null, null, null, null, null, null, fullyQualifiedDomainName: serverName + ".database.windows.net");
                servers.Add(new SqlAzureResource(azureSqlServer)
                {
                    ResourceGroupName = azureResourceWrapper.ResourceGroupName
                });
            }
            AzureResourceManagerMock.Setup(x => x.GetSqlServerAzureResourcesAsync(session))
                                    .Returns(Task.FromResult(servers as IEnumerable<IAzureSqlServerResource>));
        }

        internal static string GetServerName(string name)
        {           
            string azureResourceName = name;
             int separatorIndex = azureResourceName.IndexOf("/", StringComparison.OrdinalIgnoreCase);
            if (separatorIndex >= 0)
            {
                return azureResourceName.Substring(0, separatorIndex);
            }
            else
            {
                return azureResourceName;
            }
        }

        public Mock<IAzureAuthenticationManager> AzureAccountManagerMock
        {
            get;
            set;
        }

        public IAzureAuthenticationManager AzureAccountManager
        {
            get { return AzureAccountManagerMock.Object; }
        }

        public IAzureResourceManager AzureResourceManager
        {
            get { return AzureResourceManagerMock.Object; }
        }

        public Mock<IAzureResourceManager> AzureResourceManagerMock
        {
            get;
            set;
        }

    }
}
