using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Language;
using Kusto.Language.Symbols;
using Kusto.Language.Syntax;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Kusto
{
    public class KustoIntellisenseClient : IntellisenseClientBase
    {
        private readonly IKustoClient _kustoClient;

        public KustoIntellisenseClient(IKustoClient kustoClient)
        {
            _kustoClient = kustoClient;
            schemaState = LoadSchemaState(kustoClient.DatabaseName, kustoClient.ClusterName);
        }
        
        public override void UpdateDatabase(string databaseName)
        {
            schemaState = LoadSchemaState(databaseName, _kustoClient.ClusterName);
        }
        
        private GlobalState LoadSchemaState(string databaseName, string clusterName)
        {
            IEnumerable<ShowDatabaseSchemaResult> tableSchemas = Enumerable.Empty<ShowDatabaseSchemaResult>();
            IEnumerable<ShowFunctionsResult> functionSchemas = Enumerable.Empty<ShowFunctionsResult>();

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                var source = new CancellationTokenSource();
                Parallel.Invoke(() =>
                    {
                        var tableQuery = $".show database {KustoQueryUtils.EscapeName(databaseName)} schema";
                        tableSchemas = _kustoClient.ExecuteQueryAsync<ShowDatabaseSchemaResult>(tableQuery, source.Token, databaseName).Result;
                    },
                    () =>
                    {
                        functionSchemas = _kustoClient.ExecuteQueryAsync<ShowFunctionsResult>(".show functions", source.Token, databaseName).Result;
                    });
            }

            return AddOrUpdateDatabase(tableSchemas, functionSchemas, GlobalState.Default, databaseName,
                clusterName);
        }
        
        /// <summary>
        /// Loads the schema for the specified database and returns a new <see cref="GlobalState"/> with the database added or updated.
        /// </summary>
        private GlobalState AddOrUpdateDatabase(IEnumerable<ShowDatabaseSchemaResult> tableSchemas,
            IEnumerable<ShowFunctionsResult> functionSchemas, GlobalState globals,
            string databaseName, string clusterName)
        {
            // try and show error from here.
            DatabaseSymbol databaseSymbol = null;

            if (databaseName != null)
            {
                databaseSymbol = LoadDatabase(tableSchemas, functionSchemas, databaseName);
            }

            if (databaseSymbol == null)
            {
                return globals;
            }

            var cluster = globals.GetCluster(clusterName);
            if (cluster == null)
            {
                cluster = new ClusterSymbol(clusterName, new[] {databaseSymbol}, isOpen: true);
                globals = globals.AddOrUpdateCluster(cluster);
            }
            else
            {
                cluster = cluster.AddOrUpdateDatabase(databaseSymbol);
                globals = globals.AddOrUpdateCluster(cluster);
            }

            return globals.WithCluster(cluster).WithDatabase(databaseSymbol);
        }
        
        /// <summary>
        /// Loads the schema for the specified database into a <see cref="DatabaseSymbol"/>.
        /// </summary>
        private DatabaseSymbol LoadDatabase(IEnumerable<ShowDatabaseSchemaResult> tableSchemas,
            IEnumerable<ShowFunctionsResult> functionSchemas,
            string databaseName)
        {
            if (tableSchemas == null)
            {
                return null;
            }

            tableSchemas = tableSchemas
                .Where(r => !string.IsNullOrEmpty(r.TableName) && !string.IsNullOrEmpty(r.ColumnName))
                .ToArray();

            var members = new List<Symbol>();
            foreach (var table in tableSchemas.GroupBy(s => s.TableName))
            {
                var columns = table.Select(s => new ColumnSymbol(s.ColumnName, GetKustoType(s.ColumnType))).ToList();
                var tableSymbol = new TableSymbol(table.Key, columns);
                members.Add(tableSymbol);
            }

            if (functionSchemas == null)
            {
                return null;
            }

            foreach (var fun in functionSchemas)
            {
                var parameters = TranslateParameters(fun.Parameters);
                var functionSymbol = new FunctionSymbol(fun.Name, fun.Body, parameters);
                members.Add(functionSymbol);
            }

            return new DatabaseSymbol(databaseName, members);
        }
        
        /// <summary>
        /// Convert CLR type name into a Kusto scalar type.
        /// </summary>
        private ScalarSymbol GetKustoType(string clrTypeName)
        {
            switch (clrTypeName)
            {
                case "System.Byte":
                case "Byte":
                case "byte":
                case "System.SByte":
                case "SByte":
                case "sbyte":
                case "System.Int16":
                case "Int16":
                case "short":
                case "System.UInt16":
                case "UInt16":
                case "ushort":
                case "System.Int32":
                case "System.Single":
                case "Int32":
                case "int":
                    return ScalarTypes.Int;
                case "System.UInt32": // unsigned ints don't fit into int, use long
                case "UInt32":
                case "uint":
                case "System.Int64":
                case "Int64":
                case "long":
                    return ScalarTypes.Long;
                case "System.Double":
                case "Double":
                case "double":
                case "float":
                    return ScalarTypes.Real;
                case "System.UInt64": // unsigned longs do not fit into long, use decimal
                case "UInt64":
                case "ulong":
                case "System.Decimal":
                case "Decimal":
                case "decimal":
                case "System.Data.SqlTypes.SqlDecimal":
                case "SqlDecimal":
                    return ScalarTypes.Decimal;
                case "System.Guid":
                case "Guid":
                    return ScalarTypes.Guid;
                case "System.DateTime":
                case "DateTime":
                    return ScalarTypes.DateTime;
                case "System.TimeSpan":
                case "TimeSpan":
                    return ScalarTypes.TimeSpan;
                case "System.String":
                case "String":
                case "string":
                    return ScalarTypes.String;
                case "System.Boolean":
                case "Boolean":
                case "bool":
                    return ScalarTypes.Bool;
                case "System.Object":
                case "Object":
                case "object":
                    return ScalarTypes.Dynamic;
                case "System.Type":
                case "Type":
                    return ScalarTypes.Type;
                default:
                    throw new InvalidOperationException($"Unhandled clr type: {clrTypeName}");
            }
        }
        
        /// <summary>
        /// Translate Kusto parameter list declaration into into list of <see cref="Parameter"/> instances.
        /// </summary>
        private IReadOnlyList<Parameter> TranslateParameters(string parameters)
        {
            parameters = parameters.Trim();

            if (string.IsNullOrEmpty(parameters) || parameters == "()")
            {
                return new Parameter[0];
            }

            if (parameters[0] != '(')
            {
                parameters = "(" + parameters;
            }

            if (parameters[parameters.Length - 1] != ')')
            {
                parameters = parameters + ")";
            }

            var query = "let fn = " + parameters + " { };";
            var code = KustoCode.ParseAndAnalyze(query);
            var let = code.Syntax.GetFirstDescendant<LetStatement>();
            
            FunctionSymbol function = let.Name.ReferencedSymbol is VariableSymbol variable
                ? variable.Type as FunctionSymbol
                : let.Name.ReferencedSymbol as FunctionSymbol;

            return function.Signatures[0].Parameters;
        }
    }
}