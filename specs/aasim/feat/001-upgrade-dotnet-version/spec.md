# Feature Specification: Upgrade .NET Version to Latest LTS

**Feature Branch**: `aasim/feat/001-upgrade-dotnet-version`  
**Created**: 2026-01-23  
**Status**: Complete  
**Input**: User description: "Vbump the dotnet version for this project to the latest dotnet"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build with Latest .NET SDK (Priority: P1)

As a developer, I want the project to build successfully using the latest .NET 10.0 LTS SDK so that I can leverage performance improvements, security patches, and new language features.

**Why this priority**: This is the core requirement - without successful builds, no other functionality works.

**Independent Test**: Can be fully tested by running `dotnet build sqltoolsservice.sln` with .NET 10.0 SDK installed and verifying zero build errors.

**Acceptance Scenarios**:

1. **Given** .NET 10.0 SDK is installed, **When** running `dotnet build sqltoolsservice.sln`, **Then** build completes successfully with no errors
2. **Given** .NET 10.0 SDK is installed, **When** running Release configuration build, **Then** all API documentation validation passes
3. **Given** .NET 10.0 SDK is installed, **When** running `build.cmd --target=Local`, **Then** Cake build completes successfully

---

### User Story 2 - All Unit Tests Pass (Priority: P1)

As a developer, I want all existing unit tests to pass on .NET 10.0 so that I can be confident the upgrade doesn't introduce regressions.

**Why this priority**: Tests validate that existing functionality works correctly after the upgrade.

**Independent Test**: Can be fully tested by running `dotnet test` on all test projects and verifying 100% pass rate.

**Acceptance Scenarios**:

1. **Given** project is built on .NET 10.0, **When** running `dotnet test` on Microsoft.SqlTools.ServiceLayer.UnitTests, **Then** all tests pass
2. **Given** project is built on .NET 10.0, **When** running `dotnet test` on Microsoft.Kusto.ServiceLayer.UnitTests, **Then** all tests pass
3. **Given** project is built on .NET 10.0, **When** running integration tests, **Then** all tests pass

---

### User Story 3 - Cross-Platform Runtime Compatibility (Priority: P1)

As a release engineer, I want the service to build and run correctly on all supported platforms so that users on Windows, macOS, and Linux can use the updated service.

**Why this priority**: SQL Tools Service must support all documented target runtimes per the constitution.

**Independent Test**: Can be fully tested by building for each runtime identifier and verifying executables are produced.

**Acceptance Scenarios**:

1. **Given** project targets .NET 10.0, **When** building for `win-x64`, `win-x86`, `win-arm64`, **Then** Windows executables are produced
2. **Given** project targets .NET 10.0, **When** building for `osx-x64`, `osx-arm64`, **Then** macOS executables are produced
3. **Given** project targets .NET 10.0, **When** building for `linux-x64`, `linux-arm64`, **Then** Linux executables are produced

---

### User Story 4 - CI/CD Pipeline Compatibility (Priority: P2)

As a release engineer, I want the Azure DevOps pipelines to work correctly with .NET 10.0 so that automated builds and releases continue to function.

**Why this priority**: Required for production releases but can be tested after local builds work.

**Independent Test**: Can be tested by running the Azure DevOps pipeline and verifying all stages complete successfully.

**Acceptance Scenarios**:

1. **Given** pipeline uses .NET 10.0 SDK, **When** build pipeline runs, **Then** all platform builds succeed
2. **Given** pipeline uses .NET 10.0 SDK, **When** release pipeline runs, **Then** all artifacts are produced correctly
3. **Given** .NET 10.0 archive names, **When** signing pipeline runs, **Then** macOS signing completes successfully

---

### Edge Cases

- What happens when a dependency doesn't support .NET 10.0? → Proceed with upgrade; keep incompatible packages on their current version with `RollForward` or multi-targeting workarounds
- How does system handle mixed .NET versions in multi-targeting projects? → Projects targeting `netstandard2.0` and `net472` must continue to work alongside `net10.0`
- What if new Roslyn analyzers introduce warnings? → Add appropriate suppressions in `.editorconfig`

## Clarifications

### Session 2026-01-23

