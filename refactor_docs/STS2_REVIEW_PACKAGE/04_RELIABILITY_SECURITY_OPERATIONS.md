# STS2 Reliability, Security, Operations, and Adoption Design

This document complements `03_TARGET_DESIGN.md`. It defines how STS2 behaves when dependencies are slow, malformed, malicious, or unavailable, and how the service moves from an opt-in branch to a supportable product capability.

## 1. Reliability model

### 1.1 Failure domains

| Domain | Examples | Local response | Cross-boundary response |
|---|---|---|---|
| Client/transport | malformed frame, closed pipe, slow reader | reject/close/bound | legacy remains isolated where possible |
| Multiplexer | parser fault, stdout failure | process transport fatal | both services may be unable to communicate |
| STS2 gateway | formatter/listener failure | STS2 fatal | mux synthesizes unavailable |
| Coordinator | journal/core/encoder failure | STS2 fatal | legacy continues |
| Journal | disk full, permission, corruption | fatal before unrecorded dispatch | safe fatal descriptor |
| Observer | slow/faulty viewer/sink | drop/disable observer | pump continues |
| Driver open | auth/network/timeout | stable request error | no leaked secret/session |
| Query stream | provider error/cancel/hang | one terminal completion | connection cleanup/close policy |
| Export | I/O/policy/replay failure | export request error | active session continues |
| Shutdown | provider ignores cancel, disk slow | bounded cleanup + checkpoint | legacy shutdown proceeds after bound |

### 1.2 Reliability invariants

Add these to the existing invariant set:

- **I17 Session failure propagation:** any required child-task failure transitions the session exactly once to fatal or stopped.
- **I18 Pending request drain:** every accepted request is completed, canceled by transport close, or failed with unavailable during fatal shutdown.
- **I19 Resource ownership:** every live session, query task, open task, CTS, semaphore, observer worker, secret lease, and capture context has one owner and reaches a terminal state.
- **I20 Barrier durability:** a successful lifecycle/export/checkpoint barrier covers every causally prior envelope and output.
- **I21 Exact-run isolation:** a reader, replay, invariant check, or export never combines run IDs.
- **I22 Replay completeness:** strict verification fails on missing or extra expected outputs at EOF.
- **I23 Observer isolation:** no auxiliary observer can block the coordinator.
- **I24 Driver advancement bound:** driver page advancement never exceeds granted credit.
- **I25 Capture-policy monotonicity:** effective capture never exceeds host policy.
- **I26 Export consistency:** every exported segment and manifest belongs to one checkpointed run and passes strict replay.
- **I27 No sensitive diagnostic bypass:** provider/server text follows the same classification policy as SQL and rows.
- **I28 Terminal close bound:** close/cancel/dispose requests terminate within configured policy even when a driver hangs.

### 1.3 Timeouts and deadlines

Timeouts that influence domain behavior must be explicit, identified, and replayable.

| Deadline | Default | Owner | Journal representation | Terminal behavior |
|---|---:|---|---|---|
| open timeout | 15 s | Runtime timer/effect | `timer.due open:<id>` | ConnectionFailed.Timeout |
| provider cancel timeout | 2 s proposed | Runtime | `timer.due cancel:<query>` | force local pump stop |
| close timeout | 5 s | Core + Runtime timer | `timer.due close:<connection>` | close terminal + forced cleanup |
| lifecycle flush | 500 ms | Multiplexer/session | barrier outcome diagnostic | forward legacy shutdown after bound |
| observer shutdown | 250 ms proposed | session | metric/diagnostic only | abandon observer |
| export snapshot | 5 s proposed | coordinator | barrier outcome | export error, session remains |
| outbound write | bounded by transport close | outbound writer | fatal diagnostic | STS2 fatal |

Wall-clock observations enter as recorded timer envelopes. Replay injects the recorded timers and does not wait.

## 2. Fatal-state protocol

### 2.1 State machine

```text
Running
  -> Stopping       orderly lifecycle
  -> Fatalizing     first unexpected required-component failure
Stopping
  -> Stopped
  -> Fatalizing     failure before stop is committed
Fatalizing
  -> Fatal          frozen descriptor, pending requests failed
Fatal
  -> Disposed
Stopped
  -> Disposed
```

The first fatal cause wins. Later failures are attached as suppressed diagnostics and never replace the externally visible descriptor.

### 2.2 Fatal algorithm

