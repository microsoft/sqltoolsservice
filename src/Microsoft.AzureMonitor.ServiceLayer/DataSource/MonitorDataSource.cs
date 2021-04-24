using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.OperationalInsights.Models;
using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client;
using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses;
using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses.Models;
using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource
{
    public class MonitorDataSource
    {
        private readonly MonitorClient _monitorClient;
        private readonly WorkspaceResponse _metadata;        
        public string ServerName => _monitorClient.WorkspaceId;
        public string DatabaseName { get; private set; }
        public string UserName { get; }
        private readonly Dictionary<string, List<NodeInfo>> _nodes;

        public MonitorDataSource(MonitorClient monitorClient, string userName)
        {
            _monitorClient = monitorClient;
            UserName = userName;
            _nodes = new Dictionary<string, List<NodeInfo>>();
            _metadata = _monitorClient.LoadMetadata();
            SetupTableGroups(monitorClient.WorkspaceId);
        }
        
        private void SetupTableGroups(string workspaceId)
        {
            var workspace = _metadata.Workspaces.First(x => x.Id == workspaceId);
            DatabaseName = $"{workspace.Name} ({workspace.Id})";
            var metadataTableGroups = _metadata.TableGroups.ToDictionary(x => x.Id);
            
            foreach (string workspaceTableGroup in workspace.TableGroups)
            {
                var tableGroup = metadataTableGroups[workspaceTableGroup];

                var tableGroupNodeInfo = new NodeInfo
                {
                    NodePath = $"/{tableGroup.Name}",
                    NodeType = NodeTypes.Folder.ToString(),
                    IsLeaf = false,
                    Label = tableGroup.Name,
                    Metadata = new ObjectMetadata
                    {
                        MetadataTypeName = NodeTypes.Folder.ToString(),
                        Name = tableGroup.Name,
                    }
                };

                _nodes.AddToValueList("/", tableGroupNodeInfo);
                
                SetupTables(tableGroupNodeInfo);
            }
        }
        
        private void SetupTables(NodeInfo tableGroupNodeInfo)
        {
            var tables = GetNonEmptyTableNames();
            var metadataTables = _metadata.Tables.ToDictionary(x => x.Name);
            
            foreach (string tableName in tables)
            {
                var table = metadataTables[tableName];

                var tableNodeInfo = new NodeInfo
                {
                    NodePath = $"{tableGroupNodeInfo.NodePath}/{table.Name}",
                    NodeType = NodeTypes.Table.ToString(),
                    IsLeaf = false,
                    Label = table.Name,
                    Metadata = new ObjectMetadata
                    {
                        MetadataTypeName = NodeTypes.Table.ToString(),
                        Name = table.Name,
                    }
                };

                _nodes.AddToValueList(tableGroupNodeInfo.NodePath, tableNodeInfo);

                SetupColumns(table, tableNodeInfo);
            }
        }
        
        private void SetupColumns(TablesModel table, NodeInfo tableNodeInfo)
        {
            foreach (var column in table.Columns)
            {
                var columnNodeInfo = new NodeInfo
                {
                    NodePath = $"{tableNodeInfo.NodePath}/{column.Name}",
                    NodeType = NodeTypes.Column.ToString(),
                    IsLeaf = true,
                    Label = column.Name,
                    Metadata = new ObjectMetadata
                    {
                        MetadataTypeName = NodeTypes.Column.ToString(),
                        Name = column.Name
                    }
                };

                _nodes.AddToValueList(tableNodeInfo.NodePath, columnNodeInfo);
            }
        }

        public List<ObjectMetadata> GetDatabases(bool includeSizeDetails)
        {
            return _metadata.Workspaces
                .Select(x => new ObjectMetadata
            {
                MetadataTypeName = NodeTypes.Database.ToString(),
                Name = x.Name,
                PrettyName = x.Name,
                SizeInMb = 0

            }).ToList();
        }

        private IEnumerable<string> GetNonEmptyTableNames()
        {
            string query = "union * | summarize count() by Type";
            var results = _monitorClient.Query(query);
            return results.Tables[0].Rows.Select(x => x[0]).OrderBy(x => x);
        }

        public void ChangeWorkspace(string newWorkspaceId)
        {
            var workspaceId = ParseWorkspaceId(newWorkspaceId);
            var workspace = _metadata.Workspaces.First(x => x.Id == workspaceId);
            DatabaseName = $"{workspace.Name} ({workspace.Id})";
        }
        
        private string ParseWorkspaceId(string workspace)
        {
            var regex = new Regex(@"(?<=\().+?(?=\))");
            
            return regex.IsMatch(workspace)
                ? regex.Match(workspace).Value
                : workspace;
        }

        public IEnumerable<NodeInfo> Expand(string nodePath)
        {
            return _nodes[nodePath].OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IDataReader> QueryAsync(string query, CancellationToken cancellationToken)
        {
            var results = await _monitorClient.QueryAsync(query, cancellationToken);
            var dataReader = MapToDataReader(results);

            return await Task.FromResult(dataReader);
        }

        private IDataReader MapToDataReader(QueryResults queryResults)
        {
            var resultTable = queryResults.Tables.FirstOrDefault();

            if (resultTable == null)
            {
                return new DataTableReader(new DataTable());
            }
            
            var dataTable = new DataTable(resultTable.Name);
            
            foreach (var column in resultTable.Columns)
            {
                dataTable.Columns.Add(column.Name, MapType(column.Type));
            }

            foreach (var row in resultTable.Rows)
            {
                var dataRow = dataTable.NewRow();

                for (int i = 0; i < row.Count; i++)
                {
                    dataRow[i] = row[i];
                }
                
                dataTable.Rows.Add(dataRow);
            }
            
            return new DataTableReader(dataTable);
        }

        /// <summary>
        /// Map Kusto type to .NET Type equivalent using scalar data types
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/scalar-data-types/">Here</seealso>
        /// <param name="type">Kusto Type</param>
        /// <returns>.NET Equivalent Type</returns>
        private Type MapType(string type)
        {
            switch (type)
            {
                case "bool": return Type.GetType("System.Boolean");
                case "datetime": return Type.GetType("System.DateTime");
                case "dynamic": return Type.GetType("System.Object");
                case "guid": return Type.GetType("System.Guid");
                case "int": return Type.GetType("System.Int32");
                case "long": return Type.GetType("System.Int64");
                case "real": return Type.GetType("System.Double");
                case "string": return Type.GetType("System.String");
                case "timespan": return Type.GetType("System.TimeSpan");
                case "decimal": return Type.GetType("System.Data.SqlTypes.SqlDecimal");
                
                default: return typeof(string);
            }
        }
    }
}