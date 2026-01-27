# Quickstart: Upgrade .NET Version to Latest LTS

**Feature**: Upgrade SQL Tools Service from .NET 8.0 to .NET 10.0 LTS  
**Estimated Time**: 2-4 hours  
**Prerequisites**: .NET 10.0 SDK installed locally

## Overview

This guide walks through upgrading the SQL Tools Service from .NET 8.0 to .NET 10.0 LTS. The upgrade follows the pattern established in commit [0a1fed4](https://github.com/microsoft/sqltoolsservice/commit/0a1fed423123d89862195ad5fbe698479453f4a4) which upgraded from .NET 7 to .NET 8.

## Pre-Implementation Checklist

- [ ] .NET 10.0 SDK installed (`dotnet --list-sdks` shows 10.0.x)
- [ ] Current `main` branch builds successfully
- [ ] All unit tests pass on current version
- [ ] Feature branch created from `main`

## Implementation Steps

### Step 1: Update SDK Version (global.json)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  },
  "msbuild-sdks": {
    "Microsoft.Build.NoTargets": "3.7.0"
  }
}
```

**Verification**: `dotnet --version` returns 10.0.x

### Step 2: Update Package Versions (Packages.props)

Update SDK-tied packages in `Packages.props`:

```xml
<!-- The following packages always need to be updated to the current .NET SDK version. -->
<ItemGroup>
  <PackageReference Update="Microsoft.Extensions.DependencyModel" Version="10.0.0" />
  <PackageReference Update="Microsoft.Extensions.FileSystemGlobbing" Version="10.0.0" />
  <PackageReference Update="System.Composition" Version="10.0.0" />
  <PackageReference Update="System.Configuration.ConfigurationManager" Version="10.0.0" />
  <PackageReference Update="System.IO.Packaging" Version="10.0.0" />
  <PackageReference Update="System.Security.Permissions" Version="10.0.0" />
  <PackageReference Update="System.Text.Encoding.CodePages" Version="10.0.0" />
  <PackageReference Update="System.Text.Encodings.Web" Version="10.0.0" />
</ItemGroup>
```

### Step 3: Update Target Framework in .csproj Files

Replace `net8.0` with `net10.0` in all project files.

**Single-target projects** (most projects):
```xml
<TargetFramework>net10.0</TargetFramework>
```

**Multi-target projects**:
- `Microsoft.SqlTools.Hosting`: `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`
- `Microsoft.SqlTools.ManagedBatchParser`: `<TargetFrameworks>net10.0;net472;netstandard2.0</TargetFrameworks>`

**Files to update** (use find-replace `net8.0` → `net10.0`):
- All 13 projects in `src/`
- All 14 projects in `test/`
- `docs/samples/jsonrpc/netcore/executequery/jsonrpc.csproj`

### Step 4: Update Build Configuration (build.json)

```json
{
  "TestProjects": {
    "Microsoft.SqlTools.ServiceLayer.UnitTests": ["net10.0"],
    "Microsoft.Kusto.ServiceLayer.UnitTests": ["net10.0"],
    ...
  }
}
```

### Step 5: Update Pipeline Files

**azure-pipelines/build-and-release.yml**:
- Update `Major` version (e.g., `'5'` → `'6'`)
- Update `Minor` version (reset to `'0'`)
- Update `ManagedBatchParserMajor` (e.g., `'4'` → `'5'`)

**azure-pipelines/build.yml**:
- Replace `net8.0` with `net10.0` in artifact paths

**azure-pipelines/release.yml**:
- Replace `net8.0` with `net10.0` in archive names

**azure-pipelines/osx-arm64-signing.yml**:
- Replace `net8.0` with `net10.0` in archive names

**azure-pipelines/createBuildDirectories.sh**:
```bash
framework10="/bin/Debug/net10.0/"
```

### Step 6: Update IDE and Script Paths

**.vscode/launch.json**:
```json
"program": "${workspaceFolder}/src/Microsoft.SqlTools.ServiceLayer/bin/Debug/net10.0/MicrosoftSqlToolsServiceLayer.dll"
```

**RefreshDllsForTestRun.cmd**:
```cmd
SET _PerfTestSourceLocation="%WORKINGDIR%\test\Microsoft.SqlTools.ServiceLayer.PerfTests\bin\%_BuildConfiguration%\net10.0\win-x64\publish"
SET _ServiceSourceLocation="%WORKINGDIR%\src\Microsoft.SqlTools.ServiceLayer\bin\%_BuildConfiguration%\net10.0\win-x64\publish"
```

**ServiceTestDriver.cs**:
```csharp
serviceHostExecutable = @"..\..\..\..\..\src\Microsoft.SqlTools.ServiceLayer\bin\Debug\net10.0\win-x64\MicrosoftSqlToolsServiceLayer.exe";
```

### Step 7: Update NuGet Package Spec

**packages/Microsoft.SqlTools.ManagedBatchParser/Microsoft.SqlTools.ManagedBatchParser.nuspec**:
- Update `targetFramework` from `net8.0` to `net10.0`
- Update all file paths from `net8.0` to `net10.0`

### Step 8: Update Analyzer Suppressions (.editorconfig)

Add to `.editorconfig` if not already present:
```ini
dotnet_diagnostic.IDE0290.severity = suggestion
dotnet_diagnostic.IDE0300.severity = suggestion
dotnet_diagnostic.IDE0301.severity = suggestion
dotnet_diagnostic.IDE0305.severity = suggestion
```

## Verification Steps

### Local Build
```bash
dotnet build sqltoolsservice.sln
dotnet build sqltoolsservice.sln -c Release
```

### Run Unit Tests
```bash
dotnet test test/Microsoft.SqlTools.ServiceLayer.UnitTests/
dotnet test test/Microsoft.Kusto.ServiceLayer.UnitTests/
```

### Cake Build
```bash
./build.cmd --target=Local
# or on macOS/Linux:
./build.sh --target=Local
```

### Cross-Platform Build (optional)
```bash
dotnet publish src/Microsoft.SqlTools.ServiceLayer -r win-x64 -c Release
dotnet publish src/Microsoft.SqlTools.ServiceLayer -r osx-arm64 -c Release
dotnet publish src/Microsoft.SqlTools.ServiceLayer -r linux-x64 -c Release
```

## Troubleshooting

### Build Errors

**"SDK 'Microsoft.NET.Sdk' not found"**
- Install .NET 10.0 SDK from https://dotnet.microsoft.com/download

**Package version conflicts**
- Run `dotnet restore --force` to refresh package cache
- Verify all SDK-tied packages use 10.0.0 version

**Analyzer warnings**
- Add suppressions to `.editorconfig` as documented in Step 8

### Test Failures

**Environment-specific test failures**
- Ensure Azure storage credentials are configured for integration tests
- Check `test/README.md` for environment variable requirements

## Rollback

If issues are discovered after merging:
1. Revert all changes with `git revert <commit-sha>`
2. The `rollForward: latestFeature` setting means builds will still work with .NET 10.x SDK
3. For immediate rollback, change `global.json` version back to `8.0.416`

## Success Criteria

- [ ] `dotnet build sqltoolsservice.sln` succeeds with zero errors
- [ ] All unit tests pass
- [ ] Release build succeeds (API documentation validated)
- [ ] Cake build (`build.cmd --target=Local`) succeeds
- [ ] CI pipeline passes (once PR is created)
