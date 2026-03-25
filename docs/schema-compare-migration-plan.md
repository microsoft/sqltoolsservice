# Schema Compare Refactoring — Step-by-Step Migration Plan

## Overview

This document describes a phased migration plan for merging the Schema Compare refactoring from the `bruzel/schema-compare` branch into `main`. The refactoring extracts host-agnostic business logic from `Microsoft.SqlTools.ServiceLayer` into `Microsoft.SqlTools.SqlCore`, introducing dependency-injection interfaces that allow different hosts (VSCode/ADS, SSMS) to plug in their own connection and script-handling implementations.

Each step is designed to be a **self-contained, compilable, and testable unit** that can be reviewed and merged independently.

> **Guiding principle for the first steps:** New interfaces and contracts are introduced in the **ServiceLayer project** first, keeping changes minimal and avoiding cross-project moves. Moving files to `SqlCore` is deferred to later steps.

---

## Step 1 — Multi-Framework Target & Conditional Compilation Fixes

**Goal:** Enable .NET Framework 4.7.2 support across projects and fix conditional compilation directives.

**Changes:**

1. **`Microsoft.SqlTools.Hosting.csproj`** — Add `net472` to `TargetFrameworks` (becomes `netstandard2.0;net8.0;net472`).
2. **`Microsoft.SqlTools.Connectors.VSCode.csproj`** — Add `net472` to `TargetFrameworks` (becomes `net472;net8.0`).
3. **`Microsoft.SqlTools.SqlCore.csproj`** — Add `net472` to `TargetFrameworks` (becomes `net6.0;net472`).
4. **`ProtocolEndpoint.cs`** — Change `#if NETSTANDARD2_0` → `#if !NET6_0_OR_GREATER` for `WaitAsync` guard.
5. **`Logger.cs`** — Swap conditional: prefer `Environment.ProcessId` under `#if NET6_0_OR_GREATER`, fall back to `Process.GetCurrentProcess().Id`.
6. **`SqlClientEventListener.cs`** — Update conditionals for `net472` support and `OSThreadId` availability.
7. **`KernelJsonSchemaBuilder.cs`** — Guard `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` attributes with `#if NET6_0_OR_GREATER`.

**Validation:** Solution compiles for all target frameworks. Existing tests pass.

---

## Step 2 — Move `DeploymentOptions` to `DacFx.Contracts` Namespace in SqlCore

**Goal:** Centralize deployment option types in `SqlCore` so they can be shared by DacFx, Schema Compare, and SqlPackage features.

**Changes:**

1. **`DeploymentOptions.cs`** (already in SqlCore) — Change namespace from `Microsoft.SqlTools.ServiceLayer.DacFx.Contracts` to `Microsoft.SqlTools.SqlCore.DacFx.Contracts`. This includes `DeploymentOptions`, `DeploymentOptionProperty<T>`, and `DeploymentScenario` types.
2. **`DacFxUtils.cs`** (SqlCore) — Update `using` to reference `Microsoft.SqlTools.SqlCore.DacFx.Contracts`.
3. **ServiceLayer files** — Add `using Microsoft.SqlTools.SqlCore.DacFx.Contracts;` to all files that reference `DeploymentOptions`:
   - `DacFx/Contracts/DeployRequest.cs`
   - `DacFx/Contracts/GenerateDeployScriptRequest.cs`
   - `DacFx/Contracts/GetDeploymentOptionsRequest.cs`
   - `DacFx/Contracts/GetOptionsFromProfileRequest.cs`
   - `DacFx/Contracts/SavePublishProfileRequest.cs`
   - `DacFx/DacFxService.cs`
   - `DacFx/DeployOperation.cs`
   - `DacFx/GenerateDeployScriptOperation.cs`
   - `SqlPackage/Contracts/GenerateSqlPackageCommandRequest.cs`
   - `SqlPackage/SqlPackageService.cs`
4. **Test files** — Update `using` statements:
   - `DacFxServiceTests.cs`
   - `SqlPackageServiceTests.cs`

**Validation:** Solution compiles. DacFx and SqlPackage tests pass (options are read/written correctly).

---

## Step 3 — Add `NormalizePublishDefaults` to `DeploymentOptions` and Update `SqlPackageService`

**Goal:** Add the method that resets STS-specific deployment overrides back to DacFx native defaults for external tool integration.

**Changes:**

