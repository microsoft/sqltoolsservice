# PLAN-M7: Hardening, evidence, and preview tag

## Scope

SPEC §16 M7 DoD: `verify.sh --full` green in a CI-capable environment; Stryker mutation
gate wired with ratchet; 10,000-seed simulator green; perf gate stable vs M3 baseline;
final generated docs committed; final verification report; tag `sts2-v2.0.0-preview`;
**human gate: final review** (the second and last mandatory stop).

## Environment reality

This local box has no running SQL Server (Docker daemon down) and Stryker.NET is not
installed. Per SPEC §14.5/§16, those gates are **CI/nightly** responsibilities and are
wired to run there, reported `n/a` locally with a clear reason. Everything that does not
need that infrastructure is verified locally.

## Vertical slices (this session)

1. Stryker config (`stryker-config.json`) targeting Core, Contracts, and Runtime pure
   units (canonical digest, redaction, envelope codec, journal manifest, WireValueEncoder)
   with the ratchet thresholds from SPEC §14.6 (Core/Contracts 70%, Runtime pure 60%,
   ratchet-up only). Wired into `verify.sh --full` as a skip-if-absent gate.
2. 10k-seed simulator: `SimulatorTests` seed count reads `STS2_SIMULATOR_SEEDS`
   (default 200 quick); `verify.sh --full` sets 10000. Locally validated at a raised
   count to build confidence; the full 10k green run is CI's.
3. Perf gate already in `--full` (M3); confirm baseline holds.
4. Final docs regenerated and committed; verification report finalized.
5. Tag `sts2-v2.0.0-preview`.
6. **STOP for the human gate.** Hand Karl the review surface (generated contract,
   scenario corpus, invariants, verification report, export privacy report, legacy diff)
   and the CI checklist (engine suite, Stryker, 10k seeds).

## CI checklist (must be green before preview ships)

- `verify.sh --full` with `STS2_SQLSERVER_CONNSTRING` set → engine suite (dialect:tsql) green.
- Stryker mutation run → Core/Contracts ≥70%, Runtime pure ≥60%, ratchet recorded.
- 10,000-seed simulator green (`STS2_SIMULATOR_SEEDS=10000`).
- Perf smoke ≥50k rows/s, <20% regression from the M3 baseline.

## Known risks

- The simulator's liveness budgets (M3) are tuned for back-to-back load; at 10k seeds CI
  should run on a machine with enough cores. Journals remain deterministic (I7).
