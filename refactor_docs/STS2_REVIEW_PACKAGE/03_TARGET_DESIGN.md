# STS2 Reviewed Target Design

**Status:** Proposed post-review architecture  
**Applies to:** SQL Tools Service v2 connectivity, query streaming, diagnostics, replay, and side-by-side v1 coexistence  
**Primary goal:** A database service whose externally relevant decisions are deterministic, inspectable, privacy-safe, and recoverable without compromising process liveness.

This document turns the strongest ideas in the current STS2 branch into a complete target design and closes the boundary gaps found in the technical review. It is intentionally independent of milestone history. The current branch is an advanced implementation of this design, not yet a complete conformance point.

## 1. Goals and non-goals

### 1.1 Goals

STS2 MUST:

1. coexist with the legacy service in one process and on one framed stdio channel;
2. keep v1 behavior unchanged when disabled and behaviorally isolated when enabled;
3. expose a minimal, versioned v2 connectivity/query protocol;
4. place all domain decisions in a synchronous deterministic reducer;
5. record every decision-relevant input and output before it is acted upon;
6. reproduce the authoritative output sequence from a valid journal without a database;
7. bound queues, result buffering, observer work, close/cancel waits, and shutdown;
8. prevent secrets and disallowed user data from entering journals, logs, state, metrics, or exports;
9. make ownership and cleanup mechanically testable;
10. degrade STS2 without taking down legacy traffic;
11. attach immutable verification evidence to the exact merge candidate.

### 1.2 Non-goals

STS2 v2.0 does not provide:

- legacy feature parity;
- language service, object explorer, edit data, scripting, plan analysis, or notebooks;
- server-side batch splitting;
- random-access result caching;
- multi-query concurrency on one connection;
- interactive authentication;
- process or AppDomain isolation;
- exactly-once delivery to the client after a process crash;
- database-effect replay.

## 2. Architectural principles

### 2.1 One decider

`Sts2CoreReducer.Decide(state, input)` is the only component allowed to choose domain state transitions or domain outputs.

Runtime components may validate transport syntax, enforce host security policy, perform I/O, classify failures, and manage handles. They MUST NOT invent domain responses outside the documented fatal/unavailable transport boundary.

### 2.2 Journaled before acted upon

Every reducer input and every reducer output is appended to the authoritative journal before:

- the reducer consumes an input;
- an RPC output is delivered;
- an effect begins;
- a configuration change affects subsequent capture;
- a lifecycle barrier is acknowledged.

### 2.3 Pure authority, live overlays

The journal contains the deterministic authoritative form. Non-deterministic live facts may be added only at explicitly documented wire/viewer overlays. Each overlay field is marked as `runtimeOnly` in schema and is excluded from replay identity.

### 2.4 Explicit ownership

Every live resource has one owner, one disposal path, and one observable state. Fire-and-forget work is forbidden unless it is tracked by an owner and included in session completion.

### 2.5 Bounded by construction

Any queue, mailbox, page, cell, timeout, observer, export, and shutdown wait has a configured bound and an overflow/timeout behavior.

### 2.6 Privacy policy precedes capture mode

Core tracks the replay-visible effective mode. The host supplies the immutable policy that defines which modes are permitted. A client may request a mode but cannot elevate beyond host policy.

### 2.7 Strict verification is not time travel

Partial replay answers “what state existed by sequence N?” Strict verification answers “is this complete journal internally valid and behaviorally reproducible?” These are distinct operations and result types.

## 3. System context and trust boundaries

### 3.1 Actors

- **v1/v2 client:** trusted to use the protocol but not trusted with capture-policy elevation.
- **Legacy ServiceHost:** existing in-process service, isolated by virtual streams.
- **STS2 Runtime:** trusted product code.
- **Database provider/server:** external and fallible; messages may contain sensitive content.
- **Journal/export consumer:** may be a support engineer or automated tool and MUST be assumed not authorized to see raw secrets or product data.
- **Auxiliary observer/viewer:** optional and potentially slow or faulty.
- **Filesystem:** durable enough for process-crash recovery according to the documented flush policy, not assumed transactional by itself.

### 3.2 Data classifications

