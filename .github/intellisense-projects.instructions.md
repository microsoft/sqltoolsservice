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

**Critical:** DacFx version in `Packages.props` must be 170.4.63-preview to match SQL Projects dependency.

After repacking from SqlProjects repo, clear cache and restore:

```powershell
# Clear NuGet cache
Remove-Item "C:\.tools\.nuget\packages\microsoft.sqlserver.dac.projects" -Recurse -Force -ErrorAction SilentlyContinue

# Restore STS
cd C:\Projects\sqltoolsservice
dotnet restore src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj
    -c Release /p:PackageVersion=0.5.22-local `
    --output "C:\Projects\sqltoolsservice\bin\nuget"

# Clear cache and restore STS
Remove-Item "C:\.tools\.nuget\packages\microsoft.sqlserver.dacfx.projects" -Recurse -Force -ErrorAction SilentlyContinue
cd C:\Projects\sqltoolsservice
dotnet restore src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj
dotnet build src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj -c Release --no-restore
```

The version pin is in `Packages.props`:
```xml
<PackageReference Update="Microsoft.SqlServer.DacFx.Projects" Version="0.5.22-local" />
```

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

```csharp
private static bool IsProjectContext(string connectionKey)
    => !string.IsNullOrEmpty(connectionKey) &&
       connectionKey.StartsWith("project_", StringComparison.Ordinal);
```

Use this everywhere a routing branch is needed. Do NOT add new flags or new branching mechanisms.

---

## `IConnectedBindingQueue` interface

Both context types are registered via the same interface. `LanguageService.BindingQueue` is typed
as `IConnectedBindingQueue` — **not** the concrete `ConnectedBindingQueue`. `CompletionService`
also takes `IConnectedBindingQueue`.

```csharp
public interface IConnectedBindingQueue
{
    string AddConnectionContext(ConnectionInfo connInfo, string featureName = null, bool overwrite = false);
    void AddProjectContext(string projectKey, IBinder binder, ParseOptions parseOptions,
        ProjectIntelliSenseEngine projectEngine = null);
    bool IsBindingContextConnected(string key);
    void Dispose();
    QueueItem QueueBindingOperation(string key, Func<IBindingContext, CancellationToken, object> bindOperation, ...);
    void CloseConnections(...);
    void OpenConnections(...);
}
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
2. Build file URI list: `project.SqlObjectScripts.Select(s => new Uri(Path.Combine(projectDir, s.Path)).AbsoluteUri)`
   — `SqlObjectScript.Path` is relative to the project directory; resolve to absolute before converting to URI.
3. `LanguageService.Instance.InitializeProjectFileContexts(fileUris, contextKey, databaseName)`
   — **BEFORE `LoadModel`** — stamps `IsConnected=true` / `ConnectionKey="project_<uri>"` /
     `ProjectDatabaseName` on every `.sql` file's `ScriptParseInfo`.
   — Ensures requests arriving while the model loads are queued against the right key, not rejected.
   — **Without this call:** every `.sql` file stays `IsConnected=false` → F12 returns "not connected".
4. `TSqlModelBuilder.LoadModel(project)` — parses all DDL scripts into a `TSqlModel` (can take
   seconds on large projects; files are already stamped so requests are accepted during this time)
5. `new LazySchemaModelMetadataProvider(model, databaseName)` — wraps model as `IMetadataProvider`
6. `new ProjectIntelliSenseEngine(model)` — engine that owns the model
7. `LanguageService.Instance.UpdateLanguageServiceOnProjectOpen(projectUri, provider, parseOptions, databaseName, engine)`
   — pass `provider` (`IMetadataProvider`), NOT a pre-built `IBinder`.
   — `UpdateLanguageServiceOnProjectOpen` creates the `IBinder` internally via
     `BinderProvider.CreateBinder(metadataProvider)` and stores it on the binding context.
   — stamps `IsConnected=true` / `ConnectionKey="project_<uri>"` on the `.sqlproj` ScriptParseInfo.
   — sends `IntelliSenseReadyNotification` to VS Code.

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
// Project file: detect via ConnectionKey prefix, not connInfo==null.
// (A project file may also have a server connection, so connInfo==null is unreliable.)
if (IsProjectContext(scriptParseInfo.ConnectionKey))
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