1. **`DeploymentOptions.cs`** (SqlCore) — Add `NormalizePublishDefaults()` method that resets the 7 SSMS-matching overrides (e.g., `DropObjectsNotInSource`, `IgnorePermissions`, etc.) to their DacFx-native values.
2. **`SqlPackageService.cs`** — Add `normalizeDefaults` parameter to `ApplyDeployOptions()` and call `NormalizePublishDefaults()` for Publish and Script actions (not for DeployReport).

**Validation:** SqlPackage unit tests pass. Deployment options normalization is verified.

---

## Step 4 — Introduce Schema Compare Interfaces (in ServiceLayer)

**Goal:** Define the two core abstractions that decouple Schema Compare operations from host-specific infrastructure. **These are placed in ServiceLayer for now** (not SqlCore).

**Changes:**

1. **Create `ISchemaCompareConnectionProvider.cs`** in `ServiceLayer/SchemaCompare/`:
   ```
   namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
   {
       public interface ISchemaCompareConnectionProvider
       {
           string GetConnectionString(SchemaCompareEndpointInfo endpointInfo);
           string GetAccessToken(SchemaCompareEndpointInfo endpointInfo);
           SchemaCompareEndpointInfo ParseConnectionString(string connectionString);
       }
   }
   ```

2. **Create `ISchemaCompareScriptHandler.cs`** in `ServiceLayer/SchemaCompare/`:
   ```
   namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
   {
       public interface ISchemaCompareScriptHandler
       {
           void OnScriptGenerated(string script);
           void OnMasterScriptGenerated(string masterScript);
       }
   }
   ```

3. **Create `AccessTokenProvider.cs`** in `ServiceLayer/SchemaCompare/`:
   - Internal class implementing `IUniversalAuthProvider` from DacFx.
   - Simple token holder used when connecting to Azure SQL with MFA.

**Validation:** Solution compiles. No behavior changes — interfaces are defined but not yet consumed.

---

## Step 5 — Introduce Host-Agnostic Schema Compare Contracts (in ServiceLayer)

**Goal:** Add the parameter and result types that Schema Compare operations will use, independent of the JSON-RPC request/response layer.

**Changes:**

1. **Create `SchemaCompare/Contracts/SchemaCompareParams.cs`** in ServiceLayer — Core parameter classes:
   - `SchemaCompareParams` (operation ID, source/target endpoints, deployment options)
   - `SchemaComparePublishDatabaseChangesParams`
   - `SchemaCompareGenerateScriptParams`
   - `SchemaComparePublishProjectChangesParams`
   - `SchemaCompareNodeParams`
   - `SchemaCompareIncludeExcludeAllNodesParams`
   - `SchemaCompareOpenScmpParams`
   - `SchemaCompareSaveScmpParams`
   - `SchemaCompareGetOptionsParams`
   - `SchemaCompareCancelParams`

2. **Create `SchemaCompare/Contracts/SchemaCompareResults.cs`** in ServiceLayer — Result types:
   - `SchemaCompareResult`
   - `SchemaCompareOpenScmpResult`
   - `SchemaCompareIncludeExcludeResult`
   - `SchemaCompareIncludeExcludeAllNodesResult`
   - `SchemaCompareOptionsResult`
   - `SchemaCompareScriptResult`

3. **Create `SchemaCompare/Contracts/SchemaCompareContracts.cs`** in ServiceLayer — Shared domain types:
   - `SchemaCompareEndpointType` enum
   - `SchemaCompareEndpointInfo` class
   - `SchemaCompareObjectId` class
   - `DiffEntry` class
   - `SchemaCompareResultBase` class

4. **Update existing ServiceLayer contract files** — existing request/response types inherit from or reference the new base types instead of defining their own copies. The existing request types in files like `SchemaCompareRequest.cs` extend the new core parameter types by adding `TaskExecutionMode` and other host-specific properties.

**Validation:** Solution compiles. Existing Schema Compare request types use the new base contracts. No runtime behavior change.

---

## Step 6 — Add `SchemaCompareOptionsRequest` Contract

**Goal:** Add the missing `schemaCompare/getDefaultOptions` endpoint contract.

**Changes:**

1. **Create `SchemaCompare/Contracts/SchemaCompareOptionsRequest.cs`** in ServiceLayer:
   - `SchemaCompareGetOptionsParams` (empty parameter class)
   - `SchemaCompareOptionsResult` (returns `DefaultDeploymentOptions`)
   - `SchemaCompareGetDefaultOptionsRequest` (RequestType definition for `"schemaCompare/getDefaultOptions"`)