| Class | Examples | Default journal | Wire | Diagnostic log | Export |
|---|---|---:|---:|---:|---:|
| Secret | password, token | never | edge only | never | never |
| User SQL | query text | digest | full to driver | governed | digest |
| Result data | row cells | digest | full to client | never | digest |
| Provider text | server message, exception text | redacted/safe detail | safe detail | governed | redacted |
| Identifiers | query/connection/open IDs | full | full | full | full |
| Structural metadata | columns, counts, phases | full | full | full | full |
| Runtime telemetry | queue depth, leases | overlay/metric | health/viewer | full | optional |

Every field in every contract schema MUST carry a classification.

## 4. Component model and dependency rules

### 4.1 Projects

1. **Contracts:** versioned DTOs, schemas, methods, errors, bounds, classifications.
2. **Core:** reducer, immutable state, transition tables, state dump.
3. **Abstractions:** database port and stable effect DTOs.
4. **Runtime:** coordinator, journal, replay, privacy, effects, observer framework, export.
5. **Hosting:** validated JSON-RPC gateway and ordered outbound writer.
6. **Multiplexer:** BCL-only framing/routing/lifecycle/ID rewriting.
7. **Bootstrap:** composition root and feature activation.
8. **Drivers.SqlClient / Drivers.Sqlite:** provider adapters.
9. **Testing:** fake driver, scenario/simulator/invariant framework.
10. **Replay tool:** strict verify, time travel, diff, explain, export check.

### 4.2 Dependency law

Core has no time, I/O, async, cancellation tokens, tasks, channels, provider types, JSON-RPC types, or legacy namespaces. Contracts have no product implementation dependencies. Runtime never references legacy. Drivers never reference Core.

Architecture tests enforce project references, namespaces, banned APIs, and public API changes.

## 5. Ownership and lifecycle

### 5.1 Ownership tree

```text
Sts2BootstrapHandle
  StdioMultiplexer
    real stdin reader
    real stdout writer
    virtual legacy channel
    virtual STS2 channel
    outbound request-ID table
  Sts2Session
    JSON-RPC listener
    bounded ordered outbound writer
    Coordinator
      bounded inbox
      CoreState
      JournalWriter
      observer dispatcher
      DriverEffectRunner
        open operations
        query pumps
        live sessions
    SecretStore
    CapturePolicy
```

### 5.2 Session completion

`Sts2Session.Completion` is a composite task that completes when the session reaches one of:

- `OrderlyStopped`;
- `TransportClosed`;
- `Fatal(FatalDescriptor)`;
- `Disposed`.

It observes RPC listener, outbound writer, coordinator, observer workers, and effect runner. An unexpected completion of any required child triggers the fatal transition exactly once.

### 5.3 Ordered shutdown

1. Multiplexer stops admitting new v2 requests.
2. Session posts `lifecycle.shutdown` through a pump barrier.
3. Pump journals lifecycle and all resulting outputs.
4. Runtime cancels/awaits effects within configured bounds.
5. Journal performs required flush/checkpoint.
6. Pump completes.
7. JSON-RPC/outbound writer completes.
8. Multiplexer marks STS2 stopped and forwards legacy shutdown.
9. Remaining resources are disposed idempotently.

Hard process exit may interrupt after any step; the journal guarantees only the durability class documented for the last completed flush point.

## 6. Transport and multiplexer

### 6.1 Framing

The real channel uses Content-Length framing. Multiplexer MUST:

- accept configured whitespace and optional headers;
- reject duplicate/conflicting Content-Length;
- use checked arithmetic;
- enforce maximum header and payload sizes;
- handle partial/coalesced reads;
- reject ambiguous duplicate top-level routing fields;
- never write unframed stdout bytes.

Malformed input follows a documented policy:

- a single malformed legacy-looking frame may be forwarded only if exact frame boundaries are safely known;
- otherwise the transport is closed or degraded to legacy with STS2 marked unavailable;
- an attacker cannot silently switch routing modes for the rest of the session.

### 6.2 Routing

- `v2/*` method -> STS2 virtual input.
- `shutdown` -> lifecycle barrier to STS2, then raw frame to legacy only.
- `exit` -> lifecycle barrier to STS2, then raw frame to legacy.
- other method -> legacy.
- response without method -> route via rewritten public ID.
- unknown response ID -> legacy plus diagnostic.

### 6.3 Outbound IDs

Every service-initiated request ID is replaced by an opaque mux ID. The table stores:

`publicId -> service, original JSON token, createdAt, expiry`.

Entries are removed on response, timeout, service death, or shutdown. Restoration preserves the original JSON ID token.

### 6.4 Failure containment

