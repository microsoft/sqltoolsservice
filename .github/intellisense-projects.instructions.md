# SQL Tools Service — Project IntelliSense Integration Guide

## !! HIGH PRIORITY: Approval Required Before Any Code Change !!

Before applying ANY code change to files covered by this guide:
1. Present the proposed change with full explanation
2. Wait for explicit user approval
3. Only then apply the edit

Violating this rule makes the answer invalid. No exceptions.

---

## Purpose

This file documents **STS-side IntelliSense integration** for SQL Projects.
It covers request routing, binding context management, and LSP endpoint implementation.
For SQL Projects library implementation details, see SqlProjects repo instructions.

---

## Architecture Principle

**STS owns request routing and LSP integration. SQL Projects owns semantic model.**

STS responsibilities:
- Project file management (open/close)
- Request routing (ownerUri → ConnectionKey → BindingContext)
- Parser pipeline (Parse → Bind using IMetadataProvider from SQL Projects)
- Resolver pipeline (FindCompletions, QuickInfo, etc.)
- LSP protocol handling
- File stamping with ConnectionKey

STS does NOT:
- Build or manage TSqlModel directly
- Create duplicate metadata indexes
- Bypass IMetadataProvider contract
- Resolve identifiers outside of SqlParser binder

---

## Package Dependency

