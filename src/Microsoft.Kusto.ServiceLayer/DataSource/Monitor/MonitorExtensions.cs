using System;
using System.Data;
using System.Linq;
using Kusto.Language.Symbols;
using Microsoft.Azure.OperationalInsights.Models;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor
{
    public static class MonitorExtensions
    {
        /// <summary>
        /// Converts QueryResults object into an IDataReader
        /// </summary>
        /// <param name="queryResults"></param>
        /// <returns></returns>
        public static IDataReader ToDataReader(this QueryResults queryResults)
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
                    dataRow[i] = row[i] ?? DBNull.Value as object;
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
        private static Type MapType(string type)
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

        public static ScalarSymbol ToSymbolType(this string type)
        {
            switch (type)
            {
                case "bool": return ScalarTypes.Bool;
                case "datetime": return ScalarTypes.DateTime;
                case "dynamic": return ScalarTypes.Dynamic;
                case "guid": return ScalarTypes.Guid;
                case "int": return ScalarTypes.Int;
                case "long": return ScalarTypes.Long;
                case "real": return ScalarTypes.Real;
                case "string": return ScalarTypes.String;
                case "timespan": return ScalarTypes.TimeSpan;
                case "decimal": return ScalarTypes.Decimal;
                
                default: return ScalarTypes.String;
            }
        }
    }
}