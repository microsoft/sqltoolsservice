# PLAN-M2: Connection vertical slice

## Scope

SPEC §16 M2 DoD: `v2/initialize`, `v2/connection.open`, `v2/connection.cancel`,
`v2/connection.close` end-to-end on FakeDriver; connection state machine in the
generated doc; connection fault scenarios green; error model + JSON-RPC error shape
verified; secret side table lifecycle verified; PublicAPI updated. Plus (pulled from the
spec's quick-gate list): the scenario runner (YamlDotNet, DEV-004) and a first simulator
(200 seeds) over connection ops.

Out of scope: query streaming (M3), real drivers (M4/M5), capture-mode switching (M3
with setCapture).

## Vertical slices

1. **Driver port** (Abstractions) — IDbDriver/IDbSession, ConnectionOpenRequest,
   ServerInfo, SecretMaterial, ExecEvent records (full §10.1 shape; query parts unused
   until M3), DbDriverException with stable Sts2.* codes.
2. **FakeDriver** (Testing) — scripted open outcomes (ok/timeout/auth/network/hang),
   cancellation, lease tracking, deterministic ServerInfo.
3. **Core connection machine** — Connections map (Opening/Open/Closing), open-id index,
   maxConnections -> Sts2.Busy, duplicate openId -> Sts2.InvalidRequest, idempotent
   cancel/close, driver.open/driver.cancelOpen/driver.close effects, initialize
   (idempotent, mustUnderstand_ rejection), ping via Core (latestSeq real).
4. **Runtime DriverEffectRunner** — resolves secret tokens at the edge, owns CTS per
   open and session handles, maps driver exceptions to stable codes, removes secrets
   when the open attempt completes (side-table lifecycle), bounded close.
5. **Hosting gateway rework** — all v2 requests funnel through the coordinator
   (redact -> post -> await journaled result/error); notifications re-emit; ping through
   Core; M0 direct target removed.
6. **Bootstrap/E2E** — session composition (side table, journal under <logdir>/sts2,
   effect runner, coordinator, gateway); enabled-mode E2E extended (initialize +
   journal dir created); disabled-mode unchanged.
7. **Scenario runner** (YamlDotNet) — §14.2 format: driver script, inbound steps with
   bind/$profiles/waitFor, expect.outbound matching, invariant checks (I1, I5, I6, I7,
   I12), per-scenario journal + immediate replay.
8. **Activate M2 scenarios** — open-timeout/auth/network, open-cancel-race,
   duplicate-openid, max-connections-busy, close-idempotent, error-connectionfailed-*,
   error-invalidrequest, error-notfound, secret-canary-connection-open,
   initialize-idempotent, initialize-must-understand-rejected.
9. **Simulator v1** — seeded random connection op/fault schedules, 200 seeds quick;
   invariants + replay per seed; repro line on failure.
10. **Docs/gates/report** — STATE-MACHINE gains the connection machine; CONTRACT marks
    M2 methods live; verify.sh scenario + simulator gates live; report + tag sts2-m2.

## Known risks

- Gateway rework touches the only E2E-proven path; keep ping E2E green throughout.
- Effect-runner cancellation races (cancel vs open completion) need the scenario
  runner's race coverage, not just unit tests.
- Simulator determinism: all randomness from the seed, ids from seq, no wall-clock in
  assertions.
