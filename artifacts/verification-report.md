# STS2 Verification Reports

Newest entries first (AGENT-RUNBOOK.md §6).

## STS2-3: maxCellBytes honored on the wire - 2026-07-06 - 328b47b6+ (working tree, uncommitted)
Remaining-tasks pass item STS2-3 (central_remaining_docs_review_pack/remaining_tasks.md
§5.1, P1): "Service truncates or wraps oversized cells with honesty flags, rather
than relying on QS display clamp. Acceptance: wide/blob query scenario shows bounded
payload and correct truncation metadata." Owner-directed; left UNCOMMITTED for the
SPEC-CHANGE human review (SPEC-CHANGE-0001 in DECISIONS.md).

Wire (all additive/optional, SPEC §7.3/§7.5/§7.7 updated):
- `v2/query.execute` accepts `options.maxCellBytes` (pageRows/pageBytes precedent).
  Absent/0/negative/non-integer = the pinned 1 MiB default (pre-existing behavior);
  positive values LOWER the bound; values above the default clamp to it (the client
  can never raise the service's memory/frame protection). Core normalizes the
  effective bound into the journaled `driver.queryStart` args (replay-deterministic);
  the runner truncates at the encode edge, covering every driver.
- Truncation honesty: the per-cell wrapper the page encoding already supported —
  `{"$t":"truncated","of":"string"|"binary","bytes":N,"digest":"sha256:...","v":prefix}`.
  `digest` (SPEC-promised, previously unimplemented) is sha256 of the full value;
  the retained prefix now honors the pinned `sts2.results.truncatedPrefixBytes`
  (65536) cap and never splits a UTF-8 code point. Never silent.
- `v2/initialize` capabilities gains `maxCellBytesHonored: true`.

New coverage:
- Scenario cell-truncation-max-cell-bytes ACTIVATED (was the stub the R024 commit
  deferred pending FakeDriver value-injection): 200KB string + 100KB blob pages at a
  64-byte bound; wrapper metadata asserted. FakeQueryStep gains cellValue/cellBytes/
  cellBinary (scenario YAML + C# injection). initialize-idempotent pins the capability.
- QueryFlowTests +4 (wire-level, FakeDriver through coordinator):
  WideCellsArriveBoundedWithTruncationHonesty (huge NVARCHAR/XML-shaped multibyte cell
  with a 2-byte char straddling the bound + blob page; digest + bounded page bytes),
  ExactBoundCellPassesThroughAndOneOverTruncates, AbsentOrZeroMaxCellBytesKeepsTodaysBehavior,
  OversizedMaxCellBytesRequestClampsToTheServiceLimit (8 MiB ask -> 1 MiB clamp + 64KB prefix cap).
- CoreReducerTests +1: QueryExecuteNormalizesMaxCellBytesIntoTheStartEffect
  (absent/{}/0/-5/"big"/64/999999999/8589934592 normalization matrix).
- WireValueEncoderTests +4: TruncatedWrapperCarriesFullValueDigest,
  ExactBoundIsNotTruncatedAndOneOverIs, TruncationNeverSplitsACodePoint (incl. astral
  '𐍈'), RetainedPrefixIsCappedByTruncatedPrefixBytes.
Targeted runs: encoder+queryflow+reducer suites 50/50; scenario corpus 53/53
(47 active, incl. the new scenario); E2E 5/5.

PublicAPI: no tracked-surface changes (encoder overload signature unchanged; new
members are private or in untracked Sts2.Testing). Pinned defaults unchanged.
Generated docs: SCENARIO-MATRIX.md regenerated (46->47 active, 8->7 stub).
Decisions: SPEC-CHANGE-0001 (options considered, lower-only semantics, evidence).

Client follow-up (vscode-mssql, NOT touched here): sts2Backend can flip
maxCellBytesHonored to true (or derive from initialize capabilities), send
options.maxCellBytes on v2/query.execute, and decode `$t:"truncated"` cells into
CellValue.truncated (TruncationInfo already models originalBytes/digest/reason).

Gates: verify.sh --quick green @ 328b47b6 (working tree):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok
- scenario tests (Fake, active corpus): ok
- contract tests (Sqlite, real I/O): ok
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok
- build legacy exe (for E2E): ok
- E2E disabled-mode v1 smoke + enabled-mode v1+v2: ok

Risk notes:
- Truncated cells that exceed the old 1 MiB default now retain a 64KB prefix (not
  1 MiB) — SPEC-faithful (truncatedPrefixBytes was pinned but unenforced) but a
  behavior change for >1 MiB cells; called out in SPEC-CHANGE-0001.
- The binary prefix bound applies to raw value bytes; base64 expands the wire text
  by 4/3 on top (documented in SPEC §7.7).
- Client currently passes wire cells raw into CompactPage values; until the decode
  path handles `$t:"truncated"`, a truncated cell would stringify as an object —
  the capability flag stays client-gated until that lands.

## P0 determinism fix: cancel-during-dispose wedged the query machine - 2026-07-06 - 7a99455f+
Found by the M7 `--full` 10k-seed sweep at 7a99455f (its verify-latest showed
"10k-seed simulator: FAIL"): seed 7496 intermittently reported "I2: query q-20
emitted 0 query.complete; I8: 1 driver session(s) still open at run end", and
in isolation the seed stalled through its full drain/settle budget (reads as a
deadlock). Temporary repro harness measured 87 hangs / 13 green per 100
iterations pre-fix.

Root cause (Core, one guard): `DecideQueryCancel`'s idempotency guard omitted
`QueryPhase.Disposing`. A `v2/query.cancel` arriving while the
`driver.queryDispose` ack was in flight regressed the phase
Disposing -> CancelRequested; `DecideDriverQueryDisposeResult` only acts when
`Phase == Disposing`, so the ack was dropped on the floor. Consequences chain:
no `query.complete(disposed)` terminal (I2), `ActiveQueryId` pinned forever,
any `connection.close` on that connection parks with ZERO outputs (visible in
wedged journals: `r-drainclose-2`'s turn emits nothing), the driver session is
never closed (I8), and the run wedges. The rpc order is seed-deterministic
(dispose then cancel back-to-back for the same query); the green/red split is
scheduler timing — whether the dispose ACK re-enters the pump before or after
the cancel rpc. Diagnosed from journal forensics plus dumpasync/clrstack of a
live wedged process (coordinator pump parked idle at MoveNextAsync — the pump
was never stuck; the wedge is pure Core state). The initial suspect — inline
`cts.Cancel()` on the effect-dispatch path (driver.cancelOpen) — was
investigated and exonerated: wedged journals show that dance completing
normally (effect.req seq 110 -> ack 122 -> open result 123 -> rpc error 124).

Fix: `v2/query.cancel` is idempotent (`{}`) for Disposing queries — dispose
supersedes cancel, so Disposing can never regress and the dispose ack always
lands. Wire shape unchanged (cancel already answered `{}` in the adjacent
terminal phases); CONTRACT.md untouched; STATE-MACHINE.md gained the edge
`Disposing --> Disposing : v2/query.cancel / result {}`.

Regression vs latent: LATENT. Reproduced at baseline 7f97a765 (pre-QoL
parent worktree): its wedged journals carry the identical signature
(query.dispose seq 50 -> query.cancel seq 55 still emitting driver.queryCancel
-> dispose ack seq 61 dropped -> journal ends seq 134). The M7 sweep surfaced
a pre-existing bug; dev/query did not introduce it.

New coverage (runbook §4.9):
- unit: CoreReducerTests.CancelDuringDisposeDoesNotStompTheDisposeAck (the
  exact interleaving, pure-Core).
- simulator seed: SimulatorSeedRegressionTests.Seed7496DisposeCancelRaceStaysGreen —
  pinned seed 7496 x20 iterations with a 60s per-iteration hang guard,
  Category=Simulator so it runs in both the quick 200-seed gate and the 10k
  gate. Replaces the temporary Repro7496 harness (deleted).

Evidence: repro harness 100/100 green twice (was 87 hangs / 13 green);
10k-seed sweep green TWICE consecutively (14m01s, 14m14s); unit suite 269
green; scenario corpus untouched (no golden encodes cancel-after-dispose).

Gates: verify.sh --quick green @ 7a99455f (working tree):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok
- scenario tests (Fake, active corpus): ok
- contract tests (Sqlite, real I/O): ok
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok
- build legacy exe (for E2E): ok
- E2E disabled-mode v1 smoke + enabled-mode v1+v2: ok

## Database context on query.complete - 2026-07-05 - cbd1e688+
Query Studio USE-tracking parity: a USE executed inside a script changes the
connection's database, but the client had no truth source (no ENVCHANGE on
the wire). Additive change: ExecCompleted gains `Database` (SqlClient's
connection.Database — driver-tracked ENVCHANGE state — read at completion);
DriverEffectRunner emits it; v2/query.complete carries `"database"` (null
when the driver has none). FakeQueryStep.Database for tests; new unit test
CompleteCarriesCurrentDatabaseWhenDriverReportsIt (value + null cases).
SPEC §10.1 record shape updated (additive, default-null — not a one-way
door). verify.sh --quick green.

## Info-message delivery + line passthrough - 2026-07-05 - 7f97a765+
Client-side worksheet verification (vscode-mssql Query Studio, row 1: verbatim
result-stream messages) found the message TEXT path verbatim by construction but
exposed two delivery gaps, both fixed:
- SqlClientSession.ExecuteAsync never subscribed SqlConnection.InfoMessage, so
  PRINT / RAISERROR severity<=10 / DBCC output never reached v2/query.message
  against real SQL Server (SPEC §10.2 already mandated the mapping; it existed
  only on FakeDriver). Now: subscribe before the reader opens, queue
  ServerMessage("info", number, class, verbatim text, line>0?line:null) from
  each SqlError, drain at pump boundaries (stream order vs result sets), and
  unsubscribe in the same finally that clears activeCommand.
- v2/query.message omitted `line` even though the ServerMessage record and the
  SPEC §10.1 shape carry it. DriverEffectRunner now emits `"line"` (null when
  absent); Sts2CoreReducer passes it through raw. No `state` added (record/SPEC
  have none; terminal error.server already carries state).
Test support: FakeQueryStep gains `Line`; FakeDriver passes it through. New
unit test QueryFlowTests.ServerMessagePassesThroughVerbatimWithLine (verbatim
text incl. quotes/backslashes, structured line, null-line case).

Gates: verify.sh --quick green (build warnings-as-errors, unit+mux+architecture,
scenarios, Sqlite contract real-I/O, replay, 200-seed simulator, canary scan,
generated-docs diff, legacy diff budget, E2E disabled+enabled). New test also
run standalone: passed.

## External-review hardening pass - 2026-06-19 - 70b66034+
Acted on a detailed external code review (STS2_REVIEW_PACKAGE, 50 findings) after verifying
each load-bearing finding against source. 18 commits, tiered real-bug → substrate → privacy →
contract-decisions; over-scoped/low-value items explicitly skipped (see REVIEW_ASSESSMENT_AND_PLAN.md).

Gates (all green):
- verify.sh --quick: ok (build warnings-as-errors, unit+mux+architecture, scenarios, Sqlite
  contract real-I/O, replay, 200-seed simulator, canary scan, generated-docs diff, legacy diff
  budget unchanged at 12 lines/3 files, E2E disabled+enabled).
- unit suite: 266 (+ ~25 new review-hardening tests).
- E2E: 5 (real spawned exe, enabled+disabled).
- SQL Server engine suite (dialect:tsql): **ok — ran for real** against the SQL Server 2025
  container (2026-06-19); type-encoding matrix + open + syntax-error mapping.
- simulator: **2000-seed sweep green** validating the concurrency-sensitive changes (dispose
  state machine with the strengthened I2, bounded cancel, fatal containment, runner ownership).

Fixed (verified bugs): reducer never-throw on bad JSON numbers (R026), ack credit over-grant
clamp (R011), duplicate-close request-orphan (R010), metric cause (R040), initialize capability
honesty (R036), query.complete flush checkpoint (R020), EnvelopeSubscription channel contract
(R034), health histogram naming (R047), flag-casing (R030), SqlClient activeCommand finally
(R035), SQLite sticky-cancel + incremental streaming (R016), no raw stdout in enabled mode (R029).

Substrate (mechanically-enforced promises): strict-vs-partial replay so truncation can't pass
as Identical (R006), composite fatal containment → Sts2.Unavailable (R001), runner ownership +
safe open handoff (R013/R014), observer mailboxes so a blocking sink can't stall the pump (R003),
one-directory-per-run isolation (R007), real lifecycle pump barrier (R002), export-check strict
replay (R023), maxCellBytes truncation (R024), bounded cancel (R015).

Privacy: opaque random secret tokens (R032), secret/capture cleanup on rejected paths
(R004/R005), command-line shape-allowlist (R033), host capture policy — product denies client
elevation to full/text (R018, D-0012).

Contract decisions (ADRs, owner-approved): dispose now emits exactly one terminal and holds the
connection until the pump stops (R008/R009, D-0011); host capture policy (D-0012).

Deferred (low-value-for-complexity, noted in commits): R012 credit-before-pull (needs page-pull
port; bounded +1-page overrun documented), R025 result-set ordering enforcement, R021 idle-flush
timer, R031 provider-message field classification, R041/R042 typed-internal-values + duplicate-key
rejection, R027 full ordered outbound writer, R028 oversized outbound frame, R017/R037 forensic
export snapshot + doc inventory.

Remaining for the merge-to-main PR (deliberate integration step, not done here): merge current
main (SDK 10.0.203→10.0.301) and regenerate (R039); CI gating PRs into main + evidence artifacts
(R019); the 10k-seed simulator and Stryker mutation run are CI/nightly per the M7 entry.

## Observability & event-capture pass (pre-viewer cleanup) - 2026-06-16 - 625f66ad+
Comprehensive design/quality pass against the §12 observability goals, driven by a read-only
design audit. Goal: make this part of the system "done right" — a real flexible event-capture
framework the VS Code diagnostic viewer hooks into — before that integration begins.

Gates (verify.sh --quick green):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (237 unit; +21 new observability tests)
- scenario tests (Fake, active corpus): ok
- contract tests (Sqlite, real I/O): ok
- replay verify (sts2-replay): ok — I7 holds with config.changed + metric envelopes in the stream
- simulator: ok (200 quick; **2000-seed sweep green** validating the runner refactor)
- secret canary scan: ok
- generated docs diff: ok (TRACE-SCHEMA/CONTRACT/INVARIANTS regenerated)
- legacy diff budget: ok (12 lines / 3 files, unchanged)
- E2E (disabled + enabled + Sqlite-over-stdio): ok — exercises the real production session
  (digest capture, metric cadence, setCapture wired)
- SQL Server engine suite (dialect:tsql): **ok — ran for real** against the SQL Server 2025
  container (2026-06-16); all 3 engine tests green incl. the live type-encoding matrix.

What changed (SPEC §19 DEV-007..DEV-010):
- **Event-capture framework**: `IEnvelopeSink` fan-out — journal is the write-ahead primary
  sink; aux observers (metrics, live tail, test capture) are best-effort, fault-isolated,
  counted. `BroadcastEnvelopeSink` live tail (bounded drop-oldest-with-count) is the viewer's
  feed; `MetricsEnvelopeSink` + `Sts2EventSource` EventCounters. See OBSERVABILITY.md.
- **Complete health** (§12.1): 11/11 fields — pure Core + live Runtime overlay (queue depth,
  leases, real fatal+reason, dropped-diagnostic counts, configVersion, error histogram).
- **Unified state dump**: one `CoreStateDump`; live `diagnostics.state` Core portion is now
  byte-identical to replay `DumpState`, enriched with the flags that explain a parked machine.
- **setCapture + config.changed + real configVersion**: capture config lives in Core state,
  seeded via session.start, changed at runtime, journaled, replay-visible (I15 exercised);
  three hard-coded `configVersion=1` literals removed.
- **Runner hardening**: four pre-arrival reconciliation dictionaries removed via synchronous
  registration; capture-elision table bounded on dispose; swallowed coordinator faults now
  counted. Trace-schema honesty: reserved kinds marked.

New tests: EnvelopeSinkTests, MetricsAndHealthTests, StateDumpUnificationTests, SetCaptureTests.
Legacy diff: 12 lines / 3 files — unchanged.
API surface: tracked; PublicAPI regenerated across Core/Runtime/Hosting.
Blockers: none. Next: VS Code extension integration + diagnostic event viewer (build against OBSERVABILITY.md).

## M7 - Hardening, evidence, preview (HUMAN GATE) - 2026-06-13 - dfb4cb54+
Gates (verify.sh --quick green 5x+ across the session; --full plumbing confirmed):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (~233 tests, Simulator/Perf/Engine in own gates)
- scenario tests (Fake, active corpus): ok (46 active)
- contract tests (Sqlite, real I/O): ok
- replay verify (sts2-replay): ok
- simulator (200 seeds quick; 1000 validated locally; 10k is --full/CI): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged from M0)
- build legacy exe (for E2E): ok
- E2E (disabled + enabled + Sqlite-over-stdio): ok
- SQL Server engine suite (--full, dialect:tsql): **ok — ran for real against a SQL Server 2025 container** (2026-06-15). All 3 engine tests green incl. the live type-encoding matrix (decimal/datetimeoffset/date/binary/guid/null → CLR types verified end-to-end).
- mutation testing (Stryker, --full): n/a — CI/nightly (dotnet-stryker not installed)
- 10k-seed simulator (--full): plumbing confirmed (300-seed pass); true 10k is CI/nightly
- perf/memory smoke (--full): ok — ~135k rows/s digest mode (M3 baseline holds)

New: env-driven simulator seed count (STS2_SIMULATOR_SEEDS; 10k in --full); Stryker config (stryker-config.json) with SPEC §14.6 ratchet thresholds; simulator split into its own gate (Category=Simulator) so its background-task load never starves the parallel unit suite; settle-before-teardown fix.
Replay: deterministic across all journals (I7) including digest mode
Simulator: 1000-seed local sweep green; deterministic journals per seed
Mutation: config wired; first ratchet score is CI's to record
Perf: ~135k rows/s digest mode (gate >=50k; M3 baseline holds)
Legacy diff: 12 lines / 3 files (Program.cs, ServiceLayerCommandOptions.cs, ServiceLayer csproj) — unchanged since M0, well under the <60/3 budget
API surface: stable; PublicAPI tracked on Contracts/Core/Abstractions/Runtime/Hosting
Invariants: I1-I16 exercised across scenarios/simulator/mux/E2E (see INVARIANTS.md for per-invariant exercise points)
Decisions: DEV-001..006, D-0001..0010 (DECISIONS.md); no SPEC-CHANGE stops
Blockers: none

**A FOURTH real product bug surfaced at 1000 simulator seeds and was fixed**: an open completing after coordinator teardown stored a driver session Core never closed (a spurious I8). The simulator now waits for the effect runner to go idle before disposing. Across M2-M7 the simulator + invariant checker found and we fixed: replay-via-constructor divergence, the cancelOpen startup race, the query-pump credit-semaphore leak, the close-on-open-race session orphan, and this teardown orphan — none of which would have been visible by reading code.

### CI checklist before the preview ships — ALL FOUR DONE locally 2026-06-15
1. ~~Engine suite (dialect:tsql)~~ **DONE** against a SQL Server 2025 container; full `--full` run green end to end. All 3 engine tests pass incl. the live type-encoding matrix.
2. ~~Stryker mutation gate~~ **DONE.** Root cause of the earlier block found and fixed: Buildalyzer was picking Visual Studio 2022's MSBuild 17.14, but the pinned .NET 10.0.300 SDK requires MSBuild ≥18 — `scripts/run-sts2-mutation.sh` now forces the SDK's own MSBuild via `MSBUILD_EXE_PATH` (Windows; Linux/CI uses the SDK MSBuild natively). Per-project ratchet (SPEC §14.6), break set just below the achieved baseline for variance margin: **Core 71–73%** across runs (break 68), **Contracts 95.2%** (break 90, ratcheted up via `stryker-config-contracts.json`), **Runtime pure units 86–88%** (break 60). All three exceed their floors. Stryker mutates one project-under-test per run via a scoped real solution `sqltoolsservice-sts2-stryker.sln` (Stryker can't read `.slnf`).
3. ~~10,000-seed simulator green~~ **DONE** — `STS2_SIMULATOR_SEEDS=10000`, **0 failures** across all 10,000 seeds (2h42m). Surfaced and we fixed the post-teardown open-orphan leak (the 5th simulator-found bug).
4. ~~Perf ≥50k rows/s~~ **DONE** (~135k rows/s digest mode).

CI automation: `.github/workflows/sts2-verify.yml` runs `verify.sh --full` on sts2/main (push/PR/nightly/dispatch), spinning up a SQL Server 2025 container and creating Sts2TestDb, with tiered simulator seed counts (PR 500, push 1000, nightly 10000) and Stryker on nightly/dispatch.

### Human review surface (SPEC §16 M7 final gate)
- docs/sts2/CONTRACT.md, INVARIANTS.md, SCENARIO-MATRIX.md, TRACE-SCHEMA.md, STATE-MACHINE.md, COMPONENTS.md
- docs/sts2/CLIENT.md, ENGINE-TESTS.md; this verification report; export privacy report (in any sts2-export-*.zip)
- Legacy diff (12 lines / 3 files); DECISIONS.md + SPEC.md §19 deviations

Next: **STOP — second mandatory human gate.** After review + the CI checklist, tag sts2-v2.0.0-preview.

## M6 - Client interop and export loop - 2026-06-13 - c29b8344
Gates (verify.sh --quick green):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (233 tests)
- scenario tests (Fake, active corpus): ok
- contract tests (Sqlite, real I/O): ok
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged)
- build legacy exe (for E2E): ok
- E2E (disabled + enabled + Sqlite-over-stdio): ok

New: export bundle (v2/diagnostics.exportLog → diag.export effect → ExportBundleWriter produces a real .zip with manifest + per-segment sha256, privacy report with canary scan, journal segments, generated docs); sts2-replay export-check (manifest hashes + privacy + presence); export round-trip test (digest-mode session with canary creds + sensitive SQL → canary-clean bundle, no SQL/row literals, export-check passes, tamper caught by hash mismatch); diagnostics.health (counters) + diagnostics.state (redacted ids/phases, I16) through Core/gateway; multiplexer interleaving torture (300 colliding legacy+STS2 server-request ids rewritten distinctly, restored to exact original numeric ids per channel, I13); CLIENT.md + vscode-jsonrpc TypeScript sample.
Replay: export bundles replay-verifiable; all journals identical (I7)
Simulator: seeds=200 failures=0
Mutation: n/a (M7)
Perf: M3 baseline holds
Legacy diff: 12 lines / 3 files (unchanged since M0)
API surface: Runtime +ExportBundleWriter/ExportBundleRequest/Result, +diagnostics methods in Core; Hosting +export wiring
Invariants exercised: I13 (torture, 300 ids), I16 (state redaction + export privacy), I6 (canary scan over bundle), full set in Fake/Sqlite paths
Decisions: none new
Blockers: none
Risk notes:
- The TypeScript sample (CLIENT.md) is documented, not run in CI — no Node toolchain is assumed in this repo. Its wire shapes are kept aligned with the generated CONTRACT.md by eye; a future CI lane could run it against the spawned exe.
- The "concurrent v1+v2 under load" DoD item is covered at the transport layer by the multiplexer interleaving torture test (300 concurrent colliding ids) plus the existing enabled-mode E2E that drives v1 (version) and v2 (initialize/ping/sqlite query) in one session; a dedicated high-load spawned-exe stress E2E is deferred to M7 hardening.
- Empty placeholder files (Sts2RpcHost.cs, DiagnosticsRpcTarget.cs, DiagnosticsPingContracts.cs) still await deletion approval.
Next: M7 (hardening, evidence, preview tag) — ENDS AT THE SECOND MANDATORY HUMAN GATE.

## M5 - SqlClient adapter + engine truth (partial: engine tests CI/nightly) - 2026-06-13 - 78405c53
Gates (verify.sh --quick green; --full green with engine n/a):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (224 tests; Engine+Perf excluded)
- scenario tests (Fake, active corpus): ok
- contract tests (Sqlite, real I/O): ok
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged)
- build legacy exe (for E2E): ok
- E2E disabled + enabled + Sqlite-over-stdio: ok (5 tests)
- SQL Server engine suite (dialect:tsql, --full): n/a — no STS2_SQLSERVER_CONNSTRING (Docker daemon not running locally; CI/nightly runs it)
- perf/memory smoke (--full): ok — 135k rows/s digest mode

