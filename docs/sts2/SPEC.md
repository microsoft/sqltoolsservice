# STS2 Specification: In-Place Rebuild of SqlToolsService Core

**Status:** Agent-executable draft  
**Owner:** Karl  
**Repo:** microsoft/sqltoolsservice  
**Intended path:** `docs/sts2/SPEC.md`  
**Revision date:** 2026-06-12

This document is the contract. `MUST`, `MUST NOT`, `NEVER`, and `REQUIRED` are hard requirements. `SHOULD` requires a `docs/sts2/DECISIONS.md` entry when the implementation diverges. A code agent may make reversible implementation choices, but it may not silently change the wire contract, invariants, pinned defaults, privacy model, dependency boundaries, or legacy seam.

The goal is not only to produce working code. The goal is to produce a backend component that explains itself while it runs: every state transition, RPC frame, effect, cancellation, configuration change, and failure is structured, replayable, and testable. The code should be a machine with windows, not a fog bank with methods.

---

## 0. Repo facts the agent must verify before coding

These facts were checked while preparing this spec, but the agent MUST verify them locally in M0 because `main` can move.

1. `global.json` currently pins .NET SDK `10.0.203` with `rollForward: latestFeature`.
2. `Directory.Build.props` currently sets `SqlToolsServiceDotNetVersion` to `net10.0`, `SqlCoreDotNetVersion` to `net8.0`, and `SsmsDotNetVersion` to `net472`.
3. `src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj` currently targets `$(SqlToolsServiceDotNetVersion)`.
4. `HostLoader.CreateAndStartServiceHost(SqlToolsContext, ServiceLayerCommandOptions?, Stream? inputStream = null, Stream? outputStream = null)` already accepts optional streams. This is the preferred seam.
5. The existing repo uses root folder `test`, not `tests`. New STS2 tests belong under `test/sts2` unless repo policy has changed.
6. `src/Microsoft.SqlTools.ServiceLayer/Program.cs` is the likely composition-root edit point. If this has changed, record the current path in `DECISIONS.md` before editing.

If any fact is false, the agent must update `docs/sts2/DECISIONS.md` with a `REPO-FACT` entry and adapt the smallest safe plan. Only a material change to the seam or target frameworks is a Stop Condition.

---

## 1. Mission

Build a new minimal service core, named **STS2**, inside the existing SqlToolsService process, side by side with the legacy service, while sharing the single stdio JSON-RPC channel.

STS2 exposes only two product capabilities in v2.0:

1. **Connectivity:** open, close, and cancel database connections.
2. **Query execution:** execute SQL text on an open connection and stream results forward.

Everything else stays out of STS2 v2.0: language service, IntelliSense, object explorer, edit data, scripting, execution plans, schema tools, SQL projects, notebooks, profiling, rich UI state, batch splitting, and random-access grid caching. Those remain in legacy STS or move client-side in later work.

### 1.1 Success criteria

STS2 is successful when all of these are true:

- With STS2 disabled, existing v1 behavior is unchanged except for a tiny composition-root diff.
- With STS2 enabled, v1 and v2 messages can share one stdio stream without frame interleaving, ID collision, or shutdown confusion.
- The core state machine is deterministic and replayable from journals.
- A user can export a redacted bundle that lets a developer or code agent replay the run to a sequence number and inspect the same redacted state that existed in the original run.
- Product code has enforceable dependency boundaries and generated review artifacts, so humans review contracts and evidence rather than spelunking a cavernous diff.
- Unit tests, scenario transcripts, replay verification, simulator seeds, mutation testing, secret scans, and perf smoke tests guard the implementation.

### 1.2 Non-goals for v2.0

- No feature parity with legacy STS.
- No behavior changes for v1 clients.
- No AppDomains, AssemblyLoadContext isolation, or child process isolation.
- No net472 or SSMS support for STS2 projects.
- No interactive authentication flows. The client obtains tokens; STS2 accepts opaque credential material through a redacted secret side table.
- No server-side GO splitting.
- No server-side result cache or random-access subset API. Results are forward-only with backpressure.
- No direct dependency from STS2 core to SqlClient, ADO.NET, legacy STS namespaces, or UI concepts.

---

## 2. Hard constraints and design principles

1. **Single stdio:** The process has one stdin/stdout pair using Content-Length-framed JSON-RPC. A multiplexer owns the real streams when STS2 is enabled.
2. **Default off:** STS2 activates only with `--enable-sts2` or `STS_ENABLE_STS2=1`. Disabled means no multiplexer instance, no STS2 runtime, and legacy `HostLoader` receives `null` streams so its existing default behavior remains intact.
3. **Isolation by build graph:** STS2 projects MUST NOT reference legacy ServiceLayer assemblies or namespaces. Legacy statics are unreachable by construction.
4. **Tiny seam:** Legacy edits are limited to the composition root and project references needed to call STS2 bootstrap. Target budget: less than 60 changed legacy lines across no more than 3 files. Exceeding this requires `SPEC-CHANGE`.
5. **Everything is an envelope:** RPC input, RPC output, internal commands, events, effect requests, effect responses, timers, config changes, metrics, and diagnostics are structured envelopes and are journaled before dispatch.
6. **Pure core, async edges:** `Sts2.Core` is a synchronous deterministic reducer. I/O, clocks, randomness, timers, cancellation tokens, database drivers, StreamJsonRpc, channels, logs, and file systems live outside Core.
7. **Replay is a product feature:** `journal in -> identical outbound digest sequence out` is checked for every scenario and simulator seed.
8. **Privacy before convenience:** Secrets are tokenized before journaling. Production defaults do not capture SQL text or row payload text unless explicitly enabled.
9. **Driver-agnostic runtime:** Database access is behind a port. Fast tests use FakeDriver; portable real-I/O tests use Sqlite; SQL Server truth runs in container-backed suites.
10. **Human review surface is generated:** `CONTRACT.md`, `INVARIANTS.md`, `SCENARIO-MATRIX.md`, `TRACE-SCHEMA.md`, `STATE-MACHINE.md`, `COMPONENTS.md`, and `verification-report.md` are first-class artifacts.
11. **InvariantCulture always:** Any parse, format, serialization, hashing, or display fallback that can affect output uses `CultureInfo.InvariantCulture`.
12. **No gate gaming:** Lowering thresholds, weakening tests, changing golden files to match broken output, skipping tests, hiding failures, or adding product `testMode` branches is a spec violation.

---

## 3. Architecture overview

```
                  one stdin/stdout pair, Content-Length framed JSON-RPC
                                      |
                         +------------v-------------+
                         |     StdioMultiplexer     |
                         | route, id rewrite,       |
                         | lifecycle mirroring,     |
                         | single stdout writer     |
                         +------+-------------+-----+
                                |             |
                       virtual legacy    virtual STS2
                       duplex stream     duplex stream
                                |             |
                +---------------v--+      +---v-------------------------+
                | Legacy ServiceHost|      | STS2 Hosting               |
                | unchanged services|      | StreamJsonRpc gateway      |
                +------------------+      +---+-------------------------+
                                               |
                                               | sanitized envelopes
                                               v
                                  +------------+-------------+
                                  | Runtime Coordinator      |
                                  | write-ahead journal,     |
                                  | config, scheduling,      |
                                  | effect runner, metrics   |
                                  +------+-------------+-----+
                                         |             |
                                  pure messages    effect requests
                                         |             |
                               +---------v----+   +----v----------------+
                               | Sts2.Core    |   | Driver port         |
                               | reducer      |   | SqlClient/Sqlite/   |
                               | state machine|   | Fake adapters       |
                               +--------------+   +---------------------+
```

### 3.1 Package responsibilities

- **Multiplexer** owns real stdio only when enabled. It understands JSON-RPC framing and routing, but not STS2 domain logic.
- **Hosting** translates v2 JSON-RPC methods into sanitized envelopes and converts outbound envelopes into JSON-RPC responses or notifications.
- **Runtime** owns the coordinator pump, journal, redaction, secret side table, config snapshots, effect runner, scheduler, health, metrics, and replay integration.
- **Core** owns deterministic state transitions only.
- **Drivers** own database I/O only.
- **Testing** owns FakeDriver, scenario runner, simulator, golden transcript assertions, invariant checker, and test fixture helpers.
- **Replay tool** consumes journals and exported bundles without starting the product host.

---

## 4. Repo layout and dependency boundaries

New top-level additions:

```
src/sts2/Microsoft.SqlTools.Sts2.Contracts/       # wire DTOs, errors, limits, schema attributes
src/sts2/Microsoft.SqlTools.Sts2.Core/            # pure reducer, state, command/event/effect DTOs
src/sts2/Microsoft.SqlTools.Sts2.Abstractions/    # driver port, clock/id abstractions, no concrete I/O
src/sts2/Microsoft.SqlTools.Sts2.Runtime/         # coordinator, journal, redaction, effect runner, config
src/sts2/Microsoft.SqlTools.Sts2.Hosting/         # StreamJsonRpc gateway and v2 method handlers
src/sts2/Microsoft.SqlTools.Sts2.Multiplexer/     # BCL-only stdio multiplexer
src/sts2/Microsoft.SqlTools.Sts2.Bootstrap/       # composition root called by legacy Program.cs
src/sts2/Microsoft.SqlTools.Sts2.Drivers.SqlClient/
src/sts2/Microsoft.SqlTools.Sts2.Drivers.Sqlite/
src/sts2/Microsoft.SqlTools.Sts2.Testing/         # FakeDriver, scenarios, simulator helpers
tools/sts2-replay/
test/sts2/                                        # unit, scenario, contract, e2e, architecture tests
docs/sts2/SPEC.md
docs/sts2/AGENT-RUNBOOK.md
docs/sts2/CONTRACT.md                            # generated
docs/sts2/INVARIANTS.md                          # generated + curated prose
docs/sts2/SCENARIO-MATRIX.md                     # generated
docs/sts2/TRACE-SCHEMA.md                        # generated
docs/sts2/STATE-MACHINE.md                       # generated Mermaid + tables
docs/sts2/COMPONENTS.md                          # generated component/dependency inventory
docs/sts2/DECISIONS.md
docs/sts2/BLOCKERS.md
verify.sh
verify.ps1
sqltoolsservice-sts2.slnf
```

Allowed references:

| Project | May reference |
|---|---|
| Contracts | BCL only |
| Core | Contracts |
| Abstractions | Contracts |
| Runtime | Core, Contracts, Abstractions |
| Hosting | Runtime, Core, Contracts, Abstractions, StreamJsonRpc |
| Multiplexer | BCL only, including System.IO.Pipelines |
| Bootstrap | Hosting, Runtime, Multiplexer, Drivers.SqlClient, Drivers.Sqlite, Contracts |
| Drivers.SqlClient | Abstractions, Contracts, Microsoft.Data.SqlClient, Microsoft.SqlServer.Types |
| Drivers.Sqlite | Abstractions, Contracts, Microsoft.Data.Sqlite |
| Testing | Runtime, Core, Contracts, Abstractions |
| sts2-replay | Runtime, Core, Contracts |
| test/sts2 projects | product projects under test plus Testing |
| legacy ServiceLayer exe | Bootstrap only, plus existing references |

Enforcement:

1. Project references not in this table fail an architecture test.
2. STS2 source under `src/sts2/**` MUST NOT use namespaces beginning with `Microsoft.SqlTools.ServiceLayer`, except tests may reference the legacy exe for spawned E2E only.
3. Core and Contracts MUST NOT reference `System.Data`, `Microsoft.Data.*`, `StreamJsonRpc`, `System.Threading.Channels`, file APIs, network APIs, timers, console APIs, or legacy namespaces.
4. Contracts, Core, Abstractions, Runtime, and Hosting use `Microsoft.CodeAnalysis.PublicApiAnalyzers` with checked-in shipped/unshipped API files.
5. `docs/sts2/CONTRACT.md` is generated from the Contracts assembly and v2 handler metadata. `verify.sh` regenerates and fails on diff.
6. Every product project includes a `COMPONENT.md` fragment or source-generated metadata consumed by `COMPONENTS.md`.

---

## 5. Legacy seam and activation

### 5.1 Preferred seam

Use the existing optional stream seam in `HostLoader.CreateAndStartServiceHost`. The shape should be equivalent to:

```csharp
await using Sts2BootstrapHandle sts2 = Sts2Bootstrap.TryStart(
    args,
    commandOptions,
    logFilePath);

ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(
    sqlToolsContext,
    commandOptions,
    sts2.LegacyInputStream,
    sts2.LegacyOutputStream);
```

Requirements:

- When STS2 is disabled, `Sts2BootstrapHandle.Disabled` returns `LegacyInputStream = null` and `LegacyOutputStream = null`. This preserves the current `ServiceHost.Initialize(null, null)` path.
- When STS2 is enabled, Bootstrap opens the real console streams, starts the multiplexer, starts the STS2 host on the STS2 virtual stream, and returns the legacy virtual stream pair.
- Bootstrap is `IAsyncDisposable`. On process exit, shutdown, or fatal STS2 failure, it flushes and closes STS2 journals without blocking legacy shutdown indefinitely.
- The seam must not move service registrations, alter legacy handlers, or refactor legacy startup.

### 5.2 Flag and configuration activation

STS2 is enabled when either:

- command line contains `--enable-sts2`, or
- environment variable `STS_ENABLE_STS2=1`.

Command line wins over environment when a future explicit disable flag exists. Until then, absence of both means disabled.

### 5.3 Disabled-mode invariant

A disabled-mode E2E test spawns the real exe without `--enable-sts2`, sends representative v1 traffic, and asserts:

- no STS2 journal directory is created,
- no multiplexer diagnostic log is created,
- v1 response digests match a baseline captured from unmodified `main`, ignoring fields already known to vary, such as timestamps or process IDs,
- legacy diff stays within budget.

---

## 6. StdioMultiplexer

The multiplexer is small, deterministic where practical, and dependency-free beyond the BCL. It owns the real stdout writer. Nothing else may write protocol bytes to stdout while enabled.

### 6.1 Framing

- Reads Content-Length-framed messages with UTF-8 JSON payloads.
- MUST accept `Content-Length:33` and `Content-Length: 33`.
- MUST accept optional headers such as `Content-Type: application/vscode-jsonrpc;charset=utf-8`.
- MUST tolerate partial reads, coalesced frames, arbitrary chunk boundaries, and large-but-configured frames.
- MUST reject frames above `sts2.transport.maxFrameBytes` with a diagnostic and route behavior defined by the malformed-frame rule.
- MUST never deserialize full payloads for routing. Use `Utf8JsonReader` to inspect only top-level `method`, `id`, `result`, and `error`.

### 6.2 Inbound routing from client to service

For each complete inbound frame:

1. If it has top-level `method`:
   - `method` starts with `v2/`: route to STS2.
   - `method` is `shutdown`: route the raw frame to legacy only, and inject an internal `lifecycle.shutdown` control envelope into STS2. STS2 MUST NOT send a duplicate JSON-RPC response for this legacy request.
   - `method` is `exit`: inject `lifecycle.exit` into STS2 first, wait up to `sts2.runtime.exitFlushMs` for STS2's journal-flushed signal, then forward the raw notification to legacy. Legacy exit can terminate the process; without this ordering, the journal tail is lost on exactly the runs users report. Bootstrap additionally registers a best-effort `ProcessExit` flush to cover hard exits such as parent-process death via `ProcessExitTimer`.
   - otherwise: route to legacy.
2. If it has no `method` and has `id`: it is a response to a server-initiated request. Route by the outbound request-id rewrite table, restore the original id, and consume the table entry. Unknown id routes to legacy and records a diagnostic.
3. If it is malformed or cannot be minimally parsed: forward raw bytes to legacy, record a diagnostic, and continue.

### 6.3 Outbound routing from service to client

Each virtual service channel writes complete framed messages into the multiplexer.

- **Responses and notifications:** write to real stdout unchanged, under the single writer.
- **Server-initiated requests:** if a channel emits an outbound message with both `method` and `id`, the multiplexer MUST rewrite the id to a globally unique string before writing to stdout. It stores `publicId -> channel + originalId` and restores `originalId` on the inbound response to that channel.

Rationale: legacy and STS2 can both create server-to-client request id `1`. A plain `id -> channel` table is insufficient because collisions are possible. ID rewriting makes the shared transport safe even under adversarial interleaving.

Required tests:

- concurrent legacy and STS2 outbound requests with identical numeric ids,
- string, number, and null-like id handling where JSON-RPC permits it,
- inbound response restored to the exact original id representation,
- late/duplicate response id diagnostics,
- id table cleanup on timeout, shutdown, and channel death.

### 6.4 Single stdout writer

All outbound frames pass through one writer task or one writer lock. Frames MUST never interleave. A property test writes `N` concurrent producers times `M` frames with random chunking and asserts stdout parses back into exactly `N*M` intact frames.

### 6.5 Crash containment

If STS2 throws beyond its top-level boundary:

1. Journal `diag:fatal` and flush.
2. Emit `v2/fatal` notification with a redacted summary and journal path if the transport is still alive.
3. Mark STS2 channel dead.
4. Future `v2/*` requests receive a synthesized JSON-RPC error with `data.code = Sts2.Unavailable`.
5. Legacy traffic continues.

A poison-message test asserts legacy v1 traffic still succeeds after STS2 death.

### 6.6 Multiplexer diagnostics

Multiplexer diagnostics go to the STS2 diagnostic log and journal `diag` envelopes where possible. They MUST NOT write unframed text to stdout. They SHOULD NOT write to stderr except in standalone multiplexer tests.

---

## 7. Wire contract

Transport is JSON-RPC 2.0 over Content-Length framing. STS2 Hosting SHOULD use StreamJsonRpc with `HeaderDelimitedMessageHandler` and `SystemTextJsonFormatter`, unless the currently referenced StreamJsonRpc version requires an API-name adjustment. Behavior is pinned; exact constructor names are not.

All STS2 method names are prefixed with `v2/`. All generated identifiers are strings.

### 7.1 Versioning rules

