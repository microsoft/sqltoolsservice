# PLAN-M1: Spine — envelopes, journal, replay, generated review surface

## Scope

SPEC §16 M1 DoD: envelope schema + canonical digest, redaction + secret canaries,
write-ahead JSONL journal + manifest, coordinator pump over a toy Core, the sts2-replay
CLI (run/verify/until/diff/explain) against toy state, the six generated review docs,
≥50 scenario stubs covering §14.2, and a green replay-determinism gate. Ends at the
mandatory human gate.

Out of scope: any real connection/query behavior (M2/M3), drivers, real effects beyond
the toy ones needed to prove the pump/journal/replay loop.

## Vertical slices

1. **Canonical JSON + digest** (Runtime, pure unit) — deterministic canonicalization:
   UTF-8, ordinal-sorted keys, invariant formatting, no whitespace, explicit
   `{"$redacted":...}` scalar representation; SHA-256 digest. Tests first: golden
   canonical forms, key-order independence, redacted-marker digest stability.
2. **Envelope schema** (Runtime) — `sts2.envelope/1` DTO with all §8.1 fields and kinds;
   envelope JSONL codec. Tests: round-trip, kind set, cause/seq integrity helpers.
3. **Redaction + secret side table** (Runtime) — SecretRef tokenization
   (`secret:sha256:<prefix>:<counter>`), side-table lifecycle, redaction of auth fields
   before journaling; canary fixtures + scan helper in Testing. Tests: canary never
   appears in any envelope/journal output; side table never serializes.
4. **Write-ahead journal + manifest** (Runtime) — append-only JSONL segments, manifest
   with hash chain, write-ahead + flush rules (§8.3). Tests: WAL ordering, segment
   rotation, manifest hashes, flush-on-terminal events.
5. **Coordinator pump + toy Core** (Runtime + Core) — bounded-channel pump assigning
   seq/ts/runId/configVersion, journaling before dispatch, dispatching to
   `Sts2CoreReducer.Decide(CoreState, CoreEnvelope)`; toy state machine: `v2/toy.echo`
   request -> echo result + `toy.counter` event; effect request/response loop with a toy
   effect runner. Tests: pump ordering, journal-before-dispatch, decision outputs
   journaled, gapless seq, cause chains.
6. **sts2-replay CLI** — run (outbound digest sequence), verify (batch), until --seq
   (redacted state dump), diff (first divergence + cause chain), explain (causal tree).
   Tests run the commands in-process against journals produced by slice 5.
7. **Scenario stubs + runner skeleton** (Testing) — YAML scenario format parser, ≥50
   stub files tagged per §14.2 (stubs assert "not yet implemented" cleanly and are
   counted by SCENARIO-MATRIX generation; multiplexer-layer items tagged
   adapter=multiplexer map to existing unit/E2E tests).
8. **Generated review surface** — deterministic generators emitting CONTRACT.md,
   TRACE-SCHEMA.md, INVARIANTS.md, SCENARIO-MATRIX.md, STATE-MACHINE.md, COMPONENTS.md;
   verify.sh regenerates and fails on diff.
9. **verify.sh upgrades** — replay verify over produced journals, secret canary scan,
   generated docs diff gates flip from n/a to live.

## Expected new tests/scenarios

Canonical digest goldens, envelope round-trips, redaction canaries, journal WAL/manifest,
pump ordering/causality, replay determinism (every produced journal replayed), 50
scenario stubs registered.

## Expected generated-doc changes

All six docs created and committed; regeneration is deterministic.

## Expected verification gate changes

`replay verify`, `secret canary scan`, `generated docs diff`, `scenario tests (stub
count)` flip from n/a to live gates.

## Known risks

- Canonicalization must be frozen before journals accumulate; digest changes later
  invalidate replay corpora. Mitigate with golden tests now.
- Toy Core must not leak into M2 contracts; keep `toy.*` names clearly scoped.
- Deterministic generated docs on Windows (CRLF) — generators write LF explicitly.
- Journal `ts` comes from an injected TimeProvider; replay uses recorded values only.
