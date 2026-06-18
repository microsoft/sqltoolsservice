# STS2 — What Was Built

A complete index of the STS2 subsystem: every project, the files and classes that
matter, what each does, and how they interact. This is the map; for the narrative/pitch
see [`sts_refactor.md`](sts_refactor.md), for visuals see
[`design_diagrams.tex`](design_diagrams.tex), and for the canonical contract see
[`../docs/sts2/SPEC.md`](../docs/sts2/SPEC.md) and the generated docs under `docs/sts2/`.

STS2 lives under `src/sts2/` (eleven projects) with tests under `test/sts2/`. It runs
**inside the existing service process, side-by-side with the legacy service**, sharing one
stdio JSON-RPC channel. It exposes a minimal v2 surface (connectivity + query execution +
diagnostics) and is engineered so the whole machine **explains itself while it runs**.

---

## 1. Project map

| Project | Role | Key public types |
|---|---|---|
| **Abstractions** | The driver port: how Core talks to a database without ADO.NET leaking in. | `IDbDriver`, `IDbSession`, `ConnectionOpenRequest`, `ExecEvent` family, `DbDriverException` |
| **Contracts** | Wire constants, method registry, stable error codes, defaults. | `Sts2Methods`, `Sts2ErrorCodes`, `Sts2Defaults`, `Sts2WireConstants` |
| **Core** | The pure synchronous reducer + immutable state. No I/O, time, randomness, or async. | `Sts2CoreReducer`, `CoreState`, `CoreOutput` family, `CoreDecision`, `CoreStateDump` |
| **Runtime** | Everything impure but deterministic-by-discipline: journal, coordinator pump, effects, replay, redaction, **observability**. | `Coordinator`, `JournalWriter`, `DriverEffectRunner`, `JournalReplayer`, `IEnvelopeSink` + sinks |
| **Multiplexer** | BCL-only stdio splitter: one channel, two services, id-rewriting, lifecycle mirroring. | `StdioMultiplexer`, `OutboundRequestIdTable`, `ISts2LifecycleSink` |
| **Hosting** | The StreamJsonRpc gateway that composes a session. | `Sts2Session`, `Sts2SessionOptions` |
| **Bootstrap** | The single seam into the legacy process; decides whether STS2 turns on. | `Sts2Bootstrap`, `Sts2BootstrapHandle` |
| **Drivers.Sqlite** | Real in-process driver (contract tests, real I/O). | `SqliteDriver`, `SqliteSession` |
| **Drivers.SqlClient** | Production SQL Server driver. | `SqlClientDriver`, `SqlClientSession`, `SqlClientErrorMapping` |
| **Testing** | FakeDriver, YAML scenario runner, simulator, invariant checker, doc generators. | `FakeDriver`, `ScenarioRunner`, `ConnectionSimulator`, `InvariantChecker`, `GeneratedDocs` |

Dependency direction is strict (enforced by `DependencyMatrixTests`, invariant I11):
Core depends only on Contracts; Runtime depends on Core + Abstractions + Contracts; drivers
depend on Abstractions; Hosting composes them; nothing references legacy namespaces.

---

## 2. The pipeline, end to end

```
stdin/stdout
   │
   ▼
StdioMultiplexer ── legacy frames ──► legacy JSON-RPC service (unchanged)
   │  v2 frames
   ▼
Sts2Session (StreamJsonRpc gateway)
   │  redact secrets, assign corr
   ▼
Coordinator (the single pump)
   ├─ 1. BuildEnvelope + elide capture            (CaptureElision)
   ├─ 2. JournalAsync  → JournalWriter (write-ahead, awaited)
   │                   → aux sinks (metrics, live tail, …)   ← the observability seam
   ├─ 3. Sts2CoreReducer.Decide(state, envelope)  (pure)
   └─ 4. for each output: journal it, then emit RPC / run effect
        ├─ rpc.out.* → emit to gateway (+ health/state runtime overlay)
        └─ effect.req → DriverEffectRunner ──► IDbDriver ──► effect.res back into the pump
```

Every box on that path becomes a journaled **envelope**, in a gapless `seq` order, and the
journal order is the truth. Replay re-runs the journal through the same pure reducer and
must reproduce the identical outbound digest sequence.

---

## 3. Core (`Microsoft.SqlTools.Sts2.Core`)

