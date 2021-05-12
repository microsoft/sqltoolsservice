using System;
using System.Collections.Generic;
using System.Composition;
using Kusto.Data;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    [Export(typeof(IDataSourceFactory))]
    public class DataSourceFactory : IDataSourceFactory
    {
        public IDataSource Create(ConnectionDetails connectionDetails, string ownerUri)
        {
            var dataSourceType = GetDataSourceType(connectionDetails.ServerName);
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                {
                    var kustoConnectionDetails = MapKustoConnectionDetails(connectionDetails);
                    var kustoClient = new KustoClient(kustoConnectionDetails, ownerUri);
                    var intellisenseClient = new KustoIntellisenseClient(kustoClient);
                    return new KustoDataSource(kustoClient, intellisenseClient);
                }
                case DataSourceType.LogAnalytics:
                {
                    var httpClient = new MonitorClient(connectionDetails.ServerName, connectionDetails.AccountToken);
                    var intellisenseClient = new MonitorIntellisenseClient(httpClient);
                    return new MonitorDataSource(httpClient, intellisenseClient);
                }
                default:
                    
                    throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""",
                        nameof(dataSourceType));
            }
        }

        private DataSourceType GetDataSourceType(string server)
        {
            return Guid.TryParse(server, out _) 
                ? DataSourceType.LogAnalytics 
                : DataSourceType.Kusto;
        }

        private DataSourceConnectionDetails MapKustoConnectionDetails(ConnectionDetails connectionDetails)
        {
            if (connectionDetails.AuthenticationType == "dstsAuth" || connectionDetails.AuthenticationType == "AzureMFA")
            {
                ValidationUtils.IsTrue<ArgumentException>(!string.IsNullOrWhiteSpace(connectionDetails.AccountToken),
                    $"The Kusto User Token is not specified - set {nameof(connectionDetails.AccountToken)}");
            }

            return new DataSourceConnectionDetails
            {
                ServerName = connectionDetails.ServerName,
                DatabaseName = connectionDetails.DatabaseName,
                ConnectionString = connectionDetails.ConnectionString,
                AuthenticationType = connectionDetails.AuthenticationType,
                UserToken = connectionDetails.AccountToken,
                UserName = connectionDetails.UserName,
                Password = connectionDetails.Password
            };
        }

        public static KustoConnectionStringBuilder CreateConnectionStringBuilder(DataSourceType dataSourceType, string serverName, string databaseName)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                {
                    return new KustoConnectionStringBuilder(serverName, databaseName);
                }

                default:
                    throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""",
                        nameof(dataSourceType));
            }
        }

        public static KustoConnectionStringBuilder CreateConnectionStringBuilder(DataSourceType dataSourceType, string connectionString)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                {
                    return new KustoConnectionStringBuilder(connectionString);
                }

                default:
                    throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""",
                        nameof(dataSourceType));
            }
        }

        // Gets default keywords for intellisense when there is no connection.
        public static CompletionItem[] GetDefaultAutoComplete(DataSourceType dataSourceType, ScriptDocumentInfo scriptDocumentInfo, Position textDocumentPosition){
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return KustoIntellisenseHelper.GetDefaultKeywords(scriptDocumentInfo, textDocumentPosition);
                    }

                default:
                    throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""", nameof(dataSourceType));
            }
        }

        // Gets default keywords errors related to intellisense when there is no connection.
        public static ScriptFileMarker[] GetDefaultSemanticMarkers(DataSourceType dataSourceType, ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText){
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return KustoIntellisenseHelper.GetDefaultDiagnostics(parseInfo, scriptFile, queryText);
                    }

                default:
                    throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""", nameof(dataSourceType));
            }
        }

        public static ReliableConnectionHelper.ServerInfo ConvertToServerInfoFormat(DataSourceType dataSourceType, DiagnosticsInfo clusterDiagnostics)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return new ReliableConnectionHelper.ServerInfo
                        {
                            Options = new Dictionary<string, object>(clusterDiagnostics.Options)
                        };
                    }

                default:
                    throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""", nameof(dataSourceType));
            }
        }
    }
}