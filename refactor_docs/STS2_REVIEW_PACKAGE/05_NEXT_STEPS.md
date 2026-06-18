# STS2 Concrete Next Steps

This plan turns the review into an implementation sequence. It is ordered to reduce rework: first establish correct lifetime and durability boundaries, then harden replay and query semantics, then expand tooling and adoption.

## Guiding rule

Do not build the VS Code diagnostic viewer against the current observer contract yet. First stabilize:

- session/fatal lifetime;
- pump barriers;
- exact-run journal/replay;
- observer mailbox/checkpoint semantics;
- capture policy.

Otherwise the viewer will fossilize interfaces that need to change.

## Wave 0 - Establish the final integration baseline

### 0.1 Rebase onto current `main`

**Do**
- rebase/merge the five mainline commits;
- adopt current `global.json` SDK;
- resolve build pipeline changes;
- rerun generated docs/PublicAPI;
- record the new merge base and branch divergence.

**Files**
- `global.json`
- solution/project files
- `artifacts/verification-report.md`

**Exit**
- branch is zero commits behind intended base;
- quick gate green on rebased code;
- no legacy diff budget regression.

### 0.2 Fix CI topology before relying on reports

**Do**
- make STS2 workflow run on PRs targeting `main`;
- add path filters for STS2 plus legacy seam/build files;
- run push-to-main and manual/reusable workflow;
- report exact simulator seed count;
- upload TRX/JUnit, replay/export results, mutation, perf, generated-doc hashes;
- require the check in branch protection.

**Files**
- `.github/workflows/sts2-verify.yml`
- `verify.sh`

**Exit**
- a test PR from `sts2/main` to `main` has required checks attached;
- evidence manifest names the exact SHA/merge base/environment.

### 0.3 Freeze one-way contract questions as ADRs

Create reviewed ADRs for:
- active `query.dispose` terminality/order;
- host capture policy;
- durability classes;
- timer/deadline representation;
- observer isolation/data access;
- export bundle semantics;
- replay identity/version support.

**Exit**
- no implementation continues with implicit contract changes.

## Wave 1 - Repair lifetime, fatal containment, and barriers

### 1.1 Composite session completion

**Code**
- make `Sts2Session` own and observe:
  - JSON-RPC listener;
  - ordered outbound writer;
  - `Coordinator.Completion`;
  - effect runner completion;
  - observer worker completion.
- create one atomic session state/fatal descriptor.
- update Bootstrap to monitor all unexpected completions, not only RPC faults.

**Tests**
- fault journal append;
- fault output encoder;
- stop coordinator normally/unexpectedly;
- close RPC input;
- assert future v2 gets `Sts2.Unavailable`, pending requests terminate, v1 continues.

**Closes**
- R001, part of R027.

### 1.2 Pump barrier primitive

**Code**
- extend inbox items with optional completion/barrier;
- implement `PostAndWaitCommittedAsync`;
- use it for lifecycle, checkpoint, export, graceful stop;
- complete barriers only on the pump thread after outputs and requested flush/rotate.

**Tests**
- lifecycle interleaving matrix;
- barrier cancellation/timeout;
- process exit immediately after mux forwards shutdown.

**Closes**
- R002, foundation for R017/R020/R021.

### 1.3 Effect runner becomes a real owned service

**Code**
- implement `IAsyncDisposable`;
- track every `Task`, CTS, semaphore, and live session;
- stop intake before shutdown;
- cancel and await within bounds;
- dispose all remaining resources;
- make open ownership transfer safe if effect response posting fails.

**Tests**
- shutdown/fatal at every await point;
- open succeeds as inbox closes;
- query blocked on credit during dispose;
- zero resource counters afterward.

**Closes**
- R013, R014, part of R015.

### 1.4 Bounded cancel and close

**Code**
- cancel local query token before provider cancel;
- pass a bounded token to `CancelAsync`;
- add journaled timer/deadline inputs;
- model `ClosePendingQuery`;
- terminally resolve close after deadline with forced cleanup.

