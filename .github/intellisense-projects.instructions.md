# SQL Tools Service — Project IntelliSense Architecture & Implementation Guide

## !! HIGH PRIORITY: Approval Required Before Any Code Change !!

Before applying ANY code change to files covered by this guide:
1. Present the proposed change with full explanation
2. Wait for explicit user approval
3. Only then apply the edit

Violating this rule makes the answer invalid. No exceptions.

---

## Purpose

This file is the single source of truth for all SQL project IntelliSense work in this repository.
Update it whenever the design changes. Never diverge from the path described here.

---

## Guiding Principle

**STS is a thin transport and router.** For SQL project IntelliSense, STS:

1. Builds the engine once at project open and stores it in the binding context
2. Routes each JSON-RPC IntelliSense request to the engine with a raw LSP position
3. Converts the engine's result to an LSP response

STS never builds its own index, never peels tokens, never resolves identifiers.
All semantic work lives in `Microsoft.SqlServer.Dac.Projects.IntelliSense`.

---

## Package Dependency

| Property | Value |
|----------|-------|
| Package | `Microsoft.SqlServer.Dac.Projects.IntelliSense` |
| Version (local dev) | `0.5.1-local` |
| Source (local) | `./bin/nuget/` (configured in `nuget.config`) |
| Global packages cache | `C:\.tools\.nuget\packages\` |

After repacking the library, clear the cache and restore:

```powershell
Remove-Item "C:\.tools\.nuget\packages\microsoft.sqlserver.dac.projects.intellisense" -Recurse -Force -ErrorAction SilentlyContinue
cd C:\Projects\sqltoolsservice
dotnet restore src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj
```

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
2. `TSqlModelBuilder.LoadModel(project)` — parses all DDL scripts into a `TSqlModel`
3. `new LazySchemaModelMetadataProvider(model, databaseName)` — wraps model as `IMetadataProvider`
4. `new ProjectIntelliSenseEngine(model)` — engine that owns the model
5. `LanguageService.Instance.UpdateLanguageServiceOnProjectOpen(projectUri, provider, parseOptions, databaseName, engine)`
   — pass `provider` (`IMetadataProvider`), NOT a pre-built `IBinder`.
   — `UpdateLanguageServiceOnProjectOpen` creates the `IBinder` internally via
     `BinderProvider.CreateBinder(metadataProvider)` and stores it on the binding context.
   — stamps `IsConnected=true` / `ConnectionKey="project_<uri>"` on the `.sqlproj` ScriptParseInfo only.
6. Build file URI list: `project.SqlObjectScripts.Select(s => new Uri(Path.Combine(projectDir, s.Path)).AbsoluteUri)`
   — `SqlObjectScript.Path` is relative to the project directory; resolve to absolute before converting to URI.
7. `LanguageService.Instance.InitializeProjectFileContexts(fileUris, contextKey, databaseName)`
   — stamps `IsConnected=true` / `ConnectionKey` / `ProjectDatabaseName` on every `.sql` file's `ScriptParseInfo`.
   — **This is the critical call.** Without it every `.sql` file keeps `IsConnected=false` and
     F12 falls into the "not connected" error branch regardless of the engine being registered.

**Rule:** This is the only place a `ProjectIntelliSenseEngine` is constructed. The engine is
passed into STS and stored there. `SqlProjectsService` does not hold a reference to it after this.

**Rule:** Always pass `provider` (not a pre-built `IBinder`) to `UpdateLanguageServiceOnProjectOpen`.
The method signature is `(string projectUri, IMetadataProvider metadataProvider, ...)`.
Passing an `IBinder` is a compile error (CS1503).

**Rule:** File URIs passed to `InitializeProjectFileContexts` are built via `new Uri(absolutePath).AbsoluteUri`.
No manual drive-letter case normalization is needed because `ScriptParseInfoMap` uses `OrdinalIgnoreCase`.

---

### `src/Microsoft.SqlTools.ServiceLayer/LanguageServices/ConnectedBindingContext.cs`

**Property added:**

```csharp
public ProjectIntelliSenseEngine ProjectEngine { get; set; }
```

- Non-null only for project contexts (set by `AddProjectContext`)
- Online connection contexts leave this null
- The engine is the single state object for project IntelliSense; it wraps both the `TSqlModel`
  and the `IMetadataDisplayInfoProvider`

**What was removed:** `public TSqlModel ProjectModel { get; set; }` — the raw model is no longer
stored directly; it is encapsulated inside `ProjectEngine`.

---

### `src/Microsoft.SqlTools.ServiceLayer/LanguageServices/ConnectedBindingQueue.cs`

**Method: `AddProjectContext(string, IBinder, ParseOptions, ProjectIntelliSenseEngine?)`**

Registers a binding context keyed by `"project_<projectUri>"`.
Stores `engine` on `bindingContext.ProjectEngine`.

Called by `LanguageService.UpdateLanguageServiceOnProjectOpen`.

---

### `src/Microsoft.SqlTools.ServiceLayer/LanguageServices/LanguageService.cs`

#### **`ScriptParseInfoMap` (modified)**

```csharp
private Lazy<ConcurrentDictionary<string, ScriptParseInfo>> scriptParseInfoMap
    = new Lazy<ConcurrentDictionary<string, ScriptParseInfo>>(
        () => new ConcurrentDictionary<string, ScriptParseInfo>(StringComparer.OrdinalIgnoreCase));