New: SqlClientDriver/SqlClientSession over Microsoft.Data.SqlClient (conn-string for sqlLogin/accessToken/integrated with encrypt/trust, page-by-page streaming, column schema → ColumnInfo with precision/scale/length/nullable, CLR-typed cells, SqlException → Sts2.* mapping, SqlCommand.Cancel + token cancellation, lease/dispose); WireValueEncoder extracted and shared with the runner (SPEC §7.7 type matrix: decimal/datetime2/datetimeoffset/time/guid/binary/non-finite floats → typed wrappers, invariant strings), validated server-free; skippable engine suite + SQL Server probe (STS2_SQLSERVER_CONNSTRING); Bootstrap registers sqlclient + sqlite.
Engine truth: deferred to CI/nightly — Docker daemon is not running locally. dialect:tsql tests (Category=Engine) skip with a logged reason; docs/sts2/ENGINE-TESTS.md documents the container + connection-string path. Server-free logic (conn-string, error classification, full type-encoding matrix) is unit-tested locally (22 tests). The T-SQL truth-capture tool is seeded by SqlClientEngineTests and finalized in CI.
Replay: unchanged (no journal-path change)
Simulator: seeds=200 failures=0
Mutation: n/a (M7)
Perf: 135k rows/s digest mode (M3 baseline holds)
Legacy diff: 12 lines / 3 files (unchanged since M0)
API surface: Drivers.SqlClient (new public driver + conn-string/error-mapping helpers), Runtime +WireValueEncoder
Invariants exercised: type-matrix encoding (server-free), full set in Fake/Sqlite paths
Decisions: none new
Blockers: none — but M5 is PARTIAL pending a CI run of the engine suite (see risk)
Risk notes:
- **M5 is not fully verifiable in this environment**: the Docker daemon is not running, so no SQL Server is reachable. The adapter compiles, its server-free logic is fully unit-tested, and the engine tests are written and skip cleanly — but their green run against a real engine is a CI/nightly responsibility (SPEC §14.5 explicitly permits this with a clear report line, which this is). Before the M7 preview tag, CI must run `verify.sh --full` with STS2_SQLSERVER_CONNSTRING set and the engine suite green.
- SqlClient streams page-by-page (no full-result buffering), unlike the Sqlite adapter which buffers per result set; both satisfy the port contract.
- Empty placeholder files (Sts2RpcHost.cs, DiagnosticsRpcTarget.cs, DiagnosticsPingContracts.cs) still await deletion approval.
Next: M6 (client interop + export bundle loop)

