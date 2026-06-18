# STS2 Documentation and Design-Artifact Review

**Scope:** every supplied Markdown, TeX, and PDF artifact, plus the branch's generated trace schema and verification report.  
**Recommendation:** preserve the substance, but reorganize it into a smaller authoritative documentation system with generated implementation status.

## 1. Overall assessment

The documentation set is unusually ambitious and materially improves reviewability. It contains a protocol contract, milestone plan, decisions, invariants, scenarios, component inventory, state machines, client guidance, observability guidance, implementation map, narrative pitch, and diagrams. The problem is not missing prose. The problem is **authority and truth layering**.

Today, five different kinds of content are interleaved:

1. **Normative contract:** what clients and implementations must obey.
2. **Target architecture:** why the system is shaped this way.
3. **As-built inventory:** what this branch actually contains.
4. **Evidence ledger:** what tests ran, on what commit, with what result.
5. **Agent workflow:** how an implementation agent should work.

Those categories age at different rates. Keeping them in one 1,400-line SPEC plus appended deviations and separate generated summaries creates contradictions that are hard to detect. The reviewed branch already demonstrates this: dispose semantics conflict with I2, generated CONTRACT is not a complete contract, stubs remain while the pitch says complete, and the observability guide promises nonblocking custom sinks although the code awaits them.

The redesigned documentation set should use a simple rule:

> A statement about behavior belongs in exactly one authoritative source, and every status claim is generated from executable evidence.

## 2. Recommended documentation architecture

```text
docs/sts2/
  README.md                         # navigation, status, compatibility
  protocol/
    PROTOCOL.md                     # normative wire behavior
    schemas/*.schema.json           # generated request/result/notify/error schemas
    errors.md
    versioning.md
  design/
    ARCHITECTURE.md                 # target architecture and rationale
    RELIABILITY-PRIVACY-REPLAY.md   # durability, security, replay, failure model
    OPERATIONS-ROLLOUT.md           # deployment, feature flag, telemetry, rollback
    adr/ADR-0001-*.md               # accepted one-way decisions
  implementation/
    COMPONENTS.md                   # generated as-built inventory
    TRACE-SCHEMA.md                 # generated
    STATE-MACHINES.md               # generated from transition tables
    SCENARIO-MATRIX.md              # generated with test/evidence links
    INVARIANT-COVERAGE.md           # generated definition -> owner -> tests -> status
    STATUS.md                       # generated capabilities and known gaps
  guides/
    CLIENT.md
    OBSERVABILITY.md
    SUPPORT-RUNBOOK.md
    CONTRIBUTING-STs2.md
  evidence/
    verification-<commit>.json      # immutable machine-readable result
    verification-<commit>.md        # rendered summary
```

The existing `SPEC.md` should become a migration source, not remain the permanent authority. Extract the wire contract into `protocol/`, architectural rationale into `design/`, and implementation deviations into ADRs.

## 3. Artifact-by-artifact review

### 3.1 `SPEC.md`

**What is strong**

- Clear mission and non-goals.
- Explicit package/dependency matrix.
- Precise transport requirements.
- Thoughtful privacy and replay goals.
- Pinned defaults and invariants.
- Milestone definitions with test gates.
- Excellent “no gate gaming” language.

**Problems**

- It remains labeled **Agent-executable draft** even though the branch claims M7/preview readiness.
- Normative requirements and historical implementation instructions are mixed.
- Section 19 preserves conflicting earlier text and appends deviations instead of resolving authority.
- Several implementation facts no longer match the contract:
  - active dispose is exempted from I2 in tests;
  - `sqlCapture=none` is specified but the reducer accepts only text/digest;
  - timer envelopes are reserved although bounded lifecycle decisions need deterministic timers;
  - export and capture-policy behavior are less restrictive than the security wording;
  - page byte and cell truncation requirements are not implemented.
- The M0-M7 agent plan is useful history but clutters the client/system contract.
- “MUST” statements are not linked to executable owners.

**Disposition**