- Q: If a critical dependency doesn't have a .NET 10.0 compatible version, what should be the approach? → A: Proceed with upgrade; keep incompatible packages on current version with RollForward or multi-targeting workarounds
- Q: What versioning strategy for SQL Tools Service release when upgrading .NET? → A: Bump major version to signal .NET platform change (e.g., 5.x → 6.0.0)
- Q: Should the upgrade drop support for any legacy runtimes? → A: Keep all 7 runtimes (win-x64, win-x86, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64)
- Q: Should SDK version be exact or flexible in global.json? → A: Use flexible rollForward: latestFeature (spec is reusable)
- Q: Should the spec be parameterized for future .NET upgrades? → A: Keep concrete versions; duplicate spec for future upgrades with find-replace

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST update `global.json` SDK version from `8.0.416` to latest .NET 10.0 SDK version
- **FR-002**: System MUST update `TargetFramework` in all `.csproj` files from `net8.0` to `net10.0`
- **FR-003**: System MUST update multi-targeting projects (`TargetFrameworks`) to include `net10.0` instead of `net8.0`
- **FR-004**: System MUST update `Packages.props` to use .NET 10.0 compatible package versions for SDK-tied packages:
  - `Microsoft.Extensions.DependencyModel`
  - `Microsoft.Extensions.FileSystemGlobbing`
  - `System.Composition`
  - `System.Configuration.ConfigurationManager`
  - `System.IO.Packaging`
  - `System.Security.Permissions`
  - `System.Text.Encoding.CodePages`
  - `System.Text.Encodings.Web`
- **FR-005**: System MUST update `build.json` test framework references from `net8.0` to `net10.0`
- **FR-006**: System MUST update Azure DevOps pipeline files to reference `net10.0` in artifact paths and archive names
- **FR-007**: System MUST update `.vscode/launch.json` debug paths from `net8.0` to `net10.0`
- **FR-008**: System MUST update `RefreshDllsForTestRun.cmd` paths from `net8.0` to `net10.0`
- **FR-009**: System MUST update `createBuildDirectories.sh` framework variable from `net8.0` to `net10.0`
- **FR-010**: System MUST update `Microsoft.SqlTools.ManagedBatchParser.nuspec` target framework and file paths
- **FR-011**: System MUST update `ServiceTestDriver.cs` fallback path from `net8.0` to `net10.0`
- **FR-012**: System MUST update sample project `docs/samples/jsonrpc/netcore/executequery/jsonrpc.csproj`
- **FR-013**: System MUST update package versions for dotnet tools (e.g., `PackageVersion` in service layer projects)
- **FR-014**: System MUST update `azure-pipelines/build-and-release.yml` major version numbers appropriately
- **FR-015**: System MUST add any new Roslyn analyzer suppressions to `.editorconfig` if needed for .NET 10.0

### Key Entities

