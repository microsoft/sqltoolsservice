# SQL Tools Service — Project IntelliSense Architecture & Implementation Guide

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

**Method: `BuildProjectIntelliSenseAsync(string projectUri)`**

Called fire-and-forget from `HandleOpenSqlProjectRequest` after a project is opened.

Sequence:
1. `TSqlModelBuilder.LoadModel(project)` — parses all DDL scripts into a `TSqlModel`
2. `new LazySchemaModelMetadataProvider(model, databaseName)` — wraps model as `IMetadataProvider`
3. `new MetadataDisplayInfoProvider()` — needed by `Resolver.FindCompletions` in the engine
4. `new ProjectIntelliSenseEngine(model, displayInfoProvider)` — engine that owns the model
5. `LanguageService.Instance.UpdateLanguageServiceOnProjectOpen(projectUri, provider, parseOptions, databaseName, engine)`
   — `UpdateLanguageServiceOnProjectOpen` creates the `IBinder` internally via
     `BinderProvider.CreateBinder(metadataProvider)` and stores it on the binding context

**Rule:** This is the only place a `ProjectIntelliSenseEngine` is constructed. The engine is
passed into STS and stored there. `SqlProjectsService` does not hold a reference to it after this.

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

The entry point for F12. Now has a project-file branch at the top:

```csharp
// Project file: connInfo is null but IsConnected (project binding context is registered).
if (connInfo == null && scriptParseInfo.IsConnected && scriptParseInfo.ConnectionKey != null)
{
    // ParseAndBind must run first so the ParseResult has binder annotations.
    if (RequiresReparse(scriptParseInfo, scriptFile))
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
  → BuildProjectIntelliSenseAsync (background Task)
      → TSqlModelBuilder.LoadModel()           // SqlProjects library builds model
      → new LazySchemaModelMetadataProvider()  // wraps model for binder
      → new MetadataDisplayInfoProvider()      // for Resolver
      → new ProjectIntelliSenseEngine()        // engine owns model + displayInfoProvider
      → UpdateLanguageServiceOnProjectOpen()
          → BinderProvider.CreateBinder(provider)
          → BindingQueue.AddProjectContext("project_<uri>", binder, parseOptions, engine)
              // engine stored at bindingContext.ProjectEngine
          → scriptInfo.IsConnected = true
          → scriptInfo.ConnectionKey = "project_<uri>"
          → SendEvent(IntelliSenseReadyNotification)
```

### F12 (Go-to-Definition)

```
GetDefinition(position, file, connInfo=null)
  → scriptParseInfo = GetScriptParseInfo(file.ClientUri)
  → connInfo==null + IsConnected + ConnectionKey!=null  ← project file branch
      → if RequiresReparse: ParseAndBind(scriptFile, null)
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