Pure, synchronous, no I/O. This is where the domain logic lives and the only place that
decides *what should happen*.

- **`Sts2CoreReducer`** — `Decide(CoreState, CoreEnvelope) -> CoreDecision`. One switch over
  envelope kind → request/notification/effect-response/control handlers. Holds the
  connection machine (`opening → open → closing`), the query machine (`running →
  cancelRequested → completed → disposed`), backpressure credit accounting, idempotency
  rules, and the diagnostics handlers (`health`, `state`, `setCapture`, `exportLog`). Never
  throws — malformed input becomes a stable `core.unexpectedInput` diagnostic.
- **`CoreState`** — immutable record: connections, queries, openId index, drivers,
  `MaxConnections`, **`RowCapture`/`SqlCapture`/`ConfigVersion`** (capture config now lives
  here, seeded by `session.start`, changed by `setCapture`). All maps are
  `ImmutableSortedDictionary` for deterministic output ordering.
- **`CoreOutputs`** — the verdict types: `RpcResultOutput`, `RpcErrorOutput`,
  `RpcNotifyOutput`, `EffectRequestOutput`, `DiagnosticOutput`, and **`ConfigChangedOutput`**
  (new — journals a config change and makes it replay-visible). `CoreDecision` = next state +
  array of outputs.
- **`CoreStateDump`** (new) — the *one* redacted state-dump format
  (`ToJson(state, atSeq)`), shared by live `diagnostics.state` and replay `DumpState` so they
  compare byte-for-byte. Carries the machine flags that explain why a connection/query is
  parked (`hasHandle`, `cancelRequested`, `closeAfterQuery`, `closePending`,
  `creditOutstanding`, `completeSent`).
- **`CoreEnvelope`** — the reducer's read-only view of an envelope (seq, kind, type, corr,
  payload).

---

## 4. Runtime (`Microsoft.SqlTools.Sts2.Runtime`)

The impure edge, held deterministic by discipline. Three sub-areas plus the new
Observability area.

### 4.1 Coordination — the pump

- **`Coordinator`** — the single-threaded heart. A bounded `Channel` of pending inputs; a
  pump loop that, per envelope: elides capture, builds the envelope (stamping
  `seq/ts/runId/configVersion`), **journals write-ahead and fans out to aux sinks**, calls
  the pure reducer, then journals + acts on each output. Owns the metrics sink, exposes the
  runtime health counters (`QueueDepth`, `SinkFaultCount`, `EmitFaultCount`,
  `EffectFaultCount`, `ConfigVersion`, `FatalReason`, `Metrics`), guards the pump with a
  fatal-fault handler, overlays runtime facts onto health/state responses, and samples
  `metric` envelopes on cadence.
- **`CoordinatorContracts`** — `CoordinatorOptions` (run id, queue capacity,
  `MetricSampleEvery`), `OutboundRpcMessage`, `EffectWorkItem`, `ICoordinatorInbox`,
  `ISts2EffectRunner`, and **`IEffectRunnerDiagnostics`** (new — lets the runner surface
  lease/pump counts to health across the pure boundary).
- **`CoreOutputEncoder`** — maps each `CoreOutput` to envelope fields deterministically.
  Shared by the live coordinator and the replayer so both produce byte-identical canonical
  payloads (I7 compares digests of exactly this encoding).
- **`CaptureElision`** — digest-capture: replaces row cells / SQL text with
  authoritative-digest wrappers *before* journaling, keeping an in-memory fragment side table
  that substitutes originals back at the wire/effect edges. Single-threaded pump makes
  content-digest keying race-free; the coordinator clears the table on dispose.

### 4.2 Journaling — the write-ahead log

- **`JournalWriter`** — append-only JSONL segments + manifest with chained SHA-256 hashes.
  `AppendAsync(envelope, flush)` is called *before* dispatch (write-ahead). It now also
  **implements `IEnvelopeSink`** — it is the privileged first sink.
- **`JournalReader`** / **`JournalManifest`** / **`JournalSegment`** / **`JournalOptions`** —
  read-back (including the active segment for tailing), manifest model, rotation config.
- **`CanonicalJson`** — canonical UTF-8 form (ordinal-sorted keys, number tokens verbatim,
  D-0007) and SHA-256 digests; the basis of replay equality.