- **global.json**: Controls SDK version for the entire solution
- **Directory.Build.props**: Contains shared build properties including target runtimes
- **Packages.props**: Centralized package version management
- **\*.csproj files**: Individual project target frameworks (29 projects total)
- **Azure Pipeline YAML files**: CI/CD configuration with framework-specific paths
- **NuGet nuspec files**: Package specifications with framework dependencies

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 29 project files successfully build with .NET 10.0 SDK
- **SC-002**: All unit tests pass with 100% success rate (same as before upgrade)
- **SC-003**: Build artifacts are produced for all 7 target runtimes: `win-x64`, `win-x86`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`
- **SC-004**: No new build warnings introduced (excluding intentional analyzer suppressions)
- **SC-005**: Cake build script (`build.cmd --target=Local`) completes successfully
- **SC-006**: NuGet package (`Microsoft.SqlTools.ManagedBatchParser`) can be produced targeting net10.0
- **SC-007**: CI pipeline completes successfully on Azure DevOps

## Assumptions

- .NET 10.0 is the current LTS version (as of January 2026)
- All current NuGet dependencies have .NET 10.0 compatible versions available
- No breaking API changes in .NET 10.0 affect the codebase
- The `netstandard2.0` and `net472` targets in multi-targeting projects remain unchanged

## Files to Modify

Based on the reference commit (0a1fed4), the following files require updates:

### Configuration Files
1. `global.json` - SDK version
2. `Directory.Build.props` - Target runtimes (if needed)
3. `Packages.props` - Package versions for SDK-tied packages
4. `.editorconfig` - New analyzer suppressions (if needed)
5. `build.json` - Test project framework references

### Source Projects (src/)
6. `src/Microsoft.Kusto.ServiceLayer/Microsoft.Kusto.ServiceLayer.csproj`
7. `src/Microsoft.SqlTools.Authentication/Microsoft.SqlTools.Authentication.csproj`
8. `src/Microsoft.SqlTools.Credentials/Microsoft.SqlTools.Credentials.csproj`
9. `src/Microsoft.SqlTools.Hosting/Microsoft.SqlTools.Hosting.csproj`
10. `src/Microsoft.SqlTools.ManagedBatchParser/Microsoft.SqlTools.ManagedBatchParser.csproj`
11. `src/Microsoft.SqlTools.Migration/Microsoft.SqlTools.Migration.csproj`
12. `src/Microsoft.SqlTools.ResourceProvider/Microsoft.SqlTools.ResourceProvider.csproj`
13. `src/Microsoft.SqlTools.ResourceProvider.Core/Microsoft.SqlTools.ResourceProvider.Core.csproj`
14. `src/Microsoft.SqlTools.ResourceProvider.DefaultImpl/Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj`
15. `src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj`
16. `src/Microsoft.SqlTools.Shared/Microsoft.SqlTools.Shared.csproj`
17. `src/Microsoft.SqlTools.SqlCore/Microsoft.SqlTools.SqlCore.csproj`
18. `src/Microsoft.SqlTools.Connectors.VSCode/Microsoft.SqlTools.Connectors.VSCode.csproj`

### Test Projects (test/)
19. `test/Microsoft.Kusto.ServiceLayer.UnitTests/Microsoft.Kusto.ServiceLayer.UnitTests.csproj`
20. `test/Microsoft.SqlTools.Authentication.UnitTests/Microsoft.SqlTools.Authentication.UnitTests.csproj`
21. `test/Microsoft.SqlTools.ManagedBatchParser.IntegrationTests/Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.csproj`
22. `test/Microsoft.SqlTools.Migration.IntegrationTests/Microsoft.SqlTools.Migration.IntegrationTests.csproj`
23. `test/Microsoft.SqlTools.ServiceLayer.IntegrationTests/Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj`
24. `test/Microsoft.SqlTools.ServiceLayer.PerfTests/Microsoft.SqlTools.ServiceLayer.PerfTests.csproj`
25. `test/Microsoft.SqlTools.ServiceLayer.Test.Common/Microsoft.SqlTools.ServiceLayer.Test.Common.csproj`
26. `test/Microsoft.SqlTools.ServiceLayer.TestDriver/Microsoft.SqlTools.ServiceLayer.TestDriver.csproj`
27. `test/Microsoft.SqlTools.ServiceLayer.TestDriver.Tests/Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj`
28. `test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/Microsoft.SqlTools.ServiceLayer.TestEnvConfig.csproj`
29. `test/Microsoft.SqlTools.ServiceLayer.UnitTests/Microsoft.SqlTools.ServiceLayer.UnitTests.csproj`
30. `test/Microsoft.SqlTools.Test.CompletionExtension/Microsoft.SqlTools.Test.CompletionExtension.csproj`
31. `test/ScriptGenerator/ScriptGenerator.csproj`
32. `test/TVFSample/TVFSample.csproj`

### Pipeline and Build Files
33. `azure-pipelines/build-and-release.yml` - Version numbers
34. `azure-pipelines/build.yml` - Artifact paths
35. `azure-pipelines/createBuildDirectories.sh` - Framework variable
36. `azure-pipelines/osx-arm64-signing.yml` - Archive names
37. `azure-pipelines/release.yml` - Archive names
38. `build.cake` - Runtime identifiers (if applicable)

### Other Files
39. `.vscode/launch.json` - Debug paths
40. `RefreshDllsForTestRun.cmd` - Test paths
41. `docs/samples/jsonrpc/netcore/executequery/jsonrpc.csproj`
42. `packages/Microsoft.SqlTools.ManagedBatchParser/Microsoft.SqlTools.ManagedBatchParser.nuspec`
43. `test/Microsoft.SqlTools.ServiceLayer.TestDriver/Driver/ServiceTestDriver.cs` - Fallback path