Split it. Keep a frozen copy under `history/STS2-SPEC-INITIAL.md`, then replace the live SPEC with a short index pointing to the normative protocol, target design, ADRs, and generated status.

**Concrete update**

Add a requirement ID to every normative rule, for example `STS2-PROTO-ACK-004`, and generate a coverage table:

| Requirement | Owner | Tests | Status |
|---|---|---|---|
| STS2-PROTO-ACK-004 future acks do not grant credit | Core | AckPropertyTests | failing / pending |
| STS2-DUR-003 lifecycle flush is a pump barrier | Runtime | LifecycleBarrierE2E | pending |

### 3.2 `AGENT-RUNBOOK.md`

**What is strong**

- Excellent working agreements.
- Explicit one-way versus two-way doors.
- Strong evidence/report format.
- Small-commit and reproducibility discipline.
- Good protection against test weakening.

**Problems**

- It is a bootstrap implementation prompt, not a maintenance runbook.
- It tells an agent to copy files and start from M0, which is now historical.
- It says one-way doors require a stop, while SPEC section 19 records a blanket allowance to continue with deviations. That weakens governance.
- The human gates are milestone-specific and no longer fit ongoing review.
- It does not define how to handle an accepted finding from an external technical review.

**Disposition**

Archive the kickoff prompt. Replace it with `CONTRIBUTING-STS2.md` and a shorter agent policy that applies to every future PR.

**Concrete update**

Use gates based on change risk rather than milestone:

- protocol/privacy/durability/replay change: ADR + human approval;
- state-machine change: transition-table diff + scenario + simulator seed;
- transport change: property test + spawned E2E;
- generated artifact change: source-of-truth change only, never hand edit.

### 3.3 `SCENARIO-MATRIX.md`

**What is strong**

- Honest total/active/stub counts.
- Clear tags, milestone, and adapter columns.
- Includes multiplexer behaviors in the corpus accounting.

**Problems**

- Eight stubs remain while the branch is presented as M7/preview complete.
- A row does not link to its YAML/test or latest result.
- It does not show which requirements/invariants each scenario covers.
- “stub” conflates unimplemented behavior with behavior tested elsewhere.
- It does not distinguish product-mode capture from test-mode capture.
- Several high-value boundaries are missing or stubbed: cell truncation, config change during query, fatal containment, redacted state, setCapture.

**Disposition**

Keep it generated, but expand the schema.

**Recommended columns**

`Scenario | Layer | Methods | Fault/Race | Invariants | Privacy mode | Fake | SQLite | SQL Server | E2E | Test ID | Last result | Status`

Use statuses: `active`, `unit-backed`, `e2e-backed`, `planned`, `blocked`, not a generic stub.

### 3.4 `STATE-MACHINE.md`

**What is strong**

- Concise connection and query diagrams.
- Makes backpressure and idempotency visible.
- Establishes that the reducer is the state authority.

**Problems**

- Query dispose jumps directly to `Disposed`, hiding the runner termination handshake.
- There is no `Disposing`/`Canceling` edge state.
- Connection close waiting on an active query is represented only in prose, not as a state.
- No close timeout or failure path is shown.
- Open-cancel-close races are collapsed into self-loops.
- Result-set/page ordering is absent.
- The I2 wording conflicts with the active dispose scenario.

**Disposition**

Generate diagrams from the same transition table used by Core tests. Add:
- `ClosePending`;
- `Disposing`;
- timeout/fatal transitions;
- request waiter ownership;
- query streaming substates or a companion protocol automaton.

### 3.5 `CLIENT.md`

**What is strong**

- Clear lifecycle summary.
- Correctly emphasizes one active query and acks.
- Useful error and export notes.
- Uses a familiar `vscode-jsonrpc` client.

**Problems**

- The sample creates one completion promise before the first query, does not await the first query's completion, then starts another query.
- Notifications are not filtered by query ID.
- Pages from multiple result sets/queries are put in one array.
- There is no cleanup of listeners.
- There is no error/cancel/dispose path.
- It omits process startup/readiness, shutdown, fatal handling, and mux coexistence.
- The sample is “documented rather than run,” allowing drift.