**Rule:** STS detects project vs. online by `IsProjectContext(scriptParseInfo.ConnectionKey)`.
It does NOT peel a token or extract an identifier string before routing.

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
      → build fileUris from project.SqlObjectScripts:
          new Uri(Path.Combine(projectDir, s.Path)).AbsoluteUri  // relative → absolute → URI
      → InitializeProjectFileContexts(fileUris, "project_<uri>", databaseName)
          // *** BEFORE LoadModel *** — stamps every .sql file ScriptParseInfo:
          //   IsConnected=true, ConnectionKey="project_<uri>", ProjectDatabaseName
          // Requests during model load are queued, not rejected.
          // WITHOUT THIS CALL: every .sql file keeps IsConnected=false → F12 = "not connected"
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
```

### F12 (Go-to-Definition)

```
GetDefinition(position, file, connInfo=null)
  → scriptParseInfo = GetScriptParseInfo(file.ClientUri)   ← OrdinalIgnoreCase lookup
  → IsProjectContext(scriptParseInfo.ConnectionKey)  ← project file branch
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
ParseAndBind(scriptFile, connInfo)
  → parseInfo = GetScriptParseInfo(scriptFile.ClientUri)  ← OrdinalIgnoreCase lookup
  → hasBindingContext = parseInfo.IsConnected && parseInfo.ConnectionKey != null
  → QueueBindingOperation(key: parseInfo.ConnectionKey, ...)
      → TryIncrementalParse() with bindingContext.ParseOptions
      → bindingContext.Binder.Bind(parseResults, dbName, BindMode.Batch)
          Project path: binder backed by LazySchemaModelMetadataProvider (TSqlModel)
          SMO path:     binder backed by SmoMetadataProvider (live server)
  // dbName: project → parseInfo.ProjectDatabaseName; SMO → connInfo.ConnectionDetails.DatabaseName
```

### Connection IntelliSense startup sequence

```
User connects to a server in vscode-mssql
  → STS: ConnectionService.Connect()
      → LanguageService.UpdateLanguageServiceOnConnection(info)
          Guard: if IsProjectContext(scriptInfo.ConnectionKey) → return
                 (project files must never have their key overwritten by a server connection)
          ConnectedBindingQueue.AddConnectionContext(connInfo, featureName)
              → ServerConnection = OpenServerConnection(connInfo)
              → SmoMetadataProvider.CreateConnectedProvider(serverConnection)
              → BinderProvider.CreateBinder(smoMetadataProvider)
              → bindingContext.IsConnected = true
              returns connectionKey = hash(server+db+user+auth+...)
          scriptInfo.ConnectionKey = connectionKey
          scriptInfo.IsConnected   = true
      → PrepopulateCommonMetadata() [warm up the binder]
      → ServiceHost.SendEvent(IntelliSenseReadyNotification)
```

### `ParseAndBind` — binding context lookup

For project contexts `bindingContext.ServerConnection` is `null` — intentional.
Only the `QueueTask` (SMO scripter) path accesses `ServerConnection`; it has a null-guard:

```csharp
// In QueueTask bindOperation lambda:
if (bindingContext.ServerConnection == null)
    return new DefinitionResult { IsErrorResult = true, Message = SR.PeekDefinitionNotConnectedError };