- `specVersion` uses semantic versioning, starting at `2.0.0-preview.1`.
- Unknown request fields are ignored unless the field name starts with `mustUnderstand_`, in which case the request fails with `Sts2.InvalidRequest`.
- New response fields may be added only in minor versions.
- Removing or changing a field, enum value, error code, ordering guarantee, or default is a `SPEC-CHANGE`.
- `v2/initialize` returns server capabilities and limits. Clients MUST NOT assume optional capabilities without checking them.

### 7.2 Method table

| Method | Kind | Summary |
|---|---|---|
| `v2/initialize` | request | handshake, capabilities, limits, current config summary |
| `v2/connection.open` | request | open a database connection; params include client-generated `openId` |
| `v2/connection.cancel` | request | cancel an in-flight open by `openId` |
| `v2/connection.close` | request | close a connection; active query is canceled first |
| `v2/query.execute` | request | accept a query and return `queryId`; completion arrives by notification |
| `v2/query.resultSet` | server notification | result-set metadata |
| `v2/query.rows` | server notification | forward-only row page |
| `v2/query.message` | server notification | server info/error message as data |
| `v2/query.complete` | server notification | exactly one terminal query completion |
| `v2/query.ack` | client notification | backpressure credit or high-water mark |
| `v2/query.cancel` | request | cancel query by `queryId`; idempotent |
| `v2/query.dispose` | request | release query resources; idempotent |
| `v2/diagnostics.ping` | request | echo, health summary, latest journal seq |
| `v2/diagnostics.health` | request | counters, queue depths, open leases, recent errors |
| `v2/diagnostics.state` | request | redacted state snapshot at current seq or requested seq if available |
| `v2/diagnostics.exportLog` | request | produce redacted export bundle |
| `v2/diagnostics.setCapture` | request | change capture mode at runtime; journaled config change |

### 7.3 Initialize

Request:

```json
{
  "clientName": "vscode-mssql",
  "clientVersion": "1.0.0",
  "requestedSpecVersion": "2.0",
  "capabilities": {
    "ackHighWater": true,
    "diagnosticsState": true
  }
}
```

Result:

```json
{
  "specVersion": "2.0.0-preview.1",
  "serviceVersion": "99.99.99.0",
  "capabilities": {
    "forwardOnlyStreaming": true,
    "oneActiveQueryPerConnection": true,
    "redactedReplay": true,
    "exportLog": true,
    "setCapture": true,
    "maxCellBytesHonored": true,
    "pageRowsHonored": true,
    "pageBytesHonored": true,
    "queryTimeoutHonored": true,
    "compactRows": true,
    "vectorBinaryV1": true,
    "spatialWkbV1": true
  },
  "drivers": [
    { "name": "sqlclient", "dialects": ["tsql"], "production": true },
    { "name": "sqlite", "dialects": ["sqlite", "neutral"], "production": false }
  ],
  "limits": {
    "pageRows": 1000,
    "pageBytes": 262144,
    "windowPages": 4,
    "maxCellBytes": 1048576,
    "maxFrameBytes": 67108864
  },
  "journal": {
    "capture": "digest",
    "sqlCapture": "digest",
    "latestSeq": 0
  }
}
```

`initialize` is idempotent. Repeated calls return the current summary and do not reset state.

### 7.4 Connection profile

`v2/connection.open` params:

```json
{
  "openId": "open-7",
  "profile": {
    "server": "tcp:host,1433",
    "database": "master",
    "driver": "sqlclient",
    "auth": {
      "kind": "sqlLogin",
      "user": "u",
      "password": "..."
    },
    "options": {
      "encrypt": "strict",
      "trustServerCertificate": false,
      "connectTimeoutMs": 15000,
      "queryTimeoutMs": 0,
      "applicationName": "vscode-mssql"
    }
  }
}
```

Supported `auth.kind` values:

- `sqlLogin`: `user` plus `password`.
- `accessToken`: `token` only, with optional `user` for display.
- `integrated`: no secret payload.

The RPC gateway MUST tokenize secrets before creating envelopes. Core sees only `SecretRef` tokens.

`connection.open` result:

```json
{
  "connectionId": "c-1",
  "serverInfo": {
    "product": "Microsoft SQL Server",
    "version": "16.0.0",
    "engineEdition": "Developer",
    "engineEditionId": 3,
    "dialect": "tsql"
  }
}
```

`serverInfo.engineEdition` is the display name (`serverproperty('Edition')`).
`serverInfo.engineEditionId` (optional, additive 2026-07-11) is the numeric
`serverproperty('EngineEdition')` — 5 = Azure SQL Database, 8 = Managed
Instance — for exact platform gating; the display name cannot distinguish
SQL DB from MI (both report "SQL Azure"). Absent when the probe fails or a
driver does not know it.

### 7.5 Query execute

`v2/query.execute` params:

```json
{
  "connectionId": "c-1",
  "sql": "select 1",
  "options": {
    "queryTimeoutMs": 0,
    "pageRows": 1000,
    "pageBytes": 262144,
    "maxCellBytes": 65536,
    "includeStatistics": false
  }
}
```

Result:

```json
{ "queryId": "q-1" }
```

`query.execute` accepts or rejects the query. Execution completion is reported only by `v2/query.complete`.

`options.maxCellBytes` (OPTIONAL, SPEC-CHANGE-0001) lowers the per-cell wire bound for this query below the pinned `sts2.results.maxCellBytes` default. Absent, `0`, negative, or non-integer values mean the default applies (the pre-existing behavior); a value above the default clamps to it — a client can never raise the service's memory/frame protection. Cells above the effective bound arrive as `truncated` wrappers (§7.7); truncation is never silent. The `maxCellBytesHonored` capability (§7.3) advertises this behavior.

`options.pageRows` and `options.pageBytes` (OPTIONAL, D-0014) lower this query's page limits below the pinned `sts2.results.pageRows`/`sts2.results.pageBytes` defaults, with the same normalization as `maxCellBytes` (absent/`0`/negative/non-integer = default; larger clamps to the default). A page completes when EITHER limit is reached first; byte accounting is an approximation measured at page construction, and a single row larger than `pageBytes` arrives as its own one-row page (its cells still bounded per `maxCellBytes`). `options.queryTimeoutMs` (OPTIONAL, D-0014) passes a positive value through to the provider command timeout; absent/`0`/negative/non-integer means the provider default. Core normalizes all three into the journaled `driver.queryStart` args (replay-deterministic). The `pageRowsHonored`/`pageBytesHonored`/`queryTimeoutHonored` capabilities (§7.3) advertise this behavior; page-limit enforcement lives in the production driver's page builder.

`options.compactRows` (OPTIONAL, literal `true` only; D-0016) switches this query's `v2/query.rows` notifications to the compact page shape: `rows` is replaced by `compact: { values, nullBitmap, typeHints }` plus service-measured `approxBytes`/`encodedBytes`. `values` carries the same wire-encoded cells as the legacy shape; `nullBitmap` is a base64 row-major LSB-first bitmap over the page's cells; `typeHints` is the per-column display-type taxonomy computed once per result set. Exactly one of `rows`/`compact` is present per notification. Queries without the opt-in keep the legacy shape byte-for-byte. The `compactRows` capability (§7.3) advertises support.

`options.vectorEncoding` (OPTIONAL, literal string `"binary-v1"` only; D-0019) switches this query's native vector cells to the typed vector encoding (§7.7). Any other value — absent, wrong type, unknown literal — means the default applies: vector cells arrive as their JSON-array text representation (full float32 shortest-round-trip precision, D-0018). Core normalizes the option into the journaled `driver.queryStart` args as `vectorBinary` (replay-deterministic); the typed tag is never emitted to a query that did not opt in. With `compactRows`, an opted-in query's vector columns hint `vector:f32le:v1` in `typeHints`. The `vectorBinaryV1` capability (§7.3) advertises support.

`options.spatialEncoding` (OPTIONAL, literal string `"wkb-v1"` only; D-0020) switches exactly recognized native `geometry` and `geography` cells from the D-0018 SQL Server CLR-serialization binary fallback to the typed spatial encoding (§7.7). Any other value keeps the byte-for-byte default. Core normalizes the option into journaled `driver.queryStart` args as `spatialWkb`; the driver converts native bytes with `AsBinaryZM()` so Z/M ordinates survive, and provider CLR types never cross the driver boundary. Negotiated compact columns hint `spatial:wkb:v1`; result-set metadata adds `spatial:{kind,encoding:"wkb-v1"}` only for those columns. The `spatialWkbV1` capability advertises support.

Ordering guarantees for one query:

1. `query.resultSet` for a result set precedes any `query.rows` for that result set.
2. `pageSeq` is gapless per result set.
3. `rowOffset` is monotonic per result set.
4. `query.complete` appears exactly once.
5. No `query.rows`, `query.resultSet`, or `query.message` for a query may appear after `query.complete`.

### 7.6 JSON-RPC error shape

JSON-RPC response errors MUST use numeric JSON-RPC error codes. Stable STS2 error identity lives in `error.data.code`.

Example:

```json
{
  "jsonrpc": "2.0",
  "id": "r-12",
  "error": {
    "code": -32040,
    "message": "Connection failed during authentication.",
    "data": {
      "code": "Sts2.ConnectionFailed.Auth",
      "retryable": false,
      "safeDetails": "Login failed.",
      "server": {
        "number": 18456,
        "severity": 14,
        "state": 1,
        "line": null
      },
      "corr": "r-12"
    }
  }
}
```

Stable `data.code` values:

- `Sts2.ConnectionFailed.Auth`
- `Sts2.ConnectionFailed.Network`
- `Sts2.ConnectionFailed.Timeout`
- `Sts2.QueryFailed.Server`
- `Sts2.QueryFailed.Transport`
- `Sts2.Canceled`
- `Sts2.Busy`
- `Sts2.InvalidRequest`
- `Sts2.NotFound`
- `Sts2.Unavailable`
- `Sts2.Internal`

Raw exception strings MUST NOT be contract. They may appear only in diagnostic logs after redaction and only when the configured trace level permits it.

### 7.7 Values and pages

`v2/query.rows` notification:

```json
{
  "queryId": "q-3",
  "resultSetId": 0,
  "pageSeq": 12,
  "rowOffset": 12000,
  "rows": [[1, "abc", null, {"$t":"decimal", "v":"12.50"}]],
  "last": false
}
```

Encoding rules:

- Use JSON natives where lossless and unambiguous.
- Use typed wrappers for lossy or ambiguous values: `decimal`, `datetime`, `datetime2`, `datetimeoffset`, `date`, `time`, `money`, `binary`, `guid`, `xml`, `json`, non-finite floating values, and provider-specific values.
- Typed wrapper values are invariant strings. Binary uses base64.
- Column metadata carries engine type names verbatim plus normalized fields where known: precision, scale, nullable, length, collation.
- `DBNull` becomes JSON `null`.
- Cells above the effective `maxCellBytes` (the pinned `sts2.results.maxCellBytes` default, lowered per query by `options.maxCellBytes`, §7.5) become `{"$t":"truncated", "of": "string"|"binary", "bytes": N, "digest": "sha256:...", "v": "prefix..."}` (SPEC-CHANGE-0001): `of` says how to decode the retained prefix (`binary` prefixes are base64), `bytes` is the full value's byte count, and `digest` is the sha256 of the full value bytes. Prefix bytes are capped by `min(effective maxCellBytes, sts2.results.truncatedPrefixBytes)`, never split a UTF-8 code point, and bound the raw value bytes (base64 expansion applies on top for binary). Truncation is always client-detectable via the wrapper; it is never silent.
- Core state MUST NOT contain row cell payloads.

Native vector values (D-0018/D-0019):

- Default (no opt-in): a vector cell is its JSON-array TEXT representation as produced by the engine/provider (full float32 shortest-round-trip precision), an ordinary string cell subject to the ordinary bounds. CLR UDT cells (`geometry`, `geography`, `hierarchyid`) are the engine's CLR serialization bytes as an ordinary `binary` wrapper (or `truncated` above the bound).
- With `options.vectorEncoding = "binary-v1"` (§7.5), a vector cell is the typed tag: `{"$t":"vector", "version":1, "status":"ok", "dimensions": D, "baseType":"float32", "encoding":"f32le", "byteLength": D*4, "data":"<base64>"}`. The payload field is `data` (never `v` — clients treat generic `{$t, v}` objects as scalar wrappers). `data` decodes to exactly `byteLength` little-endian IEEE 754 float32 component bytes. Per-cell `dimensions` is authoritative over column metadata.
- Vectors are never truncated: a cell the driver cannot transport completely becomes `{"$t":"vector", "version":1, "status":"unavailable", "reason": "unsupportedBaseType"|"providerValueMismatch"|"decodeFailed"|"cellLimit"}` with optional `dimensions`/`baseType` facts. Ordinary NULL uses JSON `null`, not a vector status.
- Base types other than float32 (for example preview float16) are not transported as typed cells in v1: they keep the text representation when the provider yields one, else the `unsupportedBaseType` sentinel.

Native spatial values (D-0020):

- Default (no exact opt-in) remains D-0018: `geometry`/`geography` arrive as ordinary SQL Server CLR-serialization binary wrappers. `hierarchyid` is never spatial and always keeps that fallback.
- With `options.spatialEncoding = "wkb-v1"`, a complete cell is `{"$t":"spatial","version":1,"status":"ok","kind":"geometry"|"geography","encoding":"wkb","srid":N,"wkbBytes":N,"wkb":"<base64>"}`. `wkb` is complete `AsBinaryZM()` OGC/SQL-MM WKB; `wkbBytes` is exact; ordinary NULL remains JSON null.
- Spatial cells are never truncated or partially decoded. A bounded/conversion failure becomes `{"$t":"spatial","version":1,"status":"unrenderable","kind":...,"reason":"maxCellBytes"|"conversionFailed"|"unsupportedNativeValue"|"unsupportedInterchange","srid"?:N,"sourceBytes"?:N}`. It contains no coordinate material.
- The driver recognizes spatial columns from exact provider metadata (`geometry`, `geography`, or the database-qualified `.sys.geometry`/`.sys.geography` forms), never from cell values or substring guesses. Curves and FullGlobe may be faithfully transported even when a particular client renderer classifies the WKB type as unsupported.

### 7.8 Backpressure

Defaults:

- page size: 1000 rows or 256 KiB, whichever comes first,
- window: 4 unacked pages per query.

The client sends `v2/query.ack` as either per-page credit:

```json
{ "queryId": "q-1", "resultSetId": 0, "pageSeq": 12 }
```

or high-water credit:

```json
{ "queryId": "q-1", "resultSetId": 0, "throughPageSeq": 12 }
```

Ack ordinals are PER QUERY (D-0015): the window and the credit ledger count pages per query, so `throughPageSeq` is the cumulative per-query page ordinal — NOT the per-result-set `pageSeq` carried by `v2/query.rows` (which restarts at 0 for every result set). A client that acks the per-set seq freezes its high-water after the first result set and deadlocks any multi-result-set query longer than the window. `resultSetId` in the ack is diagnostic metadata; Core does not scope credit by result set.

When the window is exhausted, the effect runner MUST stop advancing the driver's async enumerator for that query. This is the backpressure mechanism. It is not a busy sleep and not an unbounded memory queue.

### 7.9 Idempotency and lifecycle rules

- `connection.cancel(openId)` is idempotent. Unknown or already completed `openId` returns `{}`.
- `query.cancel(queryId)` is idempotent. Unknown, completed, or disposed `queryId` returns `{}`.
- `query.dispose(queryId)` is idempotent. It releases runtime resources and suppresses further non-terminal output, but it must not erase journal history.
- `connection.close(connectionId)` is idempotent for already-closed connections. If a query is active, close requests cancellation first and completes only after resources are released or a bounded close timeout expires.
- A second active query on the same connection returns `Sts2.Busy`.

---

## 8. Envelopes, redaction, and journal

### 8.1 Envelope schema

Every communication unit and effect is represented as an envelope:

```json
{
  "schema": "sts2.envelope/1",
  "runId": "run-20260612-170322-8421",
  "seq": 412,
  "ts": "2026-06-12T17:03:22.1180000Z",
  "kind": "rpc.in.request",
  "sessionId": "c-7",
  "corr": "r-91",
  "cause": 408,
  "type": "v2/query.execute",
  "configVersion": 3,
  "digest": "sha256:9f31...",
  "payload": {},
  "payloadMeta": null
}
```

Fields:

- `schema`: envelope schema identifier.
- `runId`: stable per process run.
- `seq`: gapless monotonic integer assigned by the coordinator.
- `ts`: UTC round-trip string from injected time provider. Replay uses recorded values.
- `kind`: one of `rpc.in.request`, `rpc.in.notify`, `rpc.out.result`, `rpc.out.error`, `rpc.out.notify`, `cmd`, `evt`, `effect.req`, `effect.res`, `timer.due`, `config.changed`, `state.snapshot`, `metric`, `diag`, or `control`.
- `sessionId`: logical connection/session id when applicable.
- `corr`: JSON-RPC id for RPC envelopes, effect id for effect pairs, or generated correlation id.
- `cause`: seq of the envelope that produced this envelope. `null` only for external inbound and root control events.
- `type`: RPC method, command name, event name, effect name, metric name, or diagnostic name.
- `configVersion`: current config snapshot version.
- `digest`: SHA-256 of canonical payload representation. Present even when payload is elided or redacted.
- `payload`: inline payload when capture mode permits it.
- `payloadMeta`: redaction/elision metadata when payload is omitted or partially opaque.

### 8.2 Canonical payload digest

Canonicalization MUST be deterministic:

- UTF-8 encoding,
- sorted object keys by ordinal comparison,
- invariant formatting for numbers and dates,
- no insignificant whitespace,
- explicit representation for redacted scalars.

A redacted scalar is represented as:

```json
{ "$redacted": true, "kind": "sql", "digest": "sha256:...", "bytes": 1234 }
```

For digest computation during redacted replay, this wrapper is treated as the original scalar's authoritative digest marker. This is how stripped SQL and secret-tokenized journals can still verify causal behavior without recovering private text.