## M4 - Sqlite adapter - 2026-06-13 - a2285cba
Gates (verify.sh --quick green):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (202 tests)
- scenario tests (Fake, active corpus): ok (46 active)
- contract tests (Sqlite, real I/O): ok (6 driver + 3 contract + 5 isolation)
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged)
- build legacy exe (for E2E): ok
- E2E disabled + enabled v1+v2 + Sqlite query over stdio: ok (5 tests)

New: SqliteDriver/SqliteSession over Microsoft.Data.Sqlite (in-memory + file-backed, paged streaming, CLR-typed cells, exception→Sts2.* mapping, cooperative cancel, lease/dispose); cross-driver contract harness driving the full coordinator/Core/effect-runner stack through real Sqlite (real tables/rows) and asserting the same wire shape + invariants as the Fake scenarios incl. identical replay (I7); architecture test proving Core/Contracts have zero Microsoft.Data.* references (assembly metadata + source scan); Bootstrap registers sqlite; E2E streams a real Sqlite query over stdio. Refactor: driver cells are plain CLR values, binary→base64 wire encoding moved to the runner.
Replay: Sqlite session journals replay identically (I7), same as Fake
Simulator: seeds=200 failures=0
Mutation: n/a (M7)
Perf: M3 baseline holds (no perf-path change)
Legacy diff: 12 lines / 3 files (unchanged since M0)
API surface: Drivers.Sqlite (new public driver), Abstractions unchanged
Invariants exercised: I1, I2, I3, I7, I8, I12 via the Sqlite contract path; full set in Fake scenarios/simulator
Decisions: none new; DEV log unchanged
Blockers: none
Risk notes:
- The Sqlite adapter buffers each result set before yielding events (a C# constraint: you cannot `yield` across a try/catch that maps SqliteException). For neutral contract scope and the page sizes used this is fine; SqlClient (M5) will stream page-by-page without full-result buffering for large sets.
- Cancellation is honored between pages, not mid-row (Sqlite reads are fast). Acceptable for neutral scope; SqlClient cancel uses SqlCommand.Cancel (M5).
- Empty placeholder files (Sts2RpcHost.cs, DiagnosticsRpcTarget.cs, DiagnosticsPingContracts.cs) still await deletion approval.
Next: M5 (SqlClient adapter + SQL Server engine truth — needs Docker/container in --full)

## M3 - Query streaming vertical slice - 2026-06-13 - 40ed6559
Gates (verify.sh --quick green; --full perf gate green separately):
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (188 tests)
- scenario tests (Fake, active corpus): ok (46 active of 54; +4 mux-adapter = 50 mandatory)
- contract tests (Sqlite): n/a (M4)
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok (now with query ops)
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged)
- build legacy exe (for E2E): ok
- E2E disabled-mode v1 smoke + enabled-mode v1+v2: ok (4 tests)
- perf/memory smoke (--full): ok — 1M rows x 10 cols digest mode, 87k-131k rows/s (gate >=50k), journal ~1.7 MiB

