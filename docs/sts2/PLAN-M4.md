# PLAN-M4: Sqlite adapter

## Scope

SPEC §16 M4 DoD: Sqlite adapter implemented over Microsoft.Data.Sqlite; `dialect:neutral`
contract suite green on Sqlite; real file-backed and in-memory modes tested; architecture
test asserts Core and Contracts contain zero `Microsoft.Data.*` references.

## Architecture decisions

- The driver port (IDbDriver/IDbSession) already exists from M2; the Sqlite adapter is a
  new implementation in `Drivers.Sqlite`, registered alongside FakeDriver by name.
- Contract suite: rather than re-script FakeDriver YAML for Sqlite (events vs real SQL),
  add a code-based contract harness that runs the SAME logical session (initialize, open,
  execute real neutral SQL, stream, ack, complete, close) against BOTH FakeDriver-backed
  and Sqlite-backed sessions and asserts equivalent wire shape and invariants. This is the
  honest "port works" proof the spec wants (§10.3).
- Connection profile mapping: `server` is the Sqlite data source (`:memory:` or a file
  path); `Mode`/`Cache` from options. In-memory connections must stay open for the session
  lifetime (shared handle), file-backed use the path.
- Cancellation: SqliteCommand cancellation is cooperative; the adapter threads the
  CancellationToken through ExecuteReaderAsync/ReadAsync and honors it between pages.
- Type encoding: Sqlite's dynamic typing → map to wire natives where lossless; INTEGER→
  number, REAL→number, TEXT→string, BLOB→base64 binary wrapper, NULL→json null.

## Vertical slices

1. SqliteDriver/SqliteSession over Microsoft.Data.Sqlite: open (memory + file), execute
   with paging (PageRows), result-set metadata, row encoding, ExecCompleted, exception
   mapping to Sts2.* codes, cooperative cancel, lease/dispose.
2. Sqlite integration tests (in Drivers/test): in-memory + file-backed open/execute/
   stream/types/error/cancel, lease cleanup.
3. Contract harness: neutral session driven against FakeDriver and Sqlite sessions,
   asserting equivalent wire shape + I1/I2/I3/I7/I8/I12. Wire it into the scenario gate.
4. Architecture test: Core and Contracts assemblies reference zero `Microsoft.Data.*`
   (reflection over referenced assemblies + source scan).
5. Bootstrap registers Sqlite + (placeholder) SqlClient drivers; E2E can open a Sqlite
   connection over real stdio.
6. Docs/report/tag sts2-m4.

## Known risks

- In-memory Sqlite lifetime: the connection must outlive individual commands; the session
  owns it. File-backed temp files must be cleaned.
- Microsoft.Data.Sqlite native SQLitePCL provider bundling on this SDK/runtime — verify it
  loads (the package bundles e_sqlite3); add a smoke open in tests.
- Cancellation granularity: Sqlite reads are fast; cancel is honored between pages, not
  mid-row. Acceptable for neutral contract scope.
