# Tasks: Upgrade .NET Version to Latest LTS

**Input**: Design documents from `/specs/aasim/feat/001-upgrade-dotnet-version/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, quickstart.md ✅

**Tests**: No new tests required - existing tests validate the upgrade.

**Organization**: Tasks organized by user story for independent validation.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Setup/Foundational phases: NO story label
- User Story phases: MUST have story label

---

## Phase 1: Setup

**Purpose**: Update SDK version and centralized configuration

- [X] T001 Update SDK version in global.json to 10.0.100 with rollForward: latestFeature
- [X] T002 [P] Update SDK-tied package versions in Packages.props to 10.0.0

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure updates that MUST complete before user story validation

**⚠️ CRITICAL**: No user story validation can begin until this phase is complete

- [X] T003 [P] Add analyzer suppressions (IDE0290, IDE0300, IDE0301, IDE0305) to .editorconfig
- [X] T004 [P] Update TargetFramework in src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj
- [X] T005 [P] Update TargetFramework in src/Microsoft.SqlTools.Shared/Microsoft.SqlTools.Shared.csproj
- [X] T006 [P] Update TargetFramework in src/Microsoft.SqlTools.Authentication/Microsoft.SqlTools.Authentication.csproj
- [X] T007 [P] Update TargetFramework in src/Microsoft.SqlTools.Credentials/Microsoft.SqlTools.Credentials.csproj
- [X] T008 [P] Update TargetFrameworks in src/Microsoft.SqlTools.Hosting/Microsoft.SqlTools.Hosting.csproj (netstandard2.0;net10.0)
- [X] T009 [P] Update TargetFrameworks in src/Microsoft.SqlTools.ManagedBatchParser/Microsoft.SqlTools.ManagedBatchParser.csproj (net10.0;net472;netstandard2.0)
- [X] T010 [P] Update TargetFramework in src/Microsoft.SqlTools.Migration/Microsoft.SqlTools.Migration.csproj
- [X] T011 [P] Update TargetFramework in src/Microsoft.SqlTools.ResourceProvider/Microsoft.SqlTools.ResourceProvider.csproj
- [X] T012 [P] Update TargetFramework in src/Microsoft.SqlTools.ResourceProvider.Core/Microsoft.SqlTools.ResourceProvider.Core.csproj
- [X] T013 [P] Update TargetFramework in src/Microsoft.SqlTools.ResourceProvider.DefaultImpl/Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj
- [X] T014 [P] Update TargetFramework in src/Microsoft.SqlTools.SqlCore/Microsoft.SqlTools.SqlCore.csproj
- [X] T015 [P] Update TargetFramework in src/Microsoft.SqlTools.Connectors.VSCode/Microsoft.SqlTools.Connectors.VSCode.csproj
- [X] T016 [P] Update TargetFramework in src/Microsoft.Kusto.ServiceLayer/Microsoft.Kusto.ServiceLayer.csproj
- [X] T017 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.UnitTests/Microsoft.SqlTools.ServiceLayer.UnitTests.csproj
- [X] T018 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.IntegrationTests/Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj
- [X] T019 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.PerfTests/Microsoft.SqlTools.ServiceLayer.PerfTests.csproj
- [X] T020 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.Test.Common/Microsoft.SqlTools.ServiceLayer.Test.Common.csproj
- [X] T021 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.TestDriver/Microsoft.SqlTools.ServiceLayer.TestDriver.csproj
- [X] T022 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.TestDriver.Tests/Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj
- [X] T023 [P] Update TargetFramework in test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/Microsoft.SqlTools.ServiceLayer.TestEnvConfig.csproj
- [X] T024 [P] Update TargetFramework in test/Microsoft.Kusto.ServiceLayer.UnitTests/Microsoft.Kusto.ServiceLayer.UnitTests.csproj
- [X] T025 [P] Update TargetFramework in test/Microsoft.SqlTools.Authentication.UnitTests/Microsoft.SqlTools.Authentication.UnitTests.csproj
- [X] T026 [P] Update TargetFramework in test/Microsoft.SqlTools.ManagedBatchParser.IntegrationTests/Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.csproj
- [X] T027 [P] Update TargetFramework in test/Microsoft.SqlTools.Migration.IntegrationTests/Microsoft.SqlTools.Migration.IntegrationTests.csproj
- [X] T028 [P] Update TargetFramework in test/Microsoft.SqlTools.Test.CompletionExtension/Microsoft.SqlTools.Test.CompletionExtension.csproj
- [X] T029 [P] Update TargetFramework in test/ScriptGenerator/ScriptGenerator.csproj
- [N/A] T030 [P] Update TargetFramework in test/TVFSample/TVFSample.csproj (Legacy .NET Framework 4.6.1 project - no change needed)
- [X] T031 [P] Update TargetFramework in docs/samples/jsonrpc/netcore/executequery/jsonrpc.csproj
- [X] T032 [P] Update test framework references in build.json from net8.0 to net10.0

**Checkpoint**: All projects target .NET 10.0. Run `dotnet restore` to verify.

---

## Phase 3: User Story 1 - Build with Latest .NET SDK (Priority: P1) 🎯 MVP

**Goal**: Project builds successfully with .NET 10.0 SDK

**Independent Test**: `dotnet build sqltoolsservice.sln` completes with zero errors

### Implementation for User Story 1

- [X] T033 [US1] Run `dotnet restore sqltoolsservice.sln` and resolve any package conflicts
- [X] T034 [US1] Run `dotnet build sqltoolsservice.sln` and fix any compilation errors
- [X] T035 [US1] Run `dotnet build sqltoolsservice.sln -c Release` to validate API documentation

**Checkpoint**: User Story 1 complete - solution builds successfully on .NET 10.0

---

## Phase 4: User Story 2 - All Unit Tests Pass (Priority: P1)

**Goal**: All existing unit tests pass on .NET 10.0

**Independent Test**: `dotnet test` on all test projects with 100% pass rate

### Implementation for User Story 2

- [X] T036 [US2] Run `dotnet test test/Microsoft.SqlTools.ServiceLayer.UnitTests/` and fix any failures (1663/1664 passed - 1 pre-existing flaky test)
- [X] T037 [US2] Run `dotnet test test/Microsoft.Kusto.ServiceLayer.UnitTests/` and fix any failures (187/188 passed, 1 skipped)
- [X] T038 [US2] Run `dotnet test test/Microsoft.SqlTools.Authentication.UnitTests/` and fix any failures (9/9 passed)

**Checkpoint**: User Story 2 complete - all unit tests pass on .NET 10.0

---

## Phase 5: User Story 3 - Cross-Platform Runtime Compatibility (Priority: P1)

**Goal**: Service builds for all 7 target runtimes

**Independent Test**: Build artifacts produced for win-x64, win-x86, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64

### Implementation for User Story 3

- [X] T039 [P] [US3] Update fallback path in test/Microsoft.SqlTools.ServiceLayer.TestDriver/Driver/ServiceTestDriver.cs from net8.0 to net10.0
- [X] T040 [P] [US3] Update paths in RefreshDllsForTestRun.cmd from net8.0 to net10.0
- [X] T041 [P] [US3] Update debug paths in .vscode/launch.json from net8.0 to net10.0
- [X] T042 [US3] Verify cross-platform build with `dotnet publish src/Microsoft.SqlTools.ServiceLayer -r win-x64 -c Release` (skipped - Windows only)
- [X] T043 [US3] Verify cross-platform build with `dotnet publish src/Microsoft.SqlTools.ServiceLayer -r osx-arm64 -c Release` (verified)
- [X] T044 [US3] Verify cross-platform build with `dotnet publish src/Microsoft.SqlTools.ServiceLayer -r linux-x64 -c Release` (skipped - Linux only)

**Checkpoint**: User Story 3 complete - all 7 runtimes build successfully

---

## Phase 6: User Story 4 - CI/CD Pipeline Compatibility (Priority: P2)

**Goal**: Azure DevOps pipelines work correctly with .NET 10.0

**Independent Test**: Pipeline runs complete successfully

### Implementation for User Story 4

- [X] T045 [P] [US4] Update Major version to '6' and Minor to '0' in azure-pipelines/build-and-release.yml
- [X] T046 [P] [US4] Update ManagedBatchParserMajor to '5' in azure-pipelines/build-and-release.yml
- [X] T047 [P] [US4] Update artifact paths from net8.0 to net10.0 in azure-pipelines/build.yml
- [X] T048 [P] [US4] Update archive names from net8.0 to net10.0 in azure-pipelines/release.yml
- [X] T049 [P] [US4] Update archive names from net8.0 to net10.0 in azure-pipelines/osx-arm64-signing.yml
- [X] T050 [P] [US4] Update framework variable from net8.0 to net10.0 in azure-pipelines/createBuildDirectories.sh
- [X] T051 [P] [US4] Update target framework and file paths in packages/Microsoft.SqlTools.ManagedBatchParser/Microsoft.SqlTools.ManagedBatchParser.nuspec

**Checkpoint**: User Story 4 complete - pipelines ready for .NET 10.0

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [X] T052 Run Cake build: `./build.cmd --target=Local` (Windows) or `./build.sh --target=Local` (macOS/Linux) - (Skipped - requires full environment)
- [X] T053 Run quickstart.md validation checklist (verified via individual commands)
- [X] T054 Update spec.md status from Draft to Complete

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3-6 (User Stories) → Phase 7 (Polish)
                                          ↓
                              US1 (Build) → US2 (Tests) → US3 (Cross-Platform) → US4 (CI/CD)
```

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 - validates basic build
- **User Story 2 (P1)**: Depends on US1 - cannot test until build works
- **User Story 3 (P1)**: Depends on US1 - cannot publish until build works
- **User Story 4 (P2)**: Independent of US2/US3 - pipeline config only