```

### Supported LSP features

| Feature | LSP method | Project path | Online path |
|---|---|---|---|
| **Completions** | `textDocument/completion` | ✅ bound ParseResult | ✅ SMO |
| **Completion resolve** | `textDocument/completionItem/resolve` | ✅ | ✅ |
| **Go-to-Definition / Peek** | `textDocument/definition` | ✅ `ProjectIntelliSenseEngine` | ✅ SMO `Scripter` |
| **Hover** | `textDocument/hover` | ✅ | ✅ |
| **Signature help** | `textDocument/signatureHelp` | ✅ | ✅ |
| **Syntax parse** | `sqlTools/syntaxParse` | ✅ | ✅ |
| **Diagnostics** | internal | ✅ (suppressed — no false errors) | ✅ |
| **References** | `textDocument/references` | ❌ commented out | ❌ |
| **Document highlight** | `textDocument/documentHighlight` | ❌ commented out | ❌ |

### Diagnostics suppression for project files

```csharp
// In GetSemanticMarkers():
bool isProjectFile = IsProjectContext(parseInfo?.ConnectionKey);
if (isProjectFile)
    return Array.Empty<ScriptFileMarker>();
```

The binder reports DDL objects as duplicates (they are already loaded into the metadata model),
producing false "already exists" errors. Project-level validation belongs to the build step.

---

## Invariants — Never Break These

| Invariant | Reason |
|-----------|--------|
| `IsProjectContext(ConnectionKey)` is the routing signal | Never use `connInfo == null`; a project file may also have a server connection |
| `UpdateLanguageServiceOnConnection` must NOT overwrite a project key | Guard: `if (IsProjectContext(...)) return;` at the top of the method |
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
3. **STS dispatch**: add `if (IsProjectContext(scriptParseInfo.ConnectionKey)) return Queue*Project(...)` branch
   in the existing handler, before the online fallthrough
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
`GetDefinition` checks `IsProjectContext(scriptParseInfo.ConnectionKey)` — the key must be
non-null and start with `"project_"`. With `ConnectionKey=null` the check fails and every F12
falls into the "not connected" error path.
**Fix:** Before `LoadModel`, call `InitializeProjectFileContexts` with all `.sql` file URIs from
`project.SqlObjectScripts`. This stamps `ConnectionKey` and `IsConnected=true` so the routing
check succeeds even while the model is still loading.

**Do not regress:** Any future refactor of `BuildProjectIntelliSenseAsync` must ensure
`InitializeProjectFileContexts` is called BEFORE `LoadModel`, and `UpdateLanguageServiceOnProjectOpen`
is called after the model is fully built, with matching `contextKey = $"project_{projectUri}"` in both.

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

---

## URI NORMALIZATION (CRITICAL)

**Problem:** VS Code sends URIs in different encodings:
- File stamping from SqlProjects: `file:///c:/path/to/file.sql`
- LSP requests from VS Code: `file:///c%3A/path/to/file.sql` (percent-encoded colon)

**Solution:** `ScriptParseInfoMap` uses `StringComparer.OrdinalIgnoreCase`:

```csharp
private Lazy<ConcurrentDictionary<string, ScriptParseInfo>> scriptParseInfoMap
    = new Lazy<ConcurrentDictionary<string, ScriptParseInfo>>(
        () => new ConcurrentDictionary<string, ScriptParseInfo>(StringComparer.OrdinalIgnoreCase));
```

**Why this works:**
- Windows drive letters are case-insensitive (`C:/` == `c:/`)
- Dictionary handles both `file:///C:/` and `file:///c:/` as same key
- No manual normalization needed in lookup code

**NEVER:**
- Remove `OrdinalIgnoreCase` from the map constructor
- Add manual drive-letter normalization elsewhere
- Use `Uri.UnescapeDataString()` before lookup (not needed with OrdinalIgnoreCase)

**Consequence if broken:** Files stamped with ConnectionKey but LSP requests return "not connected"

---

## F12 IMPLEMENTATION DETAILS

### Method: `QueueProjectDefinition`

**Location:** `LanguageService.cs`

**Full implementation:**