**Tests**
- provider cancel never returns;
- iterator ignores cancellation;
- close duplicate/race matrix;
- close request always terminates.

**Closes**
- R010, R015.

## Wave 2 - Make journal, replay, and export forensic-grade

### 2.1 One directory and reader per run

**Code**
- move to `sts2/<runId>/`;
- change `JournalReader` API to require manifest/run ID;
- support live active-tail read without consuming a partial final line;
- reject mixed run IDs.

**Tests**
- two runs in one parent;
- active partial line;
- corrupted segment order;
- missing segment.

**Closes**
- R007.

### 2.2 Strict envelope validation

**Code**
- strict schema parser;
- reject duplicate fields;
- recompute payload digest;
- validate root/cause, seq, run ID, config transition;
- add entity refs and cause rules for metrics.

**Tests**
- mutation corpus for every envelope field;
- duplicate JSON keys;
- invalid digest marker.

**Closes**
- R040-R042 and strengthens I5.

### 2.3 Split strict verify from partial replay

**Code**
- result enum: `Verified`, `Diverged`, `Incomplete`, `Corrupt`, `Unsupported`;
- compare corr/entity/config/cause/digest/output count;
- require pending output queue empty at EOF in strict mode;
- `until` reports partial group/pending outputs and never says identical.

**Tests**
- truncate after every sequence;
- remove/add/reorder outputs;
- mutate corr/config/cause;
- valid partial replay.

**Closes**
- R006.

### 2.4 Durability policy and writer

**Code**
- central `DurabilityPolicy`;
- query.complete becomes checkpointed;
- writer-owned timed flush;
- atomic manifest replacement;
- active segment checkpoint or rotation;
- document OS-visible versus disk-requested durability.

**Tests**
- idle interval flush;
- crash/fault injection during manifest update;
- query.complete and lifecycle abrupt termination.

**Closes**
- R020-R022.

### 2.5 Coherent export snapshot

**Code**
- request pump barrier;
- rotate/checkpoint exact run;
- pass immutable file inventory;
- apply export capture policy;
- create manifest/provenance/privacy report from actual transformations;
- strict replay inside `export-check`;
- supply or explicitly omit generated docs according to contract.

**Tests**
- export during row stream;
- two runs in parent directory;
- full-capture source exported in safe mode;
- self-consistent semantic tampering;
- zip traversal/oversize checks.

**Closes**
- R017, R023, R037.

## Wave 3 - Resolve query semantics and enforce bounds at the driver edge

### 3.1 Resolve `query.dispose` and I2

**Preferred design**
- add `Disposing`;
- stop/await runner;
- emit exactly one `query.complete` with pinned status;
- release connection only after runner stop;
- define whether dispose response precedes/follows complete.

**Code**
- Core transition table/state;
- runner `queryStopped` effect response;
- scenario and client contract.

**Tests**
- dispose before first event;
- during row page;
- while credit-exhausted;
- racing complete/error/cancel/close;
- immediately execute next query.

**Closes**
- R008, R009.

### 3.2 Correct ack model

**Code**
- track highest sent/acked page per result set;
- validate nonnegative integer bounds;
- clamp/ignore duplicates;
- reject future/cross-result-set acks;
- derive credit exactly.

**Tests**
- property generator for ack permutations and malformed numbers;
- invariant checks driver pull count, not only notification count.

**Closes**
- R011, R026.

### 3.3 Acquire credit before page advancement

**Code options**
1. manual async enumerator with credit before `MoveNextAsync`; or
2. change port to page-pull API:
   `ValueTask<ExecPage> ReadNextAsync(QueryCursor, CancellationToken)`.

Prefer the shape that can prove no database read/page materialization occurs without credit.

**Tests**
- instrument driver `MoveNext`;
- exhaust window and assert count is frozen;
- memory profile.

**Closes**
- R012.

### 3.4 Implement full paging contract

**Code**
- parse/validate query options;
- carry timeout/pageRows/pageBytes;
- byte-aware page builder;
- maxCellBytes truncation wrapper;
- preserve full column metadata;
- decide `last` semantics;
- enforce result-set/page/offset state in Core.

