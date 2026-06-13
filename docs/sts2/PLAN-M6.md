# PLAN-M6: Client interop and export loop

## Scope

SPEC §16 M6 DoD: `docs/sts2/CLIENT.md` with protocol examples; runnable TypeScript sample
(initialize/open/execute/stream/ack/cancel/dispose/close); spawned E2E driving v1 and v2
concurrently under load; export bundle round-trip (export → privacy check → replay →
state-until-seq); multiplexer interleaving torture test with legacy+STS2 server-request
id collisions.

## Vertical slices

1. **Export bundle** (Runtime): `v2/diagnostics.exportLog` produces a zip-equivalent bundle
   (manifest, privacy-report, journals, generated docs). M6 implements the bundle writer +
   `sts2-replay export-check` (manifest hashes, privacy/canary scan, replayability).
   Export defaults to safe mode (secrets tokenized, SQL digest, rows digest).
2. **diagnostics.exportLog + diagnostics.state + diagnostics.health** through Core/gateway,
   journaled; state/health return redacted snapshots (I16).
3. **Export round-trip test**: run a session, export, validate the bundle (hashes + privacy
   + replay identical + state-until-seq), assert canary-clean.
4. **Multiplexer interleaving torture test**: many concurrent legacy+STS2 server-initiated
   requests with colliding numeric ids under random chunking; assert id rewrite keeps them
   distinct and responses restore exactly (extends the M0 single-writer property test).
5. **CLIENT.md** + TypeScript sample using vscode-jsonrpc (documented, not built in CI —
   no Node toolchain assumed); the wire examples are generated from the contract.
6. **E2E concurrent v1+v2 under load** (extends the existing E2E).
7. Docs/report/tag sts2-m6.

## Known risks

- The bundle is a directory tree (not a real .zip) unless System.IO.Compression is added;
  use System.IO.Compression.ZipFile (BCL) for a real .zip — verify it's available.
- The TS sample can't run in CI without Node; keep it as a documented, copy-pasteable
  reference validated by eye against the generated contract, and note that in the report.
- diagnostics.state redaction must never leak SQL/rows/secrets (I16) — assert in tests.