| Property | Value |
|----------|-------|
| Package | `Microsoft.SqlServer.DacFx.Projects` |
| Version (local dev) | `0.6.0` |
| Source (local) | `./bin/nuget/` (configured in `nuget.config`) |
| Global packages cache | `C:\.tools\.nuget\packages\` |
| Version pin file | `Packages.props` |

**Critical:** DacFx version in `Packages.props` must match the SQL Projects dependency.

After repacking from SqlProjects repo: clear the NuGet cache for `microsoft.sqlserver.dacfx.projects`, restore STS, then rebuild. The version pin is a `PackageReference Update` for `Microsoft.SqlServer.DacFx.Projects` in `Packages.props`.

---

## Core Design: Single BindingQueue, Key-Based Routing

There is **one** `ConnectedBindingQueue` in `LanguageService`. It stores two kinds of binding
contexts, distinguished only by the `ConnectionKey` string stored on `ScriptParseInfo`:

| Context type | `ConnectionKey` format | Binder source | `ServerConnection` |
|---|---|---|---|
| Project (offline) | `"project_{projectUri}"` | `LazySchemaModelMetadataProvider` → `BinderProvider.CreateBinder()` | `null` |
| Connection (online) | `"{server}_{db}_{user}_{auth}[_...]"` (hash of connection details) | `SmoMetadataProvider.CreateConnectedProvider()` → `BinderProvider.CreateBinder()` | set (live `ServerConnection`) |

**Rule:** All routing decisions MUST be made by inspecting `ConnectionKey` via `IsProjectContext`,
never by checking `connInfo == null` or `IsConnected` directly.

---

## `IsProjectContext` helper (`LanguageService.cs`)

Returns `true` if `connectionKey` is non-null and starts with `"project_"` (ordinal comparison). Use everywhere a routing branch is needed. Do NOT add new flags or new branching mechanisms.

---

## `IConnectedBindingQueue` interface

`IConnectedBindingQueue` is the minimal public contract used by external consumers (`ConnectionService`,
`DatabaseLocksManager`). It does **not** include project-specific members.

`LanguageService.BindingQueue` is typed as `ConnectedBindingQueue` (concrete type) — **not** the interface —
because `LanguageService` needs to call `AddProjectContext` and `IsBindingContextConnected` which are
only on the concrete class. `CompletionService` also uses `ConnectedBindingQueue` directly.

It has 5 members: `CloseConnections`, `OpenConnections`, `AddConnectionContext`, `Dispose`, and `QueueBindingOperation`. `AddProjectContext`, `IsBindingContextConnected`, and `BindingContextMap` are on `ConnectedBindingQueue` and `BindingQueue<T>` — **not** on the interface.

---

## Files Changed for Project IntelliSense

### `src/Microsoft.SqlTools.ServiceLayer/SqlProjects/SqlProjectsService.cs`

**`HandleOpenSqlProjectRequest`**

Responds to VS Code immediately with `Success = true` and kicks off a background `Task.Run`.
No work is done on the request thread. Project loading (MSBuild file evaluation) and
IntelliSense model building both happen inside `BuildProjectIntelliSenseAsync`.

**Method: `BuildProjectIntelliSenseAsync(string projectUri)`**

Fire-and-forget background method. Sequence:
1. `GetProject(projectUri)` — loads and caches the `SqlProject` (MSBuild evaluation).
   Note: `projectUri` is a **file path** (e.g. `C:\...\MyProject.sqlproj`), NOT a URI.
2. Derive `projectDir` from the file path — **CRITICAL:** use `Path.GetDirectoryName(new Uri(projectUri).LocalPath)`,
   NOT `Path.GetDirectoryName(projectUri)`. `projectUri` is a file path but may contain characters
   that make `Path.GetDirectoryName` return a wrong value. The `Uri` round-trip normalises it.
3. Build file URI list: for each script path in `SqlObjectScripts`, `PreDeployScripts`, `PostDeployScripts`:
   - If absolute: `new Uri(path).AbsoluteUri`
   - If relative: `new Uri(Path.Combine(projectDir, path)).AbsoluteUri`
4. `TSqlModelBuilder.LoadModel(project)` — parses all DDL scripts into a `TSqlModel`.
5. `new LazySchemaModelMetadataProvider(model, databaseName)` — wraps model as `IMetadataProvider`;
   stored in `projectIntelliSense[projectUri]` alongside the model for disposal on project close.
6. `LanguageService.Instance.UpdateLanguageServiceOnProjectOpen(projectUri, projectMetadataProvider, parseOptions, databaseName, fileUriList)`
   — pass `provider` (`IMetadataProvider`), NOT a pre-built `IBinder`.
   — pass `fileUriList` as the last argument so the method stamps files after the context is ready.
   — `UpdateLanguageServiceOnProjectOpen` calls `AddProjectContext` FIRST, then stamps all files.
   — sends `IntelliSenseReadyNotification` to VS Code (visible as a status bar update in VS Code).

**Rule:** Always pass `provider` (not a pre-built `IBinder`) to `UpdateLanguageServiceOnProjectOpen`.
The method signature is `(string projectUri, IMetadataProvider metadataProvider, ParseOptions parseOptions, string databaseName, IEnumerable<string> fileUris = null)`.
Passing an `IBinder` is a compile error (CS1503).

**Rule:** File URIs are built via `new Uri(absolutePath).AbsoluteUri`.
`ScriptParseInfoMap` uses `OrdinalIgnoreCase` and `GetScriptParseInfo` calls `Uri.UnescapeDataString`,
so both drive-letter case and percent-encoding differences are handled automatically.

**Rule:** Do NOT call `InitializeProjectFileContexts` before `LoadModel`. Files must only be stamped
after `AddProjectContext` has registered a fully-populated binding context. Stamping before causes a
race: if a request arrives between stamp and `AddProjectContext`, `GetOrCreateBindingContext` creates
an empty context with null `MetadataProvider`, and F12 silently fails.

---

### `src/Microsoft.SqlTools.ServiceLayer/LanguageServices/ConnectedBindingContext.cs`

**Properties added:**

- `MetadataProvider` (`IMetadataProvider`): set by `AddProjectContext`; cast to `LazySchemaModelMetadataProvider` inside `QueueProjectTask` to call `TryGetSourceInformation` for F12 source lookup.
- `OverrideParseOptions` (`ParseOptions`): set by `AddProjectContext`; the `ParseOptions` property checks this first before constructing defaults, so project files use the correct offline parse options.

---

### `src/Microsoft.SqlTools.ServiceLayer/LanguageServices/ConnectedBindingQueue.cs`

**Method: `AddProjectContext(string projectKey, IBinder binder, ParseOptions parseOptions, IMetadataProvider? metadataProvider = null)`**

Registers an offline binding context keyed by `"project_<projectUri>"` (no server connection required).
- Stores `binder`, `metadataProvider`, and `parseOptions` on the `ConnectedBindingContext`
- Sets `IsConnected = true`, `BindingTimeout = DefaultBindingTimeout`
- The `metadataProvider` (a `LazySchemaModelMetadataProvider`) is used by `QueueProjectTask` for
  source location lookup via `TryGetSourceInformation`

Called by `LanguageService.UpdateLanguageServiceOnProjectOpen`.

---

### `src/Microsoft.SqlTools.ServiceLayer/LanguageServices/LanguageService.cs`

#### **`ScriptParseInfoMap` (modified)**

Uses `StringComparer.OrdinalIgnoreCase`. Required on Windows because VS Code sends file URIs with a lowercase drive letter (`file:///c:/...`) while .NET's `Uri` produces uppercase (`file:///C:/...`). Without this, `GetScriptParseInfo` misses stamped entries, causing F12 to fall through to "not connected".

**Rule:** Do not add manual drive-letter case normalization elsewhere — the map handles it.

---

#### **`UpdateLanguageServiceOnProjectOpen` (modified)**

Signature: `(string projectUri, IMetadataProvider metadataProvider, ParseOptions parseOptions, string databaseName, IEnumerable<string> fileUris = null)`

Order of operations inside this method is critical:
1. `BinderProvider.CreateBinder(metadataProvider)` — creates the binder
2. `BindingQueue.AddProjectContext(contextKey, binder, parseOptions, metadataProvider)` — registers the **fully-populated** binding context FIRST
3. Stamps the `.sqlproj` URI's `ScriptParseInfo`: `ConnectionKey`, `IsConnected=true`, `ProjectDatabaseName`
4. `InitializeProjectFileContexts(fileUris, contextKey, databaseName)` — stamps all `.sql` files (only if `fileUris != null`)
5. Sends `IntelliSenseReadyNotification` — visible in VS Code status bar as an IntelliSense status text update

