# Research: Upgrade .NET Version to Latest LTS

**Date**: 2026-01-23  
**Feature**: [spec.md](spec.md)  
**Plan**: [plan.md](plan.md)

## Research Task 1: .NET 10.0 SDK Availability

### Decision
Use .NET 10.0 SDK with `rollForward: latestFeature` in global.json.

### Rationale
- .NET 10.0 is the current LTS release (November 2025 release, supported until November 2028)
- Latest SDK version as of January 2026: `10.0.2` (per Microsoft download page)
- Using `rollForward: latestFeature` allows automatic adoption of patch updates within the feature band
- This aligns with clarification to keep spec reusable for future upgrades

### Alternatives Considered
- Pin exact SDK version (e.g., `10.0.100`): Rejected - reduces flexibility, requires spec updates for patches
- Use `rollForward: latestMajor`: Rejected - too aggressive, could cause unexpected breaking changes

---

## Research Task 2: Package Compatibility Audit

### Decision
Update SDK-tied packages to 10.0.x versions. Keep other packages on current versions.

### Rationale
The following packages MUST be updated to match .NET SDK version:
- `Microsoft.Extensions.DependencyModel` → 10.0.0
- `Microsoft.Extensions.FileSystemGlobbing` → 10.0.0
- `System.Composition` → 10.0.0
- `System.Configuration.ConfigurationManager` → 10.0.0
- `System.IO.Packaging` → 10.0.0
- `System.Security.Permissions` → 10.0.0
- `System.Text.Encoding.CodePages` → 10.0.0
- `System.Text.Encodings.Web` → 10.0.0

Non-SDK-tied packages can remain on current versions:
- `Microsoft.Data.SqlClient` (5.1.4) - supports .NET 10
- `Microsoft.SqlServer.SqlManagementObjects` (170.18.0) - supports .NET 10
- `Azure.Identity` (1.10.3) - supports .NET 10
- `Newtonsoft.Json` (13.0.3) - netstandard2.0 compatible

### Alternatives Considered
- Update all packages to latest: Rejected - risk of introducing unrelated breaking changes
- Keep SDK-tied packages on 8.0.x: Rejected - may cause runtime issues with .NET 10

---

## Research Task 3: Breaking Change Assessment

### Decision
No code changes required for SQL Tools Service. Breaking changes are in areas not used by this project.

### Rationale
Reviewed .NET 10 breaking changes (https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0):

**Not Applicable to SQL Tools Service:**
- ASP.NET Core changes: Service uses JSON-RPC over stdio, not ASP.NET
- Windows Forms changes: Service is a console application
- WPF changes: Service is a console application
- Entity Framework Core: Service uses Microsoft.Data.SqlClient directly

**Potentially Relevant (Low Risk):**
- `BufferedStream.WriteByte no longer performs implicit flush`: May affect file operations, but code already uses explicit flush
- `System.Text.Json checks for property name conflicts`: Service uses Newtonsoft.Json
- `Single-file apps no longer look for native libraries in executable directory`: Service is not distributed as single-file
- `OpenSSL 1.1.1 or later required on Unix`: Already a requirement for current .NET 8

**SDK/MSBuild Changes:**
- `dotnet new sln defaults to SLNX file format`: Not creating new solutions
- `PackageReference without a version raises an error`: Already using version attributes
- `NuGet packages with no runtime assets aren't included in deps.json`: Monitor for issues

### Alternatives Considered
- Pre-emptively refactor code for all breaking changes: Rejected - unnecessary work for changes not affecting this codebase
- Wait for .NET 10.1 to avoid early issues: Rejected - .NET 10.0 is stable LTS

---

## Research Task 4: Analyzer Updates

### Decision
Add suppressions for new .NET 10 analyzers that may cause warnings on existing patterns.

### Rationale
Based on .NET 8 → 10 upgrade patterns, likely new analyzer warnings:
- `IDE0290` - Use primary constructors (suggestion, not error)
- `IDE0300` - Use collection expressions (suggestion, not error)
- `IDE0301` - Use collection expressions for empty (suggestion, not error)
- `IDE0305` - Use collection expressions (suggestion, not error)

These are style suggestions, not errors. Add suppressions to `.editorconfig` to maintain existing code style:
```
dotnet_diagnostic.IDE0290.severity = suggestion
dotnet_diagnostic.IDE0300.severity = suggestion
dotnet_diagnostic.IDE0301.severity = suggestion
dotnet_diagnostic.IDE0305.severity = suggestion
```

### Alternatives Considered
- Adopt all new C# 14 patterns: Rejected - scope creep, separate effort
- Ignore warnings without suppressions: Rejected - noisy build output

---

## Research Task 5: Multi-Targeting Compatibility

### Decision
Update multi-targeting projects to use `net10.0` instead of `net8.0`, preserving `netstandard2.0` and `net472`.

### Rationale
Projects with multi-targeting:
- `Microsoft.SqlTools.Hosting`: `netstandard2.0;net10.0`
- `Microsoft.SqlTools.ManagedBatchParser`: `net10.0;net472;netstandard2.0`

The `netstandard2.0` and `net472` targets are preserved for:
- PowerShell cmdlet compatibility
- Legacy client support
- Broader NuGet package compatibility

### Alternatives Considered
- Drop netstandard2.0: Rejected - breaks PowerShell integration
- Drop net472: Rejected - still needed for some legacy scenarios

---

## Summary

| Research Area | Status | Action |
|--------------|--------|--------|
| SDK Version | ✅ Resolved | Use 10.0.x with latestFeature rollforward |
| Package Versions | ✅ Resolved | Update SDK-tied packages to 10.0.0 |
| Breaking Changes | ✅ Resolved | No code changes required |
| Analyzer Updates | ✅ Resolved | Add suppressions for IDE0290, IDE0300, IDE0301, IDE0305 |
| Multi-Targeting | ✅ Resolved | Preserve netstandard2.0 and net472 |

**All NEEDS CLARIFICATION items resolved. Ready for Phase 1.**