2. **Update `SchemaCompareService.cs`** — Register the new request handler that returns `DeploymentOptions.GetDefaultSchemaCompareOptions()`.

**Validation:** Solution compiles. The new endpoint can be called and returns default deployment options.

---

## Step 7 — Refactor `SchemaCompareOperation` to Use `ISchemaCompareConnectionProvider`

**Goal:** The core comparison operation no longer depends on `ConnectionInfo` directly; instead it receives an `ISchemaCompareConnectionProvider`.

**Changes:**

1. **`SchemaCompareOperation.cs`** — Change constructor:
   - Remove `ConnectionInfo sourceConnInfo, ConnectionInfo targetConnInfo` parameters.
   - Add `ISchemaCompareConnectionProvider connectionProvider` parameter.
   - Replace `ITaskOperation` with `IDisposable`.
   - Change `Execute(TaskExecutionMode mode)` → `Execute()`.
   - Remove `SqlTask` property.

2. **`SchemaCompareUtils.cs`** — Add `CreateSchemaCompareEndpoint()` static method that uses `ISchemaCompareConnectionProvider` to build DacFx endpoints (Database, Dacpac, Project).

**Validation:** Solution compiles. Schema Compare operations work with the provider abstraction.

---

## Step 8 — Refactor Remaining Operations to Use Interfaces

**Goal:** All Schema Compare operations use the new interfaces instead of ServiceLayer types.

**Changes:**

1. **`SchemaCompareGenerateScriptOperation.cs`** — Add `ISchemaCompareScriptHandler` for script delivery; remove `SqlTask` dependency.
2. **`SchemaComparePublishChangesOperation.cs`** (abstract base) — Replace `ITaskOperation` with `IDisposable`; simplify `Execute()`.
3. **`SchemaComparePublishDatabaseChangesOperation.cs`** — Accept `SchemaComparisonResult` directly in constructor.
4. **`SchemaComparePublishProjectChangesOperation.cs`** — Same pattern.
5. **`SchemaCompareOpenScmpOperation.cs`** — Add `ISchemaCompareConnectionProvider` for parsing SCMP connection strings.
6. **`SchemaCompareSaveScmpOperation.cs`** — Add `ISchemaCompareConnectionProvider` for creating endpoints.
7. **`SchemaCompareIncludeExcludeNodeOperation.cs`** — Accept `SchemaComparisonResult` directly; remove task infrastructure.
8. **`SchemaCompareIncludeExcludeAllNodesOperation.cs`** — Same pattern.

**Validation:** Solution compiles. All operations use the new interfaces.

---

## Step 9 — Create VSCode/ADS Adapter Implementations

**Goal:** Implement the host-specific adapters that bridge SqlCore-style operations to the ServiceLayer infrastructure.

**Changes:**

1. **Create `VsCodeConnectionProvider.cs`** in `ServiceLayer/SchemaCompare/`:
   - Implements `ISchemaCompareConnectionProvider`.
   - Delegates to `ConnectionService` for connection string lookups and Azure MFA token retrieval.
   - Implements `ParseConnectionString()` to reconstruct `SchemaCompareEndpointInfo` from SCMP connection strings.

2. **Create `VsCodeScriptHandler.cs`** in `ServiceLayer/SchemaCompare/`:
   - Implements `ISchemaCompareScriptHandler`.
   - Uses a `Func<SqlTask>` delegate for deferred `SqlTask` lookup.
   - Feeds generated scripts to `SqlTask.AddScript()`.

3. **Create `SchemaCompareTaskAdapter.cs`** in `ServiceLayer/SchemaCompare/`:
   - Implements `ITaskOperation`.
   - Wraps execute/cancel/error delegates.
   - Enables `SqlTaskManager` to manage host-agnostic operations.

**Validation:** Solution compiles. Adapter implementations are available for the service to use.

---

## Step 10 — Rewire `SchemaCompareService` to Use Adapters

**Goal:** The service creates adapters and delegates to refactored operations.

**Changes:**

1. **`SchemaCompareService.cs`** — Rewrite request handlers to:
   - Create `VsCodeConnectionProvider` for connection resolution.
   - Create `VsCodeScriptHandler` for script delivery.
   - Wrap operations in `SchemaCompareTaskAdapter` for SqlTaskManager integration.
   - Add `ToCore()`/`FromCore()` mapping methods for endpoint info conversion.
   - Add `ToCoreParams()` for parameter mapping.

