# Schema Compare Migration — Phase 3 Summary

## Overview

This document describes the concrete steps taken in **Phase 3** of the Schema Compare refactoring. Phase 3 completes the migration by moving all Schema Compare operation logic from `Microsoft.SqlTools.ServiceLayer` into `Microsoft.SqlTools.SqlCore`, making operations host-agnostic, and rewiring the ServiceLayer to use an adapter pattern.

Phase 3 builds on the foundation established in:
- **Phase 1** ([PR #2635](https://github.com/microsoft/sqltoolsservice/pull/2635)) — Multi-framework targeting & conditional compilation
- **Phase 2** (`bruzel/SCRefactor2`) — Contracts, interfaces & DeploymentOptions foundation in SqlCore

---

## Step 1 — Move Operations to SqlCore (Host-Agnostic)

All 10 Schema Compare operation and utility files were recreated in `SqlCore/SchemaCompare/` with host-agnostic signatures. Each was refactored to remove all ServiceLayer dependencies (`ITaskOperation`, `SqlTask`, `ConnectionInfo`, `TaskExecutionMode`).

### Files Created in `SqlCore/SchemaCompare/`

| File | Key Changes |
|------|-------------|
| `SchemaCompareOperation.cs` | Constructor takes `(SchemaCompareParams, ISchemaCompareConnectionProvider)` instead of `(SchemaCompareParams, ConnectionInfo, ConnectionInfo)`. Implements `IDisposable` instead of `ITaskOperation`. `Execute()` takes no parameters. |
| `SchemaCompareGenerateScriptOperation.cs` | Uses `ISchemaCompareScriptHandler` for script delivery instead of `SqlTask.AddScript()`. Constructor accepts optional `ISchemaCompareScriptHandler`. |
| `SchemaComparePublishChangesOperation.cs` | Abstract base class. `Execute()` takes no parameters. Implements `IDisposable`. |
| `SchemaComparePublishDatabaseChangesOperation.cs` | Extends `SchemaComparePublishChangesOperation`. Accepts `SchemaComparisonResult` directly in constructor. |
| `SchemaComparePublishProjectChangesOperation.cs` | Same pattern as database changes. |
| `SchemaCompareOpenScmpOperation.cs` | Constructor takes `(SchemaCompareOpenScmpParams, ISchemaCompareConnectionProvider)`. Uses `ISchemaCompareConnectionProvider.ParseConnectionString()` instead of `ConnectionService`. |
| `SchemaCompareSaveScmpOperation.cs` | Constructor takes `(SchemaCompareSaveScmpParams, ISchemaCompareConnectionProvider)`. Uses interface for endpoint creation. |
| `SchemaCompareIncludeExcludeNodeOperation.cs` | Accepts `SchemaComparisonResult` directly. Implements `IDisposable`. |
| `SchemaCompareIncludeExcludeAllNodesOperation.cs` | Same pattern as single-node. |
| `SchemaCompareUtils.cs` | `CreateSchemaCompareEndpoint()` takes `ISchemaCompareConnectionProvider` instead of `ConnectionInfo`. Uses `AccessTokenProvider` from `SqlCore.Utility`. Uses a static compiled `Regex` instead of `[GeneratedRegex]` (SqlCore uses LangVersion 9.0). |

### SR Strings Added to SqlCore

Two localized strings were added to `SqlCore/Localization/` (`sr.strings`, `sr.cs`, `sr.resx`):
- `SchemaCompareExcludeIncludeNodeNotFound` — Error when a specified change can't be found in the model
- `OpenScmpConnectionBasedModelParsingError` — Error parsing SCMP connection information

---

## Step 2 — Delete Original ServiceLayer Operation Files

The following 10 files were deleted from `ServiceLayer/SchemaCompare/`:

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

---

## Step 3 — Create VSCode/ADS Adapter Implementations

Three new adapter files were created in `ServiceLayer/SchemaCompare/` to bridge the ServiceLayer infrastructure to the SqlCore interfaces:

### `VsCodeConnectionProvider.cs`
Implements `ISchemaCompareConnectionProvider`. Bridges `ConnectionService` to the host-agnostic interface:
- `GetConnectionString()` — Uses `ConnectionService.TryFindConnection()` + `BuildConnectionString()` to resolve connection strings from `OwnerUri`
- `GetAccessToken()` — Returns Azure MFA token from `ConnectionInfo` when `AuthenticationType == AzureMFA`
- `ParseConnectionString()` — Delegates to `ConnectionService.ParseConnectionString()` and maps to `SchemaCompareEndpointInfo`

### `VsCodeScriptHandler.cs`
Implements `ISchemaCompareScriptHandler`. Bridges script delivery to `SqlTask`:
- `OnScriptGenerated()` → `SqlTask.AddScript(SqlTaskStatus.Succeeded, script)`
- `OnMasterScriptGenerated()` → `SqlTask.AddScript(SqlTaskStatus.Succeeded, masterScript)`
- Accepts a `Func<SqlTask>` to lazily resolve the task (since `SqlTask` is assigned by `SqlTaskManager` after adapter creation)

### `SchemaCompareTaskAdapter.cs`
Implements `ITaskOperation`. Wraps host-agnostic operations for consumption by `SqlTaskManager`:
- Uses delegate-based `Action _execute`, `Action _cancel`, `Func<string> _getError` to avoid coupling to specific operation types
- `Execute(TaskExecutionMode mode)` → delegates to `_execute()` (mode is ignored since SqlCore operations don't use it)
- Exposes `SqlTask` property required by `ITaskOperation` contract

---

## Step 4 — Rewire `SchemaCompareService.cs`

The service was rewritten to create adapters and delegate to SqlCore operations using type aliases:

```csharp
using CoreOps = Microsoft.SqlTools.SqlCore.SchemaCompare;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
```

### Handler Rewiring Pattern

Each handler now follows this pattern:

1. Create a `VsCodeConnectionProvider` from `ConnectionServiceInstance`
2. Map ServiceLayer params → SqlCore params via helper methods
3. Instantiate the SqlCore operation
4. Wrap in `SchemaCompareTaskAdapter` (for task-managed operations)
5. Feed to `SqlTaskManager` as before

### Type Mapping Helpers Added

| Method | Purpose |
|--------|---------|
| `ToCore(SchemaCompareEndpointInfo)` | Maps ServiceLayer endpoint info (with `ConnectionDetails`) → SqlCore endpoint info |
| `ToCoreParams(SchemaCompareParams)` | Maps full params including both endpoints and `DeploymentOptions` |
| `ToCoreSaveScmpParams(SchemaCompareSaveScmpParams)` | Maps save-SCMP params including excluded objects |
| `FromCore(SchemaCompareEndpointInfo)` | Maps SqlCore endpoint info → ServiceLayer format, reconstructing `ConnectionDetails` from `ConnectionString` |
| `FromCoreOpenScmpResult(SchemaCompareOpenScmpResult)` | Maps the full open-SCMP result back to ServiceLayer types |

### Handler Summary

| Handler | How It Delegates |
|---------|-----------------|
| `HandleSchemaCompareRequest` | `SchemaCompareTaskAdapter` wrapping `CoreOps.SchemaCompareOperation` |
| `HandleSchemaCompareGenerateScriptRequest` | `SchemaCompareTaskAdapter` wrapping `CoreOps.SchemaCompareGenerateScriptOperation` + `VsCodeScriptHandler` |
| `HandleSchemaComparePublishDatabaseChangesRequest` | `SchemaCompareTaskAdapter` wrapping `CoreOps.SchemaComparePublishDatabaseChangesOperation` |
| `HandleSchemaComparePublishProjectChangesRequest` | `SchemaCompareTaskAdapter` wrapping `CoreOps.SchemaComparePublishProjectChangesOperation` |
| `HandleSchemaCompareIncludeExcludeNodeRequest` | Direct call to `CoreOps.SchemaCompareIncludeExcludeNodeOperation.Execute()` |
| `HandleSchemaCompareIncludeExcludeAllNodesRequest` | Direct call to `CoreOps.SchemaCompareIncludeExcludeAllNodesOperation.Execute()` |
| `HandleSchemaCompareOpenScmpRequest` | Direct call to `CoreOps.SchemaCompareOpenScmpOperation.Execute()` with `VsCodeConnectionProvider` |
| `HandleSchemaCompareSaveScmpRequest` | Direct call to `CoreOps.SchemaCompareSaveScmpOperation.Execute()` with `VsCodeConnectionProvider` |
| `HandleSchemaCompareCancelRequest` | Unchanged — uses cancellation action dictionary |

---

## Step 5 — Update Tests

### New Test Infrastructure

- **`TestConnectionProvider.cs`** — Sealed test implementation of `ISchemaCompareConnectionProvider` that wraps source and target `ConnectionInfo` directly for integration tests. Determines which `ConnectionInfo` to use based on `OwnerUri` matching.

### Updated Test Files

| File | Changes |
|------|---------|
| `SchemaCompareServiceTests.cs` | Added `CoreOps`/`CoreContracts` using aliases. Updated ~22 operation constructors to use `CoreOps.` namespace with `TestConnectionProvider`. Replaced ~43 `Execute(TaskExecutionMode)` calls with parameterless `Execute()`. Qualified `SchemaCompareUtils.CreateDiffEntry` with `CoreOps.`. Added `ToCoreParams`, `ToCoreEndpoint`, `ToCoreSaveScmpParams` helpers. |
| `SchemaCompareServiceOptionsTests.cs` | Same pattern: added usings, updated 8 operation constructors, removed `TaskExecutionMode` args, added conversion helpers. |
| `SchemaCompareTests.cs` (unit tests) | Updated `using` from `Microsoft.SqlTools.ServiceLayer.SchemaCompare` → `Microsoft.SqlTools.SqlCore.SchemaCompare` for `SchemaCompareUtils`. |

---

## File Change Summary

| Category | Count | Action |
|----------|-------|--------|
| SqlCore operations (new) | 10 | Operations + Utils created as host-agnostic |
| SqlCore localization | 3 | SR strings, resources, generated code updated |
| ServiceLayer operations (deleted) | 10 | Original operations removed |
| ServiceLayer adapters (new) | 3 | `VsCodeConnectionProvider`, `VsCodeScriptHandler`, `SchemaCompareTaskAdapter` |
| ServiceLayer service (modified) | 1 | `SchemaCompareService.cs` fully rewired |
| Test infrastructure (new) | 1 | `TestConnectionProvider.cs` |
| Test files (modified) | 3 | `SchemaCompareServiceTests.cs`, `SchemaCompareServiceOptionsTests.cs`, `SchemaCompareTests.cs` |

---

## Validation

- ✅ SqlCore builds successfully (net8.0 and net472)
- ✅ ServiceLayer builds successfully
- ✅ Unit tests build and pass (6/6 schema compare tests)
- ✅ Integration tests build successfully
- ✅ Code review — no issues
- ✅ CodeQL security scan — no alerts

---

## Architecture After Phase 3

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
│  Adapters:                                   │
│    ├─ VsCodeConnectionProvider               │
│    ├─ VsCodeScriptHandler                    │
│    └─ SchemaCompareTaskAdapter               │
│                                             │
│  SchemaCompare/Contracts/                    │
│    └─ (wire-format types + TaskExecutionMode)│
└─────────────────────────────────────────────┘
```