**Tests/scenarios**
- activate `cell-truncation-max-cell-bytes`;
- page row/byte boundaries;
- UTF-8 and binary sizes;
- rows-before-metadata/gaps/duplicates;
- multiple result sets.

**Closes**
- R024, R025.

### 3.5 Fix adapters

**SQLite**
- per-query CTS;
- stream incrementally;
- no full-result list;
- successful query after cancel.

**SqlClient**
- clear active command in finally;
- adapter-level single-query gate;
- bounded cancel;
- sanitize errors/messages;
- verify precision/scale/length/collation behavior.

**Closes**
- R016, R035, part of R031.

## Wave 4 - Make privacy and observability contracts real

### 4.1 Host capture policy

**Code**
- add `CapturePolicy` to Bootstrap/Session;
- product default denies full/text;
- Core receives allowed modes in `session.start`;
- setCapture includes reason/duration and cannot exceed policy;
- optional automatic journaled reversion;
- capability generated from policy.

**Tests**
- product E2E rejects elevation;
- permitted development mode journals actor/reason/version/expiry;
- config change during query scenario becomes active.

**Closes**
- R018, R036.

### 4.2 Secret leases and opaque tokens

**Code**
- request-scoped `SecretLease`;
- transfer only to accepted open effect;
- clear every terminal/fatal path;
- random/HMAC token format;
- safe memory zeroing where practical.

**Tests**
- invalid/Busy/duplicate/unknown-driver opens;
- session fatal before effect;
- cross-run token nondeterminism.

**Closes**
- R004, R032.

### 4.3 Turn-scoped capture restoration

**Code**
- remove session-wide fragment dictionary;
- create per-turn restoration context;
- typed internal captured values, not public JSON marker objects;
- destroy context after output/effect handoff.

**Tests**
- rejected executes and suppressed late events;
- forged `$redacted` objects;
- retained-memory profile.

**Closes**
- R005, R041.

### 4.4 Provider-message classification

**Code**
- stable safe message DTO;
- raw provider detail only in governed diagnostic channel;
- apply capture policy to query messages;
- scan command-line manifest via allowlist.

**Tests**
- canaries in provider exceptions/messages/server metadata/args;
- scan journal/log/state/export.

**Closes**
- R031, R033.

### 4.5 Observer mailboxes and checkpoints

**Code**
- coordinator uses `TryWrite` to per-sink bounded mailbox;
- worker invokes sink;
- exact dropped seq ranges;
- sink timeout/disable policy;
- redacted view declaration;
- correct live-tail ring implementation.

**Tests**
- sink never completes;
- sink throws intermittently;
- subscriber read/eviction/dispose races;
- viewer re-sync exact range.

**Closes**
- R003, R034, R047.

## Wave 5 - Transport, contract, docs, and client quality

### 5.1 Transport hardening

**Code**
- explicit oversized outbound status handling;
- checked length arithmetic;
- duplicate Content-Length rejection;
- strict top-level routing fields;
- shared activation parser;
- stderr/file emergency logging only;
- mark STS2 dead on any unexpected session completion.

**Tests**
- transport smuggling corpus;
- overflow/oversize;
- raw stdout emergency;
- mixed-case flags;
- normal EOF.

**Closes**
- R028-R030, R042.

### 5.2 Generate a real wire contract

**Code/docs**
- source DTO metadata or schemas;
- generate JSON Schema and TypeScript;
- full required/optional/bounds/errors/preconditions/ordering/idempotency reference;
- capabilities derived from composition.

**Tests**
- schema conformance for every method and notification;
- unknown/mustUnderstand/duplicate behavior.

**Closes**
- R036, R044.

### 5.3 Fix and run the client sample

**Code**
- queryId-keyed completion map;
- per-result-set page handling and ack;
- cancel/dispose/error/fatal;
- shutdown;
- listener cleanup;
- execute helper.

**CI**
- Node setup only for the sample job;
- run against spawned SQLite-enabled service.

**Closes**
- R045.