```

Using `OrdinalIgnoreCase` is required for correctness on Windows. VS Code sends file URIs with a
lowercase drive letter (`file:///c:/...`) while .NET's `Uri` produces uppercase (`file:///C:/...`).
Without this, `GetScriptParseInfo(file.ClientUri)` misses the entry stamped by
`InitializeProjectFileContexts`, returning a blank `ScriptParseInfo` with `IsConnected=false`,
which causes every F12 to fall through to the "not connected" error branch.

**Rule:** Do not add manual drive-letter case normalization elsewhere — the map handles it.

---

#### **`UpdateLanguageServiceOnProjectOpen` (modified)**

Signature:
```csharp
public async Task UpdateLanguageServiceOnProjectOpen(
    string projectUri,
    IMetadataProvider metadataProvider,
    ParseOptions parseOptions,
    string databaseName,
    ProjectIntelliSenseEngine projectEngine = null)
```

- Creates the binder internally: `BinderProvider.CreateBinder(metadataProvider)`
- Registers binding context via `BindingQueue.AddProjectContext(contextKey, binder, parseOptions, engine)`
- Sets `scriptInfo.ConnectionKey`, `scriptInfo.IsConnected`, `scriptInfo.ProjectDatabaseName`
- Sends `IntelliSenseReadyNotification`

#### **`GetDefinition` (modified)**

The entry point for F12. Has a project-file branch at the top:

```csharp
// Project file: connInfo is null but IsConnected (project binding context is registered).
if (connInfo == null && scriptParseInfo.IsConnected && scriptParseInfo.ConnectionKey != null)
{
    // Always ParseAndBind — do not short-circuit with RequiresReparse.
    // The cached ParseResult may predate the project context (file was open before the model
    // finished building), so it may have been parsed but never bound. Binder annotations are
    // required by Resolver.FindCompletions inside the engine. ParseAndBind is incremental:
    // it reuses the existing parse tree when text is unchanged and just re-runs Bind().
    scriptParseInfo.ParseResult = ParseAndBind(scriptFile, null).GetAwaiter().GetResult();
    return QueueProjectDefinition(textDocumentPosition, scriptParseInfo);
}
```

Falls through to the existing SMO path only for online connections.

**Rule:** STS detects project vs. online by `connInfo == null`. It does NOT peel a token
or extract an identifier string before routing.

#### **`QueueProjectDefinition` (new private method)**

```csharp
private DefinitionResult QueueProjectDefinition(
    TextDocumentPosition textDocumentPosition,
    ScriptParseInfo scriptParseInfo)
```

Queues a `BindingOperation` that:
1. Converts LSP 0-based position → parser 1-based (`line+1`, `col+1`)
2. Calls `bindingContext.ProjectEngine.GetDefinition(scriptParseInfo.ParseResult, parserLine, parserColumn)`
   — the `ParseResult` was already bound by `ParseAndBind` before this method was called
3. Converts `SourceInformation` → LSP `Location` (1-based → 0-based, file path → URI)

**Rule:** This method passes the already-bound `ParseResult` and a position to the engine.
It does not extract tokens, does not call `ScriptDocumentInfo.GetPeekDefinitionTokens`,
does not call `Resolver` directly.

#### **What was removed from `QueueTask`**