**Disposition**

Replace the snippet with a runnable sample project tested in CI. Provide an `executeQuery()` helper with per-query state and ack policy.

### 3.6 `COMPONENTS.md`

**What is strong**

- Compact dependency inventory.
- Project/package boundaries are immediately visible.
- Generated from source.

**Problems**

- It lists references but not **ownership**, **threading**, **lifetime**, or **failure policy**, which are the most important aspects of this design.
- It does not state which component is allowed to block.
- It omits the replay tool in the project table.
- It does not show public API stability or data classifications.
- The prose says BCL-only where analyzer package references may still exist as build-only references, which should be clarified.

**Disposition**

Keep generated and add:
- owns / owned by;
- execution model;
- accepted input and emitted output;
- failure boundary;
- data classification handled;
- disposal responsibility;
- public API version.

### 3.7 `CONTRACT.md`

**What is strong**

- Clear method list and stable error identities.
- Useful milestone origin.
- Correctly distinguishes numeric JSON-RPC code from stable `data.code`.

**Problems**

At 45 lines, it is not a “wire contract.” It lacks:

- request/result/notification schemas;
- required versus optional fields;
- numeric/string bounds;
- unknown-field behavior;
- preconditions and state errors;
- exact idempotency semantics;
- notification ordering;
- ack semantics per result set;
- value encoding and truncation;
- capture/privacy behavior;
- compatibility/versioning rules;
- examples and negative examples.

**Disposition**

Generate JSON Schema and TypeScript definitions from the same contract source used by Hosting/Core validation. Render a full reference from those schemas.

### 3.8 `DECISIONS.md`

**What is strong**

- Repo facts are excellent.
- Decisions are concise and tied to evidence.
- It records why the shutdown behavior differs from generic LSP expectations.

**Problems**

- Decisions and deviations are split between this file and SPEC section 19.
- Entries do not consistently include status, alternatives, consequences, or supersession.
- There is no ADR for the major dispose/I2 semantic change.
- There is no ADR for sensitive runtime capture being client-controlled.
- A simple chronological list becomes hard to query.

**Disposition**

Convert one-way decisions to ADR files. Keep a generated ADR index. Retain repo facts in a separate `REPO-FACTS.md`.

### 3.9 `ENGINE-TESTS.md`

**What is strong**

- Clear local versus CI responsibility.
- Useful commands.
- Honest about SQLite limitations.
- Good intent to capture engine truth.

**Problems**

- It says the full truth-capture tool is built on the harness in CI, but the document does not identify a runnable tool, command, output manifest, or provenance.
- It uses a mutable container tag in examples.
- It does not state SQL Server version/edition compatibility policy.
- It lacks cleanup/isolation details and data-generation constraints.
- The verification evidence is prose rather than an immutable run artifact.

**Disposition**

Document exact tool entry points and freeze-format schema. Pin engine image digest for release evidence. Publish captured corpus hash and source engine metadata.

### 3.10 `INVARIANTS.md`

**What is strong**

- The invariant set is understandable and valuable.
- Test names are included for some items.
- It keeps determinism, privacy, backpressure, and transport safety visible.

**Problems**

- The introduction says the scenario runner and simulator check the invariants on every run, but the implementation checker handles only a subset.
- I2 conflicts with dispose behavior.
- I5 does not distinguish root kinds precisely and its checker does not require a cause for non-root envelopes.
- I8 talks about limits but the checker receives only a final leaked-session count.
- I15/I16 enforcement descriptions are narrower than the invariant statements.
- “Exercised by” is not the same as mechanically proven.

**Disposition**

Replace with generated `INVARIANT-COVERAGE.md` whose source is executable invariant registrations. For each invariant show:
- formal statement;
- scope/assumptions;
- enforcement owner;
- test classes/scenarios;
- mutation/fault that proves the test can fail;
- latest evidence.

### 3.11 `OBSERVABILITY.md`

**What is strong**

- A good integration-oriented guide.
- Explains the authoritative journal versus auxiliary observers.
- Clearly describes live tail, metrics, health/state, and setCapture.
- Honest about reserved envelope kinds.