**Why this order matters:** `AddProjectContext` must run before any file is stamped. Once a file has
`IsConnected=true` and a `ConnectionKey`, any incoming LSP request will call `GetOrCreateBindingContext`
for that key. If the context doesn't exist yet, an empty context (null `MetadataProvider`) is created
and F12 fails silently. With `AddProjectContext` first, the context is always populated before requests can reach it.

#### **`GetDefinition` (modified)**

F12 entry point. At the top: if `IsProjectContext(scriptParseInfo.ConnectionKey)`, call `ParseAndBind` unconditionally (not gated on `RequiresReparse` — the cached `ParseResult` may predate the project context and be unbound), then call `QueueProjectTask`. Falls through to the SMO path only for online connections.

**Rule:** STS detects project vs. online by `IsProjectContext(scriptParseInfo.ConnectionKey)`. It does NOT peel a token or extract an identifier string before routing.

#### **`QueueProjectTask` (private method for project F12)**

Queues a binding operation via the project binding context that:
1. Converts LSP 0-based position → parser 1-based (`line+1`, `col+1`)
2. Casts `bindingContext.MetadataProvider` to `LazySchemaModelMetadataProvider`
3. Extracts token text at cursor: `TokenManager.FindToken` + `GetToken`, strips `[`/`]` brackets. This is the bare unqualified name (e.g. `"Orders"`) used only for matching, NOT for source lookup.
4. Resolves identifier semantically:
   `Resolver.FindCompletions(parseResult, parserLine, parserColumn, bindingContext.MetadataDisplayInfoProvider)`
   — the binder annotations on `ParseResult` are required (set by `ParseAndBind` before this call)
5. Matches the resolved declaration against the token text:
   `declarations.FirstOrDefault(d => d.Title == tokenText, OrdinalIgnoreCase)`
   — picks the right declaration from the list; the matched `Declaration.DatabaseQualifiedName` is the fully-qualified name (e.g. `"ProjectDB.dbo.Orders"`).
6. Looks up source location:
   `lazyProvider.TryGetSourceInformation(match.DatabaseQualifiedName, out sourceInfo)`
   — if not found, strips the database prefix via `StripDatabasePrefix` and tries again
   — this handles dotted schema names (e.g. `"ProjectDB.SwaggerPetstore.Models.Get0ItemsItem"`
     has index key `"SwaggerPetstore.Models.Get0ItemsItem"` after the DB prefix strip)
7. Converts `SourceInformation` → LSP `Location` (1-based → 0-based, file path → URI)

**Key points:**
- Passes the already-bound `ParseResult` from `scriptParseInfo` (set by `ParseAndBind`)
- Does NOT extract token text or call `GetPeekDefinitionTokens` (that is for the SMO path)
- Uses `Resolver.FindCompletions` directly in STS — there is no separate `ProjectIntelliSenseEngine`
- Helper `StripDatabasePrefix` removes the first segment from a dotted name

#### **What was removed from `QueueTask`**

The old `cbc.ProjectModel != null` branch (which called `TSqlModelBuilder.FindDefinition` with a token string) is gone. `QueueTask` now handles online (server-connected) paths only. A null guard at the top returns an error `DefinitionResult` when `bindingContext.ServerConnection == null`.

---

## Request Routing — Complete Flow

### Project open

`HandleOpenSqlProjectRequest` responds to VS Code immediately with `Success=true`, then kicks off `BuildProjectIntelliSenseAsync` in the background. The sequence is:

1. `GetProject(projectUri)` — MSBuild evaluation. `projectUri` is a file path, not a URI.
2. Derive `projectDir` using `Path.GetDirectoryName(new Uri(projectUri).LocalPath)` — NOT `Path.GetDirectoryName(projectUri)`.
3. Build `fileUriList` from `project.SqlObjectScripts`, `PreDeployScripts`, `PostDeployScripts` — resolve relative paths with `Path.Combine(projectDir, path)`, then `new Uri(absolutePath).AbsoluteUri`.
4. `TSqlModelBuilder.LoadModel(project)` — SqlProjects library parses all DDL scripts.
5. `new LazySchemaModelMetadataProvider(model, databaseName)` — wraps model as `IMetadataProvider`.
6. `projectIntelliSense[projectUri] = (model, metadataProvider)` — stored for disposal on project close.
7. `UpdateLanguageServiceOnProjectOpen(projectUri, provider, parseOptions, databaseName, fileUriList)` — pass `IMetadataProvider` and `fileUriList`. Inside: calls `AddProjectContext` FIRST (binding context fully populated), THEN stamps all files, THEN sends `IntelliSenseReadyNotification`.

### F12 (Go-to-Definition)

