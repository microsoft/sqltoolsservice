using System.Collections.Generic;
using System.Linq;
using Kusto.Language;
using Kusto.Language.Symbols;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor
{
    public class MonitorIntellisenseClient : IntellisenseClient
    {
        private readonly MonitorClient _monitorClient;

        public MonitorIntellisenseClient(MonitorClient monitorClient)
        {
            _monitorClient = monitorClient;
            schemaState = LoadSchemaState(monitorClient.LoadMetadata());
        }

        private GlobalState LoadSchemaState(WorkspaceResponse metadata)
        {
            var globalState = GlobalState.Default;

            var members = new List<Symbol>();
            foreach (var table in metadata.Tables)
            {
                var columnSymbols = table.Columns.Select(x => new ColumnSymbol(x.Name, x.Type.ToSymbolType()));

                var tableSymbol = new TableSymbol(table.Name, columnSymbols);
                members.Add(tableSymbol);
            }
            
            var databaseSymbol = new DatabaseSymbol(metadata.Workspaces.First().Id, members);
            return globalState.WithDatabase(databaseSymbol);
        }

        public override void UpdateDatabase(string databaseName)
        {
            var workspace = _monitorClient.LoadMetadata();
            schemaState = LoadSchemaState(workspace);
        }
    }
}