**Problems**

- It claims a slow observer cannot stall the pump, but custom sinks are awaited.
- “Dropped tells the viewer exactly where to re-sync” overstates the current API; the subscription exposes a count, not a gap range/checkpoint.
- “recentErrors” is a lifetime histogram.
- The fatal health snapshot is not available through the pump after the pump has failed.
- There is no observer protocol version or compatibility contract.
- Sensitive payload visibility to custom sinks is not governed.
- It does not define sink disposal or session shutdown behavior.

**Disposition**

Keep as a guide, but base it on an isolated observer-mailbox design. Add a viewer checkpoint protocol:
`lastSeenSeq`, `firstAvailableSeq`, `droppedFromSeq`, `droppedThroughSeq`, `journalRunId`.

### 3.12 `sts_refactor.md`

**What is strong**

- Memorable narrative and excellent explanation of why the refactor matters.
- Communicates pure core, replay, privacy, and side-by-side adoption to a broad audience.
- Good “machine with windows” framing.

**Problems**

- “Byte-identical behavior” is too strong for current replay, which compares selected authoritative payload digests and excludes runtime overlays/delivery.
- “Complete, tamper-evident record” overstates active-segment and manifest behavior.
- “Slow sink drops, never blocks” is not true for arbitrary sinks.
- “Safe to adopt” is premature before final CI/rebase and lifecycle hardening.
- It blends pitch with current implementation status.

**Disposition**

Keep as `VISION.md`, clearly label goals versus current verified capabilities. Link each proof claim to generated evidence.

### 3.13 `what_was_built.md`

**What is strong**

- The best as-built map in the set.
- Good project-by-project and class-by-class orientation.
- Explains the end-to-end pipeline and observability additions.
- Helpful for reviewers and future maintainers.

**Problems**

- Calls itself “complete” while important gaps/stubs remain.
- Says eleven projects but the product table enumerates ten; the replay tool appears outside the table.
- Some source comments still mention removed toy state.
- It repeats behavior claims that belong in the normative design.
- It lacks a known-limitations section.
- It is hand-maintained enough to drift from source.

**Disposition**

Rename to `IMPLEMENTATION-STATUS.md`, generate project/type counts, and include:
- implemented;
- partially implemented;
- reserved;
- blocked;
- release-gating findings;
- last verified commit.

### 3.14 `design_diagrams.tex`

**What is strong**

- Consistent visual language and restrained palette.
- Covers the central concepts, especially journal-first flow and pure/live overlay.
- Pure TeX with no external assets makes it reproducible.
- Captions explain the architectural point rather than merely naming boxes.

**Problems**

- Most diagrams are resized to page width, making text small.
- Page 2 of the PDF has one sequence diagram and a large empty lower half.
- The architecture diagram makes journal fan-out look like `JournalWriter -> sinks`, while code publishes from Coordinator after append.
- The observer diagram promises slow-sink isolation not implemented for custom sinks.
- No trust/privacy boundary.
- No ownership/disposal diagram.
- No fatal propagation/shutdown barrier.
- No strict replay validation pipeline.
- Backpressure does not show credit before `MoveNext`.
- State machines omit pending/disposal/timeout states.
- No rollout/compatibility view.

**Disposition**

Replace with the supplied v2 visual set in this review package. Keep the palette, but use one readable concept per landscape page and explicitly label **authoritative current behavior** versus **recommended hardening**.

### 3.15 `design_diagrams.pdf`

**What is strong**

- Clean rendering, no obvious clipping.
- Page 1 gives a good architecture/fan-out overview.
- Page 3's overlay and live-tail figures are compact and understandable.
- Page 4's config/state-machine material is a useful review aid.

**Problems visible in the rendered pages**

- On page 1, the component labels are small relative to the surrounding prose.
- Page 2 wastes substantial space and could use the lower half for a cause-chain/replay inset.
- Page 3's live-tail diagram does not show sequence checkpoints or gap recovery.
- Page 4's state machines are too compressed to expose close/dispose races.
- The PDF has no diagram index, source commit, status legend, or distinction between current and target design.

