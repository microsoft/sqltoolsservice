//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    internal static class FakeDataFactory
    {
        //public static ExtensionProperties CreateServiceProperties(IList<Lazy<IServerDiscoveryProvider, IExportableMetadata>> exports)
        //{
        //    FakeExportProvider fakeProvider = new FakeExportProvider(f => ((FakeInstanceExportDefinition)f).Instance);
        //    foreach (var export in exports)
        //    {
        //        var metadata = new Dictionary<string, object>()
        //        {
        //            {"ServerType", export.Metadata.ServerType},
        //            {"Category", export.Metadata.Category},
        //            {"Id", export.Metadata.Id },
        //            {"Priority", export.Metadata.Priority}
        //        };

        //        var definition = new FakeInstanceExportDefinition(typeof(IServerDiscoveryProvider), export.Value, metadata);
        //        fakeProvider.AddExportDefinitions(definition);
        //    }
        //    var trace = new Mock<ITrace>();
        //    trace.Setup(x => x.TraceEvent(It.IsAny<TraceEventType>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object[]>()))
        //         .Returns(true);

        //    var metadata2 = new Dictionary<string, object>()
        //        {
        //            {"Id", Guid.NewGuid().ToString()},
        //            {"Priority", 0}
        //        };
        //    var traceDefinition = new FakeInstanceExportDefinition(typeof(ITrace), trace, metadata2);
        //    fakeProvider.AddExportDefinitions(traceDefinition);

        //    ExtensionProperties serviceProperties = new ExtensionProperties(false);
        //    serviceProperties.Providers = new ExportProvider[] { fakeProvider };
        //    TypeCatalog typeCatalog = new TypeCatalog(typeof(FakeTrace));
        //    serviceProperties.AddCatalog(typeCatalog);
        //    return serviceProperties;
        //}

        internal static AzureDatabaseDiscoveryProvider CreateAzureDatabaseDiscoveryProvider(
            Dictionary<string, List<string>> subscriptionToDatabaseMap)
        {
            AzureTestContext testContext = new AzureTestContext(subscriptionToDatabaseMap);

            AzureDatabaseDiscoveryProvider databaseDiscoveryProvider = new AzureDatabaseDiscoveryProvider();
            databaseDiscoveryProvider.AccountManager = testContext.AzureAccountManager;
            databaseDiscoveryProvider.AzureResourceManager = testContext.AzureResourceManager;

            return databaseDiscoveryProvider;
        }

        internal static AzureSqlServerDiscoveryProvider CreateAzureServerDiscoveryProvider(Dictionary<string, List<string>> subscriptionToDatabaseMap)
        {
            AzureTestContext testContext = new AzureTestContext(subscriptionToDatabaseMap);

            AzureSqlServerDiscoveryProvider serverDiscoveryProvider = new AzureSqlServerDiscoveryProvider();
            serverDiscoveryProvider.AccountManager = testContext.AzureAccountManager;
            serverDiscoveryProvider.AzureResourceManager = testContext.AzureResourceManager;

            return serverDiscoveryProvider;
        }

        //internal static IDependencyManager AddDependencyProvider<T>(T provider,
        //    ServerDefinition serverDefinition, IDependencyManager existingDependencyManager = null)
        //    where T : IExportable
        //{
        //    return AddDependencyProviders(new Dictionary<T,ServerDefinition>() {{ provider, serverDefinition}}, existingDependencyManager);
        //}

        //internal static IDependencyManager AddDependencyProviders<T>(Dictionary<T, ServerDefinition> providers, IDependencyManager existingDependencyManager = null)
        //    where T : IExportable
        //{
        //    IDependencyManager dependencyManager = existingDependencyManager ?? new Mock<IDependencyManager>();

        //    IEnumerable<ExportableDescriptor<T>> exportableDescriptors =
        //        providers.Select(x => new ExportableDescriptorImpl<T>(
        //            new ExtensionDescriptor<T, IExportableMetadata>(
        //                new Lazy<T, IExportableMetadata>(
        //                    () => x.Key,
        //                    new ExportableAttribute(x.Value.ServerType, x.Value.Category,
        //                        typeof (T), Guid.NewGuid().ToString())))));               

        //    dependencyManager.Setup(x => x.GetServiceDescriptors<T>()).Returns(exportableDescriptors);

        //    return dependencyManager;
        //}

        internal static ServiceResponse<ServerInstanceInfo> CreateServerInstanceResponse(int numberOfServers, ServerDefinition serverDefinition, Exception exception = null)
        {
            List<ServerInstanceInfo> servers = new List<ServerInstanceInfo>();
            for (int i = 0; i < numberOfServers; i++)
            {
                servers.Add(new ServerInstanceInfo(serverDefinition)
                {
                    Name = Guid.NewGuid().ToString(),
                    FullyQualifiedDomainName = Guid.NewGuid().ToString()
                });
            }
            ServiceResponse<ServerInstanceInfo> response;
            if (exception != null)
            {
                response = new ServiceResponse<ServerInstanceInfo>(servers, new List<Exception> { exception });
            }
            else
            {
                response = new ServiceResponse<ServerInstanceInfo>(servers);
            }

            return response;
        }

        internal static ServiceResponse<DatabaseInstanceInfo> CreateDatabaseInstanceResponse(int numberOfServers, ServerDefinition serverDefinition = null, 
            string serverName = "", Exception exception = null)
        {
            serverDefinition = serverDefinition ?? ServerDefinition.Default;
            List<DatabaseInstanceInfo> databases = new List<DatabaseInstanceInfo>();
            for (int i = 0; i < numberOfServers; i++)
            {
                databases.Add(new DatabaseInstanceInfo(serverDefinition, serverName, Guid.NewGuid().ToString()));
            }
            ServiceResponse<DatabaseInstanceInfo> response;
            if (exception != null)
            {
                response = new ServiceResponse<DatabaseInstanceInfo>(databases, new List<Exception> { exception });
            }
            else
            {
                response = new ServiceResponse<DatabaseInstanceInfo>(databases);
            }

            return response;
        }

        //internal static UIConnectionInfo CreateUiConnectionInfo(string baseDbName)
        //{
        //    SqlConnectionStringBuilder connectionStringBuilder = CreateConnectionStringBuilder(baseDbName);
        //    return CreateUiConnectionInfo(connectionStringBuilder);
        //}
        //internal static UIConnectionInfo CreateUiConnectionInfo(SqlConnectionStringBuilder connectionStringBuilder)
        //{
        //    UIConnectionInfo ci = UIConnectionInfoUtil.GetUIConnectionInfoFromConnectionString(connectionStringBuilder.ConnectionString, (new SqlServerType()));
        //    ci.PersistPassword = connectionStringBuilder.PersistSecurityInfo;
        //    return ci;
        //}


        //internal static SqlConnectionStringBuilder CreateConnectionStringBuilder(string baseDbName)
        //{
        //    return CreateConnectionStringBuilder(baseDbName, InstanceManager.DefaultSql2011);
        //}


        //internal static SqlConnectionStringBuilder CreateConnectionStringBuilder(string baseDbName, InstanceInfo dbInstance)
        //{
        //    string dbName = ConnectionDialogHelper.CreateTestDatabase(baseDbName, dbInstance);
        //    string dbConnectionString = dbInstance.BuildConnectionString(dbName);
        //    SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(dbConnectionString);
        //    connectionStringBuilder.ApplicationName = Guid.NewGuid().ToString();
        //    connectionStringBuilder.ConnectTimeout = 123;
        //    connectionStringBuilder.Encrypt = true;
        //    connectionStringBuilder.ApplicationIntent = ApplicationIntent.ReadWrite;
        //    connectionStringBuilder.AsynchronousProcessing = true;
        //    connectionStringBuilder.MaxPoolSize = 45;
        //    connectionStringBuilder.MinPoolSize = 3;
        //    connectionStringBuilder.PacketSize = 600;
        //    connectionStringBuilder.Pooling = true;
        //    connectionStringBuilder.TrustServerCertificate = false;
        //    return connectionStringBuilder;
        //}

        //internal static ConnectionInfo CreateConnectionInfo(string baseDbName, IEventsChannel eventsChannel = null)
        //{
        //    ConnectionInfo connectionInfo = new ConnectionInfo(eventsChannel);
        //    UIConnectionInfo uiConnectionInfo = CreateUiConnectionInfo(baseDbName);
        //    connectionInfo.UpdateConnectionInfo(uiConnectionInfo);
        //    return connectionInfo;
        //}
    }
}
