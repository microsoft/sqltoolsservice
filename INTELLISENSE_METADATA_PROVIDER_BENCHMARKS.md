# IntelliSense Metadata Provider Benchmarks

This document defines the benchmark protocol for comparing the existing SMO metadata provider with the catalog-backed provider.

## Toggle

The provider is controlled by `mssql.intelliSense.enableCatalogMetadataProvider`:

```json
{
  "mssql": {
    "intelliSense": {
      "enableCatalogMetadataProvider": true
    }
  }
}
```

Values:

- `true`: force the catalog provider.
- `false`: force the SMO provider.
- omitted/null: preserve default behavior; `SQLTOOLS_ENABLE_CATALOG_METADATA_PROVIDER=true` or `1` can still enable the catalog provider.

Changing the setting rebuilds IntelliSense for currently connected SQL files.

## Hypothesis

The catalog provider should reduce cold metadata build time and cache refresh time on databases with many objects because it:

- avoids SMO server/database object graph construction;
- loads database names separately from database object metadata;
- loads per-database schema/object snapshots only when referenced;
- loads columns and parameters only for requested objects;
- reuses provider-level cached collections for repeated binder traversal.

## Benchmark Database

The default synthetic benchmark database is `keep_SQLToolsLargeIntelliSensePerfTestDb`.

Default shape:

| Dimension | Default |
| --- | ---: |
| Schemas | 24 |
| Tables per schema | 80 |
| Columns per table | 24 |
| Views per schema | 12 |
| Stored procedures per schema | 12 |
| Scalar functions per schema | 6 |
| Inline table-valued functions per schema | 6 |
| Synonyms per schema | 12 |

Approximate object count: 2,928 schema-owned objects and 46,080 table columns before system objects.

The shape can be changed with environment variables:

- `SQLTOOLS_INTELLISENSE_BENCHMARK_DATABASE_NAME`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_SCHEMAS`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_TABLES_PER_SCHEMA`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_COLUMNS_PER_TABLE`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_VIEWS_PER_SCHEMA`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_PROCS_PER_SCHEMA`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_FUNCTIONS_PER_SCHEMA`
- `SQLTOOLS_INTELLISENSE_BENCHMARK_SYNONYMS_PER_SCHEMA`

Recommended profiles:

| Profile | Database name | Schemas | Tables/schema | Columns/table | Use |
| --- | --- | ---: | ---: | ---: | --- |
| Default large | `keep_SQLToolsLargeIntelliSensePerfTestDb` | 24 | 80 | 24 | Main gate |
| Very large | `keep_SQLToolsVeryLargeIntelliSensePerfTestDb` | 40 | 125 | 32 | Stress gate |
| Wide objects | `keep_SQLToolsWideIntelliSensePerfTestDb` | 16 | 50 | 96 | Column-load stress |

## Scenarios

| Scenario | Test method | Measures |
| --- | --- | --- |
| Cold ready, SMO | `LargeSyntheticColdReadySmoMetadataProvider` | connect through `textDocument/intelliSenseReady` |
| Cold ready, catalog | `LargeSyntheticColdReadyCatalogMetadataProvider` | connect through `textDocument/intelliSenseReady` |
| Cross-schema completion, SMO | `LargeSyntheticCrossSchemaCompletionSmoMetadataProvider` | completion latency after cache ready for `SELECT * FROM [s010].` |
| Cross-schema completion, catalog | `LargeSyntheticCrossSchemaCompletionCatalogMetadataProvider` | same |
| Three-part column completion, SMO | `LargeSyntheticThreePartColumnCompletionSmoMetadataProvider` | completion latency for `[db].[schema].[table] AS target WHERE target.` |
| Three-part column completion, catalog | `LargeSyntheticThreePartColumnCompletionCatalogMetadataProvider` | same |

## Controls

- Run SMO and catalog tests against the same already-created database.
- Use the same SQL Server instance, compatibility level, credentials, client machine, and service build.
- Use at least 10 iterations for each test; report p50, p90, average, and all raw iterations.
- Discard the first full test pass if SQL Server buffer/cache warmup is being studied separately.
- Do not run Object Explorer or unrelated workload against the same SQL Server during measurement.
- Record SQL Server version, database compatibility level, CPU, memory, storage type, and network locality.

## Commands

Build the service and perf tests, then run with `dotnet test`. Set `SQLTOOLSSERVICE_EXE` to the service executable under test and `ResultFolder` to a clean output directory.

```bash
export SQLTOOLSSERVICE_EXE=/path/to/MicrosoftSqlToolsServiceLayer
export ResultFolder=/tmp/sqltools-intellisense-bench
export NumberOfRuns=10

dotnet test test/Microsoft.SqlTools.ServiceLayer.PerfTests/Microsoft.SqlTools.ServiceLayer.PerfTests.csproj \
  --filter FullyQualifiedName~IntellisenseLargeSyntheticTests
```