**Disposition**

Use the v2 PDF generated with this package.

### 3.16 Branch-only `TRACE-SCHEMA.md`

**What is strong**

- Honest emitted/reserved status.
- Useful field table and redaction markers.
- Number-token canonicalization is explicitly recorded.

**Problems**

- It says cause is null only for external inbound/root control, while metric samples are emitted with null cause.
- It does not provide payload schemas by kind/type.
- It does not version redaction markers separately.
- The secret token format should not expose a raw digest prefix.

**Disposition**

Generate per-type schemas and a root-kind allowlist. Include compatibility rules for future envelope versions.

### 3.17 `artifacts/verification-report.md`

**What is strong**

- Evidence-oriented structure.
- Reports simulator, mutation, engine, perf, legacy diff, and generated docs.
- Includes risk notes and next steps.

**Problems**

- It is mutable prose committed in the same branch it validates.
- Commit labels contain `+` rather than immutable exact SHAs.
- The current head has no attached workflow run/status visible.
- Gate labels can say 10k while CI overrides the count.
- Environment, dependency lock, container digest, and artifact hashes are incomplete.
- Reports do not distinguish self-run local evidence from protected CI evidence.

**Disposition**

Generate a signed/machine-readable evidence manifest in CI from the exact commit. Render the Markdown summary from it. Never hand-edit outcomes.

## 4. Cross-document contradictions to resolve

| Topic | Documented claim | As-built/reviewed reality | Required decision |
|---|---|---|---|
| Query terminality | Every accepted query completes exactly once | active dispose suppresses complete | emit terminal or revise protocol/invariant |
| Observer isolation | slow observer never stalls pump | arbitrary sink is awaited | mailbox isolation or narrow contract |
| Replay identity | production behavior byte-identical | verifier ignores corr and can pass truncated EOF | define strict authoritative identity |
| Journal flush | bounded interval and completion flush | append-driven interval; query.complete not flush point | formal durability policy |
| Export safety | safe redacted bundle, replay checked | verbatim shared-dir copy; check omits replay | snapshot/redaction/check redesign |
| Capture | privacy by construction | client can enable full/text | host policy and authorization |
| Invariant coverage | I1-I16 checked every run | checker implements subset | generated owner matrix |
| Preview status | M7/final/complete language | branch behind main, stubs and blockers | status page and final gate |
| Client sample | await each completion | sample starts second query first | runnable fixed sample |

## 5. Documentation quality gates

Add these to `verify.sh`:

1. Every method has generated request/result/notification/error schemas.
2. Every invariant has an executable owner and at least one negative/fault test.
3. Every scenario row links to a real test or is explicitly planned.
4. Counts in project/scenario/status docs are generated.
5. No document uses “complete,” “identical,” “bounded,” “safe,” or “never blocks” without a linked definition/test.
6. Every ADR referenced by code exists and is accepted.
7. Client sample compiles and runs.
8. Mermaid/TikZ state diagrams are generated from transition tables or checked against them.
9. Verification reports contain exact SHA, merge base, environment, seed count, image digest, and artifact hashes.
10. The release status page fails if the branch is behind its target base or has release-critical stubs.

## 6. Recommended immediate edits

- Rename `sts_refactor.md` to `VISION.md`.
- Rename `what_was_built.md` to `IMPLEMENTATION-STATUS.md` and generate its inventory.
- Archive the original `SPEC.md`; extract `PROTOCOL.md`, three design docs, and ADRs.
- Replace the kickoff `AGENT-RUNBOOK.md` with a maintenance/contribution runbook.
- Expand generated `CONTRACT.md` into schemas plus reference prose.
- Generate an invariant ownership matrix.
- Correct the client sample and run it in CI.
- Replace the diagrams with `diagrams/STS2_DESIGN_VISUALS_V2.*`.
- Add a prominent status banner: **reviewed target, current gaps, last verified commit, branch divergence**.

The documentation already has the right raw material. The next level is not more pages. It is sharper authority, generated truth, and fewer sentences that can outrun the code.