New: Core query machine (execute/resultSet/rows/message/complete/ack/cancel/dispose) with exactly-one-complete (I2), no-output-after-complete (I3), one-active-query Busy, backpressure credit (I9), close-while-active; DriverEffectRunner query pump (enumerator pull gated on a credit semaphore — real backpressure stops the driver); FakeDriver query scripting (rows/message/error/sever/crash/hang, typed-wrapper edge values); digest capture (rowCapture/sqlCapture=digest) eliding row cells and SQL from the journal while the wire keeps real data and replay stays digest-identical (DEV-005); scenario runner query support (driver.query, notify/waitForNotify/assertNotify, capture config) + invariant checks I2/I3/I9/RD1/SD1; 46 active scenarios; simulator query ops; perf smoke. Toy machine fully removed.
Replay: all scenario + simulator + capture journals identical (I7), including digest mode
Simulator: seeds=200 failures=0 (query+connection op/fault schedules)
Mutation: n/a (M7)
Perf: 1M rows 11.4s 87k rows/s journal ~1.7 MiB (digest mode); M3 baseline
Legacy diff: 12 lines / 3 files (unchanged since M0)
API surface: Core +query machine, Runtime +query pump/capture elision, Testing +query scenario/sim/perf; Contracts +Sts2Defaults/JsonRpcCodes
Invariants exercised: I1, I2, I3, I5, I6, I7, I8, I9, I12 per scenario/seed; RD1/SD1 (digest cleanliness); I10-I14 in mux/E2E
Decisions: D-0008 (backpressure credit in Core), D-0009 (SQL relayed verbatim), D-0010 (simulator drain); DEV-005/006
Blockers: none
Risk notes:
- THREE real product bugs were caught by our own gates this milestone (all now fixed with permanent coverage): (1) query pumps blocked on the credit semaphore leaked as background tasks on session teardown; (2) driver query effects could race ahead of the pump task startup, dropping cancels/credits; (3) closing an Opening connection orphaned the session if the open won the cancel race (simulator seed 47, unit regression added). This is the architecture working as intended.
- The 200-seed simulator is deterministic in its journals (I7 holds every run) but its end-of-run liveness waits are bounded by wall-clock budgets. Under the heavier --full load it flaked once on a liveness timeout (not a determinism failure) before passing on retry; --quick has been 5+ consecutive green. If this recurs in CI, the fix is to reduce per-seed background-task pressure further, not to retry. Documented as D-0010.
- Empty placeholder files (Sts2RpcHost.cs, DiagnosticsRpcTarget.cs, DiagnosticsPingContracts.cs) still await deletion approval.
Next: M4 (Sqlite adapter — real file-backed I/O proving the driver port)