1. `Interlocked.CompareExchange` the session state to Fatalizing.
2. Stop accepting v2 input.
3. Complete the coordinator inbox writer.
4. Cancel/await owned work within bounds.
5. Fail all pending request completions.
6. If journal is usable, append `diag:fatal` through an emergency append path and checkpoint.
7. Freeze `FatalDescriptor`.
8. Notify mux; mux emits one redacted `v2/fatal` if possible.
9. Future v2 requests receive synthesized `Sts2.Unavailable`.
10. Legacy virtual streams remain open.

### 2.3 Recursive failure

If the journal caused the fatal error, do not retry normal append recursively. Write only to:
- an already-open emergency file descriptor if available;
- stderr as a last resort, never stdout;
- the mux's in-memory fatal snapshot.

The absence of a fatal journal entry must not prevent containment.

## 3. Resource-lifetime protocol

### 3.1 Owned operation record

```csharp
internal sealed class OwnedOperation
{
    public required string Id { get; init; }
    public required Task Task { get; init; }
    public required CancellationTokenSource Cancellation { get; init; }
    public required OperationKind Kind { get; init; }
    public required long CauseSeq { get; init; }
}
```

Runner dictionaries store operation objects, not only sessions/CTS. Removal happens after task completion and terminal observation posting.

### 3.2 Query pump disposal

1. mark query pump stopping;
2. cancel local iterator token immediately;
3. invoke provider `CancelAsync` under a separate bounded token;
4. await iterator task/finally;
5. dispose command/reader resources;
6. post `driver.queryStopped`;
7. Core emits/records terminal semantics and releases connection;
8. dispose semaphore/CTS and remove operation.

No new query starts on that connection before step 7.

### 3.3 Open ownership

A successfully returned provider session is either:
- transferred to the live registry and acknowledged to Core; or
- disposed by the open operation.

There is no state where a session exists but neither Runtime nor Core can identify it.

### 3.4 Secret lifecycle

Secret memory is scoped to:
- request validation;
- a transferred open effect;
- no longer than the open attempt.

All invalid/rejected/duplicate/limit/fatal paths dispose the lease. Tests inspect counts and canary memory-owning objects through test-only introspection, not product backdoors.

## 4. Security and privacy threat model

### 4.1 Assets

- database credentials and access tokens;
- SQL text;
- result cells;
- server/provider messages;
- server/database/user identifiers;
- journal/export integrity;
- v1 transport availability;
- operator trust in replay evidence.

### 4.2 Adversaries/failures

- malformed or buggy client;
- compromised extension able to call diagnostics;
- low-privilege support recipient of an export;
- malicious database text in errors/messages;
- disk corruption or manual journal editing;
- slow/faulty observer;
- accidental raw stdout logging;
- dictionary attack against secret-derived tokens;
- mixed-run export caused by shared directories.

### 4.3 Required controls

1. Public schema validation before internal marker creation.
2. Host capture policy with deny-by-default elevation.
3. Random/HMAC secret tokens.
4. Safe provider-message classifier.
5. Exact-run directories and manifests.
6. Strict digest/sequence/cause/replay validation.
7. Observer redacted-view contract.
8. No raw stdout static gate.
9. Export content policy and scan.
10. Immutable CI provenance.

### 4.4 Privacy report

The export privacy report is generated from actual transformations, not fixed prose. It includes:

- source capture modes and config-version ranges;
- fields transformed by classification;
- counts/bytes elided;
- secret-token count;
- SQL/row/provider-text policy;
- scanner version and signatures;
- scanner findings;
- whether full-capture content existed;
- exact run/checkpoint;
- pass/fail.

## 5. Operational SLOs and budgets

Initial preview targets should be conservative and measured on representative CI hardware.

| Metric | Preview target |
|---|---:|
| initialize p95 | < 50 ms excluding process startup |
| accepted execute response p95 | < 50 ms before driver work |
| coordinator turn p99 excluding journal flush | < 10 ms |
| terminal flush p95 | < 100 ms local SSD |
| row throughput digest mode | >= 50k rows/s baseline gate |
| allocation | explicit bytes/row baseline, ratchet down |
| max row memory/query | page under construction + configured window metadata |
| cancel p95 cooperative driver | < 500 ms |
| close hard bound | <= configured 5 s + small scheduling margin |
| shutdown barrier | <= 500 ms default, outcome reported |
| observer publish | O(number of observers), nonblocking |
| fatal containment | pending/future v2 terminated within 1 s |
| resource cleanup | zero owned operations after stop bound |
| replay verify | linear in journal bytes with bounded memory |
| export pause | barrier < 250 ms target; copying happens after rotation |

SLO violations are metrics and diagnostics, not reasons to weaken correctness.

## 6. Observability model

### 6.1 Metrics taxonomy

**Gauges**
- inbox depth;
- outbound depth;
- observer mailbox depth by sink;
- open sessions;
- opens in flight;
- active query pumps;
- secret leases;
- capture contexts.

