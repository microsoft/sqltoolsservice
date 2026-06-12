# STS2 Decisions Log

Two-way doors are recorded in one line and work proceeds. `SPEC-CHANGE` entries stop for human review.
`REPO-FACT` entries record local verification of SPEC §0 facts.

## REPO-FACT entries (M0, 2026-06-12)

- **RF-0001:** `global.json` pins SDK `10.0.203` with `rollForward: latestFeature`. Local SDK 10.0.300 satisfies it. CONFIRMED.
- **RF-0002:** `Directory.Build.props` sets `SqlToolsServiceDotNetVersion=net10.0`, `SqlCoreDotNetVersion=net8.0`, `SsmsDotNetVersion=net472`. CONFIRMED.
- **RF-0003:** `src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj` targets `$(SqlToolsServiceDotNetVersion)`. CONFIRMED.
- **RF-0004:** `HostLoader.CreateAndStartServiceHost(SqlToolsContext, ServiceLayerCommandOptions?, Stream? inputStream = null, Stream? outputStream = null)` exists at `src/Microsoft.SqlTools.ServiceLayer/HostLoader.cs:62` and passes streams to `ServiceHost.Instance.Initialize(inputStream, outputStream)`. Preferred seam is feasible. CONFIRMED.
- **RF-0005:** Test root is `test/`, not `tests/`. STS2 tests go under `test/sts2`. CONFIRMED.
- **RF-0006:** `src/Microsoft.SqlTools.ServiceLayer/Program.cs` is the composition root; it calls `HostLoader.CreateAndStartServiceHost(sqlToolsContext, commandOptions)` with no stream args (line 64). CONFIRMED.
- **RF-0007:** `Microsoft.SqlTools.Utility.CommandOptions` (src/Microsoft.SqlTools.Hosting/Utility/CommandOptions.cs) treats unknown `--` arguments as errors: it prints usage to **stdout** via `Console.WriteLine` and sets `ShouldExit=true`. A raw `--enable-sts2` argument would therefore exit the process and emit unframed stdout text. Mitigation: `ServiceLayerCommandOptions` already pre-filters service-layer-only args (`-d`, `--developers`) out of the base parse via the `serviceLayerCommandArgs` array. Adding `--enable-sts2` to that filter array is a one-line legacy change inside the seam budget. STS2 Bootstrap parses the raw `args` itself.
- **RF-0008:** The repo has a root `sqltoolsservice.sln`. The `sqltoolsservice-sts2.slnf` solution filter will reference it; STS2 projects must also be added to `sqltoolsservice.sln` for the filter to resolve.
- **RF-0009:** Package versions are centralized in `Packages.props` (`PackageReference Update` pattern, version-less `PackageReference Include` in csproj). NuGet feed is nuget.org plus a local folder feed. `StreamJsonRpc`, `Microsoft.Data.Sqlite`, and analyzer packages are not yet referenced anywhere; they will be added to `Packages.props`.
- **RF-0010:** Repo-wide `Directory.Build.props` already enforces `TreatWarningsAsErrors`, `Nullable=enable`, `EnforceCodeStyleInBuild`, and `GenerateDocumentationFile` for all projects, including new STS2 projects.

## Decisions

- **D-0001** (two-way, 2026-06-12): No root `CLAUDE.md` existed. Created one containing only the runbook's STS2 branch-rules snippet.
- **D-0002** (two-way, 2026-06-12): Working branch is `sts2/main` created from `main` at 77498823, per runbook recommendation.
- **D-0003** (two-way, 2026-06-12): STS2 test framework is xunit (already centrally versioned in `Packages.props`; repo uses both nunit and xunit). M0 test projects: `test/sts2/Microsoft.SqlTools.Sts2.UnitTests` (unit + multiplexer + architecture + banned-API tests) and `test/sts2/Microsoft.SqlTools.Sts2.E2ETests` (spawned-exe stdio tests).
- **D-0004** (two-way, 2026-06-12): `--enable-sts2` is filtered from legacy arg parsing via `serviceLayerCommandArgs` (see RF-0007); Bootstrap owns STS2 flag parsing.