For the most isolated measurements, run each scenario with an exact filter so each scenario gets a fresh testhost:

```bash
dotnet test test/Microsoft.SqlTools.ServiceLayer.PerfTests/Microsoft.SqlTools.ServiceLayer.PerfTests.csproj \
  --filter FullyQualifiedName=Microsoft.SqlTools.ServiceLayer.PerfTests.IntellisenseLargeSyntheticTests.LargeSyntheticColdReadySmoMetadataProvider
```

For a larger stress shape:

```bash
export SQLTOOLS_INTELLISENSE_BENCHMARK_SCHEMAS=40
export SQLTOOLS_INTELLISENSE_BENCHMARK_DATABASE_NAME=keep_SQLToolsVeryLargeIntelliSensePerfTestDb
export SQLTOOLS_INTELLISENSE_BENCHMARK_TABLES_PER_SCHEMA=125
export SQLTOOLS_INTELLISENSE_BENCHMARK_COLUMNS_PER_TABLE=32
export SQLTOOLS_INTELLISENSE_BENCHMARK_VIEWS_PER_SCHEMA=20
export SQLTOOLS_INTELLISENSE_BENCHMARK_PROCS_PER_SCHEMA=20
export SQLTOOLS_INTELLISENSE_BENCHMARK_FUNCTIONS_PER_SCHEMA=10
export SQLTOOLS_INTELLISENSE_BENCHMARK_SYNONYMS_PER_SCHEMA=20
```

## Result Table

Fill this from the JSON files emitted to `ResultFolder`.

| Scenario | Provider | p50 ms | p90 ms | avg ms | Iterations | Notes |
| --- | --- | ---: | ---: | ---: | --- | --- |
| Cold ready | SMO | 702.4 | 719.5 | 696.5 | 10 | Local run, exact-filter scenario |
| Cold ready | Catalog | 412.6 | 422.3 | 410.7 | 10 | Local run, exact-filter scenario |
| Cross-schema completion | SMO | 15.5 | 22.9 | 17.3 | 10 | Local run, exact-filter scenario |
| Cross-schema completion | Catalog | 24.7 | 31.8 | 27.1 | 10 | Local run, exact-filter scenario |
| Three-part column completion | SMO | 10.5 | 11.6 | 10.9 | 10 | Local run, exact-filter scenario |
| Three-part column completion | Catalog | 13.8 | 16.0 | 14.5 | 10 | Local run, exact-filter scenario |

Local run details:

- Date: 2026-05-28.
- SQL Server connection: local configured `sqlOnPrem` profile.
- Database shape: default large synthetic database.
- Result folder: `/tmp/sqltools-intellisense-bench`.
- The class-level filter was valid for smoke testing, but exact-filter scenario runs were used for the table above to avoid shared testhost effects.

Raw iterations, in milliseconds:

| Scenario | Provider | Iterations |
| --- | --- | --- |
| Cold ready | SMO | 632.822, 673.841, 693.165, 700.647, 701.519, 703.324, 709.182, 709.838, 719.218, 721.767 |
| Cold ready | Catalog | 386.097, 402.521, 403.752, 411.802, 411.912, 413.233, 415.126, 418.081, 422.255, 422.634 |
| Cross-schema completion | SMO | 14.321, 14.507, 14.620, 14.680, 14.871, 16.122, 17.733, 19.404, 22.760, 23.737 |
| Cross-schema completion | Catalog | 22.853, 23.038, 23.653, 24.354, 24.403, 25.060, 31.113, 31.520, 31.641, 33.065 |
| Three-part column completion | SMO | 9.693, 10.248, 10.379, 10.517, 10.534, 10.541, 10.858, 10.964, 11.285, 14.306 |
| Three-part column completion | Catalog | 12.693, 13.556, 13.633, 13.708, 13.731, 13.890, 14.330, 15.162, 15.686, 18.327 |

## Acceptance Bar

The catalog provider should meet all of these before default enablement:

- cold ready p50 at least 3x faster than SMO on the default synthetic shape;
- cold ready p90 under 10 seconds on a local SQL Server instance for the default synthetic shape;
- cross-schema completion p50 under 250 ms after cache ready;
- three-part column completion p50 under 250 ms after cache ready;
- no regression in completion correctness for current database, cross-schema, and three-part object references.

Current local run interpretation:

- Catalog cold ready was faster than SMO, but only about 1.7x faster on p50, so it does not yet meet the 3x cold-ready bar.
- Catalog completion latency was below the 250 ms target, but SMO was still faster for the measured cross-schema and three-part post-ready completion cases.
- Correctness checks passed for cross-schema table completion and three-part column completion in both providers.
