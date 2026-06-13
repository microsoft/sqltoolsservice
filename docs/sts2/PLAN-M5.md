# PLAN-M5: SqlClient adapter and engine truth

## Scope

SPEC §16 M5 DoD: SqlClient adapter; SQL Server container suite scripted for
`verify.sh --full`; `dialect:tsql` corpus; T-SQL truth capture tool; type encoding
matrix tests; SqlClient cancellation and disposal leases verified.

## Environment reality (recorded)

Docker is installed locally but the daemon is not running. Per SPEC §16/§14.5, engine
tests (anything needing a live SQL Server) are **skipped locally with an explicit report
line** and run in CI/nightly. This milestone therefore delivers, and verifies locally:
- the SqlClient adapter (compiles, server-free logic unit-tested);
- the container suite + `dialect:tsql` corpus + truth-capture tool, all **gated** to skip
  cleanly when no server is reachable;
- type-encoding and exception-mapping unit tests that need no server.
The container gate's green run is a CI responsibility (documented).

## Architecture decisions

- SqlClientDriver/SqlClientSession mirror the Sqlite adapter shape over
  Microsoft.Data.SqlClient (already referenced by the repo). Streaming is page-by-page
  (no full-result buffering): SqlException mapping is done by wrapping the reader pump in
  a helper that converts exceptions to DbDriverException at the page boundary.
- Cancellation: `SqlCommand.Cancel()` plus CancellationToken cooperation.
- Type metadata: provider type name verbatim + precision/scale/length/nullable/collation
  from GetColumnSchema; typed wrappers for decimal/datetime*/money/binary/guid/xml.
- Connection string built from sanitized profile + secret material; encrypt/trust honored.
- Engine availability probe: a SqlServerAvailability helper tries a fast connect to the
  configured test server (env STS2_SQLSERVER_CONNSTRING); tests are [SkippableFact] when
  it is unreachable.

## Vertical slices

1. SqlClientDriver/SqlClientSession: open, paged streaming, column schema → ColumnInfo,
   CLR-typed cells (decimal/DateTime/DateTimeOffset/Guid/byte[]/etc.), SqlException →
   Sts2.* mapping, SqlCommand.Cancel cancellation, lease/dispose.
2. Server-free unit tests: connection-string building, exception classification table,
   cell encoding for provider CLR types (no server needed).
3. Engine probe + skippable T-SQL contract tests (dialect:tsql) mirroring the Sqlite
   contract harness; skip with a reported reason when no server.
4. T-SQL truth-capture tool scaffold (tools or Testing): runs a T-SQL corpus against the
   container and freezes FakeDriver scripts; CI-only.
5. verify.sh --full: SQL Server container suite + truth-capture gates that print
   "skipped: no SQL Server reachable" locally and run in CI.
6. Report (engine gate = skipped-local/CI), tag sts2-m5.

## Known risks

- Microsoft.Data.SqlClient 6.x async cancellation semantics differ from Sqlite; cover with
  the container cancel test (CI).
- Type encoding fidelity (decimal scale, datetimeoffset) can only be truly validated
  against the engine; the unit tests cover the mapping logic, CI covers the values.