1. `GetScriptParseInfo(file.ClientUri)` — OrdinalIgnoreCase + `UnescapeDataString` lookup.
2. `IsProjectContext(scriptParseInfo.ConnectionKey)` — routes to project branch if true.
3. `ParseAndBind(scriptFile, null)` — always, not gated on `RequiresReparse`. Writes binder annotations onto the parse tree.
4. `QueueProjectTask(position, scriptParseInfo)` — binding operation calls `Resolver.FindCompletions` to get a `Declaration` with `DatabaseQualifiedName` (e.g. `"ProjectDB.dbo.Customers"`), then `lazyProvider.TryGetSourceInformation(match.DatabaseQualifiedName, out sourceInfo)`. On miss, strips the DB prefix via `StripDatabasePrefix` and retries (handles dotted schemas like `"SwaggerPetstore.Models.Get0ItemsItem"`).
5. Converts `SourceInformation` (1-based file path) to LSP `Location` (0-based URI).

### Completions / Hover (existing, unchanged path)

`ParseAndBind` routes to the project binder automatically because `scriptParseInfo.ConnectionKey` is `"project_{uri}"`. The binder is backed by `LazySchemaModelMetadataProvider`. No separate code path is needed for completions. `dbName` for the bind call: project — `parseInfo.ProjectDatabaseName`; SMO — `connInfo.ConnectionDetails.DatabaseName`.

### Connection IntelliSense startup sequence

`ConnectionService.Connect()` calls `LanguageService.UpdateLanguageServiceOnConnection`. The guard at the top returns immediately if `IsProjectContext(scriptInfo.ConnectionKey)` — project files must never have their key overwritten by a server connection. For non-project files: `AddConnectionContext` opens a `ServerConnection`, creates a `SmoMetadataProvider` and binder, stamps `ConnectionKey` and `IsConnected=true`, then `PrepopulateCommonMetadata()` warms the binder, then `IntelliSenseReadyNotification` is sent.

### `ParseAndBind` — binding context lookup

For project contexts `bindingContext.ServerConnection` is `null` — intentional. Only the `QueueTask` (SMO scripter) path accesses `ServerConnection`; it has a null-guard at the top that returns an error `DefinitionResult`.

### Supported LSP features

| Feature | LSP method | Project path | Online path |
|---|---|---|---|
| **Completions** | `textDocument/completion` | ✅ bound ParseResult | ✅ SMO |
| **Completion resolve** | `textDocument/completionItem/resolve` | ✅ | ✅ |
| **Go-to-Definition / Peek** | `textDocument/definition` | ✅ `QueueProjectTask` (`Resolver` + `LazySchemaModelMetadataProvider`) | ✅ SMO `Scripter` |
| **Hover** | `textDocument/hover` | ✅ | ✅ |
| **Signature help** | `textDocument/signatureHelp` | ✅ | ✅ |
| **Syntax parse** | `sqlTools/syntaxParse` | ✅ | ✅ |
| **Diagnostics** | internal | ✅ (suppressed — no false errors) | ✅ |
| **References** | `textDocument/references` | ❌ commented out | ❌ |
| **Document highlight** | `textDocument/documentHighlight` | ❌ commented out | ❌ |

### Diagnostics suppression for project files

`GetSemanticMarkers` checks `IsProjectContext(parseInfo?.ConnectionKey)` and returns an empty marker array for project files. The binder reports DDL objects as duplicates (already loaded into the metadata model), producing false errors; project-level validation belongs to the build step.

---

## Invariants — Never Break These

| Invariant | Reason |
|-----------|--------|
| `IsProjectContext(ConnectionKey)` is the routing signal | Never use `connInfo == null`; a project file may also have a server connection |
| `UpdateLanguageServiceOnConnection` must NOT overwrite a project key | Guard: `if (IsProjectContext(...)) return;` at the top of the method |
| `QueueProjectTask` uses `Resolver.FindCompletions(parseResult, line, col, ...)` — never a raw token string | Token-string peeling bypasses binding; the binder must annotate identifiers first |
| `LazySchemaModelMetadataProvider` stored on `ConnectedBindingContext.MetadataProvider` | Avoids rebuilding the metadata index per request |
| `QueueProjectTask` does not call `GetPeekDefinitionTokens` | That path is for the SMO/online scripter; it pre-extracts tokens because SMO needs a name, not a position |
| `ParseAndBind` must run before `GetDefinition` | `GetDefinition` reads binder annotations; they only exist after `Bind()` has been called |
| `SqlProjectsService.projectIntelliSense` holds `(TSqlModel, LazySchemaModelMetadataProvider)` | Dispose both on project close; do not hold separate model references in `LanguageService` |

---

## Future IntelliSense Endpoints (pattern to follow)

For each new feature (hover, diagnostics, signature help):

1. **`LazySchemaModelMetadataProvider`**: add `TryGet*(qualifiedName, out result)` if source lookup is needed
2. **STS `LanguageService`**: add `QueueProject*(position, scriptParseInfo)` that calls `Resolver.*` + `lazyProvider.TryGet*`
3. **STS dispatch**: add `if (IsProjectContext(scriptParseInfo.ConnectionKey)) return QueueProject*(...)` branch
   in the existing handler, before the online fallthrough