### 8.3 Journal files

The journal is append-only JSONL, plus a manifest.

Default location:

```
<sqltools-log-dir>/sts2/
  journal-<runId>.manifest.json
  journal-<runId>-0001.jsonl
  journal-<runId>-0002.jsonl
```

Manifest includes:

- schema version,
- run id,
- service version,
- git commit if available,
- process id,
- OS/runtime info,
- command-line flags with secrets removed,
- config snapshots,
- driver package versions,
- segment list with byte counts and SHA-256 hashes,
- previous-segment hash chain.

Write-ahead rule: an envelope is appended before it is dispatched. The writer flushes on request terminal responses, query completion, fatal diagnostics, lifecycle shutdown, and at a bounded interval for high-volume row streams.

### 8.4 Capture modes

Two orthogonal switches control capture:

- `sts2.capture = full | digest | minimal`
- `sts2.sqlCapture = text | digest | none`

Defaults:

- local tests: `capture=full`, `sqlCapture=text` with synthetic SQL only,
- local development when explicitly enabled: `capture=full`, `sqlCapture=text`,
- product default: `capture=digest`, `sqlCapture=digest`.

Mode behavior:

| Mode | Behavior |
|---|---|
| `full` | Payloads inline except secrets, which are always tokenized. |
| `digest` | Row pages elide cells to count, bytes, and digest. Large payloads may be summarized. |
| `minimal` | Payloads that may contain user data are replaced by typed redacted wrappers and digests. |
| `sqlCapture=text` | SQL text is journaled after secret scan. Not allowed in product default. |
| `sqlCapture=digest` | SQL text becomes digest, length, and statement-shape summary. |
| `sqlCapture=none` | SQL text is omitted except for a redaction marker. Replay is structural only for SQL-bearing effects. |

`v2/diagnostics.setCapture` can change capture at runtime. Every change emits a `config.changed` envelope and increments `configVersion`.

### 8.5 Secret handling

Credential fields are tokenized before journaling:

- `password`, `accessToken`, `token`, connection-string password keys, and any field under `auth` except `kind` and `user` become `SecretRef` tokens.
- Token format: an opaque random reference, currently
  `secret:ref:<32-hex-random>:<counter>`; it is not derived from the secret.
- The real secret is stored in an in-memory side table owned by Runtime and passed only to the effect runner.
- The side table is never serialized, logged, exported, or exposed by diagnostics.
- Gateway-created refs are tracked per request and removed on every terminal path.
  The effect runner also removes refs it resolves when the related open attempt
  completes; cleanup is idempotent.

Canary tests inject known fake secrets and grep every produced artifact.

### 8.6 Export bundle

`v2/diagnostics.exportLog` produces a shareable bundle:

```
sts2-export-<runId>.zip
  manifest.json
  privacy-report.json
  journals/*.jsonl
  diagnostics/*.log
  generated/CONTRACT.md
  generated/TRACE-SCHEMA.md
  generated/STATE-MACHINE.md
  generated/COMPONENTS.md
```

Export defaults to safe mode:

- secrets remain tokenized,
- SQL is stripped to digest unless caller explicitly asks for text and product config permits it,
- row payloads remain digest-only unless capture already contains full synthetic/test data,
- privacy report lists every redaction rule applied and canary scan result.

---

## 9. Runtime coordinator and deterministic core

### 9.1 Runtime pump

Runtime owns one coordinator pump backed by a bounded channel. Each input envelope is processed in this order:

1. assign `seq`, `ts`, `runId`, and `configVersion`,
2. journal envelope,
3. dispatch to Core reducer or Runtime control handler,
4. receive Core decision,
5. journal produced outbound messages, effect requests, timers, metrics, and diagnostics,
6. emit RPC output or schedule effects.

The journal order is the truth. Multi-session nondeterminism is captured as ordered input, then replayed in that order.

### 9.2 Core reducer

Core exposes a shape equivalent to:

```csharp
public static CoreDecision Decide(CoreState state, CoreEnvelope msg);
```

`CoreDecision` contains:

- next immutable `CoreState`,
- outbound RPC messages to emit,
- effect requests to run,
- timers to schedule,
- diagnostics to journal.

Core requirements:

- no I/O,
- no `async` or `await`,
- no `CancellationToken`,
- no `Task`, `Thread`, `Channel`, or timers,
- no driver handles except serializable ids,
- no row cell payloads in state,
- no exceptions escaping as control flow. Invalid input becomes a stable error output.

### 9.3 Determinism commandments

Enforced by analyzers, architecture tests, unit tests, scenario replay, and simulator replay:

1. No `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `Stopwatch`, or `Environment.TickCount` in Core, Contracts, or Abstractions.
2. No `Guid.NewGuid`, `Random`, `RandomNumberGenerator`, or ambient id creation in Core.
3. No `Task.Run`, `ThreadPool`, timers, locks, or channels in Core.
4. No culture-sensitive parse, format, compare, sort, or hash.
5. Any collection whose order reaches output is explicitly ordered or insertion-ordered.
6. Cancellation is a message (`v2/query.cancel`, `v2/connection.cancel`), not StreamJsonRpc transport cancellation.
7. All effect responses re-enter through the coordinator as envelopes.
8. Replay determinism is checked for every scenario and fuzz seed.

### 9.4 Effect runner

Runtime effect runner is the bridge to async I/O.

- It receives journaled `effect.req` envelopes.
- It calls driver ports, timers, file export writers, or other edge effects.
- It converts every result, yielded row page, exception classification, timeout, or cancellation observation into `effect.res` envelopes.
- It owns live driver handles and maps them to serializable handle ids.
- It enforces leases and emits metrics for open sessions, active readers, queue depths, and cancellation latency.

---

## 10. Driver port and adapters

The driver port lives in `Sts2.Abstractions`, not Core. Core emits serializable effect requests; Runtime translates them to driver calls.

### 10.1 Port shape

Representative shape:

```csharp
public interface IDbDriver
{
    string Name { get; }
    DriverCapabilities Capabilities { get; }
    ValueTask<IDbSession> OpenAsync(ConnectionOpenRequest request, CancellationToken ct);
}

public interface IDbSession : IAsyncDisposable
{
    ServerInfo Server { get; }
    IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, CancellationToken ct);
    ValueTask CancelAsync(string queryId, CancellationToken ct);
}

