# STS2: A SQL Tools Service That Explains Itself While It Runs

## The pitch in one paragraph

SqlToolsService is a long-lived back end behind the MSSQL extensions: it opens database
connections and streams query results to editors. Over years it became hard to reason
about — connection state, cancellation, and streaming live scattered across async
callbacks, and when something went wrong in the field the only evidence was a log line and
a guess. **STS2 is a new, minimal service core that replaces the fog with a machine that has
windows.** It runs *inside the same process, beside the legacy service*, sharing one stdio
channel, and exposes just connectivity and query execution. Its defining property: every
single thing it does — every request, decision, effect, and result — is a journaled event
in a gapless, replayable log, and the running system fans those events out to anyone who
wants to watch. You can replay a production session offline and get byte-identical behavior.
You can attach a live viewer and watch the machine think. That is the whole bet:
**observability and determinism are not features bolted on; they are the architecture.**

## Why the old shape was hard, and what we changed

A conventional service answers an RPC by mutating shared state across a web of `async`
methods. The control flow *is* the call stack, and the call stack is gone the moment it
unwinds. There is no artifact that says "this is the exact sequence of things that
happened," so a bug that only reproduces under a specific interleaving of cancellation and
streaming is nearly impossible to chase.

STS2 inverts this. It splits the service into a **pure decision core** and an **impure
edge**, joined by a single **journaled event log**:

- The **Core** is a pure synchronous function: `Decide(state, event) → (newState, outputs)`.
  No time, no randomness, no async, no I/O, no driver APIs. Given the same state and event
  it always produces the same decision. All the domain logic — the connection machine, the
  query machine, backpressure credit, idempotency — lives here and nowhere else.
- The **Runtime** does everything impure (sockets, files, ADO.NET, clocks) but is held
  deterministic by discipline: it is the only place effects run, and it records every effect
  request and response as an event.
- The **Coordinator** is a single-threaded pump that ties them together. For each event it:
  appends it to the **write-ahead journal**, hands it to the Core, then journals and acts on
  each output. The journal order is the truth.

Because the Core is pure and every external observation (a driver result, a timer, a config
change) re-enters as a *recorded* event, you can take the journal from a real run, feed it
back through the same Core, and reproduce the identical outbound sequence — without a
database, without the original timing, without secrets. That is **replay**, and it is the
single most valuable debugging tool the service has ever had.

## The technical pillars, and how each serves the goal

**1. The envelope and the write-ahead journal.** Every RPC frame, internal effect, event,
config change, and diagnostic becomes one `Sts2Envelope` with a gapless `seq`, a `cause`
pointer to the envelope that produced it, a canonical-JSON `digest`, and a `configVersion`.
It is appended to an append-only JSONL log *before* it is dispatched. The log is segmented
with chained SHA-256 hashes. *Goal served:* there is always a complete, tamper-evident,
causally-linked record of what happened — the substrate everything else reads.

**2. The pure reducer.** Decisions are data, not side effects. *Goal served:* the hard part
of the system — the concurrency-sensitive state machines — is testable as a pure function
and reproducible by replay.

**3. The driver port.** Core never sees ADO.NET; it emits `effect.req` envelopes
("open this", "stream that") that the Runtime executes against an `IDbDriver`, posting
results back as `effect.res`. *Goal served:* the database edge is swappable (Fake, SQLite,
SQL Server), and every interaction with it is in the trace.

**4. Backpressure as state.** Streaming credit lives in Core; the runner's enumerator blocks
on a per-query semaphore when credit runs out. *Goal served:* flow control is visible and
replayable, not an emergent property of socket buffers.

**5. Privacy by construction.** Secrets are tokenized before any envelope exists; in digest
capture mode, row cells and SQL text are elided to authoritative-digest wrappers *before*
journaling, with the originals living only in an in-memory side table substituted back at
the wire edge. *Goal served:* you can ship a production journal to a developer and it still
replays digest-identically, with no secrets, SQL, or row data in it.

**6. The multiplexer.** One BCL-only component splits the stdio channel, routes v2 to STS2
and everything else to legacy untouched, rewrites server-request ids so the two can't
collide, mirrors lifecycle, and contains STS2 crashes. *Goal served:* STS2 ships beside the
proven legacy service with a 12-line legacy footprint and no risk to the existing path.

**7. The event-capture framework (the newest pillar).** Observability is a first-class
fan-out seam, `IEnvelopeSink`: every journaled envelope is delivered, in `seq` order, to any
number of observers. The journal is just the privileged first sink; metrics, a live tail,
and test capture are more sinks. They are best-effort and fault-isolated — a slow or broken
observer is counted and skipped, never stalling the pump or breaking the write-ahead rule.
*Goal served:* the system doesn't just *record* itself, it *broadcasts* itself, and new
tools (the diagnostic viewer, dashboards, an attached debugger) hook in without touching the
core. This is the framework "all the changes can hook into later."

