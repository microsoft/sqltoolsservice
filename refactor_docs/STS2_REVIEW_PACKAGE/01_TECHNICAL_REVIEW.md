# STS2 Refactor: Detailed Technical and Code Review

**Repository:** `microsoft/sqltoolsservice`  
**Reviewed branch:** `sts2/main`  
**Reviewed head:** [`c9fbd1e40ec8`](https://github.com/microsoft/sqltoolsservice/commit/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849)  
**Comparison target:** `main`  
**Review date:** 2026-06-18  
**Disposition:** **Not ready to tag or merge as a preview yet.** The architectural direction is excellent, but the reviewed snapshot has release-blocking correctness, durability, resource-lifetime, privacy, replay, and CI-evidence gaps.

## 1. Review scope and confidence

This review covers:

- the pending branch relative to `main`;
- the STS2 product projects, replay/export tooling, tests, workflows, scripts, and the small legacy seam;
- all supplied design and generated-review documents;
- the supplied TeX/PDF diagrams;
- cross-checks between documented invariants and the implementation.

The branch was **51 commits ahead and 5 commits behind `main`** at review time. Its merge base was `774988239b32c7942ada76d3f77527829df8e181`. The reviewed branch still pins SDK `10.0.203`, while current `main` pins `10.0.301`, so all evidence must be regenerated after rebasing.

This was a read-only review. I inspected the branch through GitHub and the attached source/design artifacts. I did **not** independently compile or execute the branch in this environment. The branch contains a strong self-reported verification ledger, but the reviewed head had no attached GitHub workflow runs or combined status checks visible through the connector. Findings therefore distinguish code-proven facts from verification claims.

## 2. Executive assessment

STS2 is a rare refactor with an actual architectural thesis rather than a folder shuffle: the pure reducer, journal-first processing, effect boundary, side-by-side transport migration, and executable invariants form a coherent system. The tiny legacy seam is especially disciplined. The branch also demonstrates unusually good engineering hygiene through generated docs, simulator seeds, mutation testing, engine tests, public API tracking, and explicit decisions.

The weak points sit where deterministic architecture meets live process reality:

1. **The fatal and shutdown paths are not yet transactionally connected to the pump.**
2. **Resource ownership is incomplete across coordinator, effect tasks, sessions, secrets, and elided fragments.**
3. **Replay and export are less strict than the design claims.**
4. **Backpressure and dispose semantics do not fully enforce the public contract at the actual driver edge.**
5. **Sensitive capture and server/provider messages need a stronger policy boundary.**
6. **The release workflow and documentation overstate the evidence attached to the merge candidate.**

The right next move is not a large rewrite. It is a focused hardening pass that turns the current architectural promises into mechanically enforced contracts.

## 3. What is notably good

### 3.1 The build graph is a real safety boundary

Core and Contracts remain BCL-only, drivers sit behind `IDbDriver`/`IDbSession`, Hosting composes rather than decides, and the legacy executable references only Bootstrap. This is exactly the kind of refactor boundary that survives future team churn.

### 3.2 The pure reducer is the right center of gravity

Connection/query lifecycle, idempotency, backpressure credit, and configuration versioning are explicit state. Deterministic IDs and immutable sorted maps make state/output comparison practical rather than aspirational.

### 3.3 Journal-before-dispatch is a powerful debugging primitive

The envelope, sequence, correlation, cause, digest, and config version give production failures a durable shape. Sharing `CoreOutputEncoder` between live execution and replay is a particularly good choice.

### 3.4 The side-by-side migration seam is disciplined

The multiplexer lets v1 and v2 coexist on one transport, protects outbound request IDs, owns the single stdout writer, mirrors lifecycle, and keeps the legacy diff tiny. This makes staged adoption feasible.

### 3.5 Runtime overlays preserve the pure/replay boundary

Keeping queue depth, driver leases, and other live facts out of the authoritative journaled result is conceptually clean. The same basic pattern is useful for health, state, and sensitive-value restoration.

### 3.6 Observability is modeled as data flow, not logging calls

`IEnvelopeSink`, metrics, EventSource, and live tail all observe the same recorded stream. That is a much stronger foundation for a viewer and support tooling than ad hoc logging.

### 3.7 The test strategy has unusual breadth

Scenario transcripts, a seeded simulator, SQLite contract tests, SQL Server engine tests, replay verification, mutation testing, E2E transport tests, generated-doc checks, API checks, and secret canaries are the right ingredients.

### 3.8 Decisions and deviations are visible

The repo records repo facts, two-way choices, and implementation deviations. That makes the branch inspectable and gives future maintainers a trail of intent.

## 4. Release recommendation

Do not create `sts2-v2.0.0-preview` or merge to `main` until:

- all findings **STS2-R001 through STS2-R019** are closed or explicitly accepted through reviewed ADRs;
- strict replay and exact-run export pass adversarial corruption tests;
- shutdown/fatal/resource cleanup are proven by spawned-process tests;
- the query-dispose/I2 contract is resolved;
- the branch is rebased onto current `main`;
- the final commit has immutable CI evidence attached to a PR targeting `main`;
- release-critical scenario stubs are active.

A preview may deliberately carry feature gaps. It should not carry ambiguous terminality, potentially leaked credentials/sessions, a false-positive replay verifier, or a CI workflow that does not gate the final merge.

## 5. Most important design corrections

### 5.1 Make the pump operation atomic at its boundaries

Introduce a pump input abstraction with completion:

```csharp
internal sealed record PumpInput(
    PendingEnvelope Envelope,
    PumpCompletion? Completion);

internal sealed class PumpCompletion
{
    public TaskCompletionSource<PumpOutcome> Source { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
```

Lifecycle, export snapshot, capture-policy changes, and orderly shutdown should all use the same pump barrier. A successful barrier means every causally prior input and output has been journaled, the requested durability action has completed, and the caller has a stable sequence number.

### 5.2 Give the session one ownership tree

The intended ownership should be explicit:

```text
Sts2BootstrapHandle
  owns StdioMultiplexer
  owns Sts2Session
        owns JsonRpc/outbound writer
        owns Coordinator
              owns JournalWriter
              owns auxiliary observer mailboxes
              owns DriverEffectRunner
                    owns open tasks, query tasks, sessions, CTS/semaphores
        owns SecretStore
```

Disposal must walk that tree in a documented order and leave no task or secret behind.

### 5.3 Split strict verification from time-travel replay

`replay until` is intentionally partial. `replay verify` is forensic validation. They should not share an `Identical=true` terminal state. Strict verification must validate the journal itself before comparing reducer outputs.

### 5.4 Put sensitive capture behind a host policy

Core may own the replay-visible current mode, but Runtime/Bootstrap must own what modes are permitted. The reducer should receive an allowed-mode set in `session.start` and reject forbidden transitions deterministically.

### 5.5 Move credit in front of database advancement

The semantic promise is not merely “no more than four notifications outstanding.” It is “do not ask the driver for page five until page credit exists.” The port and runner need to enforce that exact promise.

## 6. Component-by-component review

### 6.1 Multiplexer and Bootstrap

**Strengths:** BCL-only implementation, exact ID restoration, single real stdout owner, side-by-side routing, and lifecycle awareness are all good.

**Required work:** explicitly handle oversized outbound frames; align activation parsing; eliminate every raw stdout fallback; treat normal unexpected STS2 completion as death; connect coordinator failure to the mux; harden duplicate framing headers; scrub fatal reasons and command-line metadata.

### 6.2 Hosting and Coordinator

**Strengths:** thin RPC methods, redaction before envelopes, one bounded input queue, write-ahead ordering, shared output encoder, and pure/live response separation.

**Required work:** composite session lifetime, pending-request failure, pump barriers, awaitable bounded outbound delivery, observer mailboxes, turn-scoped elision, effect-runner ownership, and a defined durability policy.

### 6.3 Core

**Strengths:** immutable state, deterministic identifiers, explicit phases, stable errors, idempotency, and replay-visible capture config.

**Required work:** resolve dispose terminality, preserve duplicate close request terminality, validate numeric ranges, track result-set/page state, make ack semantics exact, represent closing/disposal deadlines, and separate public JSON DTOs from internal captured values.

### 6.4 Effect runner and drivers

**Strengths:** synchronous registration before async execution removes an important race class; driver abstraction is clean; SQL Server type mapping is headed in the right direction.

**Required work:** track and await tasks; two-phase session ownership; bounded cancellation; no concurrent commands after dispose; pre-MoveNext credit; per-query SQLite cancellation; true streaming SQLite pages; page byte limits; cell truncation; query options; safe provider messages; complete metadata.

### 6.5 Journal, replay, and export

**Strengths:** canonical payload hashing, append-only JSONL, segment hashes, cause links, and no effect re-execution are strong foundations.

**Required work:** exact-run readers, strict validation, EOF completeness, atomic manifests, active-tail semantics, real timed flush, terminal flush policy, coherent export snapshot, policy-based redaction, full export-check replay, and immutable bundle provenance.

### 6.6 Observability

**Strengths:** one envelope stream feeding metrics/live tools, per-subscriber bounded live tail, process counters, and unified state dumps.

**Required work:** custom-sink isolation, correct channel reader semantics, explicit seq-gap ranges, truthful “recent” metrics, fatal snapshot availability after pump death, and stable observer protocol/versioning.

### 6.7 Tests and CI

**Strengths:** broad and thoughtful test stack, with simulator-discovered bugs already improving the design.

**Required work:** make the invariant ownership matrix truthful; remove I2 exemptions or change the contract; activate release-critical stubs; add adversarial corruption/lifecycle/privacy tests; attach CI to PRs targeting `main`; report actual seed counts; publish immutable test provenance.

## 7. Detailed findings

The following register is ordered by severity. Each entry includes impact, a concrete repair, and a validating test. The CSV companion is suitable for issue import.


## Blocker findings

### STS2-R001 - Coordinator failure is not connected to STS2 fatal containment

**Area:** Failure containment  
**Confidence:** High  
**Evidence:** Sts2Session.Completion exposes rpc.Completion only; Bootstrap observes that task only on fault, while Coordinator has a separate pump task that can fault.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L86-L87) · [`src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/Sts2Bootstrap.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/Sts2Bootstrap.cs#L98-L102) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L160-L200)

**Impact.** A journal/core/sink failure can stop the only consumer of the bounded inbox without marking STS2 unavailable. Existing and subsequent requests can wait forever rather than receiving Sts2.Unavailable.

**Recommendation.** Create a composite session lifetime task over RPC, coordinator, outbound writer, and effect runner. On first unexpected completion: atomically enter fatal state, complete the inbox, fail all pending RPCs, best-effort append/flush a fatal record, and call MarkSts2Dead.

**Validation.** Fault-inject JournalWriter.AppendAsync and CoreOutputEncoder; assert pending and future v2 requests terminate with Sts2.Unavailable while v1 still works.

### STS2-R002 - Shutdown/exit flush is not a pump barrier

**Area:** Lifecycle durability  
**Confidence:** High  
**Evidence:** SignalLifecycleAsync enqueues lifecycle control and immediately calls JournalWriter.FlushAsync. Enqueue completion does not mean the pump processed the lifecycle event or prior queued inputs.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L159-L164) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L121-L138)

**Impact.** The exact tail that shutdown handling is meant to preserve can remain unprocessed or buffered when legacy terminates the process.

**Recommendation.** Add a pump-owned barrier input with a TaskCompletionSource. The pump must journal the lifecycle event, drain all causally prior outputs, flush, then complete the barrier. The multiplexer waits on that single operation.

**Validation.** Queue requests/effect results immediately before shutdown, delay the pump, and assert all expected envelopes including lifecycle.shutdown are durable before the mux forwards shutdown.

### STS2-R003 - A custom IEnvelopeSink can block the coordinator indefinitely

**Area:** Observability  
**Confidence:** High  
**Evidence:** Coordinator awaits CompositeEnvelopeSink, which awaits each registered sink in sequence. The documentation says a slow observer cannot stall the pump, but only the built-ins happen to be nonblocking.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L275-L284) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Observability/CompositeEnvelopeSink.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Observability/CompositeEnvelopeSink.cs#L41-L57)

**Impact.** An extension-provided sink can stop request processing and violate the advertised isolation contract.

**Recommendation.** Wrap each auxiliary sink in a bounded, single-consumer mailbox. Coordinator publication must be TryWrite-only. Define overflow policy, timeout/disable policy, last delivered seq, drop range, and per-sink health.

**Validation.** Attach a sink that never completes. Sustain traffic and assert coordinator latency, journal ordering, bounded memory, and precise sink-drop/fault counters.

### STS2-R004 - SecretSideTable entries leak for requests rejected before a driver resolves them

**Area:** Privacy / memory  
**Confidence:** High  
**Evidence:** Gateway tokenizes secrets before Core validation. SecretSideTable has no session clear/dispose. DriverEffectRunner removes only tokens it resolved; an unknown driver returns before BuildOpenRequest and a Busy/invalid request never reaches the runner.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L186-L205) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Redaction/SecretSideTable.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Redaction/SecretSideTable.cs) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L232-L295)

**Impact.** Credential material can remain in process memory for the whole service lifetime.

**Recommendation.** Make tokenization return a request-scoped SecretLease. Transfer ownership only to a scheduled open effect; dispose the lease on every terminal response, enqueue failure, session shutdown, and fatal path. Add ClearAndZero where practical.

**Validation.** Send invalid, duplicate, over-limit, and unknown-driver opens with canary secrets; assert side-table count returns to zero after each terminal response and after fatal/dispose.

### STS2-R005 - Capture-elision fragments can accumulate for rejected or suppressed inputs

**Area:** Privacy / memory  
**Confidence:** High  
**Evidence:** The coordinator owns a session-wide digest-to-fragment dictionary. Entries are removed only when a matching wire/effect substitution occurs, and otherwise are cleared only at coordinator disposal.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L42-L43) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/CaptureElision.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/CaptureElision.cs#L23-L27) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/CaptureElision.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/CaptureElision.cs#L113-L131)

**Impact.** Repeated rejected query.execute requests or late/suppressed row events can retain arbitrary SQL or row payloads in memory.

**Recommendation.** Use a per-input capture context owned by one pump turn. Outputs receive explicit restoration tokens. Dispose the context at the end of the turn regardless of output shape.

**Validation.** Send 100k Busy/NotFound executes with unique SQL and suppressed row events; assert the fragment count and retained bytes return to zero after each turn.

### STS2-R006 - Full replay can report Identical for a truncated journal

**Area:** Replay correctness  
**Confidence:** High  
**Evidence:** JournalReplayer queues expected outputs but does not require the queue to be empty at end-of-input. It also discards corr during comparison.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Replay/JournalReplayer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Replay/JournalReplayer.cs#L68-L71) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Replay/JournalReplayer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Replay/JournalReplayer.cs#L109-L156)

**Impact.** A missing final result, notification, effect request, or config.changed record can pass the principal determinism gate.

**Recommendation.** Separate strict Verify from partial Until. Strict mode validates seq/runId/cause/configVersion, payload digest, kind/type/corr, output cardinality, and an empty expected-output queue at EOF. Partial mode must return Incomplete rather than Identical.

**Validation.** Truncate a journal after every envelope position and mutate corr/cause/configVersion/payload without updating digest; strict verify must fail at the first inconsistency.

### STS2-R007 - JournalReader.ReadAll can combine multiple runs

**Area:** Journal isolation  
**Confidence:** High  
**Evidence:** When given a manifest path, ReadAll uses only its directory and enumerates journal-*-*.jsonl for every run in ordinal filename order.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Journaling/JournalReader.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Journaling/JournalReader.cs#L33-L58)

**Impact.** Replay, invariant checks, live tail recovery, and exports can ingest unrelated sessions, creating false gaps/divergence or leaking data across bundles.

**Recommendation.** Resolve one run explicitly from the manifest/runId. Prefer one directory per run. Add ReadRun(manifest), TailRun(runId), and a multi-run catalog API that never conflates streams.

**Validation.** Place two interleaved run sets in one directory; assert each manifest reads only its run and an incomplete active trailing line is handled as Pending, not corruption.

### STS2-R008 - query.dispose contradicts invariant I2 and suppresses query.complete

**Area:** Query lifecycle  
**Confidence:** High  
**Evidence:** Core moves an active query directly to Disposed and the active scenario explicitly states disposed queries are exempt from I2, while SPEC and generated invariants say every accepted query emits exactly one query.complete.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L510-L555) · [`test/sts2/scenarios/dispose-while-streaming.yaml`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/test/sts2/scenarios/dispose-while-streaming.yaml#L3-L5) · [`docs/sts2/INVARIANTS.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/INVARIANTS.md)