4. STS uses `Resolver.*` directly via the binder — there is no separate `ProjectIntelliSenseEngine` to call

---

## What We Are NOT Doing

| Rejected approach | Why |
|-------------------|-----|
| Extracting token text as a string and doing string-match source lookup | `Resolver.FindCompletions` returns the binder-resolved `DatabaseQualifiedName` which is unambiguous even when the same short name exists in multiple schemas |
| Dictionary/index in **STS or LanguageService** separate from the metadata provider | `LazySchemaModelMetadataProvider` owns its own `_sourceLocations` index; no duplicate index in STS |
| SMO scripting for project files | SMO requires a live server connection (`ServerConnection == null` for project contexts) |
| Re-parsing files from disk in STS on each F12 | The bound `ParseResult` from `ParseAndBind` already exists; use it |
| Storing raw `TSqlModel` directly on `ConnectedBindingContext` | `SqlProjectsService.projectIntelliSense` holds it; `ConnectedBindingContext.MetadataProvider` holds the wrapped provider |

---

## Known Fixed Bugs (do not re-introduce)

### F12 returned "not connected" for all project .sql files

**Root cause 1 (compile error):** `BuildProjectIntelliSenseAsync` was passing a pre-built `IBinder`
as argument 2 to `UpdateLanguageServiceOnProjectOpen`, which expects `IMetadataProvider`.
This caused CS1503 — the project IntelliSense code was never compiled or executed.
**Fix:** Pass `provider` directly. The method creates the binder internally.

**Root cause 2 (logical bug):** `InitializeProjectFileContexts` was called BEFORE `LoadModel`.
This created a race: files were stamped with `IsConnected=true` pointing to `"project_x"`, but
`AddProjectContext` (which puts the real binding context into `BindingContextMap`) hadn't run yet.
If any LSP request arrived during model loading, `GetOrCreateBindingContext("project_x")` created
an empty context with `MetadataProvider=null`. F12 then ran against this empty context and silently
returned no results. One project reliably reproduced this; another project was fast enough to
never hit the race window.
**Fix:** Move `InitializeProjectFileContexts` to AFTER `AddProjectContext` inside
`UpdateLanguageServiceOnProjectOpen`. Pass `fileUriList` into `UpdateLanguageServiceOnProjectOpen`
as an optional parameter. The order is now: `AddProjectContext` (context fully populated) →
stamp `.sqlproj` URI → `InitializeProjectFileContexts` (stamp `.sql` files) → send ready notification.

**Do not regress:** `InitializeProjectFileContexts` must NEVER be called before `AddProjectContext`.
Files must only be stamped after the binding context exists and has `MetadataProvider` set.

### F12 failed for schema-qualified names (`dbo.Orders`) but worked for unqualified (`Orders`)

**Root cause 1 — `LazyCollection.this[string]` threw instead of returning null:**
`Resolver.FindCompletions` probes `Schemas["dbo"]` by name. The old indexer threw
`KeyNotFoundException` when the name was not found. The binder expects `null` on miss.
**Fix:** Both `LazyCollection<T>` and `LazyOrderedCollection<T>` name indexers now return
`null` on miss (via `FirstOrDefault` + `#pragma warning disable CS8603`) instead of throwing.

**Root cause 2 — `BuildSchemas()` missed implicit schemas:**
DacFx `GetObjects(DacQueryScopes.UserDefined, ModelSchema.Schema)` only returns schemas that
have an explicit `CREATE SCHEMA` DDL statement. A project with only `CREATE TABLE dbo.X`
(no `CREATE SCHEMA dbo`) produced an empty schema list, so `Schemas["dbo"]` always missed.
Unqualified names (`FROM Orders`) worked because `Resolver` enumerates schemas with `foreach`
rather than by name; the qualified path (`FROM dbo.Orders`) called `Schemas["dbo"]` directly.
**Fix:** `LazyModelDatabase.BuildSchemas()` now has a third loop that walks all
`DacQueryScopes.UserDefined` objects and infers schemas from `obj.Name.Parts[0]`, registering
any schema not already seen from the explicit-DDL loops.

**Do not regress:**
- Do not restore a throwing indexer in `LazyCollection` or `LazyOrderedCollection`.
- Do not remove the third infer-from-parts loop in `BuildSchemas()`.
- Do not add `CREATE SCHEMA dbo` to test projects as a workaround — tests must pass without it.

---

## TODO: Incremental TSqlModel Updates on File Save

> **CLEANUP NOTE:** Once this feature is fully implemented and tested, remove this entire TODO section and update the "Request Routing", "Binding Context Lifecycle", and "Known Fixed Bugs" sections to reflect the new on-save update flow.

### Goal

When a SQL file in a project is saved, update only that file's objects in the `TSqlModel` and refresh IntelliSense — without rebuilding the entire model from scratch.

### Why DacFx Already Supports It