### 4.3 Envelopes

- **`Sts2Envelope`** — the universal unit: `schema, runId, seq, ts, kind, sessionId, corr,
  cause, type, configVersion, digest, payload, payloadMeta`.
- **`EnvelopeKinds`** — the closed set of 15 kinds; `EnvelopeJsonCodec` (de)serializes the
  canonical single-line form.

### 4.4 Effects — the async edge

- **`DriverEffectRunner`** — executes journaled `effect.req`s against `IDbDriver` and posts
  observations back as `effect.res`. Owns live sessions, per-open cancellation, per-query
  streaming pumps with credit semaphores, secret resolution at the very edge, and failure
  classification to stable codes. **Hardened in this pass**: opens and query pumps are now
  registered *synchronously in `Run()`* (on the pump thread, before `Task.Run`), eliminating
  the four pre-arrival reconciliation dictionaries. Implements `IEffectRunnerDiagnostics`.
- **`WireValueEncoder`** — SPEC §7.7 cell encoding (CLR types → wire JSON).

### 4.5 Replay & Redaction

- **`JournalReplayer`** — replays a journal through the pure reducer without re-executing
  effects, matching every recorded output by causal position + digest (I7). `DumpState`
  delegates to `CoreStateDump`. Now matches `config.changed` in output position so config
  changes are replay-visible (I15).
- **`SecretSideTable`** / **`SecretRedactor`** — tokenize secrets (`secret:sha256:…`) before
  any envelope exists; real material lives only in memory, removed when an open completes.

### 4.6 Observability — the event-capture framework (new)

This is the headline of the recent pass: observability is now a first-class fan-out seam,
not a single hard-wired journal consumer.

- **`IEnvelopeSink`** — the universal hook: `OnEnvelopeAsync(envelope, flush)`. Every
  journaled envelope is delivered, in `seq` order, to each registered observer.
- **`CompositeEnvelopeSink`** — ordered fan-out with fault isolation: a sink that throws is
  counted (`FaultCount`) and skipped, never reordering the stream or stalling the pump.
- **`BroadcastEnvelopeSink`** + **`EnvelopeSubscription`** — the in-process **live tail**.
  `Subscribe()` returns a bounded reader; a slow consumer drops the *oldest* envelope to
  admit the newest and counts the drop (`Dropped`/`TotalDropped`) so it can re-sync from the
  journal. The diagnostic viewer's primary feed.
- **`MetricsEnvelopeSink`** — tallies the stream by kind and counts errors by code
  (`EnvelopesByKind()`, `ErrorsByCode()`, `Total`, `Errors`); feeds health and the metric
  snapshot envelopes.
- **`Sts2EventSource`** — process-wide `EventSource` (`Microsoft-SqlTools-Sts2`) with polling
  counters (`envelopes-total`, `rpc-errors-total`, `sink-faults-total`) for
  `dotnet-counters` / any `EventListener`. BCL-only.

### 4.7 Export

- **`ExportBundleWriter`** — produces a redacted zip bundle (manifest, privacy report,
  journals, generated docs) and a `Check` that validates hashes + privacy.

---

## 5. Multiplexer (`Microsoft.SqlTools.Sts2.Multiplexer`)

BCL-only, no StreamJsonRpc. Splits one stdio channel into two services.

- **`StdioMultiplexer`** — frames inbound JSON-RPC, routes v2 methods to STS2 and everything
  else to legacy, serializes outbound writes (single-writer, I10), mirrors `shutdown`/`exit`
  to STS2 as lifecycle signals (not duplicate broadcasts), and contains STS2 crashes so the
  legacy service survives.
- **`OutboundRequestIdTable`** — rewrites server-initiated request ids so legacy and STS2
  can never collide; restores originals on the response (I13).
- **`JsonRpcFraming`** / **`JsonRpcMessageInspector`** — header-delimited framing and a
  cheap method/id inspector. **`ISts2LifecycleSink`** — the shutdown/exit callback contract.

---

## 6. Hosting & Bootstrap

- **`Sts2Session`** (`Hosting`) — composes a session: secret side table, effect runner,
  journal, coordinator, and the StreamJsonRpc `GatewayTarget` (one thin `[JsonRpcMethod]` per
  v2 method, each funneling into the coordinator). Posts the journaled `session.start`
  (service version, drivers, **capture modes**). Exposes **`LiveTail`** (the broadcast sink)
  and accepts extra observers via `Sts2SessionOptions.EnvelopeSinks`. The gateway holds no
  domain logic.