A fatal STS2 transition:

1. freezes a redacted fatal descriptor;
2. attempts an emergency journal record/flush if the journal is usable;
3. completes all pending v2 requests with `Sts2.Unavailable`;
4. emits at most one `v2/fatal` notification with stable code and safe details;
5. closes STS2 virtual input/output;
6. leaves legacy channels running.

The mux does not expose raw exception text.

## 7. Gateway, validation, and correlation

### 7.1 Public DTO validation

Hosting deserializes into versioned DTOs and validates:

- required fields;
- allowed types;
- bounds;
- duplicate JSON properties;
- unknown `mustUnderstand_` fields;
- identifier syntax/length;
- numeric range;
- capture-policy request constraints.

Invalid public input becomes a JSON-RPC error before sensitive tokenization when possible.

### 7.2 Secret leases

Tokenization returns an owned `SecretLease`:

```csharp
public sealed class SecretLease : IAsyncDisposable
{
    public IReadOnlyDictionary<string, SecretRef> References { get; }
    internal bool TransferToEffect(string effectId);
}
```

The request path disposes the lease unless ownership is transferred to a scheduled open effect. The effect removes/zeroes material after open completes. Session disposal clears every remaining lease.

Tokens are random opaque identifiers or HMACs under a per-run in-memory key. They contain no raw secret hash prefix.

### 7.3 Correlation

- External requests preserve the JSON-RPC ID in `corr`.
- Notifications receive a generated deterministic correlation ID where needed.
- Effects have unique effect IDs.
- Streaming observations have unique event IDs and carry `cause` to the effect or cancellation action that produced them.
- `sessionId` or typed `entityRefs` identify connection/query without parsing payload.

## 8. Pump transaction model

### 8.1 Pump input

```csharp
internal sealed record PumpInput(
    InputKind Kind,
    PendingEnvelope Envelope,
    PumpBarrier? Barrier = null);
```

The bounded channel has one reader. All external observations, lifecycle requests, timers, and control changes enter it.

### 8.2 One input turn

For one input:

1. validate Runtime preconditions and capture policy;
2. assign seq, timestamp, run ID, config version, entity refs;
3. create a turn-scoped capture context;
4. elide sensitive fields according to effective mode;
5. compute canonical payload digest;
6. append the input envelope;
7. publish nonblocking references to observer mailboxes;
8. call Core;
9. commit Core state;
10. for each output in order:
    - encode deterministically;
    - append output envelope;
    - publish observer reference;
    - enqueue ordered RPC output or start effect;
11. emit optional metric sample caused by this input;
12. execute requested barrier action, such as flush/rotate/checkpoint;
13. dispose turn capture context;
14. complete the input/barrier result.

If a pre-commit journal append fails, Core is not invoked. If an output append fails after Core state changed, the session becomes fatal because authoritative history can no longer represent the in-memory state.

### 8.3 Pump barriers

Barriers support:

- lifecycle flush;
- coherent export snapshot;
- observer checkpoint;
- graceful stop;
- explicit journal checkpoint.

A barrier completion returns:

`lastCommittedSeq`, `journalCheckpoint`, `outcome`.

## 9. Envelope and trace model

### 9.1 Required fields

```json
{
  "schema": "sts2.envelope/2",
  "runId": "opaque-run-id",
  "seq": 412,
  "ts": "2026-06-18T15:00:00.0000000Z",
  "kind": "rpc.in.request",
  "type": "v2/query.execute",
  "corr": "r-91",
  "cause": null,
  "entityRefs": {
    "connectionId": "c-7",
    "queryId": null
  },
  "configVersion": 3,
  "digest": "sha256:...",
  "payload": {},
  "payloadMeta": {
    "capture": "digest",
    "classifications": []
  }
}
```

### 9.2 Root and cause rules

Root envelopes are restricted to:

- external RPC input;
- `session.start`;
- process-originated lifecycle root when no client frame exists.

Every produced output, effect response, metric sample, timer, config change, and diagnostic has a cause less than its seq. Cause rules are schema-validated.

### 9.3 Canonicalization

Canonical JSON:

- UTF-8;
- ordinal object-key ordering;
- deterministic string escaping;
- preserved number tokens if wire-faithful policy remains accepted;
- rejected duplicate keys;
- explicit nulls where schema requires;
- versioned marker semantics.

Changing canonicalization requires a new envelope schema and migration/tool support, not an in-place change.