### 5.4 Rebuild documentation authority

**Do**
- archive initial SPEC/runbook;
- create protocol/design/ADR/status structure;
- generate component ownership and invariant coverage;
- link scenario rows to tests/evidence;
- remove overclaims;
- install v2 diagrams.

**Closes**
- R038, R044-R046, R050.

## Wave 6 - Viewer and product adoption

Begin only after Waves 1-4 stabilize the contracts.

### 6.1 Viewer protocol

Build against:
- run ID + sequence checkpoint;
- redacted authoritative envelope schema;
- gap range and journal resync;
- cause tree;
- state at seq;
- runtime overlay marked separately;
- fatal snapshot;
- capture-policy visibility.

### 6.2 Viewer capabilities

First release:
- live ordered event list;
- filter by connection/query/kind/type/code;
- cause-chain graph;
- state diff between two seqs;
- gap detection/resync;
- replay/export validation;
- privacy banner and capture mode;
- fatal summary.

Do not render arbitrary full payloads by default.

### 6.3 Preview rollout

- internal dogfood;
- extension experimental flag;
- dashboard for fatal/error/cancel/close/drop rates;
- sampled support bundle review;
- rollback rehearsal;
- limited user cohort;
- broader preview only after SLOs and privacy audit.

## First ten pull requests

1. **Rebase + CI base filter + exact evidence manifest.**
2. **Composite session completion and pending-request failure.**
3. **Pump barriers and lifecycle durability tests.**
4. **Effect runner ownership/disposal and safe open transfer.**
5. **Exact-run reader + strict replay EOF/corr/digest validation.**
6. **Atomic journal/checkpoint + export snapshot.**
7. **Dispose/I2 ADR and implementation.**
8. **Ack ledger + pre-MoveNext credit.**
9. **SQLite streaming/cancel fix + page byte/cell limits.**
10. **Capture policy, secret leases, observer mailboxes.**

Each PR should be a vertical slice with:
- failing scenario/property/E2E first;
- implementation;
- generated contract/state/invariant diff;
- replay of produced journals;
- resource/privacy assertions;
- exact verification evidence.

## Preview exit checklist

### Correctness
- [ ] I1-I28 have executable owners.
- [ ] accepted query always has pinned terminal semantics.
- [ ] duplicate close/cancel/dispose races terminate.
- [ ] driver protocol ordering is enforced.
- [ ] no future/duplicate ack expands credit.

### Durability and replay
- [ ] lifecycle/export barriers are pump-owned.
- [ ] query.complete is checkpointed.
- [ ] timed idle flush is real.
- [ ] exact-run reader and atomic manifest.
- [ ] strict verifier rejects truncation and every mutation class.
- [ ] export-check performs strict replay.

### Lifetime
- [ ] composite session completion reaches mux.
- [ ] zero unobserved/fire-and-forget tasks.
- [ ] zero sessions/CTS/semaphores/secrets/fragments after teardown.
- [ ] bounded cancel/close/shutdown.

### Privacy
- [ ] product client cannot enable full/text capture.
- [ ] secret tokens are opaque.
- [ ] provider/error/message text classified.
- [ ] safe export transforms full-capture source.
- [ ] canary corpus covers every channel.

### Transport and adoption
- [ ] no raw stdout path.
- [ ] oversized/duplicate framing handled.
- [ ] final PR to main has required CI.
- [ ] branch is current with main.
- [ ] release-critical scenario stubs are zero.
- [ ] runnable client sample passes.
- [ ] viewer uses stable checkpoint/mailbox contract.

## Definition of “next level”

STS2 reaches the next level when its strongest adjectives are measurable:

- **deterministic** means strict replay proves a complete comparison;
- **write-ahead** means a pump barrier proves order and durability;
- **bounded** means the database is not advanced without credit;
- **isolated** means a stuck observer or dead STS2 cannot stall legacy;
- **private** means host policy, not client preference, controls persistence;
- **terminal** means every accepted operation has one pinned outcome;
- **self-explaining** means a safe bundle answers the incident without trusting mutable prose.
