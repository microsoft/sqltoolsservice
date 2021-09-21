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
        private Dictionary<string, SortedDictionary<string, DataSourceObjectMetadata>> _nodes;
        private const string DatabaseKeyPrefix = "OnlyTables";
        
        public override string ClusterName => _monitorClient.WorkspaceId;
        public override string DatabaseName { get; set; }

        public MonitorDataSource(MonitorClient monitorClient, IntellisenseClientBase intellisenseClient)
        {
            _monitorClient = monitorClient;
            _intellisenseClient = intellisenseClient;
            _nodes = new Dictionary<string, SortedDictionary<string, DataSourceObjectMetadata>>(StringComparer.OrdinalIgnoreCase);
            _metadata = _monitorClient.LoadMetadata();
            DataSourceType = DataSourceType.LogAnalytics;
            SetupTableGroups(monitorClient.WorkspaceId);
        }
        
        private void SetupTableGroups(string workspaceId)
        {
            var workspace = _metadata.Workspaces.First(x => x.Id == workspaceId);
            DatabaseName = $"{workspace.Name} ({workspace.Id})";
            
            var tableGroups = _metadata.TableGroups.Where(x => workspace.TableGroups.Contains(x.Id));

            foreach (TableGroupsModel workspaceTableGroup in tableGroups)
            {
                var name = workspaceTableGroup.DisplayName ?? workspaceTableGroup.Name;
                var tableGroupNodeInfo =
                    MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Folder, name, $"{workspace.Id}.{name}");

                _nodes.SafeAdd($"{workspace.Id}", tableGroupNodeInfo);

                SetupTables(tableGroupNodeInfo.Urn, workspaceTableGroup.Tables, workspace.Id);
            }

            // custom log tables are listed in tables
            if (workspace.Tables.Any())
            {
                var name = "_Custom Logs";
                var customLogsNodeInfo =
                    MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Folder, name, $"{workspace.Id}.{name}");

                _nodes.SafeAdd($"{workspace.Id}", customLogsNodeInfo);
                SetupTables(customLogsNodeInfo.Urn, workspace.Tables, workspace.Id);
            }
        }
        
        private void SetupTables(string urn, string[] tables, string workspaceId)
        {
            var tableGroupTables = _metadata.Tables.Where(x => tables.Contains(x.Id));
            
            foreach (TablesModel metadataTable in tableGroupTables)
            {
                var tableNodeInfo = MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Table, metadataTable.Name,
                    $"{urn}.{metadataTable.Name}");

                _nodes.SafeAdd(urn, tableNodeInfo);
                _nodes.SafeAdd($"{DatabaseKeyPrefix}.{workspaceId}", tableNodeInfo);

                SetupColumns(metadataTable, tableNodeInfo.Urn);
            }
        }

        private void SetupColumns(TablesModel table, string urn)
        {
            foreach (var column in table.Columns)
            {
                var columnNodeInfo = MetadataFactory.CreateDataSourceObjectMetadata(DataSourceMetadataType.Column, column.Name,
                    $"{urn}.{column.Name}");

                _nodes.SafeAdd(urn, columnNodeInfo);
            }
        }

        public override async Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            var results = await _monitorClient.QueryAsync(query, cancellationToken);
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
                string newKey = $"{DatabaseKeyPrefix}.{parentMetadata.Urn}";
                return _nodes[newKey].Values;
            }
            
            return _nodes[parentMetadata.Urn].Values;
        }

        public override void Refresh(bool includeDatabase)
        {
            // reset the data source
            _nodes = new Dictionary<string, SortedDictionary<string, DataSourceObjectMetadata>>(StringComparer.OrdinalIgnoreCase);
            _metadata = _monitorClient.LoadMetadata();
            SetupTableGroups(_monitorClient.WorkspaceId);
        }

        public override void Refresh(DataSourceObjectMetadata objectMetadata)
        {
            Refresh(false);
        }

        public override void UpdateDatabase(string databaseName)
        {
            // LogAnalytics is treating the workspace name as the database name
            var workspaceId = ParseWorkspaceId(databaseName);
            _metadata = _monitorClient.LoadMetadata(true);
            var workspace = _metadata.Workspaces.First(x => x.Id == workspaceId);
            DatabaseName = $"{workspace.Name} ({workspace.Id})";
            _intellisenseClient.UpdateDatabase(databaseName);
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
                    DatabaseName
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
                    {"name", DatabaseName}
                }
            };
        }
    }
}