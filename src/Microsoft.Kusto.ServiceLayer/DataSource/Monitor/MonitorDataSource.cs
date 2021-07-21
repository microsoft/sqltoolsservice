using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor
{
    public class MonitorDataSource : DataSourceBase
    {
        private readonly MonitorClient _monitorClient;
        private readonly IntellisenseClientBase _intellisenseClient;
        private WorkspaceResponse _metadata;
        private Dictionary<string, List<DataSourceObjectMetadata>> _nodes;
        
        public override string ClusterName => _monitorClient.WorkspaceId;
        public override string DatabaseName { get; }

        public MonitorDataSource(MonitorClient monitorClient, IntellisenseClientBase intellisenseClient)
        {
            _monitorClient = monitorClient;
            _intellisenseClient = intellisenseClient;
            _nodes = new Dictionary<string, List<DataSourceObjectMetadata>>();
            _metadata = _monitorClient.LoadMetadata();
            DataSourceType = DataSourceType.LogAnalytics;
            SetupTableGroups(monitorClient.WorkspaceId);
        }
        
        private void SetupTableGroups(string workspaceId)
        {
            var workspace = _metadata.Workspaces.First(x => x.Id == workspaceId);
            var metadataTableGroups = _metadata.TableGroups.ToDictionary(x => x.Id);
            
            foreach (string workspaceTableGroup in workspace.TableGroups)
            {
                var tableGroup = metadataTableGroups[workspaceTableGroup];

                var tableGroupNodeInfo =
                    MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Folder, tableGroup.Name, $"{workspace.Id}.{tableGroup.Name}");

                _nodes.SafeAdd($"{workspace.Id}", tableGroupNodeInfo);
                
                SetupTables(tableGroupNodeInfo);
            }
        }
        
        private void SetupTables(DataSourceObjectMetadata tableGroupNodeInfo)
        {
            var tables = GetNonEmptyTableNames();
            var metadataTables = _metadata.Tables.ToDictionary(x => x.Name);
            
            foreach (string tableName in tables)
            {
                var table = metadataTables[tableName];

                var tableNodeInfo = MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Table, table.Name,
                    $"{tableGroupNodeInfo.Urn}.{table.Name}");

                _nodes.SafeAdd(tableGroupNodeInfo.Urn, tableNodeInfo);

                SetupColumns(table, tableNodeInfo);
            }
        }
        
        private IEnumerable<string> GetNonEmptyTableNames()
        {
            string query = "union * | summarize count() by Type";
            var results = _monitorClient.Query(query);
            return results.Tables[0].Rows.Select(x => x[0]).OrderBy(x => x);
        }
        
        private void SetupColumns(TablesModel table, DataSourceObjectMetadata tableNodeInfo)
        {
            foreach (var column in table.Columns)
            {
                var columnNodeInfo = MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Column, column.Name,
                    $"{tableNodeInfo.Urn}.{column.Name}");

                _nodes.SafeAdd(tableNodeInfo.Urn, columnNodeInfo);
            }
        }

        public override async Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            var results =  await _monitorClient.QueryAsync(query, cancellationToken);
            return results.ToDataReader();
        }

        public override Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata parentMetadata)
        {
            return new DiagnosticsInfo();
        }

        public override IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata, bool includeSizeDetails = false)
        {
            // columns are always leaf nodes
            if (parentMetadata.MetadataType == DataSourceMetadataType.Column)
            {
                return Enumerable.Empty<DataSourceObjectMetadata>();
            }

            if (parentMetadata.MetadataType == DataSourceMetadataType.Cluster && includeSizeDetails)
            {
                var child = _nodes[parentMetadata.Urn].FirstOrDefault();
                return child == null ? Enumerable.Empty<DataSourceObjectMetadata>() : _nodes[child.Urn];
            }
            
            return _nodes[parentMetadata.Urn].OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase);
        }

        public override void Refresh(bool includeDatabase)
        {
            // reset the data source
            _nodes = new Dictionary<string, List<DataSourceObjectMetadata>>();
            _metadata = _monitorClient.LoadMetadata();
            SetupTableGroups(_monitorClient.WorkspaceId);
        }

        public override void Refresh(DataSourceObjectMetadata objectMetadata)
        {
            Refresh(false);
        }

        public override void UpdateDatabase(string databaseName)
        {
            // Azure Monitor Logs doesn't use databases
        }
        
        private string ParseWorkspaceId(string workspace)
        {
            var regex = new Regex(@"(?<=\().+?(?=\))");
            
            return regex.IsMatch(workspace)
                ? regex.Match(workspace).Value
                : workspace;
        }

        public override Task<bool> Exists()
        {
            return Task.FromResult(true);
        }

        public override bool Exists(DataSourceObjectMetadata objectMetadata)
        {
            return true;
        }

        public override string GenerateAlterFunctionScript(string functionName)
        {
            throw new NotImplementedException();
        }

        public override string GenerateExecuteFunctionScript(string functionName)
        {
            throw new NotImplementedException();
        }

        public override ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText)
        {
            return _intellisenseClient.GetSemanticMarkers(parseInfo, scriptFile, queryText);
        }

        public override DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false)
        {
            return _intellisenseClient.GetDefinition(queryText, index, startLine, startColumn, throwOnError);
        }

        public override Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false)
        {
            return _intellisenseClient.GetHoverHelp(scriptDocumentInfo, textPosition, throwOnError);
        }

        public override CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false)
        {
            return _intellisenseClient.GetAutoCompleteSuggestions(scriptDocumentInfo, textPosition, throwOnError);
        }

        public override ListDatabasesResponse GetDatabases(string serverName, bool includeDetails)
        {
            return new ListDatabasesResponse
            {
                DatabaseNames = new[]
                {
                    ClusterName
                }
            };
        }

        public override DatabaseInfo GetDatabaseInfo(string serverName, string databaseName)
        {
            return new DatabaseInfo
            {
                Options = new Dictionary<string, object>
                {
                    {"id", ClusterName},
                    {"name", ClusterName}
                }
            };
        }
    }
}