using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    [Export(typeof(IDataSourceFactory))]
    public class DataSourceFactory : IDataSourceFactory
    {
        public IDataSource Create(DataSourceType dataSourceType, ConnectionDetails connectionDetails, string ownerUri)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                {
                    var kustoConnectionDetails = MapKustoConnectionDetails(connectionDetails);
                    var kustoClient = new KustoClient(kustoConnectionDetails, ownerUri);
                    return new KustoDataSource(kustoClient);
                }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"",
                        nameof(dataSourceType));
            }
        }
        
        private KustoConnectionDetails MapKustoConnectionDetails(ConnectionDetails connectionDetails)
        {
            return new KustoConnectionDetails
            {
                ServerName = connectionDetails.ServerName,
                DatabaseName = connectionDetails.DatabaseName,
                ConnectionString = connectionDetails.ConnectionString,
                AuthenticationType = connectionDetails.AuthenticationType,
                UserToken = connectionDetails.AccountToken
            };
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
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
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
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
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
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }
    }
}