### 9.4 Payload integrity

Strict readers recompute every payload digest. A redacted wrapper has a versioned digest domain such as:

`sha256("sts2-redacted/v1" || classification || canonical-original-bytes)`

so typed domains cannot collide.

## 10. Core state

### 10.1 CoreState

Core contains only deterministic serializable data:

- service/config/capability snapshot;
- connections;
- open request index;
- queries;
- deterministic counters/IDs;
- shutdown state;
- pending request correlations;
- timer/deadline IDs, not clocks;
- result-set/page protocol state.

No secret, SQL text in disallowed mode, row cell, live handle, Task, token, or runtime queue state enters Core.

### 10.2 Connection state

Recommended phases:

```text
Opening
Open
ClosePendingQuery
Closing
Closed (usually removed)
Failed (transient decision, then removed)
```

Connection fields include:

- `openId`, `openCorr`;
- `connectionId`;
- `phase`;
- `handleId` token;
- `activeQueryId`;
- `closeWaiters` or one primary waiter plus idempotent duplicate policy;
- `cancelRequested`;
- `deadlineId`.

#### Open

- Duplicate live `openId` -> `InvalidRequest`.
- Limit exceeded -> `Busy`.
- Open effect success -> result and Open.
- Open error/cancel -> one error, remove state.
- Cancel request -> immediate `{}`, effect cancel, original open later terminates.
- Close cannot be requested by an unknown connection ID; in-flight open is canceled by `openId`.

#### Close

- Unknown/already closed -> `{}`.
- Open/no query -> Closing + close effect.
- Active query -> ClosePendingQuery, cancel query, start close deadline.
- Duplicate close -> deterministic idempotent result or registered waiter, never overwritten corr.
- Query terminal/disposed -> Closing.
- Deadline -> force runner cleanup, terminal close result/error per contract.
- Close effect terminal -> answer all waiters exactly once, remove state.

### 10.3 Query state

Recommended phases:

```text
Starting
Running
CancelRequested
Disposing
Completed
Disposed
```

Query fields include:

- query/connection IDs;
- phase;
- execute correlation;
- current result-set ID and whether metadata was seen;
- expected page seq and row offset per result set;
- pages sent/acked;
- credit outstanding;
- terminal status;
- complete sent;
- runner-stop acknowledgement;
- deadline ID.

### 10.4 Query terminality

Preferred contract:

> Every accepted `query.execute` produces exactly one `v2/query.complete`, including cancel, dispose, close-induced cancel, timeout, driver failure, and fatal query termination when transport remains available.

`query.dispose` is a resource command, not a way to erase terminal history. It transitions to Disposing, requests runner stop, and either:
- emits/awaits `query.complete` before returning, or
- returns immediately but still guarantees a later complete.

The exact ordering is pinned in protocol and scenarios.

### 10.5 Query protocol validation

Core validates:

- result-set metadata precedes rows;
- resultSetId progression;
- pageSeq gapless per result set;
- rowOffset monotonic and consistent;
- no messages/rows/result sets after complete;
- exactly one complete;
- rows pages only when credit was granted;
- completion only from active/canceling/disposing phases.

Invalid driver observations become a stable internal/transport terminal plus diagnostic.

## 11. Effects and driver port

### 11.1 Effect lifecycle

An effect request is journaled before runner invocation. Runner registration occurs synchronously on the pump thread, then work starts.

Each effect record tracks:

- effect ID;
- cause seq;
- kind;
- owner entity;
- task;
- cancellation source;
- ownership-transfer state;
- terminal-post state.

No task is untracked.

### 11.2 Session ownership transfer

For open:

1. runner registers operation;
2. driver returns session;
3. runner tentatively owns session;
4. runner posts effect response;
5. only after enqueue success is the session transferred into the live session registry;
6. on post failure, session is disposed.

Alternatively, registry insertion and response posting are wrapped in an ownership object whose shutdown path always disposes unknown-to-Core handles.

### 11.3 Driver contract

`IDbSession` guarantees:

- at most one active `ExecuteAsync`;
- cancellation does not permanently poison later queries;
- events follow the protocol order;
- iterator disposal releases command/reader;
- metadata fields are stable DTOs;
- provider exceptions are classified and sanitized.

A driver may expose optional capabilities such as precise byte paging or provider cancellation.

### 11.4 Provider text

Adapters return:

```csharp
public sealed record DriverFailure(
    string Code,
    string SafeMessage,
    ServerErrorDetail? Server,
    SensitiveDiagnosticRef? DiagnosticRef);
```

Raw provider messages never become wire/journal contract by default.

## 12. Backpressure and paging

### 12.1 Credit meaning

One credit authorizes the Runtime to request and deliver one row page. Credit is acquired **before** the next database page is requested.

### 12.2 Ack model

Ack is scoped by query and result set.

- Per-page ack identifies `pageSeq`.
- High-water ack identifies `throughPageSeq`.
- Values must be nonnegative and not exceed the highest sent page.
- Duplicate/out-of-order acks are idempotent.
- Ack for completed/disposed/unknown query is ignored or returns defined behavior.
- Credit is derived from unique acknowledged sent pages, never incremented blindly.

### 12.3 Page construction

A page ends at the first of:

- `pageRows`;
- `pageBytes`;
- result-set end.

Byte accounting uses final wire encoding, including wrappers. Oversized cells use a truncation wrapper with original byte count, digest, type, and bounded prefix according to capture policy.

### 12.4 Memory bound

At any time, per query Runtime retains at most:

- current page under construction;
- at most configured provider prefetch, ideally zero;
- bounded outbound frame;
- `windowPages` metadata, not duplicate row payloads;
- bounded observer references.

Core retains no row cells.

## 13. Journal design and durability

### 13.1 Run isolation

Each run has its own directory:

```text
sts2/run-<opaque-id>/
  manifest.json
  segments/000001.jsonl
  segments/000002.jsonl
  active.jsonl
  checkpoints/
```

Readers never glob across runs.

### 13.2 Segment integrity

Each closed segment records:

- first/last seq;
- byte count;
- SHA-256;
- previous segment hash;
- schema/config range.

Manifest updates use temp file + flush + atomic replace. A checkpoint identifies the committed active byte length.

### 13.3 Flush classes

Define explicit durability classes:

- **Buffered:** written to process stream buffer.
- **OS-visible:** `FlushAsync`, visible to another process.
- **Disk-requested:** platform flush-to-disk at selected critical points.
- **Checkpointed:** manifest/checkpoint atomically references committed bytes.

Policy:

- request terminal, query.complete, config change, fatal, lifecycle, export barrier -> checkpointed;
- row pages -> OS-visible within configured interval;
- metric/low-value diagnostics -> buffered/interval.

The documentation must not imply power-loss durability unless tested and supported.

### 13.4 Timed flush

A writer-owned periodic task or pump timer enforces the interval even when no later append occurs. It is tracked in session lifetime.

### 13.5 Recovery

On startup/tool read:

1. load last valid atomic manifest;
2. validate closed chain;
3. read active file through last complete newline/checkpoint;
4. classify trailing partial bytes as incomplete tail;
5. never silently merge runs;
6. surface corruption with exact segment/offset.

## 14. Replay and time travel

### 14.1 Strict verify pipeline

`sts2-replay verify`:

1. validate manifest and segment hashes;
2. validate one run/schema;
3. parse strict envelopes;
4. require gapless seq and valid roots/causes;
5. recompute payload digests;
6. validate config-version transitions;
7. replay reducer inputs;
8. compare every expected output by kind, type, corr, entity refs, config version, cause position, and digest;
9. require no missing/extra pending outputs at EOF;
10. report exact first divergence and nearest state diff.

Result states: `Verified`, `Diverged`, `Incomplete`, `Corrupt`, `UnsupportedSchema`.

### 14.2 Partial replay

`until --seq N` may stop with pending outputs. It returns:

- `Partial`;
- state at last fully applied input;
- whether N cut through an input/output group;
- pending expected outputs;
- last validated checkpoint.

It never reports `Identical`.

### 14.3 Replay identity

The identity contract is:

> For a valid complete authoritative journal and the same Core/contract version, strict replay produces the same ordered authoritative output envelopes under the defined comparison fields.

It does not claim identical timestamps, live overlays, transport delivery, database effects, or raw sensitive values.

### 14.4 Versioning

Replay tools support a bounded set of envelope/Core versions. Migrations are explicit pure transforms with tests and provenance. Unsupported versions fail clearly.

## 15. Capture, privacy, and security

### 15.1 Host policy