**Validation:** Solution compiles. Schema Compare integration tests pass end-to-end.

---

## Step 11 — Update Tests

**Goal:** Tests use the new interfaces and verify the refactored behavior.

**Changes:**

1. **Create `TestConnectionProvider.cs`** in integration tests — Test implementation of `ISchemaCompareConnectionProvider`.
2. **Update `SchemaCompareServiceTests.cs`** — Use `TestConnectionProvider` instead of raw `ConnectionInfo`.
3. **Update `SchemaCompareServiceOptionsTests.cs`** — Use new contracts and test the default options endpoint.
4. **Update `SchemaCompareTestUtils.cs`** — Updated utility methods.
5. **Update `DacFxServiceTests.cs`** — Updated namespace references for `DeploymentOptions`.
6. **Update `SchemaCompareTests.cs`** (unit tests) — Minor namespace updates.

**Validation:** All Schema Compare tests pass (both unit and integration).

---

## Step 12 — Fix SmoTreeNodes Factory Mapping (Independent Bugfix)

**Goal:** Correct the factory-to-node mapping for External Tables vs Dropped Ledger Tables in Object Explorer.

**Changes:**

1. **`SmoTreeNodes.cs`** — Swap the filter properties and tree node types:
   - `ExternalTablesChildFactory` → uses `IsExternal` filter, creates `ExternalTableTreeNode`
   - `DroppedLedgerTablesChildFactory` → uses `IsDroppedLedgerTable` filter, creates `TableTreeNode`

**Validation:** Object Explorer correctly categorizes external and dropped ledger tables.

---

## Future Steps (After Initial Merge)

These steps move files from `ServiceLayer` to `SqlCore`:

### Step F1 — Move Interfaces to SqlCore
Move `ISchemaCompareConnectionProvider`, `ISchemaCompareScriptHandler`, and `AccessTokenProvider` from `ServiceLayer` to `SqlCore`.

### Step F2 — Move Contracts to SqlCore
Move `SchemaCompareContracts.cs`, `SchemaCompareParams.cs`, and `SchemaCompareResults.cs` from `ServiceLayer` to `SqlCore`.

### Step F3 — Move Operations to SqlCore
Move all `SchemaCompare*Operation.cs` files and `SchemaCompareUtils.cs` from `ServiceLayer` to `SqlCore`.

### Step F4 — Remove ServiceLayer Operation Files
Delete the original ServiceLayer copies and update all references to use the SqlCore namespace.

---

## Dependency Graph

```
Step 1 (Framework support)
    │
    ▼
Step 2 (DeploymentOptions namespace move)
    │
    ▼
Step 3 (NormalizePublishDefaults)
    │
    ├──────────────────────────────┐
    ▼                              ▼
Step 4 (Interfaces)          Step 12 (SmoTreeNodes fix)
    │
    ▼
Step 5 (Contracts)
    │
    ▼
Step 6 (Options endpoint)
    │
    ▼
Step 7 (SchemaCompareOperation refactor)
    │
    ▼
Step 8 (All operations refactored)
    │
    ▼
Step 9 (VSCode adapters)
    │
    ▼
Step 10 (Service rewiring)
    │
    ▼
Step 11 (Test updates)
    │
    ▼
Future: F1 → F2 → F3 → F4 (Move to SqlCore)
```

## Summary

| Step | Description | Files Changed | Risk |
|------|-------------|---------------|------|
| 1 | Multi-framework + conditional compilation | ~6 | Low |
| 2 | DeploymentOptions namespace move | ~14 | Low |
| 3 | NormalizePublishDefaults | 2 | Low |
| 4 | New interfaces (ServiceLayer) | 3 new | None |
| 5 | New contracts (ServiceLayer) | 3 new + ~8 updated | Medium |
| 6 | Options endpoint | 1 new + 1 updated | Low |
| 7 | SchemaCompareOperation refactor | 2 | Medium |
| 8 | All operations refactored | 8 | Medium |
| 9 | VSCode adapters | 3 new | Low |
| 10 | Service rewiring | 1 | High |
| 11 | Test updates | 6 | Medium |
| 12 | SmoTreeNodes fix | 1 | Low |