**Impact.** Clients cannot rely on the advertised terminal notification, and the invariant checker weakens the contract rather than detecting the mismatch.

**Recommendation.** Make a one-way contract decision. Preferred: add Disposing, cancel/await the runner, emit exactly one query.complete with status disposed/canceled, then answer dispose or document ordering. Otherwise formally revise I2 and all clients/docs.

**Validation.** Contract tests assert one terminal notification for execute followed by dispose at every race point.

### STS2-R009 - Dispose releases the connection before the old driver pump has stopped

**Area:** Query concurrency  
**Confidence:** High  
**Evidence:** Core clears ActiveQueryId immediately; DriverEffectRunner removes the pump and cancels its token but does not await the enumerator task before acknowledging the effect.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L510-L555) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L209-L218)

**Impact.** A new query can start on the same IDbSession while the previous command/reader is still unwinding, violating the one-active-query guarantee at the actual database edge.

**Recommendation.** Track the pump Task and add Disposing/Disposed acknowledgements. Keep the connection occupied until the runner confirms termination and disposal.

**Validation.** Use a driver that delays iterator finally. Dispose and immediately execute; assert the second execute is Busy until the first pump has exited.

### STS2-R010 - Duplicate connection.close while a query is active can orphan the first request

**Area:** Request terminality  
**Confidence:** High  
**Evidence:** Each close in the Open+active-query path writes CloseCorr = corr and emits no immediate result. A second close overwrites the first corr.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L340-L365)