```csharp
public sealed record CapturePolicy
{
    public required CaptureMode MaxRowCapture { get; init; }
    public required SqlCaptureMode MaxSqlCapture { get; init; }
    public bool AllowRuntimeElevation { get; init; }
    public TimeSpan MaxElevationDuration { get; init; }
    public long MaxFullCaptureBytes { get; init; }
}
```

Product default: digest/digest, no runtime elevation.

### 15.2 Runtime change

A permitted `setCapture` request includes reason and optional duration. Core deterministically records effective mode/configVersion. Runtime schedules automatic reversion as a journaled timer. The change applies at a precisely defined sequence boundary returned to the client.

### 15.3 Turn-scoped restoration

Sensitive originals live only in a turn context:

```text
input original -> classify/elide -> journal/reducer
                              -> output restoration token
turn completes -> context destroyed
```

No session-wide digest-to-original dictionary is used.

### 15.4 Observer data access

Observers register a declared view:

- metadata only;
- redacted authoritative envelope;
- governed full wire view, disabled in product by default.

Custom observers never automatically receive restored SQL/rows or secret material.

### 15.5 Logs and fatal details

Diagnostic logs use structured safe fields. Raw exception stacks are optional, separately protected, and scrubbed. Fatal wire messages expose stable code, component, journal checkpoint, and safe reason only.

## 16. Observer framework

### 16.1 Mailbox isolation

Coordinator publishes an immutable envelope reference to each observer mailbox with `TryWrite`. It never awaits observer code.

Each observer worker:

- preserves seq order;
- has bounded capacity;
- defines drop policy;
- records dropped range;
- catches faults;
- may be disabled after threshold;
- is tracked/disposed by session lifetime.

### 16.2 Live tail checkpoint protocol

A subscription exposes:

- run ID;
- first available seq;
- last delivered seq;
- dropped count;
- dropped-from/dropped-through seq;
- current journal checkpoint.

On gap, viewer reads the exact range from the journal and resumes without guesswork.

### 16.3 Metrics

Metrics distinguish:

- session totals;
- bounded recent window;
- current gauges;
- monotonic counters.

Metric envelopes carry cause and are schema-versioned. EventSource counters are process-wide and named accordingly.

## 17. Diagnostics and state

### 17.1 Health

Pure section:

- latest committed seq;
- connection/query counts;
- unacked pages;
- shutdown phase;
- config version as Core state.

Runtime overlay:

- inbox/outbound/effect/observer queue depths;
- session/open/query lease counts;
- current fatal descriptor;
- observer drops/faults;
- delivery failures;
- current journal checkpoint;
- totals and recent error window.

After coordinator fatal, Bootstrap/mux can still return a synthesized unavailable error containing the frozen safe fatal summary.

### 17.2 State

Core state dump is canonical, ordered, redacted, and versioned. Live overlay is under a clearly marked `runtime` object. Tools can request `authoritativeOnly=true`.

State never includes openId if policy considers it sensitive without classification; no SQL, row, secret, raw provider text, or live object identity.

## 18. Export design

### 18.1 Snapshot protocol

1. client requests export;
2. Core accepts/rejects and emits export effect;
3. effect asks coordinator for an export barrier;
4. pump finishes prior work, flushes, rotates active segment, writes atomic manifest/checkpoint;
5. exporter receives immutable exact-run file inventory;
6. exporter applies policy transformations;
7. exporter writes bundle to temp, verifies, then atomically renames;
8. effect response returns path/hash/size/checkpoint.

### 18.2 Bundle

```text
manifest.json
privacy-report.json
provenance.json
journals/
schemas/
status/
optional-generated-docs/
```

Manifest includes exact file hashes, run ID, seq range, schema/Core/service versions, source commit, capture policy, export tool version, and parent segment chain.

### 18.3 Export check

`export-check` validates:

- zip traversal/safety;
- manifest/schema;
- every hash;
- one exact run;
- privacy policy/content scan;
- strict replay;
- state dump;
- generated/status inventory;
- provenance.

## 19. Configuration and versioning

### 19.1 Sources and precedence

Defaults < environment < command line < initialize negotiation < permitted runtime change.

Only safe allowlisted startup fields enter manifest. Effective startup config is journaled in `session.start`.

### 19.2 Protocol version

- Major: incompatible method/schema/semantics.
- Minor: additive optional fields/capabilities.
- Preview identifier: explicitly unstable behavior.
- Envelope schema and protocol version are separate.
- Capability values are generated from composition, not hard-coded independently.

### 19.3 Unknown fields