- **`Sts2Bootstrap`** / **`Sts2BootstrapHandle`** (`Bootstrap`) — the only seam into the
  legacy process (SPEC §5). `TryStart` decides activation, builds the multiplexer + session,
  and returns a handle the legacy `Program` drives. The 12-line / 3-file legacy diff lives
  entirely here.

---

## 7. Drivers

- **`SqliteDriver`/`SqliteSession`** — real in-process SQLite; used by contract tests for
  real-I/O coverage without a server.
- **`SqlClientDriver`/`SqlClientSession`/`SqlClientErrorMapping`/`SqlClientConnectionString`**
  — production SQL Server via `Microsoft.Data.SqlClient`; maps provider errors to stable
  `Sts2.*` codes; exercised by the engine suite against a real SQL Server 2025 container.

---

## 8. Testing (`Microsoft.SqlTools.Sts2.Testing`) and the test suite

- **`FakeDriver`/`FakeQueryScript`** — deterministic scripted driver (the executable spec's
  backend).
- **`ScenarioRunner`/`ScenarioYamlParser`/`ScenarioModel`/`ScenarioCatalog`** — YAML golden
  transcripts (the executable spec); seeds capture modes into `session.start`.
- **`ConnectionSimulator`** — seeded random op/fault schedules; deterministic journals per
  seed. Found five real concurrency bugs across M2–M7.
- **`InvariantChecker`** — validates I1–I16 over a produced journal.
- **`GeneratedDocs`** — deterministic generators for CONTRACT / TRACE-SCHEMA / INVARIANTS /
  SCENARIO-MATRIX / STATE-MACHINE / COMPONENTS; `verify.sh` fails on diff.
- **`SecretCanaries`** — known canary values scanned out of every artifact (I6).

Test projects: `UnitTests` (Core, runtime, multiplexer, drivers, scenarios, docs,
architecture, perf, simulator, and the new `Observability/*`) and `E2ETests` (spawns the real
exe and drives v1 + v2 over one stdio session). The observability pass added
`EnvelopeSinkTests`, `MetricsAndHealthTests`, `StateDumpUnificationTests`, `SetCaptureTests`.

---

## 9. Diagnostics surface (what a tool can ask / observe)

| Surface | How | What you get |
|---|---|---|
| `v2/diagnostics.health` | request | counters + live runtime overlay (queue depth, leases, fatal, configVersion, dropped counts, error histogram) |
| `v2/diagnostics.state` | request | redacted machine state (`CoreStateDump`) + `runtime` handle section; identical Core portion to replay |
| `v2/diagnostics.setCapture` | request | change capture mode at runtime; journals `config.changed`, bumps `configVersion` |
| `v2/diagnostics.exportLog` | request | redacted zip bundle for offline analysis |
| live tail | `Sts2Session.LiveTail.Subscribe()` | every envelope, in order, in-process (viewer feed) |
| metrics | `Coordinator.Metrics` / `Sts2EventSource` | tallies by kind + error code; EventCounters |
| journal | `JournalReader.ReadAll(dir)` | the full trace, replayable; active segment included for tailing |
| replay | `JournalReplayer.Replay(journal)` | identical-digest reproduction + divergence reports + state dumps |

---

## 10. Docs & scripts

- `docs/sts2/SPEC.md` (+ §19 deviations DEV-001..DEV-010), `AGENT-RUNBOOK.md`, `DECISIONS.md`.
- Generated review surface: `CONTRACT.md`, `TRACE-SCHEMA.md`, `INVARIANTS.md`,
  `SCENARIO-MATRIX.md`, `STATE-MACHINE.md`, `COMPONENTS.md`.
- `docs/sts2/OBSERVABILITY.md` — the viewer-integration guide.
- `scripts/update-sts2-docs.ps1` (regenerate docs, fail-on-diff), `update-sts2-public-api.ps1`
  (harvest PublicAPI), `run-sts2-mutation.sh` (Stryker per project).
- `verify.sh --quick | --full` — the definition-of-done gates; report in
  `artifacts/verification-report.md`.