**Impact.** The first close request can never receive its required terminal JSON-RPC response, violating I1.

**Recommendation.** Keep the first waiter and return {} for later idempotent closes, or maintain a deterministic ordered set of close waiters and complete all exactly once.

**Validation.** Issue two close requests before the query terminal event and assert both correlations terminate exactly once.

### STS2-R011 - Ack accounting permits duplicate/future acknowledgements to over-grant credit

**Area:** Backpressure  
**Confidence:** High  
**Evidence:** High-water throughPageSeq is not clamped to PagesSent; per-page ack ignores pageSeq and increments a count. Negative unacked values can create credit above the configured window.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L442-L479)

**Impact.** A malformed or duplicate client ack can defeat bounded streaming and advance the driver beyond windowPages.

**Recommendation.** Track ack high-water per result set and validate pageSeq against sent ranges. Clamp monotonically, ignore duplicates, reject impossible future values, and derive credit from sent minus acknowledged only.

**Validation.** Property-test duplicate, out-of-order, cross-result-set, negative, huge, and future acks; runner permits at most windowPages pulls.

### STS2-R012 - The runner acquires credit after MoveNext has already produced a row page

**Area:** Backpressure  
**Confidence:** High  
**Evidence:** await foreach advances the async enumerator before the switch; Credits.WaitAsync occurs only after a RowsPage object has been yielded by the driver.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L323-L362)