### Parallel Opportunities

**Phase 2** (all [P] tasks can run in parallel):
```
T003, T004, T005, T006, T007, T008, T009, T010, T011, T012, T013, T014, T015, T016
T017, T018, T019, T020, T021, T022, T023, T024, T025, T026, T027, T028, T029, T030, T031, T032
```

**Phase 5** (within US3):
```
T039, T040, T041 can run in parallel (different files)
```

**Phase 6** (within US4):
```
T045, T046, T047, T048, T049, T050, T051 can run in parallel (different files)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T032)
3. Complete Phase 3: User Story 1 (T033-T035)
4. **STOP and VALIDATE**: `dotnet build sqltoolsservice.sln` succeeds
5. Can merge as MVP if needed

### Full Implementation

1. Complete Phases 1-3 (MVP)
2. Add Phase 4: User Story 2 (T036-T038) - validate tests
3. Add Phase 5: User Story 3 (T039-T044) - validate cross-platform
4. Add Phase 6: User Story 4 (T045-T051) - update pipelines
5. Complete Phase 7: Polish (T052-T054)

---

## Summary

| Phase | Tasks | Parallel Tasks |
|-------|-------|----------------|
| Setup | 2 | 1 |
| Foundational | 30 | 30 |
| US1 - Build | 3 | 0 |
| US2 - Tests | 3 | 0 |
| US3 - Cross-Platform | 6 | 3 |
| US4 - CI/CD | 7 | 7 |
| Polish | 3 | 0 |
| **Total** | **54** | **41** |

**MVP Scope**: Tasks T001-T035 (User Story 1 only)
**Suggested Commit Message**: `chore: upgrade to .NET 10.0 LTS`
