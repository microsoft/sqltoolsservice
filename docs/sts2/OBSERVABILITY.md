# STS2 Observability & Event-Capture Framework

This is the integration guide for tools that observe a running STS2 session — most
immediately the VS Code diagnostic event viewer. It describes the one seam everything
hangs off (`IEnvelopeSink`), the built-in observers, and the runtime introspection
surface. For the on-the-wire/in-journal envelope format see [TRACE-SCHEMA.md](TRACE-SCHEMA.md);
for the method contract see [CONTRACT.md](CONTRACT.md).

## The one seam: `IEnvelopeSink`

Every envelope the coordinator processes — inbound RPC, Core outputs, effects, control,
diagnostics, config changes — is journaled and then handed, **in `seq` order**, to a set
of observers. That is the whole framework: one stream, many sinks.

```csharp
public interface IEnvelopeSink
{
    ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush);
}
```

Rules that make it safe to attach anything:

- **The journal is the privileged first sink.** It is appended and awaited *before* Core
  dispatches the envelope (write-ahead, §8.3). The journal is the truth; everything else
  observes what it recorded.
- **Auxiliary sinks are best-effort.** They run after journaling, in `seq` order, and are
  fault-isolated: a sink that throws is counted (`Coordinator.SinkFaultCount`,
  `Sts2EventSource` `sink-faults-total`) and skipped. A slow or broken observer can never
  stall the pump or break write-ahead.
- **Sinks must not block.** Complete synchronously or near-synchronously. The built-ins do
  (a channel `TryWrite`, a counter increment).

Register extra sinks via `Sts2SessionOptions.EnvelopeSinks`. The session always installs
a metrics sink (owned by the coordinator) and a live-tail broadcast sink ahead of yours.

## Live tail (the viewer's primary feed)

`Sts2Session.LiveTail` is a `BroadcastEnvelopeSink`. Subscribe to receive every journaled
envelope as it happens:

```csharp
using EnvelopeSubscription sub = session.LiveTail.Subscribe();
await foreach (Sts2Envelope e in sub.Reader.ReadAllAsync(ct))
{
    // render e: e.Seq, e.Kind, e.Type, e.Corr, e.Cause, e.ConfigVersion, e.Payload, e.Digest
}
```

- Each subscriber has a **bounded buffer** (default 4096). If the consumer falls behind, the
  buffer drops the **oldest** envelope to admit the newest — a live tail wants freshness.
- Every drop is counted (`EnvelopeSubscription.Dropped`, `BroadcastEnvelopeSink.TotalDropped`).
  A non-zero `Dropped` is the viewer's signal that it missed envelopes and should re-sync from
  the journal file (`JournalReader.ReadAll`) for the gap.
- Dispose the subscription to unregister and complete the reader.

For an out-of-process or post-mortem viewer, read the journal directly: the active segment
is included by `JournalReader.ReadAll`, so a file-poll tail also works (bounded by the
journal flush interval).

## Metrics

Two ways to read metrics, both fed from the same stream:

- **Live tallies** — `Coordinator.Metrics` (`MetricsEnvelopeSink`): `Total`, `Errors`,
  `EnvelopesByKind()`, `ErrorsByCode()`. Deterministic snapshots, ideal for a dashboard.
- **EventCounters** — the process-wide `Sts2EventSource` (`Microsoft-SqlTools-Sts2`) exposes
  `envelopes-total`, `rpc-errors-total`, `sink-faults-total`. Watch with
  `dotnet-counters monitor Microsoft-SqlTools-Sts2` or any `EventListener`.
- **In the trace** — set `CoordinatorOptions.MetricSampleEvery` (production: every 1000
  inputs) and the coordinator journals a `metric` snapshot envelope on that cadence. These
  are journaled-only (never dispatched, skipped on replay), so the viewer can read metric
  history offline from a journal or export bundle.

## Runtime introspection (request/response)

- **`v2/diagnostics.health`** — pure-Core counters (latest seq, active connections/queries,
  total queries, unacked pages, shutting down) plus a live Runtime overlay: `configVersion`,
  `queueDepth`, `fatal` (+`fatalReason`), `openLeases`, `opensInFlight`, `activeQueryPumps`,
  `droppedDiagnostics` {emit, effect, sink}, `envelopesObserved`, `errorsByCodeTotal` histogram.
- **`v2/diagnostics.state`** — the redacted machine state in the one shared `CoreStateDump`
  format (connections/queries with phases, counters, and the flags that explain why a machine
  is parked: `hasHandle`, `cancelRequested`, `closeAfterQuery`, `closePending`,
  `creditOutstanding`, `completeSent`), plus a live `runtime` handle section. The Core portion
  is byte-identical to what `sts2-replay until --seq N` produces, so live and replayed state
  diff cleanly.
- **`v2/diagnostics.setCapture`** — change `rowCapture` (`full`|`digest`) / `sqlCapture`
  (`text`|`digest`) at runtime. Journals a `config.changed` envelope, bumps `configVersion`
  (visible on every subsequent envelope and in replayed state), and takes effect on the next
  envelope. Idempotent when unchanged.
- **`v2/diagnostics.exportLog`** — a redacted bundle (manifest, privacy report, journals,
  generated docs) for offline analysis.

## What a trace actually contains

Of the 15 envelope kinds in the closed schema, v2.0 emits: `rpc.in.request/notify`,
`rpc.out.result/error/notify`, `effect.req/res`, `control`, `diag`, `config.changed`, and
`metric` (opt-in). `cmd`, `evt`, `timer.due`, and `state.snapshot` are **reserved** — part
of the schema but not produced in v2.0 (see the TRACE-SCHEMA kinds table). Build the viewer
against the emitted set; treat reserved kinds as "won't appear."