**Impact.** At least one page can be read/materialized beyond the advertised window. Drivers that buffer internally can exceed the memory bound further.

**Recommendation.** Use a manual enumerator or change the driver port so row-page credit is acquired before requesting the next page. Make the no-prefetch behavior part of the adapter contract.

**Validation.** Instrument a driver MoveNext counter and assert it does not increase while credit is exhausted.

### STS2-R013 - Product disposal does not own or await DriverEffectRunner tasks and leases

**Area:** Resource ownership  
**Confidence:** High  
**Evidence:** Sts2Session keeps no effect-runner field and DisposeAsync only disposes RPC and coordinator. Coordinator disposes the journal but not the runner. The runner has a test-oriented cleanup method but is not IAsyncDisposable.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L69-L84) · [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L166-L171) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L140-L157) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L91-L132)

**Impact.** Open attempts, query pumps, sessions, semaphores, and cancellation sources can outlive the session or post into a closed coordinator.

**Recommendation.** Make the effect runner IAsyncDisposable and the session the explicit owner. Stop intake, cancel, await all tracked tasks with a bound, dispose sessions/CTS/semaphores, then close the journal/mux.

**Validation.** Leak-test shutdown at every open/query race point and assert zero tasks, handles, sessions, and side-table entries afterward.

### STS2-R014 - A successfully opened session can be orphaned if posting the open result fails

**Area:** Resource ownership  
**Confidence:** High  
**Evidence:** OpenAsync stores the IDbSession in sessions before awaiting PostOpenResultAsync; the finally block removes only secrets/open CTS.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L255-L295)

**Impact.** Coordinator shutdown/failure between the database open and effect.res enqueue leaves a live connection Core never knows about.

**Recommendation.** Treat ownership transfer as a two-phase handoff. If posting fails, remove and dispose the session. More generally, runner shutdown must reject new ownership transfers.

**Validation.** Close/fault the inbox immediately after driver.OpenAsync succeeds; assert the newly opened session is disposed.

### STS2-R015 - Query cancel and connection close have no reliable bounded completion

**Area:** Cancellation / liveness  
**Confidence:** High  
**Evidence:** driver.queryCancel awaits Session.CancelAsync with CancellationToken.None before canceling the pump token. Core parks close behind a query terminal event, but there is no journaled close timeout.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L188-L205) · [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L340-L365)

**Impact.** A provider whose CancelAsync hangs can wedge query.cancel/connection.close indefinitely, violating bounded close and request terminality.

**Recommendation.** Cancel the local pump token first, invoke provider cancellation with a bounded token, and model close/cancel deadlines as journaled timer events so replay sees the same timeout decision.

**Validation.** Use a driver whose CancelAsync never returns and whose iterator ignores cancellation; assert bounded terminal responses and lease cleanup.

### STS2-R016 - SQLite cancellation is sticky across the session and result sets are fully buffered

**Area:** SQLite adapter  
**Confidence:** High  
**Evidence:** SqliteSession owns one activeQueryCancel CTS that is never reset after Cancel; PumpResultSetAsync returns a list containing the entire result set before yielding to Runtime.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs#L22-L24) · [`src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs#L34-L81) · [`src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs#L84-L141) · [`src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/SqliteSession.cs#L165-L180)

**Impact.** After one canceled query, later queries are immediately canceled. Large SQLite queries violate bounded-memory and backpressure claims.

**Recommendation.** Create a fresh per-query CTS under a session gate and stream pages incrementally. Use an iterator-compatible exception boundary or a small internal channel, not whole-result buffering.

**Validation.** Cancel query 1, successfully run query 2, and stream a result much larger than memory/page window while measuring bounded retention.

### STS2-R017 - Export is neither a coherent run snapshot nor a guaranteed safe transformation

**Area:** Export / privacy  
**Confidence:** High  
**Evidence:** ExportBundleWriter copies every file in the shared journal directory while the writer may be active. IncludeSqlText only changes a manifest flag; files are copied verbatim. Canary scanning checks two fixed literals.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Export/ExportBundleWriter.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Export/ExportBundleWriter.cs#L62-L130)

**Impact.** Bundles can include other runs, partial segments/manifests, or full-capture user data despite a safeMode label.

**Recommendation.** Execute export through a pump barrier: rotate/flush and obtain an immutable exact-run inventory. Apply an explicit export policy and content scanner, create a new redacted journal when required, and sign/hash the complete bundle manifest.

**Validation.** Export during heavy writes with two runs in one directory and full capture. Assert exact-run consistency, replay, no prohibited data, and deterministic tamper detection.

### STS2-R018 - Any v2 client can enable full row and SQL capture at runtime

**Area:** Capture policy  
**Confidence:** High  
**Evidence:** DecideSetCapture accepts rowCapture=full and sqlCapture=text with no host policy, authorization, consent, or audit metadata.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L174-L206)

**Impact.** Production-sensitive SQL and result values can be persisted by an untrusted or compromised client.

**Recommendation.** Introduce an immutable host CapturePolicy. Product default denies sensitive capture. A permitted change requires capability negotiation, operator consent/reason, maximum duration/size, audit envelope, and automatic reversion.

**Validation.** Product-mode E2E must reject sensitive capture; opt-in test mode records actor/reason/expiry and reverts safely.

### STS2-R019 - The STS2 workflow does not protect a PR into main, and no workflow run is attached to the reviewed head

**Area:** CI / release evidence  
**Confidence:** High  
**Evidence:** pull_request.branches is sts2/main, which filters the PR base branch. The connector returned no workflow runs or combined statuses for c9fbd1e4.  
**Source:** [`.github/workflows/sts2-verify.yml`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/.github/workflows/sts2-verify.yml#L10-L18)

**Impact.** The final merge to main can bypass the advertised full gate; self-reported verification text is not equivalent to an immutable CI result.

**Recommendation.** Run the workflow on PRs targeting main with path filters, on pushes to main, and as a reusable/manual workflow. Publish test results, provenance, exact seed count, engine image digest, mutation reports, and artifact hashes. Require the check in branch protection.

**Validation.** Open a test PR from sts2/main to main and confirm required checks run and are attached to the merge commit.


## High findings

### STS2-R020 - query.complete is not a journal flush point

**Area:** Journal durability  
**Confidence:** High  
**Evidence:** IsFlushPoint switches only on envelope kind; v2/query.complete is rpc.out.notify and therefore does not force a flush.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L389-L391)