`TSqlModel` has the exact API needed — no DacFx changes required:

| Method | What it does |
|--------|-------------|
| `model.AddOrUpdateObjects(sqlText, filePath, options)` | Replace all objects that came from that file |
| `model.DeleteObjects(filePath)` | Remove all objects that came from that file |

`TSqlModelBuilder.LoadModel()` already calls `AddOrUpdateObjects` per-file on initial build. Incremental update is just calling it again on the changed file.

### The Trigger: `textDocument/didSave`

`textDocument/didSave` is listed in the STS protocol docs (`docs/guide/jsonrpc_protocol.md`) but **not yet implemented**. The client (vscode-mssql) already sends it — it is standard LSP. No client changes needed.

Hook point: add `HandleDidSaveTextDocumentNotification(uri)` in `LanguageService.cs` and register it in `ServiceHost` alongside the existing `didOpen`/`didChange`/`didClose` registrations.

Trigger on **file save only** — not on every keystroke. Reasons:
- F12 source locations are file-path-based; they only make sense when on-disk content matches the model.
- No debouncing needed — saves are infrequent enough to update synchronously.
- `sourceName` (the key DacFx uses) = file path; stays consistent between model and `_sourceLocations` index.

### Current Bottlenecks (all in sqltoolsservice — nothing in DacFx)

**1 — `LazySchemaModelMetadataProvider._sourceLocations` goes stale**
Built eagerly in the constructor by scanning all `UserDefined` objects. After `AddOrUpdateObjects()`, new/modified objects are not reflected. Needs a `Refresh()` / `InvalidateSourceLocationIndex()` method that rebuilds the index from the current model state.

**2 — `LazyModelDatabase._schemas` goes stale**
`_schemas` is a `Lazy<IMetadataCollection<ISchema>>` built once. After a model update, new schemas from the changed file won't appear. The field must be resettable to `null` so the next access re-queries the model. Add a public `InvalidateSchemasCache()` method (or make `_schemas` settable).

**3 — `LazyModelSchema` per-type collections go stale**
Each collection (Tables, Views, StoredProcedures, etc.) is a `LazyCollection<T>` built once. Same problem. Resetting `_schemas` to null cascades — new `LazyModelSchema` instances are created on next access, so their collections are fresh.

**4 — No file-save detection**
`HandleDidSaveTextDocumentNotification` does not exist. `HandleDidChangeTextDocumentNotification` updates only the parser cache (`ScriptParseInfo.ParseResult`), not the `TSqlModel`.

**5 — `ConnectedBindingQueue` cannot swap the metadata provider**
The binding context's `MetadataProvider` and `Binder` are set once at project-open. There is no `UpdateProjectContext(key, newBinder, newProvider)` method. After refreshing the provider, the binder must also be rebuilt via `BinderProvider.CreateBinder(provider)` and pushed onto the existing binding context.

### Required Code Changes

| File | Change |
|------|--------|
| `LazyModelServer.cs` (sqltoolsservice) | Expose `LazyModelDatabase` via internal property; add `InvalidateSchemasCache()` |
| `LazySchemaModelMetadataProvider.cs` (sqltoolsservice) | Add `Refresh()` — rebuilds `_sourceLocations`, calls `database.InvalidateSchemasCache()` |
| `ConnectedBindingQueue.cs` (sqltoolsservice) | Add `UpdateProjectContext(key, IBinder, IMetadataProvider)` |
| `LanguageService.cs` (sqltoolsservice) | Add `HandleDidSaveTextDocumentNotification(uri)` + `UpdateProjectContextMetadata(projectUri, provider)` |
| `ServiceHost.cs` (sqltoolsservice) | Register `textDocument/didSave` → `HandleDidSaveTextDocumentNotification` |
| `SqlProjectsService.cs` (sqltoolsservice) | Hook: on save of a project file, call `model.AddOrUpdateObjects` / `model.DeleteObjects`, then `provider.Refresh()`, then `UpdateProjectContextMetadata` |

### Save Handler Logic (pseudocode)

```
OnDidSave(fileUri):
  if not IsProjectContext(GetScriptParseInfo(fileUri).ConnectionKey): return
  projectUri = resolve owning project for fileUri
  (model, provider) = projectIntelliSense[projectUri]
  filePath = local path of fileUri
  if File.Exists(filePath):
    model.AddOrUpdateObjects(File.ReadAllText(filePath), filePath, new TSqlObjectOptions())
  else:
    model.DeleteObjects(filePath)
  provider.Refresh()
  newBinder = BinderProvider.CreateBinder(provider)
  LanguageService.UpdateProjectContextMetadata(projectUri, newBinder, provider)
  SendEvent(IntelliSenseReadyNotification, projectUri)
```

### What Is NOT Needed

- No debouncing — save events are infrequent.
- No ScriptDOM incremental parse API — `AddOrUpdateObjects` re-parses only the changed file internally.
- No vscode-mssql changes — `textDocument/didSave` is already sent by VS Code.
- No SqlProjects library changes — `TSqlModel.AddOrUpdateObjects` / `DeleteObjects` already exist.