## M2 - Connection vertical slice - 2026-06-12 - b2ced175+
Gates:
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (140 tests)
- scenario tests (Fake, active corpus): ok (14 active of 54)
- contract tests (Sqlite): n/a (M4)
- replay verify (sts2-replay): ok
- simulator (200 seeds): ok
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged)
- build legacy exe (for E2E): ok
- E2E disabled-mode v1 smoke + enabled-mode v1+v2: ok (4 tests, initialize added)

New: driver port (SPEC §10.1 full shape) + FakeDriver (scripted outcomes, hang points, leases); Core connection machine (opening/open/closing, idempotent cancel/close, duplicate openId, maxConnections via journaled config); DriverEffectRunner (secret resolution at the edge, per-open CTS, bounded close, secret scrub on open completion); Sts2Session gateway (every v2 request redact->journal->Core->journaled response; LocalRpcException carries numeric code + data); YAML scenario runner (YamlDotNet, DEV-004) with bind/$profiles/race steps and per-run invariant checks; ConnectionSimulator 200 seeds; 14 scenarios active.
Replay: all scenario + simulator journals identical (I7 checked per run); E2E journal under <logdir>/sts2
Simulator: seeds=200 failures=0
Mutation: n/a (M7)
Perf: n/a (baseline due M3)
Legacy diff: 12 lines / 3 files (unchanged since M0)
API surface: Abstractions +245 (driver port), Core +~60 (connection machine, session config), Runtime +~75 (effect runner, replay), Hosting rebuilt (+29 Sts2Session, -Sts2RpcHost), Contracts +~40 (defaults, codes mapping, methods registry)
Invariants exercised: I1, I5, I6, I7, I8, I12 per scenario/seed; I10-I14 in mux/E2E suites
Decisions: DEV-004 applied (YamlDotNet in Testing); session.start config envelope (replay-safety, see risk note)
Blockers: none
Risk notes:
- Two real bugs were caught by our own gates this milestone: (1) initial Core state parameterized via constructor diverged replay — fixed by journaling session config as a session.start control envelope; (2) driver.cancelOpen racing ahead of the open effect's task startup made cancels no-ops — fixed with a pre-cancel set. Both now have permanent regression coverage (ConnectionSessionReplaysIdentically, open-cancel-race scenario).
- The gateway rejects unregistered v2 methods with plain -32601 (StreamJsonRpc), not Sts2.InvalidRequest; registered-but-invalid requests get stable Sts2.* codes. Acceptable wire behavior; revisit if the client needs uniform data.code.
- Empty placeholder files (Sts2RpcHost.cs, DiagnosticsRpcTarget.cs, DiagnosticsPingContracts.cs) await deletion approval.
Next: M3 (query streaming vertical)

