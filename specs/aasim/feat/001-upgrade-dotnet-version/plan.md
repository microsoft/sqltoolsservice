# Implementation Plan: Upgrade .NET Version to Latest LTS

**Branch**: `aasim/feat/001-upgrade-dotnet-version` | **Date**: 2026-01-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/aasim/feat/001-upgrade-dotnet-version/spec.md`

## Summary

Upgrade SQL Tools Service from .NET 8.0 to .NET 10.0 LTS. This involves updating the SDK version in `global.json`, target frameworks in all 29+ project files, SDK-tied package versions, CI/CD pipeline paths, and build scripts. The upgrade follows the established pattern from the .NET 7→8 migration (commit 0a1fed4) and maintains compatibility with all 7 target runtimes.

## Technical Context

**Language/Version**: C# / .NET 10.0 LTS (upgrading from .NET 8.0.416)  
**Primary Dependencies**: Microsoft.Data.SqlClient, Microsoft.SqlServer.SqlManagementObjects, Azure.Identity, Newtonsoft.Json  
**Storage**: N/A (service layer, no direct storage)  
**Testing**: nUnit for unit tests, xUnit for some integration tests  
**Target Platform**: Cross-platform: win-x64, win-x86, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64  
**Project Type**: Single solution with multiple projects (service + libraries + tests)  
**Performance Goals**: N/A (version upgrade, no new performance requirements)  
**Constraints**: Must maintain backward compatibility with existing JSON-RPC contracts  
**Scale/Scope**: 29 source/test projects, 43 files to modify

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. JSON-RPC API-First | ✅ PASS | No API changes; contracts unchanged |
| II. Cross-Platform Compatibility | ✅ PASS | All 7 runtimes maintained |
| III. Unit Testing Required | ✅ PASS | Existing tests run on new framework |
| IV. API Stability & Breaking Changes | ✅ PASS | Major version bump signals platform change (clarified) |
| V. Commit Hygiene & Code Quality | ✅ PASS | Single logical change, squashable |
| VI. Localization Required | ✅ PASS | No new user-facing strings |

**GATE RESULT**: ✅ PASS - Proceeding to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/aasim/feat/001-upgrade-dotnet-version/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (N/A - no new data models)
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (N/A - no new contracts)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
# Existing structure - no new directories created
src/
├── Microsoft.SqlTools.ServiceLayer/     # Main service (TargetFramework update)
├── Microsoft.SqlTools.Hosting/          # Multi-target: netstandard2.0;net10.0
├── Microsoft.SqlTools.ManagedBatchParser/ # Multi-target: net10.0;net472;netstandard2.0
├── Microsoft.SqlTools.*/                # All other source projects
└── ...

test/
├── Microsoft.SqlTools.ServiceLayer.UnitTests/
├── Microsoft.SqlTools.ServiceLayer.IntegrationTests/
└── ...                                  # All test projects

# Configuration files at root
├── global.json                          # SDK version
├── Directory.Build.props                # Shared build properties
├── Packages.props                       # Centralized package versions
├── build.json                           # Build configuration
├── build.cake                           # Cake build script
└── .editorconfig                        # Code style + analyzer suppressions

# Azure DevOps pipelines
azure-pipelines/
├── build-and-release.yml               # Version numbers
├── build.yml                           # Artifact paths
├── release.yml                         # Archive names
├── osx-arm64-signing.yml              # Signing paths
└── createBuildDirectories.sh          # Framework variable
```

**Structure Decision**: Existing repository structure maintained. This is a configuration-only upgrade with no new source files or directories.

## Complexity Tracking

> No constitution violations to justify. This upgrade follows established patterns.

---

## Phase 0: Research

### Research Tasks

1. **Verify .NET 10.0 SDK availability**: Confirm latest stable SDK version for global.json
2. **Package compatibility audit**: Verify SDK-tied packages have 10.0.x versions
3. **Breaking change assessment**: Review .NET 10.0 release notes for breaking changes
4. **Analyzer updates**: Identify new Roslyn analyzers that may require suppressions

### Research Output

See [research.md](research.md) for detailed findings.

---

## Phase 1: Design

### Design Artifacts

Since this is a configuration upgrade with no new code:

- **data-model.md**: N/A - No new data entities
- **contracts/**: N/A - No API changes (JSON-RPC contracts unchanged)
- **quickstart.md**: Implementation guide for the upgrade process - [quickstart.md](quickstart.md)

---

## Constitution Check (Post-Design)

*Re-evaluated after Phase 1 design completion.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. JSON-RPC API-First | ✅ PASS | No API changes; contracts unchanged |
| II. Cross-Platform Compatibility | ✅ PASS | All 7 runtimes maintained per research |
| III. Unit Testing Required | ✅ PASS | Existing tests run on new framework |
| IV. API Stability & Breaking Changes | ✅ PASS | Major version bump signals platform change |
| V. Commit Hygiene & Code Quality | ✅ PASS | Single logical change, squashable |
| VI. Localization Required | ✅ PASS | No new user-facing strings |

**POST-DESIGN GATE RESULT**: ✅ PASS - Ready for Phase 2 (/speckit.tasks)

---

## Generated Artifacts Summary

| Artifact | Status | Path |
|----------|--------|------|
| spec.md | ✅ Complete | [spec.md](spec.md) |
| plan.md | ✅ Complete | [plan.md](plan.md) (this file) |
| research.md | ✅ Complete | [research.md](research.md) |
| data-model.md | ⏭️ Skipped | N/A - no new data models |
| contracts/ | ⏭️ Skipped | N/A - no API changes |
| quickstart.md | ✅ Complete | [quickstart.md](quickstart.md) |
| tasks.md | 🔜 Next | Run `/speckit.tasks` to generate |

---
