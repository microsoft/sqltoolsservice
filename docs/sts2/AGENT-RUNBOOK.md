# STS2 Agent Runbook: Kickoff Prompt for a Code Agent

**Purpose:** This is the operating prompt for implementing STS2 in `microsoft/sqltoolsservice`.  
**Companion spec:** `docs/sts2/SPEC.md`  
**Recommended branch:** `sts2/main`  
**Recommended workspace:** dedicated git worktree or container

How to use this file:

1. Copy `STS2-SPEC.md` to `docs/sts2/SPEC.md`.
2. Copy this runbook to `docs/sts2/AGENT-RUNBOOK.md`.
3. Append the `CLAUDE.md snippet` at the bottom to the repo root `CLAUDE.md` if one exists.
4. Start a fresh code-agent session and paste the **Kickoff prompt** below.
5. Run one milestone per session when practical. Each new session starts with the same kickoff and resumes from the latest report.

---

## Kickoff prompt

You are executing a long-horizon refactor of `microsoft/sqltoolsservice` called STS2. The complete technical specification is `docs/sts2/SPEC.md`. Read it fully before writing code. The spec is the contract. This runbook is your operating contract.

### 1. First orient yourself

Read these, in order:

1. `docs/sts2/SPEC.md`, especially §0, §4, §5, §6, §14, §15, and §16.
2. `artifacts/verification-report.md`, if present.
3. `docs/sts2/DECISIONS.md`, if present.
4. `docs/sts2/BLOCKERS.md`, if present.
5. `git status --short`.
6. `git log --oneline -20`.
7. Current repo files named in SPEC §0, especially `global.json`, `Directory.Build.props`, `src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj`, `src/Microsoft.SqlTools.ServiceLayer/Program.cs`, and `src/Microsoft.SqlTools.ServiceLayer/HostLoader.cs`.

Resume at the first incomplete milestone in SPEC §16. If no STS2 files or report exist, start at M0.

### 2. Repo reality check before M0 code

Before writing code in a fresh M0 start, verify the repo facts in SPEC §0 locally. Record results in `docs/sts2/DECISIONS.md` under `REPO-FACT` entries.

Pay special attention to the seam:

- Preferred seam is to use `HostLoader.CreateAndStartServiceHost(..., Stream? inputStream = null, Stream? outputStream = null)`.
- When STS2 is disabled, pass `null` streams to preserve current legacy behavior.
- When STS2 is enabled, Bootstrap provides virtual legacy streams from the multiplexer.

If the preferred seam is impossible without broader legacy refactoring, stop with a `SPEC-CHANGE` decision. Do not remodel legacy startup to make STS2 look elegant.

### 3. Autonomy contract

There are exactly two mandatory human gates:

1. End of M1: stop for review of `CONTRACT.md`, `INVARIANTS.md`, `SCENARIO-MATRIX.md`, `TRACE-SCHEMA.md`, `STATE-MACHINE.md`, `COMPONENTS.md`, and `verification-report.md`.
2. End of M7: stop for final review.

Do not stop for approval elsewhere unless a Stop Condition triggers.

Two-way doors: choose, record one line in `DECISIONS.md`, and proceed. Examples: private helper names, internal folder layout inside a pinned project, unit test organization, local implementation details, private class factoring.

One-way doors: write a `SPEC-CHANGE` entry with options, recommendation, and evidence, then stop. Examples:

- wire contract changes,
- invariant changes,
- pinned default changes,
- dependency matrix changes,
- privacy/redaction changes,
- legacy diff beyond budget,
- new external package not implied by the spec,
- moving tests away from `test/sts2`,
- changing replay semantics,
- weakening generated review artifacts.

Stop Conditions:

- one-way door encountered,
- the same gate fails after 3 genuinely different fix attempts,
- spec sections conflict,
- preferred seam is impossible within budget,
- a secret appears in any artifact,
- disabled-mode v1 smoke shows behavioral drift.

When you stop, write `docs/sts2/BLOCKERS.md` with what you tried, evidence, and best hypothesis.

### 4. Non-negotiable working agreements

1. `./verify.sh --quick` must be green before you claim a milestone is complete.
2. Never game a gate. Forbidden: deleting or weakening assertions, editing golden/scenario files to match broken behavior, adding `Skip` or retry attributes, lowering thresholds, raising the legacy-diff budget, swallowing exceptions to satisfy tests, adding product `testMode` branches, or post-processing journals so replay passes.
3. Tests first within every slice: scenario or unit test fails first, then code.
4. The journal is append-only truth. Tooling reads journals; product code never rewrites them.
5. Determinism rules in SPEC §9 are law. Wire banned API checks in M0.
6. Legacy is read-only outside the seam. You may inspect legacy code but not improve, reorganize, or opportunistically refactor it.
7. Small, inspectable commits. Commit messages state the gate evidence that changed.
8. Secrets never appear in logs, journals, snapshots, test names, comments, generated docs, or reports.
9. Every transport/state bug fix must add a scenario, simulator seed, or invariant. A lone unit test is not enough.
10. Generated docs are review artifacts. Keep them deterministic and committed.

### 5. Milestone loop

For each milestone:

1. Re-read that milestone's DoD in SPEC §16.
2. Write `docs/sts2/PLAN-M<n>.md` with:
   - scope,
   - vertical slices,
   - expected new tests/scenarios,
   - expected generated-doc changes,
   - expected verification gate changes,
   - known risks.
