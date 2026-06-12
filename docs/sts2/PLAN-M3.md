# PLAN-M3: Query streaming vertical slice

## Scope

SPEC §16 M3 DoD: `v2/query.execute`, `query.resultSet`, `query.rows`, `query.message`,
`query.complete` (exactly once), `query.ack`, `query.cancel`, `query.dispose` end-to-end
on FakeDriver; backpressure enforced (window of unacked pages, runner stops advancing
the enumerator); row digest journaling (digest capture elides row payloads); the full
query/cancel/dispose/close fault and race scenario set green with >= 50 total scenarios;
perf and memory baseline recorded. The toy machine is removed.

## Architecture decisions

- **Credit protocol for backpressure**: the enumerator pull loop lives in the effect
  runner, but the window lives in Core. Core grants credit via `driver.queryAdvance`
  effects (initial grant at start, more on acks); the runner pulls and posts one
  `driver.queryEvent` effect.res per observed event, stopping when credit is exhausted.
  Page-bearing events consume credit; metadata/message events do not.
- **Row capture modes** (SPEC §8.4): full capture journals row payloads inline (test
  default); digest capture replaces row-bearing payloads with `payloadMeta`
  {rows, bytes} + the original digest. Replay of digest journals verifies structure and
  non-row digests; full-digest replay (I7) applies to full-capture journals. Logged as
  DEV-005 (within §13.3's stated limitations).
- **close-while-query-active**: close cancels the active query first; the connection
  closes after the query reaches a terminal state (SPEC §7.9).

## Vertical slices

1. FakeDriver query scripting: scripted event sequences, hang points released by
   cancel/trigger, mid-stream errors, severed transport, multi-resultset, type edges.
2. Core query machine + runner: queryStart/Advance/Cancel/Dispose effects, queryEvent
   ingestion, window accounting, exactly-one-complete (I2), no-output-after-complete
   (I3), busy (one active query per connection), idempotent cancel/dispose.
3. Capture modes in the coordinator journaling path (full|digest for row-bearing types).
4. Scenario corpus: activate the query/cancel/dispose/close/ack/error set (~36 files);
   simulator gains query ops; invariants I2, I3, I4, I9 wired into the checker.
5. Toy machine removal (Core, runner, tests, scenarios, CONTRACT).
6. Perf smoke (1M rows x 10 cols digest mode, >= 50k rows/s) recorded as baseline;
   memory bound assertion (no row retention in Core state).
7. Docs (query state machine), report, tag sts2-m3.

## Known risks

- Backpressure correctness under racing acks/cancels — the simulator must cover ack
  storms and cancel-vs-complete races, not just scenarios.
- Digest-mode elision must keep live and replay digests aligned for non-row envelopes;
  the elision applies AFTER digest computation, never before.
- Perf target on dev hardware may differ from CI; record numbers, gate loosely (>=50k).