## Why it's great

- **You can replay production.** A redacted journal reproduces behavior byte-for-byte. Field
  bugs become deterministic test cases.
- **You can watch it think.** Subscribe to the live tail and see every request, decision,
  effect, and result in order, as it happens. Ask `diagnostics.health` for an 11-dimension
  snapshot (queue depth, leases, error histogram, fatal status) and `diagnostics.state` for
  the exact machine state with the flags that explain *why* it is parked where it is.
- **It tells the truth.** The generated review docs say which envelope kinds are actually
  emitted vs. reserved. The state dump the viewer shows live is byte-identical to the one
  replay produces. Health's `fatal` flag is real, not a constant. Nothing is faked to look
  finished.
- **It can't quietly drift.** Determinism (I7), gapless causality (I5), secret safety (I6),
  backpressure bounds (I9), lease cleanup (I8) and eleven more invariants are checked over
  every produced journal by the scenario runner and a seeded simulator — which has caught
  five real concurrency bugs that reading the code never would have.
- **It's safe to adopt.** Side-by-side with legacy, gated by Bootstrap, 12-line legacy diff.

---

## How to use it

### Turn it on
STS2 activates through `Sts2Bootstrap.TryStart` inside the legacy `Program`. When enabled,
the multiplexer routes `v2/*` methods to STS2 and everything else to the legacy service over
the same stdio channel. No separate process, port, or socket.

### Speak v2 (client)
```jsonc
// handshake
→ {"method":"v2/initialize"}
← {"specVersion":…,"capabilities":{…},"drivers":[…],"journal":{"capture":"digest",…}}

// open, query, stream with backpressure
→ {"method":"v2/connection.open","params":{"openId":"o1","profile":{…}}}
← {"connectionId":"c-7","serverInfo":{…}}
→ {"method":"v2/query.execute","params":{"connectionId":"c-7","sql":"select …"}}
← {"queryId":"q-9"}
← (notify) v2/query.resultSet { columns:[…] }
← (notify) v2/query.rows { pageSeq:0, rows:[…] }
→ (notify) v2/query.ack { queryId:"q-9", throughPageSeq:0 }   // grants credit
← (notify) v2/query.complete { status:"succeeded", rowsAffected:… }
```
One active query per connection; `connection.close` cancels an active query first. All
cancel/close/dispose are idempotent (return `{}` for unknown/terminal ids).

### Observe it live (in-process / viewer)
```csharp
using EnvelopeSubscription sub = session.LiveTail.Subscribe();
await foreach (Sts2Envelope e in sub.Reader.ReadAllAsync(ct))
    Render(e);            // e.Seq, e.Kind, e.Type, e.Corr, e.Cause, e.ConfigVersion, e.Payload
// sub.Dropped > 0  → you fell behind; re-sync the gap from the journal file.
```
Add your own observer anywhere:
```csharp
Sts2Session.Start(options with { EnvelopeSinks = [ myDashboardSink ] });
// myDashboardSink : IEnvelopeSink — must not block; failures are isolated + counted.
```

### Introspect on demand
- `v2/diagnostics.health` → counters + runtime overlay.
- `v2/diagnostics.state` → redacted machine state + `runtime` handle section.
- `v2/diagnostics.setCapture {"rowCapture":"digest","sqlCapture":"digest"}` → change capture
  at runtime; journals `config.changed`, bumps `configVersion`, takes effect next envelope.
- `v2/diagnostics.exportLog` → a redacted bundle (manifest, privacy report, journals, docs).

### Watch metrics out of process
```
dotnet-counters monitor --name <process> Microsoft-SqlTools-Sts2
# envelopes-total, rpc-errors-total, sink-faults-total
```

### Replay a journal offline
```csharp
var journal = JournalReader.ReadAll(journalDir);
ReplayResult r = JournalReplayer.Replay(journal);
// r.Identical == true → behavior reproduced exactly (I7)
// r.Divergence       → first mismatch with cause chain, if any
// JournalReplayer.DumpState(state, atSeq) → the same redacted dump the viewer shows
```

### Verify
```
./verify.sh --quick      # build, tests, scenarios, replay, simulator, canary, docs, E2E
./verify.sh --full       # + engine (real SQL Server), mutation, perf, 10k-seed simulator
```

For deeper integration detail see [`../docs/sts2/OBSERVABILITY.md`](../docs/sts2/OBSERVABILITY.md)
and the wire/trace contracts under `docs/sts2/`.