```csharp
private DefinitionResult QueueProjectDefinition(
    TextDocumentPosition textDocumentPosition,
    ScriptParseInfo scriptParseInfo)
{
    // Convert LSP position (0-based) to SqlParser position (1-based)
    int line = textDocumentPosition.Position.Line + 1;
    int column = textDocumentPosition.Position.Character + 1;
    
    string contextKey = scriptParseInfo.ConnectionKey;
    
    var operation = new QueueItem
    {
        Key = contextKey,
        BindingTimeout = BindingTimeout,
        BindingOperation = (bindingContext, cancellationToken) =>
        {
            if (!(bindingContext is ConnectedBindingContext context))
            {
                return new DefinitionResult 
                { 
                    IsErrorResult = true, 
                    Message = "Binding context not found" 
                };
            }
            
            if (context.ProjectEngine == null)
            {
                return new DefinitionResult 
                { 
                    IsErrorResult = true, 
                    Message = "Project IntelliSense engine not initialized" 
                };
            }
            
            // Call SQL Projects library to get definition
            SourceInformation sourceInfo = context.ProjectEngine.GetDefinition(
                scriptParseInfo.ParseResult, 
                line, 
                column);
            
            if (sourceInfo == null)
            {
                return new DefinitionResult 
                { 
                    IsErrorResult = true, 
                    Message = "No definition found" 
                };
            }
            
            // Convert SourceInformation to LSP Location
            string fileUri = new Uri(sourceInfo.SourceName).AbsoluteUri;
            
            var location = new Location
            {
                Uri = fileUri,
                Range = new Range
                {
                    Start = new Position
                    {
                        Line = sourceInfo.StartLine - 1,        // Back to 0-based
                        Character = sourceInfo.StartColumn - 1
                    },
                    End = new Position
                    {
                        Line = sourceInfo.StartLine - 1,
                        Character = sourceInfo.StartColumn - 1
                    }
                }
            };
            
            return new DefinitionResult
            {
                IsErrorResult = false,
                Locations = new[] { location }
            };
        }
    };
    
    BindingQueue.QueueBindingOperation(operation);
    operation.ItemProcessed.WaitOne();
    
    return (DefinitionResult)operation.Result;
}
```

**Key points:**
- Passes bound `ParseResult` from `scriptParseInfo` (already created by `ParseAndBind`)
- Does NOT extract token text or call `GetPeekDefinitionTokens`
- Calls `ProjectEngine.GetDefinition` with position only
- Converts result coordinates from 1-based (SqlParser) to 0-based (LSP)

---

## FILE STAMPING

### Method: `InitializeProjectFileContexts`

**Purpose:** Stamp all project .sql files with ConnectionKey BEFORE model loads

**Why before LoadModel:** Model loading can take minutes. Stamping first ensures LSP requests during loading are queued correctly, not rejected as "not connected".

**Implementation:**

```csharp
public async Task InitializeProjectFileContexts(
    List<string> fileUris, 
    string contextKey, 
    string databaseName)
{
    foreach (string fileUri in fileUris)
    {
        ScriptParseInfo scriptInfo = GetOrCreateScriptParseInfo(fileUri);
        scriptInfo.ConnectionKey = contextKey;
        scriptInfo.IsConnected = true;
        scriptInfo.ProjectDatabaseName = databaseName;
    }
}
```

**Called from:** `SqlProjectsService.BuildProjectIntelliSenseAsync` after building file URI list, before `LoadModel`

**Files included:**
- `project.SqlObjectScripts` (CREATE statements)
- `project.PreDeployScripts` (executed before deployment)
- `project.PostDeployScripts` (executed after deployment)

**Why Pre/Post deployment scripts:** They reference objects defined in the project, need IntelliSense for those references even though they're not parsed into TSqlModel.

---

## BINDING CONTEXT LIFECYCLE

### Creation (project open)
```
SqlProjectsService.BuildProjectIntelliSenseAsync
  → InitializeProjectFileContexts(fileUris, contextKey, databaseName)
  → TSqlModelBuilder.LoadModel(project)
  → new LazySchemaModelMetadataProvider(model, databaseName)
  → new ProjectIntelliSenseEngine(model)
  → LanguageService.UpdateLanguageServiceOnProjectOpen(...)
      → BinderProvider.CreateBinder(metadataProvider)
      → ConnectedBindingQueue.AddProjectContext(contextKey, binder, parseOptions, engine)
          → bindingContext.ProjectEngine = engine
          → bindingContext.Binder = binder
          → _bindingContextMap[contextKey] = bindingContext
```