---

## TODO: Remove 2500 per-file `parseTSqlScript` IPC calls on project open

**Problem:**
When a SQL project is opened in VS Code, `project.ts` (`readSqlObjectScripts`) calls
`parseTSqlScript` once per `.sql` file in a loop to detect `CREATE TABLE` and tag that file
as a `TableFileNode` in the project tree. For a 2500-file project this is ~3 minutes of
blocking IPC calls before the tree renders.

**Why the `TableFileNode` distinction is currently useless:**
- `TableFileNode` and `SqlObjectFileNode` have identical icons (neither sets `iconPath`).
- `package.json` has zero `when` clauses referencing `databaseProject.itemType.file.table`.
- The controller treats both identically in all `switch` blocks (same `exclude`/`delete` paths).
- `contextValue = "table"` is set but never read anywhere. Dead code carried over from ADS.

**Fix (vscode-mssql only — no STS changes needed):**

In `extensions/sql-database-projects/src/models/project.ts`, method `readSqlObjectScripts`: remove the `for` loop that calls `checkForCreateTableStatement` on every file. Replace with a `.map()` that creates all entries as `SqlObjectFileNode` with `containsCreateTableStatement = false`.

**Validation before merging:**
1. Search `package.json` for `itemType.file.table` — must return zero results (confirms no menu uses it).
2. Open a large project and verify tree renders immediately.
3. Verify right-click menu on a table file and a view file show the same options (no regression).

**No STS changes required.** Do not add a notification, do not add `markFileAsTable`, do not
add `onNotification` to `IExtension`. The fix is purely a deletion in `project.ts`.

---

## URI NORMALIZATION (CRITICAL)

**Problem:** VS Code sends URIs in different encodings:
- File stamping from SqlProjects: `file:///c:/path/to/file.sql`
- LSP requests from VS Code: `file:///c%3A/path/to/file.sql` (percent-encoded colon)

**Solution:** Two-layer normalization in `GetScriptParseInfo`:

1. `ScriptParseInfoMap` uses `StringComparer.OrdinalIgnoreCase` (set in the map constructor).
2. `GetScriptParseInfo` calls `Uri.UnescapeDataString(uri)` before the dictionary lookup.

**Why both are needed:**
- `OrdinalIgnoreCase`: handles drive-letter case differences (`C:/` == `c:/`)
- `UnescapeDataString`: handles percent-encoded URIs (`file:///c%3A/...` → `file:///c:/...`)

**NEVER:**
- Remove `OrdinalIgnoreCase` from the map constructor
- Add manual drive-letter normalization elsewhere
- Remove the `Uri.UnescapeDataString` call in `GetScriptParseInfo`

**Consequence if broken:** Files stamped with ConnectionKey but LSP requests return "not connected"

---

## F12 IMPLEMENTATION DETAILS

### Method: `QueueProjectTask`

**Location:** `LanguageService.cs`

