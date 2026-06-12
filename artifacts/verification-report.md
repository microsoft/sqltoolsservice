# STS2 Verification Reports

Newest entries first (AGENT-RUNBOOK.md §6).

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