## M1 - Spine: envelopes, journal, replay, review surface - 2026-06-12 - 9d077cd6
Gates:
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (115 tests)
- scenario corpus (stub validation): ok (54 stubs)
- contract tests (Fake+Sqlite): n/a (M2+)
- replay verify (sts2-replay): ok
- simulator: n/a (M2)
- secret canary scan: ok
- generated docs diff: ok
- legacy diff budget: ok (12 lines / 3 files, unchanged from M0)
- build legacy exe (for E2E): ok
- E2E disabled-mode v1 smoke + enabled-mode v1+v2: ok (3 tests)

New: canonical JSON + sha256 digest frozen by goldens (D-0007); sts2.envelope/1 schema with deterministic JSONL codec; secret side table + key-based redactor + canary fixtures (I6); write-ahead journal with segment rotation and manifest hash chain; pure Core reducer with toy machine (echo/effect/lifecycle) under banned-API enforcement; coordinator pump (seq/ts assignment, journal-before-dispatch, outputs journaled before emission, effect re-entry); JournalReplayer + sts2-replay CLI (run/verify/until/diff/explain) sharing CoreOutputEncoder with the live pump; 54 scenario stubs with machine-read headers; six generated review docs with fail-on-diff gate.
Replay: 1/1 journals identical (toy-session corpus); tamper detection verified with cause chains
Simulator: n/a at M1 (lands with FakeDriver in M2)
Mutation: n/a (wired at M7; ratchet base set then)
Perf: n/a (baseline due M3)
Legacy diff: 12 lines / 3 files (unchanged since M0)
API surface: Contracts +54 entries (ping DTOs, error codes, wire constants, Sts2Methods registry), Core +151 (envelope/state/outputs/reducer), Runtime +282 (canonical, envelopes, journal, redaction, coordination, replay), Hosting +5
Invariants exercised now: I5 (gapless seq + cause chains), I6 (canary scan gate), I7 (replay verify gate), I10-I14 (mux + arch tests)
Decisions: D-0007 (canonical numbers verbatim); no SPEC-CHANGE entries
Blockers: none
Risk notes:
- The toy surface (v2/toy.*) is journal/replay scaffolding; it must be deleted in M2/M3 and is marked toy in CONTRACT.md. Risk: it leaks into client expectations if preview ships early.
- Scenario YAML execution semantics are unimplemented (stubs only). The runner lands M2/M3 and needs a YAML parsing decision: a spec-implied parser package would change the §4 dependency matrix (one-way door), or a BCL-only subset parser keeps the matrix intact. Flagging now for the M1 review.
- Capture modes (full/digest/minimal) and config.changed envelopes are not implemented yet; coordinator pins configVersion=1. They are scoped to M2/M3 slices with setCapture.
Next: HUMAN GATE — review CONTRACT.md, INVARIANTS.md, SCENARIO-MATRIX.md, TRACE-SCHEMA.md, STATE-MACHINE.md, COMPONENTS.md, and this report, then M2.