3. Implement one vertical slice at a time.
4. Run targeted tests as you work.
5. Run `./verify.sh --quick` at each slice boundary where feasible.
6. When DoD is met, run `./verify.sh --quick`. Run `./verify.sh --full` when the milestone requires it or before M7.
7. Commit, tag `sts2-m<n>`, and prepend a report entry to `artifacts/verification-report.md`.
8. Continue unless this is M1, M7, or a Stop Condition.

### 6. Report format

Use this exact shape. Evidence beats narration.

```markdown
## M<n> - <name> - <date> - <commit>
Gates: <verbatim verify.sh summary block>
New: scenarios +N (<names>), invariants exercised, generated docs changed
Replay: <journals identical / total>
Simulator: <seeds, failures, repro command if any>
Mutation: <score, threshold, previous, or n/a>
Perf: <numbers or n/a>
Legacy diff: <lines/files>
API surface: <PublicAPI and CONTRACT.md delta>
Decisions: <ids added or none>
Blockers: <ids or none>
Risk notes:
- <up to 3 honest bullets about what is least certain>
Next: <milestone or human gate>
```

Do not paste code into reports. The reviewer reads generated docs, scenarios, invariants, and evidence.

### 7. M0 first actions

For a fresh start:

1. Read SPEC fully.
2. Verify repo facts in SPEC §0.
3. Confirm the HostLoader optional-stream seam is feasible.
4. Create `docs/sts2/DECISIONS.md`, `BLOCKERS.md`, and `PLAN-M0.md`.
5. Scaffold STS2 projects and `sqltoolsservice-sts2.slnf`.
6. Add architecture tests for the dependency matrix.
7. Add banned API analyzers and tests.
8. Implement Multiplexer tests first:
   - partial/coalesced frame parsing,
   - `Content-Length` variants,
   - v2 prefix routing,
   - legacy fallback routing,
   - lifecycle mirroring for `shutdown` and `exit`,
   - outbound server-request id rewriting with collisions,
   - single stdout writer property test,
   - poison STS2 crash containment.
9. Implement Multiplexer.
10. Implement Bootstrap and the tiny Program seam.
11. Implement `v2/diagnostics.ping` through STS2 Hosting.
12. Implement `verify.sh` and `verify.ps1`.
13. Run disabled-mode v1 smoke and enabled-mode E2E covering both `v2/diagnostics.ping` and one representative v1 request in the same session.
14. Run `./verify.sh --quick` and report.

### 8. M1 first actions

1. Define envelope DTOs and canonical digest.
2. Implement redaction before journaling.
3. Plant secret canary fixtures.
4. Implement write-ahead journal and manifest.
5. Implement coordinator pump with toy Core state.
6. Implement `sts2-replay run`, `verify`, `until`, `diff`, and `explain` against toy state.
7. Generate the review surface:
   - `CONTRACT.md`,
   - `TRACE-SCHEMA.md`,
   - `INVARIANTS.md`,
   - `SCENARIO-MATRIX.md`,
   - `STATE-MACHINE.md`,
   - `COMPONENTS.md`.
8. Create at least 50 scenario stubs covering SPEC §14.2.
9. Stop for human review after green quick verification.

### 9. Gate self-check before every commit

Answer these privately before committing:

- Did I touch legacy outside the seam?
- Did I change a pinned default, method schema, error code, invariant, dependency boundary, or privacy behavior?
- Did I weaken or skip a test?
- Did I update generated docs and PublicAPI files intentionally?
- Did `verify.sh --quick` run, and did I use its output in the report?
- Can a future agent reproduce any failure from a scenario name, seed, or journal path?
- Did any secret or canary appear anywhere?

If any answer is bad or uncertain, fix it or stop with a blocker.

### 10. Environment notes

- Use the SDK from `global.json`.
- Use `sqltoolsservice-sts2.slnf` for the quick loop. Do not require a full legacy build unless the seam or E2E requires it.
- Do not require SQL Server until M5. `verify.sh --quick` uses Fake and Sqlite only.
- `verify.sh --full` may use Docker for SQL Server. If Docker is absent locally, skip engine tests only with an explicit report line. CI/nightly must run them.
- `verify.sh` must run under bash. `verify.ps1` is a thin PowerShell shim.
- If StreamJsonRpc API names differ from the spec sketch, conform to the installed package and record a two-way `DECISIONS.md` entry. Behavior is pinned, not exact constructor names.

---

## CLAUDE.md snippet

Append to the repo root `CLAUDE.md`:

```markdown
## STS2 branch rules

- Contract: `docs/sts2/SPEC.md`. Operating rules: `docs/sts2/AGENT-RUNBOOK.md`.
- Definition of done: `./verify.sh --quick` green plus a report entry.
- Human gates: end of M1 and end of M7 only, unless a Stop Condition triggers.
- Never weaken, skip, delete, or retry tests to go green.
- Never edit golden/scenario output to match broken behavior.
- Never touch legacy code outside the SPEC §5 seam.
- Never log secrets, SQL text in product default, row payloads in digest mode, or unframed stdout text.
- Core is pure: no time, randomness, async, channels, driver APIs, ADO.NET, StreamJsonRpc, file I/O, console I/O, or legacy namespaces.
- Cancellation is a journaled v2 message, never hidden transport cancellation.
- Multiplexer must rewrite outbound server-request ids; a plain id-to-channel table is not enough.
- `shutdown` and `exit` are lifecycle-mirrored to STS2, not raw-broadcast as duplicate JSON-RPC requests.
- One-way doors require `SPEC-CHANGE` in `docs/sts2/DECISIONS.md` and a stop.
```
