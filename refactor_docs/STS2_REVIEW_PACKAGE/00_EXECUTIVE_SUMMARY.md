# STS2 Refactor Review: Executive Summary

**Reviewed repository:** `microsoft/sqltoolsservice`  
**Branch:** `sts2/main`  
**Snapshot:** `c9fbd1e40ec8aae43f02bd31723f2fa205d8d849`  
**Compared with:** `main`  
**Review date:** 2026-06-18

## Verdict

The refactor has an excellent architectural spine: a pure reducer, journal-first ordering, explicit effects, a tiny side-by-side legacy seam, generated review artifacts, replay, simulation, and broad test intent. It is substantially stronger than a conventional async service rewrite.

The reviewed snapshot should **not be merged or tagged as a preview yet**. The blockers are concentrated at the live-runtime edges rather than in the central concept. Fatal propagation, shutdown barriers, async ownership, replay completeness, run isolation, query dispose/ack semantics, capture policy, and merge-candidate CI evidence need one focused hardening pass.

This is good news in engineering terms. The core architecture does not need to be discarded. It needs its promises converted into mechanically enforced end-to-end contracts.

## Highest-priority findings

| ID | Area | Why it matters |
|---|---|---|
| STS2-R001 | Fatal containment | The coordinator can fail without the session/multiplexer transitioning STS2 to unavailable, leaving requests parked. |
| STS2-R002 | Lifecycle durability | Shutdown posts a control message and flushes immediately, but does not prove the pump processed the lifecycle event or its outputs. |
| STS2-R003 | Observer isolation | Custom sinks are awaited on the pump, so a slow or never-completing sink can stall all requests despite documentation claiming otherwise. |
| STS2-R004/R005 | Sensitive lifetime | Secret and capture side tables can retain data on rejected or unconsumed paths. |
| STS2-R006/R007 | Replay and run isolation | Replay can accept a truncated tail, and readers can combine multiple runs from one directory. |
| STS2-R008/R009 | Query lifecycle | Active dispose conflicts with the terminality invariant and can release a connection before the old driver task has stopped. |
| STS2-R010 | Request ownership | A duplicate close can overwrite the original waiting correlation. |
| STS2-R011/R012 | Backpressure | Ack handling can overgrant credit, and credit is acquired after the driver has already yielded a page. |
| STS2-R017/R018 | Export and privacy | Export is not a coherent run snapshot, and client capture requests are not bounded by an immutable host policy. |
| STS2-R019 | Release evidence | The workflow topology does not currently prove the exact PR-to-`main` merge candidate, and no checks were visible on the reviewed head. |

The full register contains 50 concrete findings with evidence, impact, fix, and validation criteria.

## What should be preserved

1. The project dependency matrix and tiny legacy seam.
2. `Decide(state, event)` as the single domain decider.
3. Journal-before-dispatch as the authoritative ordering rule.
4. Deterministic IDs, sorted state, and shared live/replay output encoding.
5. The side-by-side v1/v2 multiplexer rollout model.
6. Scenario, simulator, replay, engine, mutation, E2E, and secret-canary testing.
7. Generated implementation artifacts and the explicit decisions/deviations log.
8. The pure-authority plus runtime-overlay pattern, once its schema and consistency rules are explicit.

## Recommended sequence

### Wave 0: Establish a trustworthy baseline

Rebase to current `main`, adopt its SDK/build changes, and make CI run as a required check on pull requests targeting `main`. Record immutable evidence against the exact merge candidate.

### Wave 1: Repair lifetime and durability

Create one composite `Sts2Session` lifetime, connect coordinator faults to multiplexer fatal containment, add pump barriers, track all effect/observer/outbound tasks, and prove pending-request drain.

### Wave 2: Make journal/replay/export strict

Use one run per directory, atomic manifests, segment sequence ranges, checkpointed active tails, digest verification, EOF queue-empty verification, and export from an immutable checkpoint inventory.

### Wave 3: Resolve query semantics and bounds

Freeze the dispose contract in an ADR, preserve close waiters, hold the connection until the old driver pump has stopped, acquire credit before advancing the driver, clamp/deduplicate acknowledgments, and implement page-byte/cell limits.

### Wave 4: Harden privacy and observers

Introduce immutable host capture policy, request-scoped secret/capture leases, classification of provider messages, bounded observer mailboxes, explicit observer views, and exact viewer gap metadata.

### Wave 5: Earn the preview tag

Complete the scenario stubs, run the full corruption/fault matrix, attach the evidence bundle to the exact candidate, dogfood with a real client/viewer, and progress through the adoption ladder only after each gate is met.

## Deliverables in this package

- `01_TECHNICAL_REVIEW.md`: detailed code and architecture review with 50 findings.
- `02_DOCUMENTATION_REVIEW.md`: artifact-by-artifact review and a proposed documentation system.
- `03_TARGET_DESIGN.md`: complete reviewed target design.
- `04_RELIABILITY_SECURITY_OPERATIONS.md`: failure, privacy, durability, SLO, and rollout design.
- `05_NEXT_STEPS.md`: concrete implementation waves, file-level work, tests, and exit criteria.
- `06_FINDINGS_REGISTER.csv`: sortable finding register.
- `diagrams/STS2_DESIGN_VISUALS_V2.pdf`: 12-page reviewed design visual set.
- `diagrams/STS2_DESIGN_VISUALS_V2.tex`: editable LaTeX/TikZ source.

## Review limitation

The review was read-only. It used the attached artifacts and the accessible GitHub branch contents. I did not independently build or execute the branch in this environment. Verification claims in the branch report are therefore treated as self-reported evidence until reproduced by CI attached to the exact merge candidate.
