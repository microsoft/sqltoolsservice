# STS2 engine tests (dialect:tsql) — running against SQL Server

SPEC §14.5 requires the `dialect:tsql` corpus and a T-SQL truth-capture tool to run
against a real SQL Server. These are **CI/nightly** responsibilities: the local
`./verify.sh --full` reports the SQL Server engine suite as `n/a` when no server is
configured, and the engine unit tests (`Category=Engine`) skip with a logged reason
rather than failing (SPEC §14.5: "Local absence of Docker may skip engine tests only
when the report says so clearly").

## Pointing the tests at a server

Set `STS2_SQLSERVER_CONNSTRING` to a reachable SQL Server and run:

```bash
export STS2_SQLSERVER_CONNSTRING='Server=localhost,1433;Database=master;User ID=sa;Password=…;TrustServerCertificate=true'
./verify.sh --full          # the engine gate now runs for real
```

Or run the engine suite directly:

```bash
dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --filter 'Category=Engine'
```

## Container for CI

```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Str0ng!Passw0rd' \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
export STS2_SQLSERVER_CONNSTRING='Server=localhost,1433;Database=master;User ID=sa;Password=Str0ng!Passw0rd;TrustServerCertificate=true'
```

## T-SQL truth capture (CI tool)

`SqlClientEngineTests.TypeEncodingMatrixOverRealEngine` is the executable seed of the
truth-capture corpus: it runs representative T-SQL against the engine and asserts the
CLR types the adapter surfaces, which `WireValueEncoder` then maps to the pinned wire
form (validated server-free in `WireValueEncoderTests`). The full capture tool — which
freezes FakeDriver scripts from observed engine output so the fast suite tracks engine
truth without a live server — is built on this harness in CI; the freeze step writes
`test/sts2/scenarios/*.tsql.yaml` from captured rows. It is intentionally CI-only because
it requires the container to produce meaningful output.
