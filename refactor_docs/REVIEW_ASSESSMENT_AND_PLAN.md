# STS2 Review — Assessment & Calibrated Plan

My inspection of `STS2_REVIEW_PACKAGE/`, with each load-bearing finding checked against the
current source (the review was read-only/uncompiled, so I verified exact lines rather than
trust the line refs). This doc says: what's real, what I'd push back on, and a plan ordered
to the actual goal (a trustworthy self-explaining substrate for a first-party VS Code viewer).

## Verdict on the review

**It's a strong, technically sound review.** The reviewer clearly read the code carefully;
the correctness findings I spot-verified are genuine bugs, several of them in the code I wrote
this session. This is the most useful kind of review — it found real things.

**One calibration caveat.** The review grades against a *production / merge-to-`main` /
untrusted-client* bar: signed export bundles, HMAC-keyed secret tokens, forensic two-phase
ownership handoffs, full JSON-Schema wire contracts, an I1–I28 invariant matrix, required CI
branch protection. None of that is *wrong* as a north star, but STS2 today is a **preview**,
runs **in-process beside legacy**, and its client is the **first-party MSSQL extension** over
**local stdio**. That deployment reality changes the urgency of a chunk of the findings. So
below I separate *true bugs* (fix regardless) from *production hardening* (real, but pace it)
from *threat-model judgment calls* from *over-scope*.

**Agreement on the headline:** don't tag the preview or merge yet. Not for most of the
reasons listed, but because a few Tier-1 bugs directly undercut the headline adjectives —
replay can pass on a truncated journal (R006), a client ack can defeat the bounded-streaming
claim (R011), and a duplicate close can strand a request (R010). Those must close before
"deterministic / bounded / terminal" is true. After Tier 1 + the fatal/observer items, the
preview tag is earned.

---

## Tier 1 — Verified bugs. Fix now. (cheap, pure correctness, no design debate)

These I confirmed in the current tree. None require a contract decision; most are a few lines.