**Impact.** The terminal query event can remain buffered despite SPEC and JournalWriter comments claiming query completion is flushed.

**Recommendation.** Make flush policy depend on kind plus type and centralize it in a named DurabilityPolicy. Include query.complete, fatal, lifecycle, config changes, and request terminals.

**Validation.** Write query.complete, terminate abruptly before another append, and verify it is present under the documented durability model.

### STS2-R021 - The 250 ms flush interval is append-driven, not a real upper bound

**Area:** Journal durability  
**Confidence:** High  
**Evidence:** JournalWriter checks elapsed time only during AppendAsync. If the last non-flush envelope is followed by idle time, no timer flush occurs.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Journaling/JournalWriter.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Journaling/JournalWriter.cs#L84-L99)

**Impact.** Data can remain buffered indefinitely during idle periods, contradicting the stated bounded interval.

**Recommendation.** Use a writer-owned periodic flush loop or pump timer, and document process-crash versus power-loss durability. Use flush-to-disk only at selected durability points if required.

**Validation.** Append one non-terminal envelope, go idle for > interval, inspect from another process, and assert visibility/durability.

### STS2-R022 - Manifest updates are non-atomic and omit the active segment

**Area:** Journal integrity  
**Confidence:** High  
**Evidence:** WriteManifest uses File.WriteAllText directly and Segments contains only closed segments.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Journaling/JournalWriter.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Journaling/JournalWriter.cs#L136-L181)

**Impact.** A crash during manifest write can corrupt it; a live export cannot use the manifest as a complete snapshot.

**Recommendation.** Write temp+fsync+atomic replace. Represent the active segment explicitly with committed byte length/hash checkpoint, or rotate before any export/checkpoint.

**Validation.** Crash/fault-inject at each manifest write step and verify recovery chooses the last complete manifest and validates the active tail.

### STS2-R023 - export-check does not check replayability despite its contract

**Area:** Export verification  
**Confidence:** High  
**Evidence:** ExportBundleWriter.Check verifies entry presence, hashes, and canaryScan only; it never opens journal envelopes or invokes JournalReplayer.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Export/ExportBundleWriter.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Export/ExportBundleWriter.cs#L156-L198)

**Impact.** A hash-consistent but semantically invalid or non-replayable bundle can pass export-check.

**Recommendation.** Have export-check validate schema, run isolation, sequence/causality, payload digests, strict replay, privacy policy, generated-doc inventory, and tool/spec versions.

**Validation.** Create a self-consistent bundle with mutated journal+manifest hashes; export-check must reject semantic divergence.

### STS2-R024 - Query options, pageBytes, maxCellBytes, and truncation are not implemented end to end

**Area:** Protocol completeness  
**Confidence:** High  
**Evidence:** Core query.execute forwards fixed credit and SQL only; runner creates QueryExecuteRequest with fixed PageRows/PageBytes; adapters page by row count; WireValueEncoder has no max-cell truncation.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L389-L440) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L323-L336) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/WireValueEncoder.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/WireValueEncoder.cs) · [`docs/sts2/SCENARIO-MATRIX.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/SCENARIO-MATRIX.md)

**Impact.** The published wire contract and memory limits are not truthful, and the cell-truncation scenario remains a stub.

**Recommendation.** Validate/normalize query options in Core, carry them in state/effect request, implement byte-aware page construction and typed truncation wrappers before journaling, and activate boundary scenarios.

**Validation.** Boundary tests at pageRows/pageBytes/maxCellBytes ±1 with UTF-8, binary, typed values, and multi-result sets.

### STS2-R025 - Core does not enforce result-set, page sequence, or row-offset invariants

**Area:** Protocol ordering  
**Confidence:** High  
**Evidence:** DecideQueryEvent relays resultSet/rows and increments counts without tracking current result set, expected pageSeq, or expected rowOffset.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L557-L658)

**Impact.** A buggy adapter can emit rows before metadata, gaps/duplicates, or regressing offsets and STS2 will journal and forward them as valid.

**Recommendation.** Extend QueryInfo with result-set phase and expected sequence/offset. Convert violations to a single terminal internal/transport error plus diagnostic.

**Validation.** Fault scripts for rows-before-resultSet, duplicate/gapped pageSeq, wrong resultSetId, and offset regression.

### STS2-R026 - Malformed numeric fields can escape the pure reducer as exceptions

**Area:** Input robustness  
**Confidence:** High  
**Evidence:** Several paths call JsonElement.GetInt32 directly after checking only ValueKind.Number, including ack high-water and session maxConnections.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L452-L479) · [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L810-L827)

**Impact.** Out-of-range or non-integral JSON numbers can fault the coordinator, contradicting the never-throws reducer contract.

**Recommendation.** Use TryGetInt32/TryGetInt64 and centralized schema validation. Every invalid client value becomes Sts2.InvalidRequest; invalid effect input becomes a stable diagnostic/fatal classification.

**Validation.** Fuzz every numeric field with fractions, exponent extremes, >Int32, negative values, and duplicate properties.

### STS2-R027 - Notifications are fire-and-forget and asynchronous send failures are unobserved

**Area:** Outbound transport  
**Confidence:** High  
**Evidence:** HandleOutbound discards NotifyWithParameterObjectAsync. Coordinator's synchronous emit callback can only count immediate exceptions, not task failures or backlog.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L173-L184) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L393-L406)

**Impact.** A slow or broken client can cause unbounded pending sends, silent loss, reordering uncertainty, and inaccurate health.

**Recommendation.** Add one bounded outbound RPC writer with ordered awaitable delivery. Feed completion/failure back into session lifetime and define what is durable versus delivered.

**Validation.** Throttle/close stdout during a large stream; assert bounded memory, ordered sends, a fatal/unavailable transition, and no unobserved tasks.

### STS2-R028 - Outbound oversized frames are not handled safely

**Area:** Transport  
**Confidence:** High  
**Evidence:** RunOutboundPumpAsync handles NeedMoreData and MalformedHeader but not OversizedFrame before computing/slicing frameLength.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/StdioMultiplexer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/StdioMultiplexer.cs#L371-L408)

**Impact.** A service-generated oversized frame can throw in an uncontrolled location or desynchronize the channel.

**Recommendation.** Handle OversizedFrame explicitly per channel: mark STS2 dead for STS2 output; fail fast for legacy or apply a documented containment policy. Use checked arithmetic for headerLength+contentLength.

**Validation.** Emit declared and actual frames around maxFrameBytes, including long overflow and partial bodies.

### STS2-R029 - Enabled-mode fatal fallback can write unframed text to stdout

**Area:** Transport  
**Confidence:** High  
**Evidence:** Program's outer catch uses Console.WriteLine if the logger fails, even when the multiplexer owns stdout as a framed protocol channel.  
**Source:** [`src/Microsoft.SqlTools.ServiceLayer/Program.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/Microsoft.SqlTools.ServiceLayer/Program.cs#L80-L89)

**Impact.** An error path can corrupt the shared JSON-RPC stream and prevent either service from reporting a structured failure.

**Recommendation.** Route emergency text to stderr or a bootstrap-owned diagnostic file. Add a static/runtime guard that no enabled-mode code writes raw stdout.

**Validation.** Force Logger.WriteWithCallstack to fail under enabled mode and assert stdout remains parseable frames only.

### STS2-R030 - Flag parsing is case-insensitive in Bootstrap but case-sensitive in legacy filtering

**Area:** Activation  
**Confidence:** High  
**Evidence:** IsEnabled uses OrdinalIgnoreCase; ServiceLayerCommandOptions filters with array Contains default case-sensitive semantics.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/Sts2Bootstrap.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/Sts2Bootstrap.cs#L34-L47) · [`src/Microsoft.SqlTools.ServiceLayer/Utility/ServiceLayerCommandOptions.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/Microsoft.SqlTools.ServiceLayer/Utility/ServiceLayerCommandOptions.cs#L21-L34)

**Impact.** --ENABLE-STS2 can enable Bootstrap's interpretation while still reaching the legacy unknown-argument parser, which may print usage to stdout and exit.

**Recommendation.** Use one shared exact parser/normalizer or make both paths intentionally case-sensitive. Test all casing and environment/argument combinations.

**Validation.** Spawn E2E with canonical, upper-case, mixed-case, duplicate, and malformed flags.

### STS2-R031 - Raw provider and server messages are journaled as contract data

**Area:** Privacy  
**Confidence:** High  
**Evidence:** DriverEffectRunner posts DbDriverException.Message and ServerMessage.Text into effect responses; Core forwards them into query.message/query.complete and open errors.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L274-L286) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs#L365-L409) · [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L601-L627)