public abstract record ExecEvent;
public sealed record ExecStarted(string QueryId) : ExecEvent;
public sealed record ResultSetStarted(int ResultSetId, IReadOnlyList<ColumnInfo> Columns) : ExecEvent;
public sealed record RowsPage(int ResultSetId, int PageSeq, long RowOffset, CellPage Cells) : ExecEvent;
public sealed record ServerMessage(MessageClass Class, int Number, int Severity, string Text, int? Line) : ExecEvent;
public sealed record ResultSetCompleted(int ResultSetId, long RowCount) : ExecEvent;
public sealed record ExecCompleted(IReadOnlyList<long> RowsAffected, string? Database = null) : ExecEvent;
```

Port rules:

- No ADO.NET types in port signatures.
- No provider exception types in port signatures.
- Provider-specific metadata appears as strings or stable DTOs.
- Secrets are passed as `SecretMaterial` obtained from the side table, never from Core state or journal payload.

### 10.2 SqlClient adapter

Production adapter using `Microsoft.Data.SqlClient`.

Responsibilities:

- build connection strings from sanitized profile plus secret side table,
- map SqlClient exceptions to stable `Sts2.*` error codes,
- map info messages and server errors to `ServerMessage`,
- implement cancellation with `SqlCommand.Cancel` plus cancellation token cooperation,
- ensure `SqlConnection`, `SqlCommand`, `SqlDataReader`, and event handlers are disposed/unsubscribed,
- preserve SQL Server type metadata, including provider type name, precision, scale, length, nullability, and collation where available.

### 10.3 Sqlite adapter

Portable real-I/O adapter using `Microsoft.Data.Sqlite`.

Purpose:

- prove the port is honest,
- exercise real async/file behavior without SQL Server infrastructure,
- run in quick CI.

Limitations:

- It does not validate T-SQL semantics.
- It does not validate SqlClient error severity, TDS token ordering, SQL Server decimal/datetimeoffset behavior, or server message interleaving.

### 10.4 FakeDriver

Deterministic scripted driver in `Sts2.Testing`.

Capabilities:

- scripted event sequences,
- hang points released by scenario `cancel`, `ack`, or named trigger,
- open timeout, auth fail, network fail,
- mid-resultset server error,
- severed connection mid-page,
- slow consumer,
- zero rows,
- huge pages,
- multiple result sets,
- `DBNull`, nulls, decimal, datetimeoffset, binary, guid, xml/json edge values,
- duplicate cancel,
- cancel-vs-complete race,
- dispose-while-streaming,
- close-while-query-active,
- lease tracking for sessions/readers.

### 10.5 Engine truth

Scenario tags:

- `dialect:neutral`: runs on Fake, Sqlite, and SQL Server container.
- `dialect:tsql`: runs on Fake and SQL Server container.
- `dialect:sqlite`: runs on Sqlite only when needed for adapter-specific behavior.

A capture tool runs the T-SQL corpus against SQL Server in a container and freezes FakeDriver scripts from observed truth. The fast suite should track engine behavior without requiring a live server on every run.

---

## 11. Configuration

### 11.1 Precedence

Configuration sources, lowest to highest:

1. pinned defaults in this spec,
2. repo/test defaults,
3. environment variables `STS2_*`,
4. command-line flags,
5. `v2/initialize` negotiated options,
6. `v2/diagnostics.setCapture` runtime changes.

Every effective startup config is written to the journal manifest. Every runtime change emits `config.changed`.

### 11.2 Pinned defaults

| Key | Default | Notes |
|---|---:|---|
| `sts2.enabled` | false | flag-gated |
| `sts2.driver.default` | `sqlclient` | client may request `sqlite` |
| `sts2.capture` | `digest` in product, `full` in tests | secrets never full |
| `sts2.sqlCapture` | `digest` in product, `text` in tests | product text requires explicit opt-in |
| `sts2.results.pageRows` | 1000 | max rows per page |
| `sts2.results.pageBytes` | 262144 | max bytes per page |
| `sts2.results.windowPages` | 4 | unacked pages per query |
| `sts2.results.maxCellBytes` | 1048576 | cell truncation threshold |
| `sts2.results.truncatedPrefixBytes` | 65536 | max retained prefix |
| `sts2.transport.maxFrameBytes` | 67108864 | 64 MiB |
| `sts2.journal.segmentBytes` | 67108864 | 64 MiB |
| `sts2.journal.flushIntervalMs` | 250 | row-stream flush bound |
| `sts2.query.oneActivePerConnection` | true | not configurable in v2.0 |
| `sts2.query.defaultTimeoutMs` | 0 | 0 means provider default/no timeout |
| `sts2.connection.connectTimeoutMs` | 15000 | profile can lower/raise within limits |
| `sts2.runtime.maxConnections` | 64 | fail with `Sts2.Busy` beyond this |
| `sts2.runtime.closeTimeoutMs` | 5000 | bounded close after cancellation |
| `sts2.runtime.exitFlushMs` | 500 | bounded journal flush before `exit` forwards to legacy |

Changing a pinned default is a `SPEC-CHANGE`.

### 11.3 State exposure controls

`v2/diagnostics.state` is enabled when STS2 is enabled, but it returns redacted state only:

- connection ids, query ids, states, counters, timestamps, config version,
- no secrets,
- no row cells,
- no SQL text unless `sqlCapture=text` and the request explicitly asks for it.

---

## 12. Observability and self-inspection

STS2 has three observability channels:

1. **Journal:** replayable activity log, authoritative for debugging and verification.
2. **Diagnostic log:** human-readable structured log for startup, fatal errors, multiplexer diagnostics, and environment details. Never stdout.
3. **Metrics:** EventSource counters plus `v2/diagnostics.health` snapshots.

### 12.1 Health snapshot

`v2/diagnostics.health` returns:

- latest journal seq,
- coordinator queue depth,
- effect queue depth,
- active connections,
- active queries,
- open driver leases,
- unacked pages per query,
- dropped/suppressed diagnostics count,
- fatal status if any,
- current config version,
- recent error code histogram.

### 12.2 State snapshot

`v2/diagnostics.state` returns redacted `CoreState` plus Runtime handle summaries. It MUST be deterministic JSON with ordered keys and stable ordering for lists.

The same state format is used by:

- `sts2-replay until --seq N`,
- scenario failure diffs,
- exported bundles,
- optional debugger launch.

### 12.3 Generated understanding artifacts

`verify.sh` regenerates and checks:

- `CONTRACT.md`: methods, schemas, error codes, defaults, capabilities.
- `TRACE-SCHEMA.md`: envelope kinds, payload schemas, redaction markers.
- `INVARIANTS.md`: invariant definitions and examples.
- `SCENARIO-MATRIX.md`: scenarios by method, fault, invariant, dialect, adapter.
- `STATE-MACHINE.md`: Mermaid diagrams for connection, query, cancellation, close/dispose.
- `COMPONENTS.md`: projects, public types, allowed references, banned namespaces, component purpose.

These files are the primary review surface for humans and future code agents.

---

## 13. Replay and time travel

### 13.1 CLI

`tools/sts2-replay` is a dotnet tool project.

Commands:

- `sts2-replay run <journal-or-bundle>`: replay and print outbound digest sequence.
- `sts2-replay verify <dir|glob>`: batch verification used by `verify.sh`.
- `sts2-replay until <journal-or-bundle> --seq N [--break]`: replay to seq and dump redacted state or launch debugger.
- `sts2-replay diff <journal-or-bundle>`: print first divergence with cause chain.
- `sts2-replay explain <journal-or-bundle> --seq N`: print causal tree around an envelope.
- `sts2-replay export-check <bundle>`: validate manifest hashes, privacy report, canary scan, and replayability.

### 13.2 Divergence detection

Replay does not re-execute external effects.

When Core emits an `effect.req`, the replayer matches it against the recorded journal by causal position and digest. On match, it injects recorded `effect.res` envelopes. On mismatch, replay stops and reports:

- expected effect digest,
- actual effect digest,
- cause chain,
- nearest state diff,
- config version,
- scenario name or simulator seed if known.

RPC outbound envelopes are checked the same way. The outbound digest sequence must match exactly.

### 13.3 Redacted replay

Redacted replay is supported when redacted fields provide authoritative digest markers. The replayer can verify structure, ordering, state, effect causality, and outbound digest sequence without private SQL text, row cells, or secrets.

Limitations are explicit:

- Bugs in value encoding require full row capture or a targeted synthetic repro.
- Bugs depending on SQL parsing cannot be reproduced from `sqlCapture=none` because STS2 core must not parse SQL in v2.0.
- SqlClient provider bugs may require engine replay or a minimized SQL script generated from a non-redacted repro.

### 13.4 Snapshots

Runtime may emit `state.snapshot` envelopes at low frequency or on request, but v2.0 replay MUST work from the start of the journal without snapshots. Snapshot support is an optimization, not correctness machinery.

---

## 14. Testing strategy

### 14.1 Test pyramid

1. **Pure unit tests:** Core reducer, state machines, canonical digest, redaction, value encoding, error mapping tables.
2. **Multiplexer tests:** framing, routing, id rewriting, lifecycle mirroring, crash containment, single-writer property tests.
3. **Scenario tests:** golden transcript YAML with FakeDriver. This is the executable spec.
4. **Contract tests:** same scenario corpus against Fake, Sqlite, and SQL Server where tagged.
5. **Replay tests:** every produced journal is replayed.
6. **Simulator:** deterministic random operation/fault schedules.
7. **E2E stdio tests:** spawn real exe with and without STS2, drive v1 and v2 concurrently.
8. **Static gates:** architecture tests, banned APIs, PublicAPI, generated docs diff, secret scan.
9. **Mutation testing:** Stryker.NET on Core and Contracts.
10. **Performance smoke:** streaming pipeline throughput, memory, and journal overhead.

### 14.2 Scenario format

Example:

```yaml
name: cancel-mid-stream
tags: [query, cancel, dialect:neutral]
driver:
  open:
    ok:
      serverInfo: { product: "Fake 1.0", dialect: neutral }
  script:
    - execStarted
    - resultSet: { id: 0, columns: [{ name: a, type: int, nullable: false }] }
    - rowsPage:  { id: 0, rows: 1000 }
    - hang: after-first-page
inbound:
  - request:
      method: v2/connection.open
      params: { openId: open-1, profile: $profiles.fakeBasic }
    bind: { connectionId: $c }
  - request:
      method: v2/query.execute
      params: { connectionId: $c, sql: "select 1" }
    bind: { queryId: $q }
  - waitFor: { notify: v2/query.rows, count: 1 }
  - request:
      method: v2/query.cancel
      params: { queryId: $q }
expect:
  outbound:
    - result: v2/connection.open
    - result: v2/query.execute
    - notify: v2/query.resultSet
    - notify: v2/query.rows
    - result: v2/query.cancel
    - notify: { method: v2/query.complete, match: { status: canceled } }
  invariants: [I1, I2, I3, I4, I5, I6, I7, I8]
