# Phase 2: Extract Schema Compare Contracts and Interfaces to SqlCore

## Description

Phase 2 of the Schema Compare refactoring ([Phase 1: PR #2635](https://github.com/microsoft/sqltoolsservice/pull/2635)). Extracts shared type infrastructure into `SqlCore` so Schema Compare operations can be consumed by multiple hosts (VSCode/ADS, SSMS). Full plan: [`schema-compare-migration-plan.md`](../schema-compare-migration-plan.md).

**No behavioral changes.** Operations remain in ServiceLayer and work identically. This is purely structural preparation for Phase 3 (operation migration + adapter pattern).

| Phase | Description | Status |
|-------|-------------|--------|
| **Phase 1** | Multi-Framework Target & Conditional Compilation | ✅ Done ([PR #2635](https://github.com/microsoft/sqltoolsservice/pull/2635)) |
| **Phase 2** | Contracts, Interfaces & DeploymentOptions Foundation | 🔄 **This PR** |
| **Phase 3** | Operation Refactoring, Adapters & Service Rewiring | 📋 Planned |

---

## What moved to SqlCore

- **`DeploymentOptions`** and **`DacFxUtils`** — relocated with namespace change from `ServiceLayer.DacFx` → `SqlCore.DacFx`
- Added `DeploymentScenario` enum to `DeploymentOptions` so `dacfx/getDeploymentOptions` can return scenario-appropriate defaults (SchemaCompare vs Deployment)
- Added `GetDefaultPublishOptions()` factory for publish-specific defaults

## What's new in SqlCore

- **`ISchemaCompareConnectionProvider`** — abstracts connection string/token retrieval (decouples from `ConnectionService`)
- **`ISchemaCompareScriptHandler`** — abstracts script delivery (decouples from `SqlTask`)
- **`AccessTokenProvider`** — DacFx `IUniversalAuthProvider` impl for Azure MFA
- **Contract types** in `SqlCore/SchemaCompare/Contracts/`:
  - `SchemaCompareContracts.cs` — domain types (`DiffEntry`, `SchemaCompareEndpointInfo`, `SchemaCompareEndpointType`)
  - `SchemaCompareParams.cs` — all parameter types
  - `SchemaCompareResults.cs` — all result types

## ServiceLayer contract changes

ServiceLayer contracts now extend SqlCore base types, adding only host-specific fields:

```
SqlCore.SchemaCompareParams              ServiceLayer.SchemaCompareParams
├─ OperationId                           ├─ (inherits all)
├─ SourceEndpointInfo        ◄─extends── ├─ + TaskExecutionMode
├─ TargetEndpointInfo                    └─ SourceEndpointInfo adds ConnectionDetails
└─ DeploymentOptions
```

Types like `SchemaCompareEndpointType`, `DiffEntry`, and `SchemaCompareObjectId` are removed from ServiceLayer (now imported from SqlCore).

## What Phase 2 does NOT do

- ❌ Does not move operations — they stay in `ServiceLayer`
- ❌ Does not change runtime behavior — all handlers work identically
- ❌ Does not add adapters — that's Phase 3
- ❌ Does not modify `SchemaCompareService` handler logic

---

## Architecture: Before → After Phase 2 → After Phase 3

### Before (main branch)

```
┌─────────────────────────────────────────────────┐
│            Microsoft.SqlTools.ServiceLayer       │
│                                                  │
│  SchemaCompareService ──→ Operations             │
│       │                      │                   │
│       │                      ├─ ConnectionInfo   │
│       │                      ├─ SqlTask          │
│       │                      └─ ITaskOperation   │
│                                                  │
│  DacFx/Contracts/DeploymentOptions               │
│  DacFx/DacFxUtils                                │
│  SchemaCompare/Contracts/ (enums, DiffEntry, etc)│
└──────────────────────────────────────────────────┘
                  Everything in one layer
```

### After Phase 2 (this PR) — shared types extracted

```
┌──────────────────────────────────────────────────┐
│             Microsoft.SqlTools.SqlCore            │
│                                                   │
│  DacFx/Contracts/DeploymentOptions  ◄── moved     │
│  DacFx/DacFxUtils                   ◄── moved     │
│  SchemaCompare/                                    │
│    ├─ ISchemaCompareConnectionProvider  ◄── NEW    │
│    ├─ ISchemaCompareScriptHandler       ◄── NEW    │
│    ├─ AccessTokenProvider               ◄── NEW    │
│    └─ Contracts/                        ◄── NEW    │
│        ├─ SchemaCompareContracts.cs                │
│        ├─ SchemaCompareParams.cs                   │
│        └─ SchemaCompareResults.cs                  │
└───────────────────────────┬──────────────────────┘
                            │ references (project ref)
┌───────────────────────────┴──────────────────────┐
│            Microsoft.SqlTools.ServiceLayer        │
│                                                   │
│  SchemaCompareService ──→ Operations              │
│       │                      │                    │
│       │              (unchanged — still use       │
│       │               ConnectionInfo, SqlTask)    │
│                                                   │
│  SchemaCompare/Contracts/                         │
│    └─ extend SqlCore base types                   │
│       (add TaskExecutionMode, ConnectionDetails)  │
└───────────────────────────────────────────────────┘
        Operations still in ServiceLayer (no behavior change)
```

### After Phase 3 (next PR) — operations moved, adapters added

```
┌──────────────────────────────────────────────────┐
│             Microsoft.SqlTools.SqlCore            │
│                                                   │
│  DacFx/Contracts/DeploymentOptions                │
│  DacFx/DacFxUtils                                 │
│  SchemaCompare/                                    │
│    ├─ Interfaces (connection, script)              │
│    ├─ SchemaCompareOperation          ◄── moved    │
│    ├─ SchemaCompareGenerateScriptOp   ◄── moved    │
│    ├─ SchemaComparePublish*Operation  ◄── moved    │
│    ├─ SchemaCompareOpenScmpOperation  ◄── moved    │
│    ├─ SchemaCompareSaveScmpOperation  ◄── moved    │
│    ├─ SchemaCompareInclude*Operation  ◄── moved    │
│    ├─ SchemaCompareUtils              ◄── moved    │
│    └─ Contracts/                                   │
└───────────────────────────┬──────────────────────┘
                            │
┌───────────────────────────┴──────────────────────┐
│            Microsoft.SqlTools.ServiceLayer        │
│                                                   │
│  SchemaCompareService                             │
│    ├─ creates VsCodeConnectionProvider            │
│    ├─ creates VsCodeScriptHandler                 │
│    └─ wraps operations in TaskAdapter             │
│                                                   │
│  Adapters (NEW):                                  │
│    ├─ VsCodeConnectionProvider   ◄── implements   │
│    ├─ VsCodeScriptHandler        ◄── implements   │
│    └─ SchemaCompareTaskAdapter   ◄── ITaskOp wrap │
│                                                   │
│  SchemaCompare/Contracts/ (wire-format types)     │
└───────────────────────────────────────────────────┘
     SSMS can now directly use SqlCore operations
     with its own ISchemaCompareConnectionProvider
```

---

## How ServiceLayer contracts extend SqlCore types

The key design pattern: SqlCore defines **host-agnostic base types**, ServiceLayer adds **host-specific fields** (`TaskExecutionMode`, `ConnectionDetails`):

```
SqlCore.SchemaCompareEndpointInfo              ServiceLayer.SchemaCompareEndpointInfo
┌─────────────────────────────┐               ┌──────────────────────────────────┐
│  EndpointType               │               │  (inherits all SqlCore fields)   │
│  ServerName                 │ ◄──extends──  │  + ConnectionDetails             │
│  DatabaseName               │               │    (VSCode/ADS-specific)         │
│  OwnerUri, PackageFilePath  │               └──────────────────────────────────┘
│  ExtractTarget, etc.        │
└─────────────────────────────┘
```

---

## File change summary (~39 files)

| Category | Count | Nature of change |
|----------|-------|-----------------|
| **SqlCore — moved** | 2 | `DeploymentOptions.cs`, `DacFxUtils.cs` (namespace change) |
| **SqlCore — new** | 7 | 3 interfaces + 3 contract files + migration plan doc |
| **ServiceLayer contracts — updated** | 8 | Extend SqlCore base types, remove duplicated enums/classes |
| **ServiceLayer DacFx/SqlPackage — updated** | 10 | `using` statement updates only |
| **ServiceLayer operations — updated** | 6 | `using` statement updates only |
| **Test files — updated** | 6 | `using` statement updates only |
| **Total** | **~39** | **~1,234 insertions, ~211 deletions** |

> **~75% of the modified ServiceLayer files are `using` statement changes only** — the actual structural work is in the 9 new/moved SqlCore files and 8 updated ServiceLayer contracts.

---

## Reviewer guide

1. **Start with the migration plan** — [`schema-compare-migration-plan.md`](../schema-compare-migration-plan.md) for full context
2. **Review new SqlCore types** (the core of this PR):
   - `SqlCore/SchemaCompare/ISchemaCompareConnectionProvider.cs`
   - `SqlCore/SchemaCompare/ISchemaCompareScriptHandler.cs`
   - `SqlCore/SchemaCompare/Contracts/SchemaCompareContracts.cs`
   - `SqlCore/SchemaCompare/Contracts/SchemaCompareParams.cs`
   - `SqlCore/SchemaCompare/Contracts/SchemaCompareResults.cs`
3. **Review moved files** — `DeploymentOptions.cs` and `DacFxUtils.cs` (namespace changes + `DeploymentScenario` enum addition)
4. **Spot-check ServiceLayer contracts** — `SchemaCompareRequest.cs` is the most significant; others follow the same pattern
5. **Skip `using`-only changes** — ~24 files are purely `using` statement additions