**Impact.** Provider messages can contain server names, database names, object names, SQL fragments, or row values and bypass SQL/row capture controls.

**Recommendation.** Define a field-level data classification. Journal stable codes and redacted safe details by default; keep full diagnostics only in a separately governed PII channel. Apply capture policy to server message text.

**Validation.** Inject canaries into provider/server messages and assert product-mode journals, state, health, logs, and exports remain clean.

### STS2-R032 - Secret tokens reveal a raw SHA-256 prefix

**Area:** Privacy  
**Confidence:** High  
**Evidence:** SecretSideTable token format includes the first 12 hex characters of SHA-256(secret).  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Redaction/SecretSideTable.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Redaction/SecretSideTable.cs#L41-L57)

**Impact.** Anyone with a journal can confirm low-entropy candidate passwords and correlate identical secrets within/across runs.

**Recommendation.** Use a cryptographically random opaque token or HMAC with a per-run in-memory key. Digest metadata for secrets should never be derived from the raw secret without a secret key.

**Validation.** Assert tokens are non-deterministic across runs and cannot be predicted from candidate values.

### STS2-R033 - Command-line sanitization is substring-based and incomplete

**Area:** Privacy  
**Confidence:** High  
**Evidence:** Bootstrap records every arg except those containing the word password.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/Sts2Bootstrap.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/Sts2Bootstrap.cs#L83-L96)

**Impact.** Access tokens, connection strings, user-supplied paths, and separate flag values can enter the manifest.

**Recommendation.** Use an allowlist of safe startup flags and structured redaction for key=value and next-argument forms. Record hashes/counts where useful, not raw unknown arguments.

**Validation.** Canary every supported secret-bearing command-line shape and scan manifest/export.

### STS2-R034 - EnvelopeSubscription declares SingleReader although producer eviction also reads

**Area:** Live tail  
**Confidence:** High  
**Evidence:** The channel is created SingleReader=true, but TryPush calls channel.Reader.TryRead while the subscriber concurrently reads Reader.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Observability/EnvelopeSubscription.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Observability/EnvelopeSubscription.cs#L27-L63)

**Impact.** The channel's single-reader optimization contract is violated, making behavior under concurrent eviction/consumption unsupported and drop accounting fragile.

**Recommendation.** Set SingleReader=false or implement a small locked ring buffer with explicit gap metadata. Check the second TryWrite result and distinguish dropped-oldest from dropped-newest/disposed.

**Validation.** Race producer eviction, consumer reads, and dispose under stress; verify order and exact gap ranges.

### STS2-R035 - SqlClientSession can leave activeCommand pointing at a disposed command after exceptions

**Area:** Adapter semantics  
**Confidence:** High  
**Evidence:** activeCommand is cleared only after the await using scopes complete normally, not in finally.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Drivers.SqlClient/SqlClientSession.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Drivers.SqlClient/SqlClientSession.cs#L40-L78)

**Impact.** Later CancelAsync/DisposeAsync operates on a stale disposed command and obscures true active-state diagnostics.

**Recommendation.** Assign/clear active command under a session gate in try/finally and reject accidental concurrent ExecuteAsync calls at the adapter boundary.

**Validation.** Fault ExecuteReader, ReadAsync, and NextResultAsync; assert active command is null and a subsequent query behaves correctly.

### STS2-R036 - initialize advertises exportLog=false although the method is implemented

**Area:** Contract  
**Confidence:** High  
**Evidence:** DecideInitialize sets capabilities.exportLog to false while diagnostics.exportLog is registered and implemented.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L102-L130) · [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L264-L268)

**Impact.** Clients following capability negotiation will not use a supported feature; clients ignoring it see inconsistent contract behavior.

**Recommendation.** Generate capabilities from the composed feature set and tests, not literals. Report false only when export is genuinely unavailable.

**Validation.** Contract test compares initialize capabilities to registered methods/effect configuration.

### STS2-R037 - Product export does not supply generated review documents

**Area:** Export  
**Confidence:** High  
**Evidence:** ExportBundleWriter supports GeneratedDocs, but Sts2Session's export template passes only runId, journal directory, and output directory.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Hosting/Sts2Session.cs#L103-L110) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Export/ExportBundleWriter.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Export/ExportBundleWriter.cs#L21-L38)