The old project branch:
```csharp
if (bindingContext is ConnectedBindingContext cbc && cbc.ProjectModel != null)
{
    var info = TSqlModelBuilder.FindDefinition(cbc.ProjectModel, tokenText);  // REMOVED
    ...
}
```
This entire block is gone. `QueueTask` now handles online connections only.

---

## Request Routing — Complete Flow

### Project open

```
HandleOpenSqlProjectRequest
  → SendResult(Success=true)   ← immediate response; VS Code is never blocked
  → BuildProjectIntelliSenseAsync (background Task.Run)
      → GetProject(projectUri)                 // MSBuild file evaluation, loads into cache
                                               // projectUri = file path, e.g. C:\...\MyProject.sqlproj
      → TSqlModelBuilder.LoadModel(project)    // SqlProjects library parses all DDL scripts
      → new LazySchemaModelMetadataProvider()  // wraps model as IMetadataProvider for binder
      → new ProjectIntelliSenseEngine(model)   // engine owns TSqlModel; used for F12
      → UpdateLanguageServiceOnProjectOpen(projectUri, provider, ...)
          // pass provider (IMetadataProvider), NOT a pre-built IBinder
          → BinderProvider.CreateBinder(provider)   // binder created here, inside this method
          → BindingQueue.AddProjectContext("project_<uri>", binder, parseOptions, engine)
              // engine stored at bindingContext.ProjectEngine
          → stamps .sqlproj ScriptParseInfo: IsConnected=true, ConnectionKey="project_<uri>"
          → SendEvent(IntelliSenseReadyNotification)
      → build fileUris from project.SqlObjectScripts:
          new Uri(Path.Combine(projectDir, s.Path)).AbsoluteUri  // relative → absolute → URI
      → InitializeProjectFileContexts(fileUris, "project_<uri>", databaseName)
          // stamps every .sql file ScriptParseInfo: IsConnected=true, ConnectionKey, ProjectDatabaseName
          // WITHOUT THIS CALL: every .sql file has IsConnected=false → F12 returns "not connected"
```

### F12 (Go-to-Definition)

```
GetDefinition(position, file, connInfo=null)
  → scriptParseInfo = GetScriptParseInfo(file.ClientUri)   ← OrdinalIgnoreCase lookup
  → connInfo==null + IsConnected + ConnectionKey!=null  ← project file branch
      → ParseAndBind(scriptFile, null)  ← always, not conditional on RequiresReparse
            → IBinder.Bind([parseResult], databaseName, Batch)
                  // binder writes semantic annotations onto the parse tree
      → QueueProjectDefinition(position, scriptParseInfo)
          → BindingQueue.QueueBindingOperation(key="project_<uri>", ...)
              → bindingContext.ProjectEngine.GetDefinition(parseResult, parserLine, parserColumn)
                    // Inside SqlProjects library:
                    // Step 1: TokenManager.FindToken(line, col) → token at cursor
                    // Step 2: Resolver.FindCompletions(boundParseResult, ...) → Declaration.DatabaseQualifiedName
                    // Step 3: strip database prefix → schema-qualified name
                    // Step 4: O(N) scan of model.GetObjects() ← known issue, O(1) fix planned
                    //         → TSqlObject.GetSourceInformation()
              → SourceInformation { SourceName (file path), StartLine, StartColumn }
          → convert to LSP Location (0-based, file URI)
  → return DefinitionResult
```

### Completions / Hover (existing, unchanged path)

```
ParseAndBind(scriptFile, connInfo=null)
  → hasProjectContext = (connInfo==null && IsConnected && ConnectionKey != null)
  → QueueBindingOperation(key="project_<uri>")
      → IBinder.Bind(parseResults, databaseName, BindMode.Batch)
          // binder writes semantic annotations onto the parse tree
          // these annotations are what GetDefinition reads in step 2
```

---

## Invariants — Never Break These

| Invariant | Reason |
|-----------|--------|
| `connInfo == null` is the signal for a project file | Online files always have a `ConnectionInfo`; project files never do |
| STS passes `(ParseResult, line, col)` to the engine — never a token string | Token peeling bypasses binding; the engine resolves semantically |
| One `ProjectIntelliSenseEngine` per open project, stored on `ConnectedBindingContext` | Avoids rebuilding the model per request |
| `QueueProjectDefinition` does not call `GetPeekDefinitionTokens` | That path is for the SMO/online scripter; it pre-extracts tokens because SMO needs a name, not a position |
| `ParseAndBind` must run before `GetDefinition` | `GetDefinition` reads binder annotations; they only exist after `Bind()` has been called |
| `ProjectEngine` disposes the `TSqlModel` | Do not hold a separate reference to the model outside the engine |

