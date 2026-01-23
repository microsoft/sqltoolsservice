<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0 (new principle added)
Modified principles: N/A
Added sections:
  - Principle VI: Localization Required
Removed sections: N/A
Templates requiring updates:
  - plan-template.md ✅ (Constitution Check section compatible)
  - spec-template.md ✅ (requirements/testing sections aligned)
  - tasks-template.md ✅ (phase structure compatible)
Follow-up TODOs: None
-->

# SQL Tools Service Constitution

## Core Principles

### I. JSON-RPC API-First

All features MUST be exposed through the JSON-RPC over stdio protocol. The service acts as a
host-agnostic backend for SQL tooling clients (VS Code, Azure Data Studio, etc.).

- Every new capability MUST define JSON-RPC request/response contracts before implementation
- Contracts MUST follow the established message format in `docs/guide/jsonrpc_protocol.md`
- Breaking changes to existing contracts are prohibited without major version increment
- All API responses MUST include proper error handling with meaningful error codes

**Rationale**: The service's value proposition is providing a consistent, cross-platform API for
SQL tooling. Contract stability is essential for client integrations.

### II. Cross-Platform Compatibility

The service MUST build and run correctly on all supported target runtimes:
`win-x64`, `win-x86`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`.

- Platform-specific code MUST be isolated and clearly marked
- File paths MUST use platform-agnostic handling
- All features MUST be tested on Windows, macOS, and Linux before release
- Dependencies MUST support all target platforms

**Rationale**: SQL Tools Service powers tools used across diverse development environments.

### III. Unit Testing Required

All new features and bug fixes MUST include adequate unit tests. The Release build and all
unit tests MUST pass before any PR can be merged.

- Tests MUST validate mainline scenarios and common failure scenarios
- Integration tests are required for: connection management, query execution, Azure storage
- Use nUnit framework for all test projects
- Test projects follow naming convention: `Microsoft.SqlTools.[Component].UnitTests`
- Environment-specific tests (e.g., Azure storage) MUST document required configuration

**Rationale**: The README explicitly states "Write unit tests to validate new features and bug
fixes" as a contribution requirement.

### IV. API Stability & Breaking Changes

Public APIs are sacred. Changes that risk breaking existing clients MUST be carefully evaluated
and approved by core maintainers.

- New features SHOULD extend existing contracts, not modify them
- Deprecated APIs MUST remain functional for at least one major version
- All public API interfaces MUST be documented (enforced by Release build)
- Version format: `MAJOR.MINOR.PATCH.REVISION` per `Directory.Build.props`

**Rationale**: Multiple SQL tools depend on this service; breaking changes cascade to all clients.

### V. Commit Hygiene & Code Quality

All contributions MUST follow established commit hygiene and code quality standards.

- Commit messages MUST be clear, imperative, and reference issues when fixing bugs
- Related commits MUST be squashed; separate commits for distinct logical changes
- Code MUST pass `EnforceCodeStyleInBuild` and `EnableNETAnalyzers` checks
- Nullable reference types are enabled project-wide (`<Nullable>enable</Nullable>`)
- Documentation comments are required for public APIs

**Rationale**: Good commit history aids debugging, code review, and project maintainability.

### VI. Localization Required

All user-facing error messages and display strings MUST be localized.

- Error messages MUST NOT be hardcoded as string literals in code
- Add new strings to `sr.strings` resource files
- After adding or modifying localized strings, regenerate resources: `build.cmd --target=SRGen`
- XLIFF template files in `/localization/` are used for translation
- Supported languages: de, es, fr, it, ja, ko, pt-br, ru, zh-hans, zh-hant

**Rationale**: SQL Tools Service is used globally; localized error messages improve user experience
for non-English speakers and meet Microsoft's internationalization standards.

## Technology Constraints

**Runtime**: .NET (current LTS version per `global.json`)
**Build System**: MSBuild + Cake for advanced scenarios (packaging, resource generation)
**Target Framework**: Cross-platform .NET targeting all specified runtimes
**Protocol**: JSON-RPC over stdio, implementing portions of VS Code Language Server Protocol
**Testing**: nUnit for unit tests, integration tests for database/Azure interactions
**Code Analysis**: Roslyn analyzers enabled, nullable reference types enforced
**Localization**: XLIFF-based resource files in `/localization/`

**Package Structure**:
- Source: `src/Microsoft.SqlTools.*` - organized by component/layer
- Tests: `test/Microsoft.SqlTools.*` - mirror source structure
- Shared: `Microsoft.SqlTools.Shared` for cross-cutting concerns

## Development Workflow

**Branch Strategy**:
- Direct commits to `main` are NOT allowed
- Create feature branches for all changes
- PRs require passing CI (build + unit tests) before merge

**Quality Gates**:
1. Release configuration build MUST succeed (validates API documentation)
2. All unit tests MUST pass
3. Code review required from maintainers
4. Breaking changes require explicit maintainer approval

**Review Process**:
- PRs MUST include high-level summary of changes
- Reviewers check compliance with contribution guidelines
- Address all review feedback before merge
- Final commit cleanup (squash if needed) before merge

**Security**:
- Security vulnerabilities MUST NOT be reported via public GitHub issues
- Report security issues to Microsoft Security Response Center (MSRC)
- Follow Microsoft's Coordinated Vulnerability Disclosure policy

## Governance

This constitution supersedes ad-hoc development practices. All PRs and code reviews MUST
verify compliance with these principles.

**Amendment Process**:
1. Propose changes via PR to this document
2. Core maintainers review and approve
3. Update version according to semantic versioning:
   - MAJOR: Principle removal or redefinition
   - MINOR: New principle or significant expansion
   - PATCH: Clarifications and wording improvements
4. Update `LAST_AMENDED_DATE` on approval

**Compliance**:
- Complexity beyond these principles MUST be justified in PR description
- Exceptions require explicit maintainer approval with documented rationale
- Use `README.md` and `docs/guide/` for detailed runtime development guidance

**Version**: 1.1.0 | **Ratified**: 2026-01-23 | **Last Amended**: 2026-01-23