**Impact.** The advertised bundle inventory and actual product output diverge.

**Recommendation.** Inject a versioned generated-doc catalog or stop promising docs in every product bundle. Prefer schema/tool metadata plus a docs version URL/hash to avoid stale duplication.

**Validation.** E2E export asserts exact manifest inventory and versions.

### STS2-R038 - The invariant checker implements only a subset while docs imply I1-I16 are checked on every run

**Area:** Invariant evidence  
**Confidence:** High  
**Evidence:** InvariantChecker handles I1,I2,I3,I5,I6,I7,I8,I9,I12 plus two capture checks; other invariants are owned by separate tests or not enforced there. I5 validates a cause only when present, not the documented requirement that every non-root has one.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Testing/InvariantChecker.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Testing/InvariantChecker.cs#L34-L203) · [`docs/sts2/INVARIANTS.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/INVARIANTS.md)

**Impact.** Reviewers can mistake a green scenario/simulator run for complete invariant coverage, and missing cause links can pass.

**Recommendation.** Generate an invariant ownership matrix from executable registrations: definition, enforcement layer, test names, last evidence. Strengthen I5 root classification and require causes for all produced outputs/metrics.

**Validation.** A meta-test fails if any invariant lacks an executable owner or if generated docs overstate coverage.

### STS2-R039 - The branch is five commits behind main and pins an older SDK

**Area:** Branch integration  
**Confidence:** High  
**Evidence:** Compare shows sts2/main ahead 51 and behind 5 from merge base 77498823. Branch global.json is 10.0.203; main is 10.0.301.  
**Source:** [`global.json`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/global.json)

**Impact.** The final integration has untested build/toolchain and mainline behavior drift; generated API/docs and legacy diff budget can change after rebase.

**Recommendation.** Rebase or merge main before further design work, resolve SDK/build changes, then rerun the complete gate and regenerate all evidence on the final merge candidate.

**Validation.** Record final merge-base, zero-behind status, and immutable CI run URLs/hashes in the review report.


## Medium findings

### STS2-R040 - Metric envelopes are roots even though they are produced by a pump turn

**Area:** Trace schema  
**Confidence:** High  
**Evidence:** MaybeSampleMetricsAsync builds metric envelopes with cause=null.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L203-L219)

**Impact.** Causal explain tools cannot connect a metric sample to the input that triggered it; this conflicts with the trace-schema root rule.

**Recommendation.** Set cause to the just-processed input or define a documented system-root category and update I5/schema accordingly.

**Validation.** Trace-schema test checks allowed root kinds/types.

### STS2-R041 - Wire JSON and internal redaction markers are not separated

**Area:** Internal schema  
**Confidence:** High  
**Evidence:** Core accepts sql as either string or arbitrary object to support $redacted wrappers; a client can send an object that resembles an internal marker.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs#L389-L404) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/CaptureElision.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/CaptureElision.cs#L64-L73)

**Impact.** Untrusted wire shapes can enter an internal protocol and resolve to empty SQL or confusing replay behavior.

**Recommendation.** Validate public DTOs before redaction, then use typed internal values (Captured<T>/DigestRef) that cannot be supplied by JSON-RPC clients.

**Validation.** Send forged $redacted objects and duplicate sql fields; expect InvalidRequest.

### STS2-R042 - Duplicate JSON keys and duplicate Content-Length headers are not rejected

**Area:** JSON/framing hardening  
**Confidence:** Medium  
**Evidence:** JsonRpcFraming takes the last Content-Length encountered; JSON inspection/reducer property lookup allows ambiguous duplicate members.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/JsonRpcFraming.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/JsonRpcFraming.cs#L64-L90) · [`src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/JsonRpcMessageInspector.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/JsonRpcMessageInspector.cs#L31-L88)

**Impact.** Different layers can interpret an ambiguous frame differently, creating routing or validation inconsistencies.

**Recommendation.** Reject duplicate Content-Length, conflicting framing headers, duplicate top-level method/id, and duplicate contract fields. Add a strict JSON ingress validator.

**Validation.** Transport-smuggling corpus with duplicate/case-variant headers and duplicate JSON properties.

### STS2-R043 - Hot paths repeatedly parse JSON strings and retain undisposed JsonDocument buffers

**Area:** Performance / allocations  
**Confidence:** High  
**Evidence:** Core, coordinator overlays, capture elision, effect runner, and encoder frequently return JsonDocument.Parse(...).RootElement without an owning document lifetime or Clone.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Core/Sts2CoreReducer.cs) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Effects/DriverEffectRunner.cs)

**Impact.** High-volume row streaming creates avoidable allocations and delays buffer reclamation; lifetime assumptions are brittle.

**Recommendation.** Use typed immutable DTOs and Utf8JsonWriter at boundaries. Where JsonDocument is necessary, dispose and Clone the element. Benchmark allocations/retained bytes, not throughput alone.

**Validation.** Allocation profile for 1M rows and repeated diagnostics; set a bytes-per-row and Gen2 retention budget.

### STS2-R044 - The generated CONTRACT is a method index, not a complete wire contract

**Area:** Documentation  
**Confidence:** High  
**Evidence:** CONTRACT.md contains method and error-code tables but no request/response schemas, constraints, notification ordering, versioning, capture policy, examples, or compatibility rules.  
**Source:** [`docs/sts2/CONTRACT.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/CONTRACT.md)

**Impact.** Client authors and reviewers still need to read the 1,400-line SPEC and source to know the protocol.

**Recommendation.** Generate JSON Schemas/TypeScript types and a contract reference containing required/optional fields, defaults, bounds, errors, state preconditions, idempotency, and examples.

**Validation.** Schema-conformance tests round-trip every request/result/error/notification and fail generated-doc drift.

### STS2-R045 - The TypeScript client sample races query completions

**Area:** Documentation  
**Confidence:** High  
**Evidence:** It creates one completion Promise before executing a create-table query, does not await that query's completion, then starts a second query despite one-active-query-per-connection.  
**Source:** [`docs/sts2/CLIENT.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/CLIENT.md)

**Impact.** Copying the sample can produce Sts2.Busy or associate the first query's completion with the second query.

**Recommendation.** Provide a queryId-keyed completion registry and execute helper that subscribes before the request, filters rows/completion by queryId, acks per result set, handles errors/cancel/dispose, and cleans listeners.

**Validation.** Run the sample in CI against the spawned service.

### STS2-R046 - The documents overstate completeness and contain status contradictions

