//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    [CollectionDefinition("IntelliSense large synthetic benchmarks", DisableParallelization = true)]
    public sealed class IntellisenseLargeSyntheticCollection
    {
        public const string Name = "IntelliSense large synthetic benchmarks";
    }

    [Collection(IntellisenseLargeSyntheticCollection.Name)]
    public class IntellisenseLargeSyntheticTests
    {
        private const int DefaultSchemaCount = 24;
        private const int DefaultTablesPerSchema = 80;
        private const int DefaultColumnsPerTable = 24;
        private const int DefaultViewsPerSchema = 12;
        private const int DefaultProceduresPerSchema = 12;
        private const int DefaultFunctionsPerSchema = 6;
        private const int DefaultSynonymsPerSchema = 12;
        private const int IntelliSenseReadyTimeout = 550000;
        private const int ConnectionTimeout = 60000;

        private static readonly SemaphoreSlim SyntheticDatabaseLock = new SemaphoreSlim(1, 1);
        private static bool syntheticDatabaseCreated;

        private static string SyntheticDatabaseName =>
            Environment.GetEnvironmentVariable("SQLTOOLS_INTELLISENSE_BENCHMARK_DATABASE_NAME") ??
            Common.LargeIntelliSensePerfTestDatabaseName;

        [Fact]
        public async Task LargeSyntheticColdReadySmoMetadataProvider()
        {
            await RunColdReadyBenchmark(enableCatalogMetadataProvider: false);
        }

        [Fact]
        public async Task LargeSyntheticColdReadyCatalogMetadataProvider()
        {
            await RunColdReadyBenchmark(enableCatalogMetadataProvider: true);
        }

        [Fact]
        public async Task LargeSyntheticCrossSchemaCompletionSmoMetadataProvider()
        {
            await RunCompletionBenchmark(
                enableCatalogMetadataProvider: false,
                query: "SELECT * FROM [s010].",
                expectedLabels: new[] { "t0000" });
        }

        [Fact]
        public async Task LargeSyntheticCrossSchemaCompletionCatalogMetadataProvider()
        {
            await RunCompletionBenchmark(
                enableCatalogMetadataProvider: true,
                query: "SELECT * FROM [s010].",
                expectedLabels: new[] { "t0000" });
        }

        [Fact]
        public async Task LargeSyntheticThreePartColumnCompletionSmoMetadataProvider()
        {
            await RunCompletionBenchmark(
                enableCatalogMetadataProvider: false,
                query: $"SELECT * FROM [{SyntheticDatabaseName}].[s001].[t0001] AS target WHERE target.",
                expectedLabels: new[] { "c0001" });
        }

        [Fact]
        public async Task LargeSyntheticThreePartColumnCompletionCatalogMetadataProvider()
        {
            await RunCompletionBenchmark(
                enableCatalogMetadataProvider: true,
                query: $"SELECT * FROM [{SyntheticDatabaseName}].[s001].[t0001] AS target WHERE target.",
                expectedLabels: new[] { "c0001" });
        }

        private static async Task RunColdReadyBenchmark(
            bool enableCatalogMetadataProvider,
            [CallerMemberName] string benchmarkName = "")
        {
            await EnsureSyntheticLargeDatabase();

            await RunBenchmarkIterations(benchmarkName, async (timer) =>
            {
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    await SetCatalogMetadataProvider(testService, enableCatalogMetadataProvider);

                    timer.Start();
                    await testService.ConnectForQuery(
                        TestServerType.OnPrem,
                        "SELECT * FROM ",
                        queryTempFile.FilePath,
                        SyntheticDatabaseName,
                        timeout: ConnectionTimeout);
                    await WaitForIntelliSenseReady(testService, queryTempFile.FilePath);
                    timer.End();

                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

        private static async Task RunCompletionBenchmark(
            bool enableCatalogMetadataProvider,
            string query,
            string[] expectedLabels,
            [CallerMemberName] string benchmarkName = "")
        {
            await EnsureSyntheticLargeDatabase();

            await RunBenchmarkIterations(benchmarkName, async (timer) =>
            {
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    await SetCatalogMetadataProvider(testService, enableCatalogMetadataProvider);
                    await testService.ConnectForQuery(
                        TestServerType.OnPrem,
                        query,
                        queryTempFile.FilePath,
                        SyntheticDatabaseName,
                        timeout: ConnectionTimeout);
                    await WaitForIntelliSenseReady(testService, queryTempFile.FilePath);

                    CompletionItem[] completions = await testService.CalculateRunTime(
                        () => testService.RequestCompletion(queryTempFile.FilePath, query, 0, query.Length + 1),
                        timer);

                    Assert.True(
                        expectedLabels.Any(expected => ContainsCompletionLabel(completions, expected)),
                        $"Expected at least one completion label in [{string.Join(", ", expectedLabels)}]. Actual: [{string.Join(", ", completions?.Select(c => c.Label) ?? Array.Empty<string>())}]");

                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

        private static async Task RunBenchmarkIterations(string benchmarkName, Func<TestTimer, Task> benchmark)
        {
            TestTimer timer = new TestTimer { PrintResult = true };
            for (int iteration = 0; iteration < TestRunner.Instance.NumberOfRuns; iteration++)
            {
                Console.WriteLine($"{benchmarkName} iteration {iteration}");
                await benchmark(timer);
                Thread.Sleep(5000);
            }
            timer.Print(benchmarkName);
        }

        private static async Task SetCatalogMetadataProvider(
            TestServiceDriverProvider testService,
            bool enableCatalogMetadataProvider)
        {
            var settings = new SqlToolsSettings();
            settings.SqlTools.IntelliSense.EnableCatalogMetadataProvider = enableCatalogMetadataProvider;
            await testService.RequestChangeConfigurationNotification(
                new DidChangeConfigurationParams<SqlToolsSettings>
                {
                    Settings = settings
                });
        }

        private static async Task WaitForIntelliSenseReady(
            TestServiceDriverProvider testService,
            string ownerUri)
        {
            DateTime timeout = DateTime.UtcNow.AddMilliseconds(IntelliSenseReadyTimeout);
            while (DateTime.UtcNow < timeout)
            {
                IntelliSenseReadyParams readyParams =
                    await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 100000);
                if (string.Equals(readyParams?.OwnerUri, ownerUri, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            throw new TimeoutException($"Timed out waiting for IntelliSenseReady for {ownerUri}.");
        }

        private static bool ContainsCompletionLabel(CompletionItem[] completions, string expectedLabel)
        {
            if (completions == null)
            {
                return false;
            }

            return completions.Any(item =>
                string.Equals(item.Label, expectedLabel, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Label, $"[{expectedLabel}]", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Label, $"\"{expectedLabel}\"", StringComparison.OrdinalIgnoreCase));
        }

        private static async Task EnsureSyntheticLargeDatabase()
        {
            if (syntheticDatabaseCreated)
            {
                return;
            }

            await SyntheticDatabaseLock.WaitAsync();
            try
            {
                if (syntheticDatabaseCreated)
                {
                    return;
                }

                int schemaCount = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_SCHEMAS",
                    DefaultSchemaCount,
                    minimumValue: 11);
                int tablesPerSchema = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_TABLES_PER_SCHEMA",
                    DefaultTablesPerSchema,
                    minimumValue: 2);
                int columnsPerTable = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_COLUMNS_PER_TABLE",
                    DefaultColumnsPerTable,
                    minimumValue: 1);
                int viewsPerSchema = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_VIEWS_PER_SCHEMA",
                    DefaultViewsPerSchema,
                    minimumValue: 0);
                int proceduresPerSchema = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_PROCS_PER_SCHEMA",
                    DefaultProceduresPerSchema,
                    minimumValue: 0);
                int functionsPerSchema = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_FUNCTIONS_PER_SCHEMA",
                    DefaultFunctionsPerSchema,
                    minimumValue: 0);
                int synonymsPerSchema = GetIntEnvironmentVariable(
                    "SQLTOOLS_INTELLISENSE_BENCHMARK_SYNONYMS_PER_SCHEMA",
                    DefaultSynonymsPerSchema,
                    minimumValue: 0);

                Console.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Preparing synthetic IntelliSense benchmark database '{0}' with schemas={1}, tables/schema={2}, columns/table={3}, views/schema={4}, procs/schema={5}, functions/schema={6}, synonyms/schema={7}.",
                        SyntheticDatabaseName,
                        schemaCount,
                        tablesPerSchema,
                        columnsPerTable,
                        viewsPerSchema,
                        proceduresPerSchema,
                        functionsPerSchema,
                        synonymsPerSchema));

                SqlTestDb.CreateNew(
                    TestServerType.OnPrem,
                    doNotCleanupDb: true,
                    databaseName: SyntheticDatabaseName,
                    query: BuildSyntheticLargeDatabaseQuery(
                        schemaCount,
                        tablesPerSchema,
                        columnsPerTable,
                        viewsPerSchema,
                        proceduresPerSchema,
                        functionsPerSchema,
                        synonymsPerSchema));

                syntheticDatabaseCreated = true;
            }
            finally
            {
                SyntheticDatabaseLock.Release();
            }
        }

        private static int GetIntEnvironmentVariable(string name, int defaultValue, int minimumValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) && parsed >= minimumValue
                ? parsed
                : defaultValue;
        }

        private static string BuildSyntheticLargeDatabaseQuery(
            int schemaCount,
            int tablesPerSchema,
            int columnsPerTable,
            int viewsPerSchema,
            int proceduresPerSchema,
            int functionsPerSchema,
            int synonymsPerSchema)
        {
            var script = new StringBuilder();
            script.AppendLine("SET NOCOUNT ON;");

            for (int schemaIndex = 0; schemaIndex < schemaCount; schemaIndex++)
            {
                string schemaName = FormatName("s", schemaIndex, 3);
                script.AppendLine($"IF SCHEMA_ID(N'{schemaName}') IS NULL EXEC(N'CREATE SCHEMA [{schemaName}]');");

                for (int tableIndex = 0; tableIndex < tablesPerSchema; tableIndex++)
                {
                    string tableName = FormatName("t", tableIndex, 4);
                    string tableFullName = $"[{schemaName}].[{tableName}]";
                    script.Append("IF OBJECT_ID(N'");
                    script.Append(tableFullName);
                    script.AppendLine("', N'U') IS NULL");
                    script.Append("EXEC(N'");
                    script.Append(EscapeSqlLiteral(BuildCreateTableStatement(schemaName, tableName, columnsPerTable)));
                    script.AppendLine("');");
                }

                for (int viewIndex = 0; viewIndex < viewsPerSchema; viewIndex++)
                {
                    int tableIndex = viewIndex % tablesPerSchema;
                    string viewName = FormatName("v", viewIndex, 4);
                    string tableName = FormatName("t", tableIndex, 4);
                    string createView = $"CREATE VIEW [{schemaName}].[{viewName}] AS SELECT [Id], [c0001] FROM [{schemaName}].[{tableName}]";
                    AppendCreateObjectIfMissing(script, schemaName, viewName, "V", createView);
                }

                for (int procIndex = 0; procIndex < proceduresPerSchema; procIndex++)
                {
                    int tableIndex = procIndex % tablesPerSchema;
                    string procedureName = FormatName("p", procIndex, 4);
                    string tableName = FormatName("t", tableIndex, 4);
                    string createProcedure = $"CREATE PROCEDURE [{schemaName}].[{procedureName}] @id int, @name nvarchar(64) = NULL AS BEGIN SET NOCOUNT ON; SELECT [Id], [c0001] FROM [{schemaName}].[{tableName}] WHERE [Id] = @id; END";
                    AppendCreateObjectIfMissing(script, schemaName, procedureName, "P", createProcedure);
                }

                for (int functionIndex = 0; functionIndex < functionsPerSchema; functionIndex++)
                {
                    int tableIndex = functionIndex % tablesPerSchema;
                    string tableName = FormatName("t", tableIndex, 4);
                    string scalarFunctionName = FormatName("fn_scalar_", functionIndex, 4);
                    string tableFunctionName = FormatName("fn_table_", functionIndex, 4);
                    string createScalarFunction = $"CREATE FUNCTION [{schemaName}].[{scalarFunctionName}](@id int) RETURNS int AS BEGIN RETURN @id; END";
                    string createTableFunction = $"CREATE FUNCTION [{schemaName}].[{tableFunctionName}](@id int) RETURNS TABLE AS RETURN SELECT [Id], [c0001] FROM [{schemaName}].[{tableName}] WHERE [Id] = @id";

                    AppendCreateObjectIfMissing(script, schemaName, scalarFunctionName, "FN", createScalarFunction);
                    AppendCreateObjectIfMissing(script, schemaName, tableFunctionName, "IF", createTableFunction);
                }

                for (int synonymIndex = 0; synonymIndex < synonymsPerSchema; synonymIndex++)
                {
                    int tableIndex = synonymIndex % tablesPerSchema;
                    string synonymName = FormatName("syn_t", synonymIndex, 4);
                    string tableName = FormatName("t", tableIndex, 4);
                    string createSynonym = $"CREATE SYNONYM [{schemaName}].[{synonymName}] FOR [{schemaName}].[{tableName}]";
                    AppendCreateObjectIfMissing(script, schemaName, synonymName, "SN", createSynonym);
                }
            }

            return script.ToString();
        }

        private static string BuildCreateTableStatement(string schemaName, string tableName, int columnsPerTable)
        {
            var createTable = new StringBuilder();
            createTable.Append($"CREATE TABLE [{schemaName}].[{tableName}] ([Id] int NOT NULL");
            for (int columnIndex = 1; columnIndex <= columnsPerTable; columnIndex++)
            {
                createTable.Append(", ");
                createTable.Append(FormatColumnDefinition(columnIndex));
            }
            createTable.Append($", CONSTRAINT [PK_{schemaName}_{tableName}] PRIMARY KEY ([Id]))");
            return createTable.ToString();
        }

        private static string FormatColumnDefinition(int columnIndex)
        {
            string columnName = FormatName("c", columnIndex, 4);
            switch (columnIndex % 8)
            {
                case 0:
                    return $"[{columnName}] datetime2 NULL";
                case 1:
                    return $"[{columnName}] int NULL";
                case 2:
                    return $"[{columnName}] nvarchar(128) NULL";
                case 3:
                    return $"[{columnName}] decimal(18, 4) NULL";
                case 4:
                    return $"[{columnName}] bit NULL";
                case 5:
                    return $"[{columnName}] uniqueidentifier NULL";
                case 6:
                    return $"[{columnName}] varbinary(64) NULL";
                default:
                    return $"[{columnName}] bigint NULL";
            }
        }

        private static void AppendCreateObjectIfMissing(
            StringBuilder script,
            string schemaName,
            string objectName,
            string objectType,
            string createStatement)
        {
            script.Append("IF OBJECT_ID(N'");
            script.Append($"[{schemaName}].[{objectName}]");
            script.Append("', N'");
            script.Append(objectType);
            script.AppendLine("') IS NULL");
            script.Append("EXEC(N'");
            script.Append(EscapeSqlLiteral(createStatement));
            script.AppendLine("');");
        }

        private static string FormatName(string prefix, int index, int digits)
        {
            return prefix + index.ToString("D" + digits, CultureInfo.InvariantCulture);
        }

        private static string EscapeSqlLiteral(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