### Disposal (project close)
**NOT YET IMPLEMENTED** - project contexts are never removed from `_bindingContextMap`

**TODO:** Add `RemoveProjectContext(string contextKey)` method that:
1. Removes binding context from map
2. Disposes `ProjectEngine` (which disposes `TSqlModel`)
3. Clears `ConnectionKey` from all stamped files

---

## PERFORMANCE CHARACTERISTICS (STS side)

| Operation | Time | Blocking? |
|-----------|------|-----------|
| InitializeProjectFileContexts (stamp 3000 files) | <100ms | Yes (per-file loop) |
| ParseAndBind (incremental, bound before) | <10ms | Yes |
| ParseAndBind (incremental, never bound) | ~50ms | Yes (first bind) |
| QueueProjectDefinition | <5ms | Yes (waits on queue) |
| ProjectEngine.GetDefinition | Variable | Yes (O(N) scan, fix planned) |

**Key insight:** File stamping and binding are fast. Model loading (SQL Projects side) is the bottleneck.

---

## DEBUGGING CHECKLIST

### F12 returns "not connected"

1. **Check ConnectionKey is stamped:**
   ```csharp
   ScriptParseInfo info = GetScriptParseInfo(fileUri);
   Debug.WriteLine($"ConnectionKey: {info.ConnectionKey}");  
   // Should be "project_{projectUri}", NOT null
   ```

2. **Check IsProjectContext routing:**
   ```csharp
   bool isProject = IsProjectContext(info.ConnectionKey);
   // Should be true for .sql files in project
   ```

3. **Check binding context exists:**
   ```csharp
   var context = BindingQueue.GetBindingContext(info.ConnectionKey);
   // Should not be null
   ```

4. **Check ProjectEngine is set:**
   ```csharp
   if (context is ConnectedBindingContext cbc)
   {
       Debug.WriteLine($"ProjectEngine: {cbc.ProjectEngine != null}");
       // Should be true
   }
   ```

### F12 returns "No definition found"

1. **Check ParseResult is bound:**
   ```csharp
   if (scriptParseInfo.ParseResult == null)
       // ParseAndBind was not called
   ```

2. **Check Resolver.FindCompletions returns results:**
   ```csharp
   // Inside ProjectEngine.GetDefinition
   var declarations = Resolver.FindCompletions(parseResult, line, column, displayInfoProvider);
   if (declarations == null || !declarations.Any())
       // Binder didn't resolve the identifier
   ```

3. **Check object exists in TSqlModel:**
   ```csharp
   // Inside SQL Projects library
   var allObjects = model.GetObjects(DacQueryScopes.UserDefined);
   var match = allObjects.FirstOrDefault(o => 
       string.Join(".", o.Name.Parts).Equals(qualifiedName, OrdinalIgnoreCase));
   // If null, object wasn't parsed into model
   ```

### Performance issues

1. **Model loading takes >10 minutes:**
   - Expected: ~2 minutes per 1000 files
   - If slower: disk I/O bottleneck, antivirus scanning .sql files

2. **F12 takes >1 second:**
   - Set breakpoint in `ProjectEngine.GetDefinition`
   - Step 4 (O(N) scan) is the bottleneck
   - Fix: implement O(1) dictionary lookup (see SQL Projects instructions)

---

## LESSONS LEARNED (STS-specific)

1. **OrdinalIgnoreCase critical for Windows drive letters** - VS Code sends both `c:/` and `C:/`
2. **Stamp files BEFORE LoadModel** - Ensures requests during loading are queued, not rejected
3. **Pre/Post deployment scripts need stamping** - They reference project objects even though not in model
4. **Always ParseAndBind before GetDefinition** - Binder annotations required by Resolver
5. **Never check `connInfo == null` for routing** - Project files may also have server connections
6. **IsProjectContext is the single routing signal** - All branches must check ConnectionKey prefix
7. **ProjectEngine owns TSqlModel disposal** - Don't hold separate model reference in STS