**Area:** Documentation / status  
**Confidence:** High  
**Evidence:** SPEC remains an agent-executable draft with deviations appended; what_was_built says eleven projects and complete; scenario matrix still has 8 stubs; pitch says byte-identical production behavior; verification report says human gate/tag pending.  
**Source:** [`docs/sts2/SPEC.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/SPEC.md) · [`docs/sts2/SCENARIO-MATRIX.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/SCENARIO-MATRIX.md) · [`docs/sts2/what_was_built.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/what_was_built.md) · [`docs/sts2/sts_refactor.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/sts_refactor.md) · [`artifacts/verification-report.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/artifacts/verification-report.md)

**Impact.** Reviewers cannot tell normative target, as-built behavior, verified evidence, and remaining work apart.

**Recommendation.** Split normative contract, target design, ADRs, as-built inventory, and verification ledger. Add a generated implementation-status table and replace absolute claims with evidence-scoped wording.

**Validation.** Docs linter checks counts/status against source/scenarios and requires all claims to link to an executable gate or ADR.

### STS2-R047 - Health calls a lifetime histogram recentErrors and fatal health is unavailable after pump failure

**Area:** Observability semantics  
**Confidence:** High  
**Evidence:** MetricsEnvelopeSink accumulates errors for the whole session; OverlayRuntimeHealth labels them recentErrors. If the coordinator pump is dead, it cannot answer diagnostics.health.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Observability/MetricsEnvelopeSink.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Observability/MetricsEnvelopeSink.cs) · [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L300-L337)

**Impact.** Operators may misread error recency, and the most important fatal snapshot cannot be queried through the failed pump.

**Recommendation.** Rename to errorsByCodeTotal or implement a bounded time/sequence window. Maintain a host-level last-fatal snapshot exposed by mux/bootstrap even after coordinator death.

**Validation.** Time-window tests and fatal containment E2E query/synthesized error include the frozen fatal summary.

### STS2-R048 - sessionId is largely unused in envelopes

**Area:** Trace usability  
**Confidence:** Medium  
**Evidence:** Coordinator output encoding/build calls commonly pass sessionId=null even for connection/query effects and notifications.  
**Source:** [`src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/src/sts2/Microsoft.SqlTools.Sts2.Runtime/Coordination/Coordinator.cs#L221-L299)

**Impact.** Viewer filtering and causal analysis must parse payloads to group activity, weakening the envelope's indexing value.

**Recommendation.** Define sessionId semantics and populate it deterministically from connection/query state or add explicit entityRefs metadata.

**Validation.** Every connection/query envelope can be grouped without parsing arbitrary payload JSON.

### STS2-R049 - The full gate label says 10k seeds even when CI intentionally overrides it to 500 or 1000

**Area:** Verification UX  
**Confidence:** High  
**Evidence:** verify.sh hard-codes the gate name while the workflow sets STS2_SIMULATOR_SEEDS by event tier.  
**Source:** [`verify.sh`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/verify.sh#L74-L77) · [`verify.sh`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/verify.sh#L171-L172) · [`.github/workflows/sts2-verify.yml`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/.github/workflows/sts2-verify.yml#L31-L39)

**Impact.** Verification summaries can imply stronger evidence than actually ran.

**Recommendation.** Print the effective seed count and tier in the gate name/report; require 10k only for release/nightly provenance.

**Validation.** CI artifact names and summaries expose exact seed range/count.

### STS2-R050 - Eight scenario entries remain stubs, including contract and privacy boundaries

**Area:** Design / testing  
**Confidence:** High  
**Evidence:** SCENARIO-MATRIX reports 46 active and 8 stubs, including cell truncation, config-change-during-query, fatal containment, redacted state, and setCapture.  
**Source:** [`docs/sts2/SCENARIO-MATRIX.md`](https://github.com/microsoft/sqltoolsservice/blob/c9fbd1e40ec8aae43f02bd31723f2fa205d8d849/docs/sts2/SCENARIO-MATRIX.md)

**Impact.** The branch is described as M7/preview-ready while important cross-component behaviors lack executable scenario evidence.

**Recommendation.** Promote true behaviors to active scenario/E2E entries and distinguish unit-test-backed transport rows from unimplemented stubs. Do not ship preview until all release-critical rows are active.

**Validation.** Release gate requires zero Blocker/High stubs and generated matrix links each row to test IDs/results.


## 8. Verification gaps to add immediately

Add these focused suites before broad feature work:

1. **Fatal matrix:** journal append throws, reducer/encoder throws, observer hangs, RPC writer fails, effect response posts after shutdown.
2. **Lifecycle matrix:** queued input + effect response + shutdown/exit at every interleaving; process exits immediately after mux forwards.
3. **Resource matrix:** open/query/close/dispose/fatal at every await boundary; zero sessions/tasks/CTS/secrets/fragments after teardown.
4. **Replay corruption corpus:** truncation, duplicated/skipped seq, bad cause, wrong corr, wrong config version, payload/digest mismatch, extra/missing output, mixed runs, partial final line.
5. **Backpressure properties:** driver MoveNext count never exceeds granted credit; ack duplicates/future/out-of-order cannot increase the window.
6. **Privacy corpus:** secrets/SQL/rows embedded in provider messages, errors, command line, metadata, state, logs, and exports.
7. **Transport corpus:** duplicate Content-Length, overflow, oversized outbound, malformed then valid frames, normal STS2 EOF, raw stdout emergency path.
8. **Client contract suite:** generated schemas and a runnable TypeScript client tested against the spawned executable.

## 9. Merge strategy

1. Rebase onto `main` and regenerate the comparison report.
2. Land a **lifetime and barriers** slice first: R001, R002, R013-R015, R027.
3. Land **strict journal/replay/export** next: R006, R007, R017, R020-R023.
4. Land **query semantics/backpressure**: R008-R012, R016, R024-R026.
5. Land **privacy policy**: R004, R005, R018, R031-R033, R041.
6. Land **transport/observability hardening**: R003, R028-R030, R034, R040, R047-R048.
7. Reconcile docs, activate scenarios, run final CI on the exact merge candidate, and only then tag preview.

## 10. Bottom line

The branch has the bones of a dramatically better SQL Tools Service. The architecture is not the problem. The remaining risk is concentrated in promises that cross an asynchronous or process boundary: “flushed,” “terminal,” “bounded,” “isolated,” “redacted,” and “identical.” Tighten those words into executable contracts and STS2 becomes a compelling preview rather than an impressive prototype with sharp edges.