Unknown fields are ignored unless `mustUnderstand_`. Duplicate known fields are invalid. New enum values are capability/version governed.

## 20. Error and fatal model

### 20.1 Stable errors

Wire errors contain:

- numeric JSON-RPC code;
- stable `Sts2.*` data code;
- safe message;
- retryable;
- correlation;
- optional structured server detail;
- optional diagnostic reference.

Raw exception message is not contract.

### 20.2 Fatal descriptor

```json
{
  "code": "Sts2.Fatal.JournalWrite",
  "component": "journal",
  "safeReason": "The authoritative journal could not be updated.",
  "lastCommittedSeq": 411,
  "checkpoint": "sha256:...",
  "retryable": false
}
```

Fatal handling is idempotent and tested under recursive failure, including journal unavailable.

## 21. Performance and resource budgets

Track and gate:

- coordinator p50/p95/p99 turn latency;
- rows/sec and bytes/sec;
- allocation bytes/row;
- maximum retained row bytes/query;
- inbox/outbound/observer capacities;
- journal overhead;
- flush latency;
- cancel/close latency;
- export pause and memory;
- session cleanup time.

Throughput gates do not replace correctness bounds. SQLite contract tests must also stream without whole-result buffering.

## 22. Verification model

### 22.1 Requirement ownership

Every normative requirement maps to:

- implementation owner;
- unit/property/scenario/E2E tests;
- invariant where applicable;
- negative test/mutant;
- latest CI evidence.

### 22.2 Required suites

- reducer transition/property tests;
- strict ingress schema fuzzing;
- multiplexer framing/ID/lifecycle properties;
- scenario corpus;
- Fake/SQLite/SQL Server contract matrix;
- strict replay corruption corpus;
- simulator with deterministic repro;
- resource/fatal/lifecycle matrix;
- privacy corpus;
- export round trip;
- runnable client interop;
- mutation tests;
- performance/allocation tests;
- disabled/enabled v1+v2 E2E.

### 22.3 CI provenance

Final evidence records exact:

- commit and merge base;
- SDK/dependency lock;
- OS/runtime;
- SQL Server image digest/version;
- test counts and result files;
- simulator seed algorithm/range/count;
- mutation report hashes;
- perf machine class;
- generated-doc hashes;
- branch-behind count.

The required workflow runs on PRs targeting `main` and is branch-protected.

## 23. Rollout

### 23.1 Stages

1. internal opt-in with Fake/SQLite diagnostics;
2. extension experimental flag and shadow diagnostics;
3. SQL Server connectivity/query preview for selected clients;
4. monitored percentage rollout;
5. default-on for supported client versions;
6. legacy retirement only after separate feature migration.

### 23.2 Rollback

STS2 remains default-off until preview exit. A client can fall back to v1 after `Sts2.Unavailable` before creating v2-only state. Server feature flag and environment kill switch disable STS2 on next process launch. No on-the-fly switch routes an active v2 connection into v1.

### 23.3 Compatibility telemetry

Collect privacy-safe counts for:

- initialize versions/capabilities;
- open/query success by stable code;
- cancel/close latency;
- backpressure stalls;
- fatal component;
- journal/export failures;
- observer drops;
- v2 fallback.

## 24. Required ADRs before preview

1. Query dispose terminality and ordering.
2. Durability classes and power-loss guarantee.
3. Capture elevation policy.
4. Provider/server message classification.
5. Canonical number-token policy and duplicate-key handling.
6. Timer representation for bounded close/cancel/reversion.
7. Export bundle content and generated-doc strategy.
8. Observer data-access and isolation contract.
9. Replay compatibility/version support window.
10. SQLite adapter role and bounded-streaming requirement.

## 25. Conformance checklist

A build conforms to this design only when:

- one composite session completion controls mux fatal state;
- lifecycle and export use pump barriers;
- all live work is owned and awaited;
- strict replay rejects every corruption class;
- readers/export are exact-run;
- dispose contract matches I2 and client docs;
- backpressure prevents driver advancement without credit;
- product capture cannot be elevated by an unprivileged client;
- provider text is classified/redacted;
- custom observers cannot stall the pump;
- every terminal/durability point is tested;
- final CI is attached to the exact PR merge candidate.

This target preserves the branch's best idea: the journal is not a debugging accessory, it is the spine. The hardening above makes that spine trustworthy when the process is slow, failing, shutting down, or handling sensitive data.
