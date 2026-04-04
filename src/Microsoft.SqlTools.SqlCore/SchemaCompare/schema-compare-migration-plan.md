# Schema Compare Refactoring — Revised Migration Plan

## Overview

This document describes the phased migration plan for refactoring Schema Compare to extract host-agnostic business logic from `Microsoft.SqlTools.ServiceLayer` into `Microsoft.SqlTools.SqlCore`. This enables the Schema Compare operations to be consumed by different hosts (VSCode/ADS, SSMS) through dependency-injection interfaces.

The migration is organized into **3 phases**, each delivered as an independent, compilable PR:

| Phase | Description | Status |
|-------|-------------|--------|
| **Phase 1** | Multi-Framework Target & Conditional Compilation | ✅ Done ([PR #2635](https://github.com/microsoft/sqltoolsservice/pull/2635)) |
| **Phase 2** | Contracts, Interfaces & DeploymentOptions Foundation | 🔄 This PR |
| **Phase 3** | Operation Refactoring, Adapters & Service Rewiring | 📋 Planned |

```
Phase 1 (PR #2635 — Framework support) ✅ Done
    │
    ▼
Phase 2 (Contracts + Interfaces + DeploymentOptions)
    │
    ▼
Phase 3 (Operations + Adapters + Service Rewiring)
```

---

## Phase 1 — Multi-Framework Target & Conditional Compilation ✅

**PR:** [#2635](https://github.com/microsoft/sqltoolsservice/pull/2635)

Enables .NET Framework 4.7.2 support across projects and fixes conditional compilation directives.

**Changes:**
- `Microsoft.SqlTools.Hosting.csproj` — Add `net472` to `TargetFrameworks`
- `Microsoft.SqlTools.Connectors.VSCode.csproj` — Add `net472` to `TargetFrameworks`
- `Microsoft.SqlTools.SqlCore.csproj` — Add `net472` to `TargetFrameworks`
- `ProtocolEndpoint.cs` — Fix conditional: `#if NETSTANDARD2_0` → `#if !NET6_0_OR_GREATER`
- `Logger.cs` — Swap conditional for `Environment.ProcessId` vs `Process.GetCurrentProcess().Id`
- `SqlClientEventListener.cs` — Update conditionals for `net472` support and `OSThreadId`
- `KernelJsonSchemaBuilder.cs` — Guard `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` with `#if NET6_0_OR_GREATER`

---

## Phase 2 — Contracts, Interfaces & DeploymentOptions Foundation

**Goal:** Establish all shared abstractions, contracts, and type infrastructure in `SqlCore` that the refactored operations will depend on. No behavioral changes to existing operations — purely additive and structural. Operations remain in `ServiceLayer` and continue to function as before.

**Risk:** Low-Medium (mostly namespace moves, new additive types, and using statement updates)

### 2.1 Move `DeploymentOptions` to `SqlCore`

**Files changed:**
- **Moved:** `ServiceLayer/DacFx/Contracts/DeploymentOptions.cs` → `SqlCore/DacFx/Contracts/DeploymentOptions.cs`
- Namespace changed from `Microsoft.SqlTools.ServiceLayer.DacFx.Contracts` → `Microsoft.SqlTools.SqlCore.DacFx.Contracts`

**Content additions:**

### 2.2 Move `DacFxUtils` to `SqlCore`

**Files changed:**
- **Moved:** `ServiceLayer/DacFx/DacFxUtils.cs` → `SqlCore/DacFx/DacFxUtils.cs`
- Namespace changed from `Microsoft.SqlTools.ServiceLayer.DacFx` → `Microsoft.SqlTools.SqlCore.DacFx`

### 2.3 Create Schema Compare Interfaces in `SqlCore`

Three new files define the core abstractions that decouple Schema Compare from host-specific infrastructure:

**`SqlCore/SchemaCompare/ISchemaCompareConnectionProvider.cs`**
```csharp
public interface ISchemaCompareConnectionProvider
{
    string GetConnectionString(SchemaCompareEndpointInfo endpointInfo);
    string GetAccessToken(SchemaCompareEndpointInfo endpointInfo);
    SchemaCompareEndpointInfo ParseConnectionString(string connectionString);
}
```
- `GetConnectionString()` — Builds a connection string from endpoint info (VSCode uses `ConnectionService`; SSMS uses its own infrastructure)
- `GetAccessToken()` — Returns an Azure MFA access token if applicable, `null` otherwise
- `ParseConnectionString()` — Parses a raw connection string (from SCMP files) into endpoint info with `ServerName`, `DatabaseName`, `UserName`

**`SqlCore/SchemaCompare/ISchemaCompareScriptHandler.cs`**
```csharp
public interface ISchemaCompareScriptHandler
{
    void OnScriptGenerated(string script);
    void OnMasterScriptGenerated(string masterScript);
}
```
- `OnScriptGenerated()` — Called when a deployment script is generated (VSCode feeds it to `SqlTask.AddScript`)
- `OnMasterScriptGenerated()` — Called when a master database script is generated (for Azure SQL DB)

**`SqlCore/SchemaCompare/AccessTokenProvider.cs`**
- Internal class implementing DacFx's `IUniversalAuthProvider`
- Simple token holder used when connecting to Azure SQL with MFA authentication

### 2.4 Create Schema Compare Contracts in `SqlCore`

Three new files define host-agnostic parameter, result, and domain types:

**`SqlCore/SchemaCompare/Contracts/SchemaCompareContracts.cs`** — Shared domain types:
| Type | Description |
|------|-------------|
| `SchemaCompareEndpointType` enum | Database, Dacpac, Project |
| `SchemaCompareEndpointInfo` class | Host-agnostic endpoint info (no `ConnectionDetails`) |
| `SchemaCompareObjectId` class | Identifies a schema object by name parts and SQL type |
| `DiffEntry` class | Represents a single difference in a schema comparison |
| `SchemaCompareResultBase` class | Base class with `Success` and `ErrorMessage` |

**`SqlCore/SchemaCompare/Contracts/SchemaCompareParams.cs`** — Core parameter types:
| Type | Description |
|------|-------------|
| `SchemaCompareParams` | Main comparison params (OperationId, Source/Target, DeploymentOptions) |
| `SchemaComparePublishDatabaseChangesParams` | Publish to database params |
| `SchemaCompareGenerateScriptParams` | Generate script params (extends publish params) |
| `SchemaComparePublishProjectChangesParams` | Publish to SQL project params |
| `SchemaCompareNodeParams` | Include/exclude single node params |
| `SchemaCompareIncludeExcludeAllNodesParams` | Include/exclude all nodes params |
| `SchemaCompareOpenScmpParams` | Open SCMP file params |
| `SchemaCompareSaveScmpParams` | Save SCMP file params (extends SchemaCompareParams) |
| `SchemaCompareGetOptionsParams` | Get default options params (empty) |
| `SchemaCompareCancelParams` | Cancel operation params |

**`SqlCore/SchemaCompare/Contracts/SchemaCompareResults.cs`** — Core result types:
| Type | Description |
|------|-------------|
| `SchemaCompareResult` | Comparison result with OperationId, AreEqual, Differences |
| `SchemaCompareOpenScmpResult` | SCMP parse result with endpoints and options |
| `SchemaCompareIncludeExcludeResult` | Include/exclude result with affected/blocking dependencies |
| `SchemaCompareIncludeExcludeAllNodesResult` | All-nodes include/exclude result |
| `SchemaCompareOptionsResult` | Default deployment options result |
| `SchemaCompareScriptResult` | Generated script result (Script + MasterScript) |

### 2.5 Update ServiceLayer Schema Compare Contracts

Existing ServiceLayer contract files are updated to reference SqlCore base types instead of defining their own copies:

| File | Change |
|------|--------|
| `SchemaCompareRequest.cs` | Removes local `SchemaCompareEndpointType` enum, `DiffEntry` class; imports from SqlCore. `SchemaCompareEndpointInfo` keeps `ConnectionDetails` (ServiceLayer-specific). `SchemaCompareParams` keeps `TaskExecutionMode`. |
| `SchemaCompareGenerateScriptRequest.cs` | `SchemaCompareGenerateScriptParams` extends SqlCore `SchemaCompareGenerateScriptParams`, adds `TaskExecutionMode` |
| `SchemaCompareIncludeExcludeNodeRequest.cs` | `SchemaCompareNodeParams` extends SqlCore version, adds `TaskExecutionMode` |
| `SchemaCompareIncludeExcludeAllNodesRequest.cs` | `SchemaCompareIncludeExcludeAllNodesParams` extends SqlCore version, adds `TaskExecutionMode` |
| `SchemaCompareOpenScmpRequest.cs` | Removes local `SchemaCompareObjectId`; imports from SqlCore |
| `SchemaComparePublishDatabaseChangesRequest.cs` | `SchemaComparePublishDatabaseChangesParams` extends SqlCore version, adds `TaskExecutionMode` |
| `SchemaComparePublishProjectChangesRequest.cs` | `SchemaComparePublishProjectChangesParams` extends SqlCore version, adds `TaskExecutionMode` |
| `SchemaCompareSaveScmpRequest.cs` | Uses SqlCore types |

### 2.7 Update Using Statements

All ServiceLayer files that reference `DeploymentOptions` or `DacFxUtils` are updated:

**DacFx files (add `using Microsoft.SqlTools.SqlCore.DacFx.Contracts;` and/or `using Microsoft.SqlTools.SqlCore.DacFx;`):**
- `DacFx/Contracts/DeployRequest.cs`
- `DacFx/Contracts/GenerateDeployScriptRequest.cs`
- `DacFx/Contracts/GetDeploymentOptionsRequest.cs`
- `DacFx/Contracts/GetOptionsFromProfileRequest.cs`
- `DacFx/Contracts/SavePublishProfileRequest.cs`
- `DacFx/DacFxService.cs`
- `DacFx/DeployOperation.cs`
- `DacFx/GenerateDeployScriptOperation.cs`

**SqlPackage files:**
- `SqlPackage/Contracts/GenerateSqlPackageCommandRequest.cs`
- `SqlPackage/SqlPackageService.cs`

**SchemaCompare operation files (add `using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;` for types like `DiffEntry`, `SchemaCompareEndpointType`):**
- `SchemaCompare/SchemaCompareOperation.cs`
- `SchemaCompare/SchemaCompareOpenScmpOperation.cs`
- `SchemaCompare/SchemaCompareSaveScmpOperation.cs`
- `SchemaCompare/SchemaCompareIncludeExcludeNodeOperation.cs`
- `SchemaCompare/SchemaCompareIncludeExcludeAllNodesOperation.cs`
- `SchemaCompare/SchemaCompareUtils.cs`

### 2.8 Update Test Files

- `DacFxServiceTests.cs` — Add `using Microsoft.SqlTools.SqlCore.DacFx.Contracts;`
- `SqlPackageServiceTests.cs` — Add `using Microsoft.SqlTools.SqlCore.DacFx.Contracts;`
- `SchemaCompareServiceTests.cs` — Add SqlCore contract usings
- `SchemaCompareServiceOptionsTests.cs` — Add SqlCore contract usings
- `SchemaCompareTestUtils.cs` — Add SqlCore contract usings
- `SchemaCompareTests.cs` — Update namespace references

### Phase 2 File Summary

| Category | Files | Action |
|----------|-------|--------|
| **SqlCore (new)** | 8 files | `DeploymentOptions.cs`, `DacFxUtils.cs`, 3 interfaces, 3 contracts |
| **ServiceLayer (deleted)** | 2 files | `DeploymentOptions.cs` and `DacFxUtils.cs` removed (moved to SqlCore) |
| **ServiceLayer contracts (updated)** | 10 files | Reference SqlCore types; new `SchemaCompareOptionsRequest.cs` |
| **ServiceLayer DacFx/SqlPackage (updated)** | 10 files | Using statement updates |
| **ServiceLayer operations (updated)** | 6 files | Using statement updates only (operations not moved) |
| **SchemaCompareService.cs** | 1 file | Added options handler |
| **Test files (updated)** | 6 files | Using statement updates |
| **Total** | ~40 files | 811 insertions, 215 deletions |

### Validation

- ✅ SqlCore compiles for all target frameworks
- ✅ ServiceLayer compiles
- ✅ Unit tests compile
- ✅ Integration tests compile
- ✅ No behavioral changes to existing Schema Compare operations

---

## Phase 3 — Operation Refactoring, VSCode Adapters & Service Rewiring

**Goal:** Refactor all Schema Compare operations to use the new interfaces (making them host-agnostic), create VSCode/ADS adapter implementations, rewire `SchemaCompareService` to use adapters, and update tests. This completes the full end-to-end migration.

**Risk:** Medium-High (behavioral changes — all operations are refactored and the service layer is rewired)

### 3.1 Move and Refactor Operations to `SqlCore`

All Schema Compare operation files are moved from `ServiceLayer/SchemaCompare/` to `SqlCore/SchemaCompare/` and refactored to remove ServiceLayer dependencies:

**`SchemaCompareOperation.cs`**
| Before (ServiceLayer) | After (SqlCore) |
|----------------------|-----------------|
| Constructor takes `SchemaCompareParams`, `ConnectionInfo sourceConnInfo`, `ConnectionInfo targetConnInfo` | Constructor takes `SchemaCompareParams`, `ISchemaCompareConnectionProvider connectionProvider` |
| Implements `ITaskOperation` | Implements `IDisposable` |
| `Execute(TaskExecutionMode mode)` | `Execute()` |
| Has `SqlTask` property | No `SqlTask` dependency |
| Uses `ConnectionService` directly for endpoints | Uses `SchemaCompareUtils.CreateSchemaCompareEndpoint()` with `ISchemaCompareConnectionProvider` |

**`SchemaCompareGenerateScriptOperation.cs`**
| Before | After |
|--------|-------|
| Implements `ITaskOperation` | Implements `IDisposable` |
| Uses `SqlTask.AddScript()` for script delivery | Uses `ISchemaCompareScriptHandler.OnScriptGenerated()` |
| Constructor takes `SchemaCompareGenerateScriptParams`, `SchemaComparisonResult` | Adds optional `ISchemaCompareScriptHandler` parameter |

**`SchemaComparePublishChangesOperation.cs`** (abstract base)
| Before | After |
|--------|-------|
| Implements `ITaskOperation` | Implements `IDisposable` |
| `Execute(TaskExecutionMode mode)` | `Execute()` |
| Has `SqlTask` property | No `SqlTask` dependency |

**`SchemaComparePublishDatabaseChangesOperation.cs`**
- Accepts `SchemaComparisonResult` directly in constructor
- Removes dependency on operation ID lookup

**`SchemaComparePublishProjectChangesOperation.cs`**
- Same pattern as database changes

**`SchemaCompareOpenScmpOperation.cs`**
| Before | After |
|--------|-------|
| Uses `ConnectionService.ParseConnectionString()` directly | Uses `ISchemaCompareConnectionProvider.ParseConnectionString()` |
| Constructor takes `SchemaCompareOpenScmpParams` only | Adds `ISchemaCompareConnectionProvider` parameter |

**`SchemaCompareSaveScmpOperation.cs`**
| Before | After |
|--------|-------|
| Uses `ConnectionService` for endpoint creation | Uses `ISchemaCompareConnectionProvider` via `SchemaCompareUtils.CreateSchemaCompareEndpoint()` |
| Constructor takes `SchemaCompareSaveScmpParams` only | Adds `ISchemaCompareConnectionProvider` parameter |

**`SchemaCompareIncludeExcludeNodeOperation.cs`**
- Accepts `SchemaComparisonResult` directly in constructor
- Removes `ITaskOperation` dependency
- Implements `IDisposable`

**`SchemaCompareIncludeExcludeAllNodesOperation.cs`**
- Same pattern as single-node operation

**`SchemaCompareUtils.cs`**
| Before | After |
|--------|-------|
| `CreateSchemaCompareEndpoint()` takes `SchemaCompareEndpointInfo`, `ConnectionService` | Takes `SchemaCompareEndpointInfo`, `ISchemaCompareConnectionProvider` |
| Uses `ConnectionService` directly | Uses `ISchemaCompareConnectionProvider.GetConnectionString()` and `GetAccessToken()` |

### 3.2 Delete Original ServiceLayer Operation Files

Once moved to SqlCore, the following files are deleted from `ServiceLayer/SchemaCompare/`:
- `SchemaCompareOperation.cs`
- `SchemaCompareGenerateScriptOperation.cs`
- `SchemaComparePublishChangesOperation.cs`
- `SchemaComparePublishDatabaseChangesOperation.cs`
- `SchemaComparePublishProjectChangesOperation.cs`
- `SchemaCompareOpenScmpOperation.cs`
- `SchemaCompareSaveScmpOperation.cs`
- `SchemaCompareIncludeExcludeNodeOperation.cs`
- `SchemaCompareIncludeExcludeAllNodesOperation.cs`
- `SchemaCompareUtils.cs`

### 3.3 Create VSCode/ADS Adapter Implementations

Three new files in `ServiceLayer/SchemaCompare/` bridge the ServiceLayer infrastructure to the SqlCore interfaces:

**`VsCodeConnectionProvider.cs`**
```csharp
internal class VsCodeConnectionProvider : ISchemaCompareConnectionProvider
{
    private readonly ConnectionService _connectionService;

    public string GetConnectionString(SchemaCompareEndpointInfo endpointInfo)
    {
        // Uses ConnectionService.TryFindConnection() + BuildConnectionString()
    }

    public string GetAccessToken(SchemaCompareEndpointInfo endpointInfo)
    {
        // Returns Azure MFA token from ConnectionInfo if applicable
    }

    public SchemaCompareEndpointInfo ParseConnectionString(string connectionString)
    {
        // Uses ConnectionService.ParseConnectionString()
    }
}
```

**`VsCodeScriptHandler.cs`**
```csharp
internal class VsCodeScriptHandler : ISchemaCompareScriptHandler
{
    private readonly Func<SqlTask> _getTask;

    public void OnScriptGenerated(string script)
    {
        _getTask()?.AddScript(SqlTaskStatus.Succeeded, script);
    }

    public void OnMasterScriptGenerated(string masterScript)
    {
        _getTask()?.AddScript(SqlTaskStatus.Succeeded, masterScript);
    }
}
```

**`SchemaCompareTaskAdapter.cs`**
```csharp
internal class SchemaCompareTaskAdapter : ITaskOperation
{
    private readonly Action _execute;
    private readonly Action _cancel;
    private readonly Func<string> _getError;

    public SqlTask SqlTask { get; set; }
    public string ErrorMessage => _getError?.Invoke();

    public void Execute(TaskExecutionMode mode) => _execute();
    public void Cancel() => _cancel();
}
```
- Wraps host-agnostic operations as `ITaskOperation` for `SqlTaskManager`
- Uses delegates for execute/cancel/error to avoid coupling to specific operation types

### 3.4 Rewire `SchemaCompareService.cs`

The service is rewritten to create adapters and delegate to refactored SqlCore operations:

**New pattern for all handlers:**
```csharp
// Before (Phase 2 — direct ServiceLayer operation)
var operation = new SchemaCompareOperation(parameters, sourceConnInfo, targetConnInfo);
SqlTaskManager.AddTask(operation);

// After (Phase 3 — adapter pattern)
var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
var coreParams = ToCore(parameters);  // map ServiceLayer → SqlCore types
var coreOp = new CoreOps.SchemaCompareOperation(coreParams, connectionProvider);

var adapter = new SchemaCompareTaskAdapter(
    execute: () => coreOp.Execute(),
    cancel:  () => coreOp.Cancel(),
    getError: () => coreOp.ErrorMessage
);
SqlTaskManager.AddTask(adapter);
```

**New mapping helper methods added to `SchemaCompareService`:**
| Method | Description |
|--------|-------------|
| `ToCore(SchemaCompareEndpointInfo)` | Maps ServiceLayer endpoint info (with ConnectionDetails) to SqlCore endpoint info |
| `FromCore(SchemaCompareEndpointInfo)` | Maps SqlCore endpoint info back to ServiceLayer format, constructing ConnectionDetails |
| `ToCoreParams(SchemaCompareParams)` | Maps full params including endpoints |

**Handler rewiring summary:**
| Handler | Adapter Used | Script Handler |
|---------|-------------|----------------|
| `HandleSchemaCompareRequest` | `SchemaCompareTaskAdapter` wrapping `SchemaCompareOperation` | N/A |
| `HandleSchemaCompareGenerateScriptRequest` | `SchemaCompareTaskAdapter` wrapping `SchemaCompareGenerateScriptOperation` | `VsCodeScriptHandler` |
| `HandleSchemaComparePublishDatabaseChangesRequest` | `SchemaCompareTaskAdapter` wrapping `SchemaComparePublishDatabaseChangesOperation` | N/A |
| `HandleSchemaComparePublishProjectChangesRequest` | `SchemaCompareTaskAdapter` wrapping `SchemaComparePublishProjectChangesOperation` | N/A |
| `HandleSchemaCompareIncludeExcludeNodeRequest` | Direct call (no task needed) | N/A |
| `HandleSchemaCompareIncludeExcludeAllNodesRequest` | Direct call (no task needed) | N/A |
| `HandleSchemaCompareOpenScmpRequest` | Direct call with `VsCodeConnectionProvider` | N/A |
| `HandleSchemaCompareSaveScmpRequest` | Direct call with `VsCodeConnectionProvider` | N/A |

### 3.5 Update Tests

**New test infrastructure:**
- `TestConnectionProvider.cs` — Test implementation of `ISchemaCompareConnectionProvider` for integration tests

**Updated test files:**
| File | Change |
|------|--------|
| `SchemaCompareServiceTests.cs` | Uses `TestConnectionProvider`; updated to work with new adapter pattern |
| `SchemaCompareServiceOptionsTests.cs` | Updated for new contracts |
| `SchemaCompareTestUtils.cs` | Updated utility methods |
| `SchemaCompareTests.cs` (unit tests) | Minor namespace updates |

### Phase 3 File Summary

| Category | Files | Action |
|----------|-------|--------|
| **SqlCore operations (moved)** | 10 files | Operations + Utils moved from ServiceLayer, refactored to use interfaces |
| **ServiceLayer operations (deleted)** | 10 files | Originals deleted |
| **ServiceLayer adapters (new)** | 3 files | `VsCodeConnectionProvider`, `VsCodeScriptHandler`, `SchemaCompareTaskAdapter` |
| **SchemaCompareService.cs** | 1 file | Full rewiring to use adapters |
| **Test infrastructure (new)** | 1 file | `TestConnectionProvider` |
| **Test files (updated)** | ~4 files | Updated for new architecture |
| **Total** | ~25 files | Major behavioral change |

### Validation

- All Schema Compare operations work end-to-end through the new architecture
- All unit tests pass
- All integration tests pass
- Operations are now host-agnostic and can be consumed by SSMS or other clients

---

## Architecture Diagram

### Before (Main)
```
┌─────────────────────────────────────────────┐
│           Microsoft.SqlTools.ServiceLayer    │
│                                             │
│  SchemaCompareService ──→ Operations        │
│       │                      │              │
│       │                      ├─ ConnectionInfo
│       │                      ├─ SqlTask     │
│       │                      └─ ITaskOperation
│       │                                     │
│  DacFx/Contracts/DeploymentOptions          │
│  DacFx/DacFxUtils                           │
└─────────────────────────────────────────────┘
```

### After Phase 2
```
┌─────────────────────────────────────────────┐
│              Microsoft.SqlTools.SqlCore      │
│                                             │
│  DacFx/Contracts/DeploymentOptions ←────────┤ (moved from ServiceLayer)
│  DacFx/DacFxUtils           ←───────────────┤ (moved from ServiceLayer)
│  SchemaCompare/                              │
│    ├─ ISchemaCompareConnectionProvider  NEW  │
│    ├─ ISchemaCompareScriptHandler       NEW  │
│    ├─ AccessTokenProvider               NEW  │
│    └─ Contracts/                        NEW  │
│        ├─ SchemaCompareContracts             │
│        ├─ SchemaCompareParams                │
│        └─ SchemaCompareResults               │
└──────────────────────────────┬──────────────┘
                               │ references
┌──────────────────────────────┴──────────────┐
│           Microsoft.SqlTools.ServiceLayer    │
│                                             │
│  SchemaCompareService ──→ Operations        │
│       │                      │              │
│       │                      ├─ ConnectionInfo
│       │                      ├─ SqlTask     │
│       │                      └─ ITaskOperation
│       │                                     │
│  SchemaCompare/Contracts/                    │
│    └─ (inherit from SqlCore contracts)       │
│    └─ SchemaCompareOptionsRequest    NEW     │
└─────────────────────────────────────────────┘
```

### After Phase 3 (Final)
```
┌─────────────────────────────────────────────┐
│              Microsoft.SqlTools.SqlCore      │
│                                             │
│  DacFx/Contracts/DeploymentOptions           │
│  DacFx/DacFxUtils                            │
│  SchemaCompare/                              │
│    ├─ ISchemaCompareConnectionProvider       │
│    ├─ ISchemaCompareScriptHandler            │
│    ├─ AccessTokenProvider                    │
│    ├─ SchemaCompareOperation          ←──────┤ (moved from ServiceLayer)
│    ├─ SchemaCompareGenerateScriptOp   ←──────┤
│    ├─ SchemaComparePublish*Operation  ←──────┤
│    ├─ SchemaCompareOpenScmpOperation  ←──────┤
│    ├─ SchemaCompareSaveScmpOperation  ←──────┤
│    ├─ SchemaCompareInclude*Operation  ←──────┤
│    ├─ SchemaCompareUtils              ←──────┤
│    └─ Contracts/                             │
└──────────────────────────────┬──────────────┘
                               │ references
┌──────────────────────────────┴──────────────┐
│           Microsoft.SqlTools.ServiceLayer    │
│                                             │
│  SchemaCompareService                        │
│    ├─ creates VsCodeConnectionProvider       │
│    ├─ creates VsCodeScriptHandler            │
│    └─ wraps operations in TaskAdapter        │
│                                             │
│  Adapters (NEW):                             │
│    ├─ VsCodeConnectionProvider               │
│    ├─ VsCodeScriptHandler                    │
│    └─ SchemaCompareTaskAdapter               │
│                                             │
│  SchemaCompare/Contracts/                    │
│    └─ (wire-format types + TaskExecutionMode)│
└─────────────────────────────────────────────┘
```