---

## Future IntelliSense Endpoints (pattern to follow)

For each new feature (hover, diagnostics, signature help):

1. **SqlProjects library**: add `Get*(ParseResult, int, int) → Result` to `ProjectIntelliSenseEngine`
2. **STS `LanguageService`**: add `Queue*Project(position, scriptParseInfo)` that routes to engine
3. **STS dispatch**: add `if (connInfo == null ...) return Queue*Project(...)` branch in the
   existing handler, before the online fallthrough
4. STS **never** calls `Resolver.*` directly for project files — that belongs in the engine

---

## What We Are NOT Doing

| Rejected approach | Why |
|-------------------|-----|
| STS peeling tokens and passing strings to the library | Bypasses binding; ambiguous when same name exists in two schemas |
| Dictionary/index in **STS or LanguageService** | "Yet another metadata index" outside the engine — explicitly prohibited by the design doc |
| SMO scripting for project files | SMO requires a live server connection |
| Re-parsing files from disk in STS on each F12 | The bound `ParseResult` from `ParseAndBind` already exists; use it |
| Storing raw `TSqlModel` on `ConnectedBindingContext` | Replaced by `ProjectEngine` which encapsulates model + display provider |
| Global scan of `model.GetObjects()` by unqualified name | Non-deterministic; use binder-resolved schema-qualified name |

**Note:** A `Dictionary<string, TSqlObject>` built **inside `ProjectIntelliSenseEngine`** at
construction time is the correct O(1) fix for the step 4 scan. This is explicitly NOT the same
as the rejected "index in STS" — that was about storing navigation state in `LanguageService`
or `ConnectedBindingContext` alongside the engine.

---

## Known Fixed Bugs (do not re-introduce)

### F12 returned "not connected" for all project .sql files

**Root cause 1 (compile error):** `BuildProjectIntelliSenseAsync` was passing a pre-built `IBinder`
as argument 2 to `UpdateLanguageServiceOnProjectOpen`, which expects `IMetadataProvider`.
This caused CS1503 — the project IntelliSense code was never compiled or executed.
**Fix:** Pass `provider` directly. The method creates the binder internally.

**Root cause 2 (logical bug):** `InitializeProjectFileContexts` was never called.
Without it, every `.sql` file kept `IsConnected=false` / `ConnectionKey=null`.
`GetDefinition` checks `connInfo==null && IsConnected && ConnectionKey!=null` — all three
must be true to enter the project branch. With `IsConnected=false` the check fails and
every F12 falls into the "not connected" error path.
**Fix:** After `UpdateLanguageServiceOnProjectOpen`, call `InitializeProjectFileContexts`
with all `.sql` file URIs from `project.SqlObjectScripts`.

**Do not regress:** Any future refactor of `BuildProjectIntelliSenseAsync` must ensure
both `UpdateLanguageServiceOnProjectOpen` and `InitializeProjectFileContexts` are called,
in that order, with matching `contextKey = $"project_{projectUri}"`.

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

In `extensions/sql-database-projects/src/models/project.ts`, method `readSqlObjectScripts`:

Remove the `for` loop that calls `checkForCreateTableStatement` on every file.
Replace with a simple `.map()` that creates all entries as `SqlObjectFileNode` (pass `false`
for `containsCreateTableStatement`):

```typescript
// BEFORE (slow: 2500 IPC calls)
for (const f of filesSet.values()) {
    const entry = this.createFileProjectEntry(f, EntryType.File);
    const containsTable = await this.sqlProjectsService.parseTSqlScript(f.fsPath, ...);
    entry.containsCreateTableStatement = containsTable;
    this._sqlObjectScripts.push(entry);
}

// AFTER (fast: zero IPC calls)
this._sqlObjectScripts = Array.from(filesSet.values()).map(f =>
    this.createFileProjectEntry(f, EntryType.File, undefined, false),
);
```

**Validation before merging:**
1. Search `package.json` for `itemType.file.table` — must return zero results (confirms no menu uses it).
2. Open a large project and verify tree renders immediately.
3. Verify right-click menu on a table file and a view file show the same options (no regression).

**No STS changes required.** Do not add a notification, do not add `markFileAsTable`, do not
add `onNotification` to `IExtension`. The fix is purely a deletion in `project.ts`.