| ID | Bug | Why it's real |
|---|---|---|
| **R026** | Reducer throws on bad JSON numbers (`GetInt32()` after only a `ValueKind.Number` check) in ack `throughPageSeq` and `session.start maxConnections`. | Violates the never-throws reducer contract; a `1e999`/`1.5`/`>Int32` value faults the pump. Fix: `TryGetInt32`. |
| **R011** | Ack over-grants credit. The high-water branch sets `pagesAcked = Max(pagesAcked, through+1)` with **no clamp to `PagesSent`**, so `unacked` goes negative and `creditToGrant` can exceed the window. | Defeats the I9 bounded-streaming guarantee with one malformed/duplicate ack. Verified at `Sts2CoreReducer` ack handler. |
| **R006** | Strict replay reports `Identical` for a truncated journal — the EOF path returns `Identical=true` without requiring the pending-output queue to be empty, and discards `corr` in the comparison. | Undermines the central determinism gate (I7). Split strict `Verify` (empty-queue + corr/cause/configVersion/digest) from partial `Until` (returns `Incomplete`). |
| **R010** | Duplicate `connection.close` while a query is active overwrites `CloseCorr`; the first close's correlation is lost and never answered (I1 violation). | Verified in the `Open + active query` close branch. Fix: keep the first waiter, answer later closes `{}`. |
| **R034** | `EnvelopeSubscription` creates the channel `SingleReader=true`, but `TryPush` (producer) calls `Reader.TryRead` to evict while the consumer also reads. | Violates the channel's single-reader contract → undefined under concurrent eviction. My code. Fix: `SingleReader=false` or a small locked ring. |
| **R020** | `query.complete` is not a flush point (`IsFlushPoint` keys on kind only; it's `rpc.out.notify`), despite SPEC/JournalWriter comments claiming completion is flushed. | Honesty/durability gap. Fix: flush policy on (kind, type), include query.complete + lifecycle + config + terminals. |
| **R036** | `initialize` advertises `capabilities.exportLog = false` though `diagnostics.exportLog` is implemented. | Capability lies to clients. Generate capabilities from the composed feature set, not a literal. |
| **R040** | `metric` envelopes are journaled with `cause=null`, but they're produced by a pump turn and the schema's root rule says non-root envelopes carry a cause. My code. | Pick one: set cause to the triggering input, or define a documented "system-root" category and update I5/schema. Cheap. |
| **R047** | Health field `recentErrors` is actually a **lifetime** error histogram, not recent. My code. | Rename to `errorsByCodeTotal` (or implement a real window). One-line honesty fix. |
| **R030** | Activation flag casing mismatches: Bootstrap uses `OrdinalIgnoreCase`, legacy arg filtering is case-sensitive, so `--ENABLE-STS2` can enable STS2 yet still hit the legacy unknown-arg path. | Real edge bug. Share one parser/normalizer. |
| **R035** | `SqlClientSession.activeCommand` is cleared only on the normal path, not in `finally`, so after an exception it points at a disposed command. | Real. Clear under a session gate in `try/finally`. |
| **R016** | SQLite driver: one session-wide cancel CTS never reset (a canceled query makes *later* queries cancel immediately), and result sets are fully buffered into a list (breaks bounded-memory). | Real adapter bugs. Per-query CTS + incremental streaming. |
| **R012** | Runner acquires credit *after* `await foreach` has already pulled a page (one-page overrun beyond the window). | Real for a truly streaming driver; soft today because Fake/SQLite buffer anyway. Manual enumerator or page-pull port. |

**Recommendation:** land Tier 1 as one or two focused PRs with tests. This is the highest
value-to-effort work in the whole package and removes the genuine blockers to the preview tag.

---

## Tier 2 — Real gaps that directly serve the viewer/self-explaining goal

These make the promises mechanical instead of conventional, and they're exactly the substrate
the viewer will depend on — so they should land *before* the viewer hardens against them.

- **R001 — fatal containment.** `Sts2Session.Completion` exposes `rpc.Completion` only; the
  Coordinator pump task is separate. I added a fatal guard that records `FatalReason` and
  re-throws, faulting `Coordinator.Completion` — but **nobody observes it**. Wire a composite
  session completion (RPC + pump + runner + outbound) so a journal/core/sink fault transitions
  STS2 to `Sts2.Unavailable` and fails pending requests. *This is the real "isolated" guarantee.*
- **R003 — observer isolation is overstated.** True finding. `CompositeEnvelopeSink` awaits each
  aux sink on the pump; my doc says "a slow observer can never stall the pump," but that only
  holds for *faults* (exceptions), not *slowness/blocking*. A blocking custom sink stalls the
  pump. Fix: per-sink bounded mailbox, coordinator publishes via `TryWrite` only, with explicit
  drop-range + per-sink health. (The built-in broadcast/metrics are already non-blocking, so the
  contract is sound — it just isn't enforced for third-party sinks.)
- **R002 — shutdown flush is not a barrier.** `SignalLifecycleAsync` enqueues the control then
  flushes immediately; enqueue ≠ processed. Add a pump-owned barrier (`PostAndWaitCommittedAsync`)
  that the pump completes only after journaling + draining prior outputs + flushing.
- **R007 — run isolation.** `JournalReader.ReadAll` globs all `journal-*-*.jsonl` in a directory,
  conflating runs that share a directory. Move to `sts2/<runId>/` and a reader keyed by run.
  *The viewer's resync and the replay gate both depend on never mixing runs.*
- **R013/R014 — session resource ownership.** The effect runner isn't `IAsyncDisposable` and
  isn't disposed by `Sts2Session.DisposeAsync`; a successful open whose `effect.res` post fails
  can orphan a live `IDbSession`. Make the runner an owned, disposable service.

**Recommendation:** these are the "viewer substrate" PR(s). They're aligned with the project's
actual purpose, not gold-plating.

---

## Tier 3 — Privacy: do the cheap cleanups, decide the policy ones

- **Do now (clear wins):**
  - **R032** opaque secret tokens — the token embeds the first 12 hex of `SHA-256(secret)`; a
    journal lets someone confirm low-entropy password candidates. Use a random/HMAC-per-run token.
  - **R004 / R005** lifetime cleanup — secret side-table entries and capture-elision fragments
    can linger on rejected/suppressed paths. A per-request/per-turn scope that disposes on every
    terminal closes both. (I already clear the elision table on dispose; this tightens it to per-turn.)
  - **R033** command-line allowlist instead of substring-`password` redaction.
- **Decide (ADR, threat-model dependent):**
  - **R018** host capture policy — should *any* v2 client be able to flip `setCapture` to
    `full/text` and persist SQL + row data? In STS2's deployment the client is the first-party
    extension, but journals/exports travel. I lean: a host `CapturePolicy` that defaults to
    deny-sensitive in product mode is the right call, but it's a one-way contract decision —
    write the ADR before coding.
  - **R031** provider/server message classification — server error text can carry object/db
    names. Real, but coupled to the capture-policy decision.

---

## Tier 4 — Real, but production/merge concerns — not preview-blockers

Pace these to when STS2 is heading to `main`, not now:

- **R019 / R039 — CI gating + rebase.** CI doesn't gate PRs into `main` and the branch trails
  `main` by 5 commits on an older SDK. Both true and worth doing **before merge**; neither blocks
  the design/viewer work. (Note: `sts2/main` is a long-lived shared branch — prefer *merging*
  `main` in over a history-rewriting rebase.)
- **R017 / R023 — forensic export.** Pump-barrier snapshot, immutable inventory, strict-replay
  inside `export-check`, signed manifest. Good target design; export is a diagnostic convenience,
  so this is a "make it forensic later" item, not a preview gate.
- **R044 / R046 — full wire-contract schemas + doc-authority restructure.** Generating JSON
  Schema/TypeScript and splitting normative/as-built/evidence docs is high value **for external
  client authors**. For a first-party viewer it's lower urgency. Worth doing; not blocking.
- **R024 — byte-aware paging (`pageBytes`/`maxCellBytes`).** The contract advertises limits it
  doesn't enforce. Fix the **honesty** now (stop advertising, or mark "not enforced in v2.0");
  defer the byte-aware page builder + truncation wrappers unless a real client needs them.

---

## Tier 5 — Where I'd push back / flag as over-scope

- **R008 "dispose contradicts I2" is a documented deviation, not a bug.** The SPEC and the
  active scenario *deliberately* exempt disposed queries from the exactly-one-`query.complete`
  rule. The reviewer frames a recorded design choice as a contradiction. The correct output is an
  **ADR** to confirm or refine the dispose terminality semantics (their "add `Disposing`, emit one
  terminal" design is reasonable) — but don't log it as a correctness blocker. R009 (release the
  connection before the old pump stops) *is* a real concurrency bug worth fixing regardless.
- **The I1–I28 expansion + meta-invariant ownership matrix (R038).** Good hygiene, but inventing
  ~12 new invariants and a generated coverage matrix is scope creep for a preview. Strengthen the
  two genuinely-weak spots (I5 root classification; flush/durability) and move on.
- **Signed bundles / HMAC per-run keys / zip-traversal scanners.** Defense beyond STS2's actual
  threat surface (in-process, local stdio, first-party client). Note as future-production, not preview.
- **R043 — replace `JsonDocument.Parse` hot paths with typed DTOs + `Utf8JsonWriter` everywhere.**
  A large rewrite of the JSON layer; the perf gate is already green (~135k rows/s). There *is* a
  small real smell (undisposed `JsonDocument` lifetime) worth a targeted fix, but the full rewrite
  is not justified by current numbers. Defer behind an allocation budget if one ever bites.
- **R027 — full ordered-awaitable bounded outbound writer.** Real (notifications are fire-and-forget),
  but for a local first-party stdio client the risk is modest. A lighter "observe the send task +
  count failures + cap backlog" is enough for preview; the full writer is a later refinement.
- **"Don't build the viewer until Waves 1–4 are done" — partly agree.** Don't *ship* or *fossilize*
  the observer contract, yes. But **do prototype** the viewer now against the live tail — it's the
  fastest way to learn what the checkpoint/gap/cause-tree contract actually needs before R003/R007
  freeze it. The advice should be "prototype to learn, harden after the substrate lands," not "wait."

---

## Recommended plan (recalibrated to: preview-quality substrate for a first-party viewer)

Ordered to close real blockers first and to not fossilize the observer contract.

1. **PR 1 — Tier-1 correctness sweep.** R026, R011, R006, R010, R034, R020, R036, R040, R047,
   R030, R035, R016, R012. Each with a failing test first. *Earns back the headline adjectives.*
2. **PR 2 — Viewer substrate.** R001 (composite session completion → fatal containment), R003
   (observer mailboxes + TryWrite), R007 (one dir per run + run-keyed reader), R013/R014 (runner
   ownership/disposal). *Stabilizes the contract the viewer will bind to.*
3. **PR 3 — Replay/durability truth.** Strict-vs-partial replay (R006 deepened), `query.complete`
   + lifecycle as durability checkpoints (R020/R021), pump barrier for lifecycle (R002).
4. **PR 4 — Privacy cheap wins.** R032 opaque tokens, R004/R005 scoped cleanup, R033 allowlist.
5. **ADRs (one-way doors) — write before the code they gate:** dispose/I2 terminality (R008),
   host capture policy (R018/R031), durability classes, observer isolation/data view.
6. **Defer behind explicit decisions:** forensic export (R017/R023), full schema docs (R044),
   byte-aware paging (R024 — but fix the *advertising* honesty now), JSON-layer rewrite (R043).
7. **Integration gate (when heading to `main`):** merge current `main` in (R039), CI gates PRs
   into `main` with exact-evidence artifacts (R019).
8. **Then** harden the viewer against the now-stable substrate.

## On the V2 diagrams in the package

The reviewer's `STS2_DESIGN_VISUALS_V2.tex/pdf` is an alternative 12-page visual set. It overlaps
my `design_diagrams.tex` (7 figures). No need for both — pick one canonical set. I'm happy to
either fold their additional views into mine or adopt theirs; flag your preference.

## The two decisions I actually need from you

1. **Target bar.** Is this preview aimed at "trustworthy substrate for the first-party viewer"
   (Tiers 1–3 + ADRs, defer Tier 4/5), or "production-hardened, merge-to-`main`, treat the client
   as untrusted" (do most of Tier 4 too)? This reshapes how much of the package we schedule.
2. **The one-way doors.** Dispose/I2 semantics and capture policy are contract decisions, not just
   code. I can draft both ADRs with a recommendation for your sign-off before any implementation.