See the [F12 (Go-to-Definition)](#f12-go-to-definition) flow above for the step-by-step walkthrough. Key implementation notes:

- Converts LSP 0-based position to SqlParser 1-based position (`line+1`, `col+1`) before calling `Resolver.FindCompletions`.
- Uses `TokenManager.FindToken` + `GetToken` to extract the bare token text at the cursor (e.g. `"Orders"`). The token text is used **only** to match the correct `Declaration` from the `FindCompletions` result list — it is never used directly for source lookup.
- `declarations.FirstOrDefault(d => d.Title == tokenText, OrdinalIgnoreCase)` picks the matching declaration whose `DatabaseQualifiedName` (e.g. `"ProjectDB.dbo.Orders"`) drives the source lookup.
- Tries `match.DatabaseQualifiedName` first in `TryGetSourceInformation`; on miss calls `StripDatabasePrefix` and retries. This two-step lookup handles dotted schema names.
- Converts `SourceInformation` coordinates from 1-based (SqlParser) to 0-based (LSP) and converts the file path to an absolute URI.

---

## FILE STAMPING

### Method: `InitializeProjectFileContexts`

**Purpose:** Stamp all project `.sql` files with `ConnectionKey`, `IsConnected=true`, and `ProjectDatabaseName`.

**MUST be called AFTER `AddProjectContext`** — never before. Files are only stamped once the binding
context is fully populated. If stamped before, an incoming LSP request can hit `GetOrCreateBindingContext`
before `AddProjectContext` runs, creating an empty context with `MetadataProvider=null`.

**Called from:** `LanguageService.UpdateLanguageServiceOnProjectOpen`, after `AddProjectContext` and after
stamping the `.sqlproj` URI, before sending `IntelliSenseReadyNotification`.

**Files included:** `project.SqlObjectScripts` (CREATE statements), `project.PreDeployScripts`, `project.PostDeployScripts`. Pre/Post deploy scripts reference project objects and need IntelliSense even though they are not parsed into `TSqlModel`.

---

## BINDING CONTEXT LIFECYCLE

**Creation** (project open): `SqlProjectsService.BuildProjectIntelliSenseAsync` loads the model via `TSqlModelBuilder.LoadModel`, creates `LazySchemaModelMetadataProvider`, stores `(model, metadataProvider)` in `projectIntelliSense[projectUri]`, then calls `UpdateLanguageServiceOnProjectOpen(projectUri, metadataProvider, parseOptions, databaseName, fileUriList)`. That method: (1) creates the binder via `BinderProvider.CreateBinder(metadataProvider)`, (2) calls `ConnectedBindingQueue.AddProjectContext(contextKey, binder, parseOptions, metadataProvider)` — stores all three on the binding context and adds it to `BindingContextMap` with `IsConnected=true`, (3) stamps the `.sqlproj` URI, (4) calls `InitializeProjectFileContexts(fileUriList)` to stamp all `.sql` files. File stamping always happens AFTER the binding context is fully populated.

### Disposal (project close)
**NOT YET IMPLEMENTED** - project contexts are never removed from `_bindingContextMap`

**TODO:** Add `RemoveProjectContext(string contextKey)` method that:
1. Removes binding context from map
2. Disposes the `TSqlModel` and `LazySchemaModelMetadataProvider` stored in `SqlProjectsService.projectIntelliSense[projectUri]`
3. Clears `ConnectionKey` from all stamped files

---

## PERFORMANCE CHARACTERISTICS (STS side)

| Operation | Time | Blocking? |
|-----------|------|-----------|
| InitializeProjectFileContexts (stamp 3000 files) | <100ms | Yes (per-file loop) |
| ParseAndBind (incremental, bound before) | <10ms | Yes |
| ParseAndBind (incremental, never bound) | ~50ms | Yes (first bind) |
| QueueProjectTask | <5ms | Yes (waits on queue) |
| Resolver.FindCompletions + TryGetSourceInformation | <5ms | Yes (O(1) via index) |

**Key insight:** File stamping and binding are fast. Model loading (SQL Projects side) is the bottleneck.

---

## DEBUGGING CHECKLIST

### F12 returns "not connected"

1. Check `scriptParseInfo.ConnectionKey` for the file URI — must be `"project_{projectUri}"`, not null. If null, `InitializeProjectFileContexts` was not called, or the URI passed to `GetScriptParseInfo` doesn't match the stamped URI (check case and percent-encoding).
2. Verify `IsProjectContext(connectionKey)` returns true.
3. Verify a binding context exists in `BindingQueue.BindingContextMap` for that key. If missing, `AddProjectContext` was not called — check `UpdateLanguageServiceOnProjectOpen` completed without exception.
4. Check `(ConnectedBindingContext).MetadataProvider` is a `LazySchemaModelMetadataProvider`. If null, `InitializeProjectFileContexts` was called before `AddProjectContext` (race condition — files were stamped before the context was populated).

### F12 returns "No definition found"

1. Verify `scriptParseInfo.ParseResult` is not null — `ParseAndBind` must have been called before `QueueProjectTask`.
2. Set a breakpoint in the `QueueProjectTask` binding operation lambda and verify `Resolver.FindCompletions` returns results. If empty: the `ParseResult` was not bound, or the cursor position conversion is wrong.
3. If `Resolver.FindCompletions` returns results but `TryGetSourceInformation` misses: inspect `match.DatabaseQualifiedName` and compare against the keys in `LazySchemaModelMetadataProvider._sourceLocations`. Step through `StripDatabasePrefix` to verify the fallback key.

### Performance issues

1. **Model loading takes >10 minutes:**
   - Expected: ~2 minutes per 1000 files
   - If slower: disk I/O bottleneck, antivirus scanning .sql files

2. **F12 takes >1 second:**
   - Set breakpoint in `QueueProjectTask` bindOperation lambda
   - Check `Resolver.FindCompletions` call time vs `TryGetSourceInformation` call time
   - `LazySchemaModelMetadataProvider._sourceLocations` is an O(1) dictionary; if slow, the bottleneck is `Resolver.FindCompletions`

---

## LESSONS LEARNED (STS-specific)

1. **OrdinalIgnoreCase critical for Windows drive letters** - VS Code sends both `c:/` and `C:/`
2. **UnescapeDataString also required** - VS Code sends `%3A` for `:` in URIs; `GetScriptParseInfo` must decode before lookup
3. **Stamp files BEFORE LoadModel** - Ensures requests during loading are queued, not rejected
4. **Pre/Post deployment scripts need stamping** - They reference project objects even though not in model
5. **Always ParseAndBind before GetDefinition** - Binder annotations required by Resolver
6. **Never check `connInfo == null` for routing** - Project files may also have server connections
7. **IsProjectContext is the single routing signal** - All branches must check ConnectionKey prefix
8. **`SqlProjectsService.projectIntelliSense` holds TSqlModel** - Dispose (model, metadataProvider) on project close; `LanguageService` does not own the model