```

Scenario features:

- `bind` captures ids from results.
- `$profiles.*` uses fixture profiles with canary secrets.
- `waitFor` is the only synchronization primitive.
- `expect.outbound` can match by method, code, status, digest, or partial JSON.
- Scenario runner prints minimal diffs, not walls of JSON.
- Each scenario writes a journal and immediately replays it.

Minimum corpus by end of M3: 50 scenarios covering:

- happy path: open, two result sets, interleaved messages, rows, complete, dispose, close,
- every FakeDriver fault,
- every stable error code,
- window exhaustion and resume,
- duplicate ack, late ack, unknown ack,
- duplicate cancel, late cancel, unknown cancel,
- dispose while streaming,
- close connection while query active,
- open cancel race,
- cancel vs complete race,
- shutdown mid-query,
- STS2 fatal containment,
- malformed frame routed to legacy,
- outbound server-request id collision,
- secret canary,
- SQL digest export replay,
- row digest replay,
- config change during query.

`STS2 fatal containment`, `malformed frame routed to legacy`, and `outbound server-request id collision` are multiplexer-layer behaviors. They are realized as multiplexer unit tests and spawned E2E tests, not YAML scenarios, and appear in `SCENARIO-MATRIX.md` with `adapter=multiplexer` so corpus accounting stays honest. The YAML scenario runner drives the coordinator and Core with FakeDriver and does not host a multiplexer.

### 14.3 Invariants

Checked by scenario runner and simulator on every run:

- **I1 Request terminality:** Every request receives exactly one terminal JSON-RPC result or error.
- **I2 Query terminality:** Every accepted query emits exactly one `query.complete`.
- **I3 No output after complete:** No query output follows `query.complete` for that query.
- **I4 Id validity:** Outbound messages never reference unknown, closed, or disposed ids except stable idempotent responses.
- **I5 Journal causality:** `seq` is gapless; every non-root envelope has a valid `cause` less than its own `seq`.
- **I6 Secret safety:** No raw secret canary, JWT shape, password key, or configured secret appears in any artifact.
- **I7 Replay determinism:** Replay reproduces the identical outbound digest sequence.
- **I8 Lease cleanup:** Driver sessions/readers are disposed by run end; live lease count never exceeds configured limit.
- **I9 Backpressure:** Unacked row pages per query never exceed `windowPages`.
- **I10 Stdout frame integrity:** Multiplexer output parses to intact frames with no interleaving.
- **I11 Dependency boundary:** STS2 assemblies reference only allowed projects and namespaces.
- **I12 JSON-RPC error shape:** Response errors have numeric `error.code` and stable `error.data.code`.
- **I13 Multiplexer id rewrite:** Server-initiated request ids cannot collide across legacy and STS2, and responses restore original ids.
- **I14 Lifecycle mirroring:** `shutdown` and `exit` flush STS2 without duplicate legacy shutdown responses.
- **I15 Config traceability:** Every effective config change is journaled and visible in replayed state.
- **I16 State redaction:** `diagnostics.state` and replay state dumps contain no secrets, row cells, or SQL text unless explicitly permitted.

### 14.4 Simulator

The simulator generates random but valid operation sequences and fault schedules from one seed:

- opens,
- open cancels,
- query executes,
- acks at random rates,
- cancels at random points,
- disposes,
- closes,
- shutdown/exit,
- driver faults,
- row sizes and result-set counts,
- config changes.

Quick run: 200 seeds. Full run: 10,000 seeds. A failure prints:

```
sts2-sim repro --seed 0x9F3A... --schedule <file>
sts2-replay diff <journal>
```

Flaky simulator failures are P0 determinism bugs, not tests to retry.

### 14.5 Engine matrix

| Scenario tag | Fake | Sqlite | SQL Server container |
|---|---:|---:|---:|
| `dialect:neutral` | quick | quick | full/nightly |
| `dialect:tsql` | quick with captured truth | no | full/nightly |
| `dialect:sqlite` | optional | quick | no |

SQL Server container runs in `verify.sh --full` and CI/nightly. Local absence of Docker may skip engine tests only when the report says so clearly.

### 14.6 Mutation, coverage, and perf

- Stryker.NET mutation score on Core and Contracts starts at 70 percent and ratchets up only. Runtime's pure units (canonical digest, redaction, envelope codec, journal manifest) are included via mutate filters at a 60 percent starting threshold, also ratchet-up only. After the Core/Runtime split, those units hold the most bug-prone deterministic logic and must not sit outside the anti-fake-test gate.
- Coverage target is at least 85 percent line coverage on STS2 assemblies, informational secondary gate.
- Perf smoke streams 1 million rows times 10 columns through the full pipeline in digest mode. Default gate: at least 50k rows/sec on CI hardware, less than 20 percent regression from baseline, and less than 10 percent journal overhead versus journaling disabled after M3 baseline.
- Memory smoke asserts bounded queues and no row-cell retention in Core state.

---

## 15. Verification scripts and reports

### 15.1 `verify.sh --quick`

Runs:

1. restore and build STS2 solution filter with warnings as errors,
2. unit tests,
3. multiplexer property tests,
4. scenario tests on Fake,
5. contract tests on Fake and Sqlite,
6. replay verify over produced journals,
7. simulator with 200 seeds,
8. architecture and banned-API tests,
9. secret canary scan over artifacts, logs, journals, generated docs, and test output,
10. regenerate `CONTRACT.md`, `TRACE-SCHEMA.md`, `STATE-MACHINE.md`, `SCENARIO-MATRIX.md`, `COMPONENTS.md`, PublicAPI files, and fail on diff,
11. legacy diff budget check,
12. disabled-mode v1 smoke plus enabled-mode v1+v2 E2E once M0 seam exists,
13. emit `artifacts/verification-report.md`.

### 15.2 `verify.sh --full`

Includes all quick gates plus:

- SQL Server container suite,
- T-SQL truth capture comparison,
- Stryker mutation testing,
- simulator with 10,000 seeds,
- perf and memory smoke,
- cross-platform spawned E2E where CI supports it,
- export bundle round-trip.

### 15.3 Report format

`artifacts/verification-report.md` prepends one entry per milestone:

```markdown
## M<n> - <name> - <date> - <commit>
Gates: build ✓ | unit 312 ✓ | scenarios 52 ✓ | fake 52 ✓ | sqlite 41 ✓ | engine n/a
Replay: 93/93 journals identical
Simulator: seeds=200 failures=0 lastFailure=none
Mutation: core 78% threshold=70 previous=76 status=ratcheted
Perf: 1M rows 14.2s 70k rows/s journalOverhead=6%
Legacy diff: 34 lines / 2 files
Generated docs: CONTRACT ✓ TRACE-SCHEMA ✓ STATE-MACHINE ✓ SCENARIO-MATRIX ✓ COMPONENTS ✓
API surface: +2 / -0 PublicAPI updated
Invariants: I1x112 I2x31 I3x88 I4x93 I5x93 I6x93 I7x52 I8x17 I9x4 I10x1 I11x1 I12x22 I13x5 I14x3 I15x4 I16x6
Decisions: D-0004, D-0005
Blockers: none
Risk notes:
- <up to 3 honest bullets>
Next: M<n+1>
```

Reports must include evidence, not code summaries.

---

## 16. Milestones

Each milestone is one PR-sized slice, one tag, and one verification report. The agent may work locally in more commits, but report by milestone.

### M0: Skeleton, repo reality, seam, multiplexer

Definition of done:

- Repo facts in §0 verified and recorded.
- STS2 solution filter created.
- Projects scaffolded with dependency matrix.
- Architecture test live.
- Banned API analyzer live for Core, Contracts, and Abstractions.
- Bootstrap uses preferred HostLoader optional-stream seam or stops with `SPEC-CHANGE` if impossible.
- Multiplexer complete: framing, routing, lifecycle mirroring, id rewriting, single stdout writer, crash containment.
- `--enable-sts2` and `STS_ENABLE_STS2=1` activation implemented.
- E2E spawned exe answers `v2/diagnostics.ping` over real stdio when enabled.
- E2E spawned exe also answers at least one representative v1 request (for example `version`) in the same enabled-mode session, proving the multiplexer does not break legacy routing on day one.
- E2E disabled-mode v1 smoke remains green.
- `verify.sh --quick` exists and is green.

### M1: Spine, journal, replay, generated review surface

Definition of done:

- Envelope schema and canonical digest implemented.
- Write-ahead JSONL journal and manifest implemented.
- Redaction and secret canary implemented.
- Coordinator pump and pure Core skeleton implemented with toy state.
- `sts2-replay run`, `verify`, `until`, `diff`, and `explain` implemented for toy state.
- Generated `CONTRACT.md`, `TRACE-SCHEMA.md`, `STATE-MACHINE.md`, `SCENARIO-MATRIX.md`, `COMPONENTS.md`, and `INVARIANTS.md` exist.
- Draft scenario corpus has at least 50 stubs covering the mandatory list.
- Replay determinism test green.
- Human gate: stop for review of contract, invariants, scenario matrix, trace schema, component map, and report.

### M2: Connection vertical slice

Definition of done:

- `initialize`, `connection.open`, `connection.cancel`, and `connection.close` implemented end to end on FakeDriver.
- Connection state machine generated doc reflects implementation.
- Connection fault scenarios green.
- Error model and JSON-RPC error shape verified.
- Secret side table lifecycle verified.
- PublicAPI files checked in.

### M3: Query streaming vertical slice

Definition of done:

- `query.execute`, `query.resultSet`, `query.rows`, `query.message`, `query.complete`, `query.ack`, `query.cancel`, and `query.dispose` implemented on FakeDriver.
- Backpressure enforced.
- Row digest journaling implemented.
- Full query/cancel/dispose/close fault and race scenario set green.
- Minimum 50 scenarios green.
- Perf and memory baseline recorded.

### M4: Sqlite adapter

Definition of done:

- Sqlite adapter implemented.
- `dialect:neutral` contract suite green on Sqlite.
- Real file-backed and in-memory Sqlite modes tested.
- Architecture test asserts Core and Contracts contain zero `Microsoft.Data.*` references.

### M5: SqlClient adapter and engine truth

Definition of done:

- SqlClient adapter implemented.
- SQL Server container suite scripted for `verify.sh --full`.
- `dialect:tsql` corpus implemented.
- T-SQL truth capture tool freezes FakeDriver scripts from container observations.
- Type encoding matrix tests pass: decimal, datetime, datetime2, datetimeoffset, date, time, money, binary, guid, xml/json text, null/DBNull, provider-specific passthrough.
- SqlClient cancellation and disposal leases verified.

### M6: Client interop and export loop

Definition of done:

- `docs/sts2/CLIENT.md` generated or written with protocol examples.
- Runnable TypeScript sample using `vscode-jsonrpc` does initialize, open, execute, stream, ack, cancel, dispose, close.
- Spawned E2E drives v1 and v2 concurrently under load.
- Export bundle round-trip passes: export, privacy check, replay, state until seq.
- Multiplexer interleaving torture test passes with legacy and STS2 server-initiated request id collisions.

### M7: Hardening, evidence, and preview tag

Definition of done:

- `verify.sh --full` green in CI-capable environment.
- Stryker gate wired with ratchet.
- 10,000-seed simulator green.
- Perf gate stable against M3 baseline or documented with `SPEC-CHANGE` if threshold must move.
- Final generated docs committed.
- Final verification report complete.
- Tag `sts2-v2.0.0-preview`.
- Human gate: final review of generated contract, scenario corpus, invariants, verification report, export privacy report, and legacy diff.

---

## 17. Pinned decisions

| # | Decision | Default |
|---|---|---|
| D1 | RPC library | StreamJsonRpc with HeaderDelimitedMessageHandler |
| D2 | JSON formatter | SystemTextJsonFormatter when available in pinned package |
| D3 | Method namespace | `v2/` prefix |
| D4 | Transport isolation | In-process multiplexer on one stdio |
| D5 | Code isolation | Build graph and architecture tests, no AppDomain/ALC/child process |
| D6 | Legacy seam | Program composition root plus Bootstrap, using HostLoader optional streams |
| D7 | Core model | Pure synchronous reducer |
| D8 | Runtime model | One coordinator pump, async effects at edges |
| D9 | Query concurrency | One active query per connection |
| D10 | Batching | No server-side GO splitting |
| D11 | Results | Forward-only streaming, window 4, page 1000 rows or 256 KiB |
| D12 | Product capture | `capture=digest`, `sqlCapture=digest` |
| D13 | Test capture | `capture=full`, synthetic SQL text allowed |
| D14 | Auth | sqlLogin, accessToken, integrated |
| D15 | Journal | JSONL with manifest and segment hash chain |
| D16 | Replay | causal-position plus digest, no effect re-execution |
| D17 | Mutation threshold | 70 percent Core/Contracts, 60 percent Runtime pure units, ratchet up only |
| D18 | Test root | `test/sts2` unless repo policy changes |

Changing any pinned decision requires `SPEC-CHANGE` and human review.

---

## 18. Agent-facing code quality rules

These rules exist because the code will often be authored or modified by AI agents.

1. Every component has a single public entry point where practical.
2. Every project has a short README or generated `COMPONENTS.md` entry describing its role, inputs, outputs, dependencies, and forbidden dependencies.
3. Public DTOs are records or immutable types with XML docs where PublicAPI requires them.
4. State transitions are table-driven or reducer-driven, not scattered through event handlers.
5. Runtime side effects are behind interfaces and produce envelopes.
6. Scenario tests should read like product stories. Unit tests can be narrow, but scenario names must be searchable in reports.
7. Any bug fix must add either a scenario, simulator seed, or invariant. A plain unit test is not enough for a state/transport bug.
8. Generated docs must be deterministic and checked into the branch.
9. Failure output must include the smallest useful diff and the replay command.
10. No clever hidden behavior: no reflection dispatch where a normal table is clearer, no ambient singletons in STS2, no product test switches, no swallowed exceptions.

*End of spec.*

---

## 19. Implementation deviations log

Per Karl (2026-06-12, M1 review gate): the sections above are the initial brainstorming
contract and are preserved verbatim. Where the implementation deliberately deviates to
make the system better, the deviation is recorded here (and mirrored in DECISIONS.md)
instead of halting for SPEC-CHANGE review. Privacy, determinism, and no-gate-gaming
rules remain non-negotiable.

| # | Date | Deviates from | Change and rationale |
|---|---|---|---|
| DEV-001 | 2026-06-12 | §6.2 / §16 | Legacy `shutdown` calls `Environment.Exit(0)` without responding and `exit` is never handled (RF-0011), so the bounded journal-flush wait (`sts2.runtime.exitFlushMs`) applies to `shutdown` as well as `exit`; `ISts2LifecycleSink.OnShutdown` is `OnShutdownAsync`. |
| DEV-002 | 2026-06-12 | §5.1 sketch | `Sts2Bootstrap.TryStart(string[] args, string? logFilePath)` omits the sketch's `commandOptions` parameter: Bootstrap cannot reference legacy types under the §4 matrix, and raw args suffice. |
| DEV-003 | 2026-06-12 | §8.2 | Canonical JSON preserves number tokens verbatim (wire-faithful) instead of normalizing through a numeric type, which would silently alter precision. Frozen by golden tests. |
| DEV-004 | 2026-06-12 | §4 matrix | Testing may additionally reference the YamlDotNet package for the §14.2 scenario runner (approved by Karl). The architecture test allowlist is updated to match. |
| DEV-005 | 2026-06-13 | §8.4 / §13.3 | Digest capture (rowCapture/sqlCapture=digest) elides row cells and SQL text to authoritative-digest wrappers BEFORE journaling and digest computation, with an in-memory fragment side table substituting originals back at the wire/effect edges. This keeps replay fully digest-identical (I7) in digest mode — stronger than §13.3's "structural only" expectation — because Core relays wrappers verbatim and digests are computed over the elided form on both record and replay. |
| DEV-006 | 2026-06-13 | §16 M3 | Backpressure credit lives in Core (granted via driver.queryAdvance on ack); the enumerator pull loop in the runner blocks on a per-query credit semaphore. Close-while-opening now closes the connection if the open wins the cancel race (was a session-leak orphan, found by simulator seed 47). |
| DEV-007 | 2026-06-16 | §12 | **Event-capture framework.** Observability is now a first-class fan-out seam (`IEnvelopeSink`): every journaled envelope is delivered, in seq order, to ordered auxiliary observers after the write-ahead journal append. The journal is the privileged first sink; aux sinks (metrics, live tail, test capture) are best-effort, fault-isolated, and counted — a slow/faulty observer can never stall the pump or break write-ahead. Built-ins: `MetricsEnvelopeSink` (+ `Sts2EventSource` EventCounters), `BroadcastEnvelopeSink` (in-process live tail with bounded drop-oldest-with-count subscriptions, the diagnostic viewer's feed). This realizes the §12 "three channels from one stream" intent as an extensible framework rather than a single hard-wired journal consumer. |
| DEV-008 | 2026-06-16 | §12.1 / §12.2 | **Complete health + unified state dump.** `diagnostics.health` Core output is pure (replay-deterministic); the coordinator overlays the live Runtime facts (queue depth, driver leases, fatal+reason, dropped-diagnostic counts, configVersion, recent-error histogram) onto the wire response — the same authoritative-journal/enriched-wire split as DEV-005. `fatal` is now real (pump-fault guard). The two divergent state dumps (`DecideState`, `JournalReplayer.DumpState`) are unified into one pure `CoreStateDump` (enriched with the machine flags that explain a parked connection/query); live `diagnostics.state` overlays a `runtime` handle section replay cannot know. |
| DEV-009 | 2026-06-16 | §8.4 / §11 | **Runtime capture config in Core state.** Capture mode (rowCapture/sqlCapture) and configVersion live in `CoreState`, seeded via the journaled `session.start` (so replay starts identically) and changed by `v2/diagnostics.setCapture`, which journals a `config.changed` envelope and bumps configVersion (I15 now exercised). `config.changed` is a Core-emitted output matched in replay position, so the change is replay-visible. The coordinator reads modes/version straight from state; `CoordinatorOptions.RowCapture/SqlCapture` and the three hard-coded `configVersion=1` literals are gone. The `metric` envelope kind is revived as opt-in journaled snapshots (`CoordinatorOptions.MetricSampleEvery`), replay-skipped and invariant-safe. |
| DEV-010 | 2026-06-16 | §9.4 | **Runner race-state consolidation.** The four pre-arrival reconciliation dictionaries in `DriverEffectRunner` are removed: opens and query pumps are registered synchronously in `Run()` (on the single pump thread, before the async `Task.Run`), so every later effect on a handle finds a live record and a missing record is an unambiguous idempotent no-op. Validated by a 2000-seed simulator sweep. TRACE-SCHEMA now states each envelope kind's emission status honestly (cmd/evt/timer.due/state.snapshot are reserved, not emitted in v2.0). |