**Counters**
- envelopes by kind/type;
- request outcomes by stable code;
- observer drops/faults/disabled;
- output delivery failures;
- journal bytes/flushes/rotations;
- replay/export failures;
- forced cancel/close cleanups;
- fatal transitions.

**Histograms**
- coordinator turn latency;
- journal append/flush latency;
- query page bytes/rows;
- cancel/close latency;
- export barrier/copy/check latency.

No metric label includes SQL, IDs with unbounded cardinality, secrets, or raw messages.

### 6.2 Health semantics

Use exact names:
- `errorsByCodeTotal`;
- `errorsByCodeRecentWindow`;
- `lastFatal`;
- `lastCommittedSeq`;
- `lastCheckpointSeq`;
- `oldestObserverSeq`;
- `capturePolicy`;
- `effectiveCapture`.

Avoid adjectives such as “recent” without a defined window.

### 6.3 Support workflow

1. Ask for `diagnostics.health`.
2. If live tail has no gap, inspect current cause chain.
3. If gap exists, note exact missing seq range and read journal.
4. Export at a checkpoint.
5. Run `export-check`.
6. Run strict replay.
7. Use `explain --seq` and state diff.
8. Attach bundle hash and tool version to issue.

## 7. Deployment and feature management

### 7.1 Activation

- Disabled remains the default through preview.
- `--enable-sts2` and `STS_ENABLE_STS2=1` use one shared parser.
- Unknown/malformed activation args never reach legacy's raw stdout usage path.
- A future explicit disable flag wins over environment.
- Activation state is recorded without raw unknown arguments.

### 7.2 Client capability handshake

Initialize advertises capabilities generated from composition and host policy:

```json
{
  "capabilities": {
    "forwardOnlyStreaming": true,
    "oneActiveQueryPerConnection": true,
    "redactedReplay": true,
    "exportLog": true,
    "runtimeCaptureChange": false,
    "strictReplaySchema": "sts2.envelope/2"
  }
}
```

A method may be registered internally but unavailable by policy; capability and error behavior must agree.

### 7.3 Rollout gates

#### Gate A: engineering preview
- all Blocker findings closed;
- zero release-critical scenario stubs;
- exact-run strict replay/export;
- fatal/lifecycle/resource matrix green;
- PR-to-main CI attached.

#### Gate B: extension experimental
- runnable TypeScript client;
- telemetry dashboards;
- support runbook;
- rollback tested;
- no raw sensitive captures by default.

#### Gate C: limited user preview
- error/fatal rates within target;
- cancel/close p95 within bound;
- no v1 regressions;
- export privacy audit;
- compatibility across supported SQL Server versions.

#### Gate D: broader rollout
- multi-version client compatibility;
- sustained performance;
- incident/support feedback;
- ADRs stable;
- upgrade/replay compatibility demonstrated.

## 8. Compatibility and migration

### 8.1 v1 coexistence

The mux is the only shared runtime component. No v2 state is inferred from v1 traffic. Lifecycle is mirrored, not duplicated. A v2 fatal does not alter legacy registrations or static state.

### 8.2 Client fallback

Fallback is allowed only before a v2 connection/query is accepted. A client that receives `Sts2.Unavailable` may start a new v1 workflow; it does not migrate an active session.

### 8.3 Journal/tool compatibility

Export bundles include the replay tool/schema compatibility range. Support tooling refuses unsupported journals rather than approximating silently.

## 9. Incident readiness

Before preview, exercise game days:

1. disk fills during input append;
2. disk fills after Core decision before output append;
3. SQL Server ignores cancel;
4. client stops reading stdout;
5. observer blocks forever;
6. active segment is truncated;
7. manifest is partially replaced;
8. process exits during lifecycle barrier;
9. provider error embeds SQL/password canary;
10. two runs share a parent directory;
11. replayer version is older than schema;
12. mainline rebase changes SDK/StreamJsonRpc behavior.

Each game day produces:
- expected state transition;
- expected client behavior;
- expected journal/export evidence;
- cleanup assertion;
- legacy continuity assertion;
- runbook update.

## 10. Operational definition of done

STS2 is operationally ready for preview when a support engineer can answer, from a safe bundle:

- what request was accepted;
- what state transition occurred;
- what effect was requested;
- what external observation returned;
- why a cancel/close/fatal decision happened;
- whether output was journaled and whether delivery succeeded;
- whether any gap/corruption exists;
- which capture policy applied;
- whether all resources were cleaned;
- which exact code/tool/environment produced the evidence.

That is the practical meaning of “a service that explains itself while it runs.”
