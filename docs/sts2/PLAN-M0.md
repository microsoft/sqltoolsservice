# PLAN-M0: Skeleton, repo reality, seam, multiplexer

## Scope

Everything in SPEC §16 M0 DoD: repo facts verified (done, see DECISIONS.md RF-0001..RF-0010),
project scaffolding with enforced dependency matrix, banned-API analyzers, the multiplexer with
its full test suite, Bootstrap + Program seam, `v2/diagnostics.ping` through STS2 Hosting,
activation flags, E2E enabled/disabled-mode tests, and `verify.sh`/`verify.ps1`.

Out of scope: journal/envelope machinery (M1), any connection or query behavior (M2/M3),
drivers beyond empty scaffolds (M4/M5).

## Vertical slices

1. **Docs + branch baseline** — spec/runbook/decisions/plan committed on `sts2/main`. (this commit)
2. **Project scaffolds + slnf + dependency matrix** — 10 `src/sts2` projects, `tools/sts2-replay`,
   2 `test/sts2` projects, all added to `sqltoolsservice.sln`, `sqltoolsservice-sts2.slnf` filter,
   `Packages.props` additions (StreamJsonRpc, Microsoft.Data.Sqlite, BannedApiAnalyzers,
   PublicApiAnalyzers), `src/sts2/Directory.Build.props` for shared STS2 settings.
   Gate: `dotnet build sqltoolsservice-sts2.slnf` green.
3. **Architecture tests** — project-reference matrix test reading csproj files; namespace ban test
   over `src/sts2/**` sources; banned-API analyzer (`BannedSymbols.txt`) wired into Core,
   Contracts, Abstractions. Tests fail first on a seeded violation, then pass clean.
4. **Multiplexer tests, then multiplexer** — framing (partial/coalesced/`Content-Length` variants,
   max frame), v2-prefix routing, legacy fallback, malformed-frame fallback, lifecycle mirroring
   (`shutdown`, `exit` flush ordering), outbound server-request id rewriting with collisions,
   single-stdout-writer property test, poison STS2 crash containment.
5. **Bootstrap + Program seam** — `Sts2Bootstrap.TryStart` returning `Sts2BootstrapHandle`
   (disabled => null streams), `--enable-sts2` / `STS_ENABLE_STS2=1` activation, one-line
   `serviceLayerCommandArgs` filter, Program.cs edit within the <60-line / 3-file budget.
6. **`v2/diagnostics.ping`** — minimal STS2 Hosting with StreamJsonRpc on the virtual STS2 stream;
   ping returns echo + health stub. No journal yet (M1); `latestSeq` reports 0.
7. **verify.sh / verify.ps1 + E2E** — quick gates that exist at M0: build, unit/multiplexer tests,
   architecture/banned-API tests, legacy-diff budget check, disabled-mode v1 smoke, enabled-mode
   E2E (`v2/diagnostics.ping` + one v1 request in same session). Stub the M1+ gates with explicit
   "n/a at M0" lines so the report format is stable.

## Expected new tests/scenarios

- Multiplexer unit/property tests (~25-35 tests).
- Architecture: dependency matrix, banned namespaces, banned APIs.
- E2E: disabled-mode v1 smoke, enabled-mode ping + v1 `version` request.

## Expected generated-doc changes

None at M0 (generated review surface lands in M1). `verify.sh` prints "generated docs: n/a at M0".

## Expected verification gate changes

`verify.sh --quick` created; first green run reported in `artifacts/verification-report.md`.

## Known risks

- StreamJsonRpc API surface may differ from spec sketch; conform to installed package (two-way decision).
- Legacy `ServiceHost.Initialize(null, null)` default path must be byte-identical when disabled;
  guarded by disabled-mode smoke.
- `exit` flush ordering depends on legacy exit handling; verify with spawned E2E, not assumptions.
- Windows dev box: `verify.sh` must run under bash (Git Bash available); verify.ps1 shim.