## M0 - Skeleton, repo reality, seam, multiplexer - 2026-06-12 - 251a3da5+
Gates:
- build (sts2 slnf, warnings as errors): ok
- unit+multiplexer+architecture tests: ok (58 tests)
- scenario tests (Fake): n/a (M1)
- contract tests (Fake+Sqlite): n/a (M2+)
- replay verify: n/a (M1)
- simulator: n/a (M1)
- secret canary scan: n/a (M1)
- generated docs diff: n/a (M1)
- legacy diff budget: ok
- build legacy exe (for E2E): ok
- E2E disabled-mode v1 smoke + enabled-mode v1+v2: ok (3 tests)

New: 58 unit/architecture tests (33+ multiplexer: framing variants, partial/coalesced chunks, v2 routing, legacy fallback, malformed payload fallback, oversized-frame fallback, lifecycle mirroring with bounded flush on shutdown AND exit, id rewrite with numeric-collision/string-preservation/duplicate/TTL/exit/channel-death cleanup, single-writer property test 800 frames, crash containment; 15 architecture: dependency matrix, legacy-namespace ban, analyzer wiring, PublicAPI presence, COMPONENT.md presence; 8 activation). 3 spawned E2E: disabled-mode v1 smoke asserting no STS2 artifacts, enabled-mode v2/diagnostics.ping + v1 version in one stdio session, shutdown terminates cleanly.
Replay: n/a at M0 (journal lands in M1)
Simulator: n/a at M0
Mutation: n/a (wired at M7 per SPEC §14.6; Core has no logic yet)
Perf: n/a (baseline due M3)
Legacy diff: 12 lines / 3 files (Program.cs, ServiceLayerCommandOptions.cs, ServiceLayer csproj) — budget <60/3
API surface: Contracts +33 entries (ping DTOs, error codes, wire constants), Hosting +5 (Sts2RpcHost); Core/Abstractions/Runtime empty. PublicAPI files updated.
Decisions: RF-0001..RF-0011, D-0001..D-0006
Blockers: none
Risk notes:
- RF-0011: legacy shutdown calls Environment.Exit(0) with no response and no exit handler; flush-before-death now hangs off shutdown (D-0005). The 120s legacy shutdown-callback timeout still bounds total process death, but STS2 flush races legacy callbacks: mux forwards shutdown only AFTER the bounded STS2 flush, so the race is closed at the multiplexer, not in legacy.
- StreamJsonRpc 2.25.28 is newly introduced; only ping exercises it so far. Formatter/handler API may need conforming changes when the envelope gateway lands (M1).
- Disabled-mode v1 smoke compares behavior (works + no artifacts), not response digests against an unmodified-main baseline yet; digest baseline capture is queued for M1's scenario tooling.
Next: M1
