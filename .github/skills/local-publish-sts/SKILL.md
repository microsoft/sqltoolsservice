---
name: local-publish-sts
description: 'Build SQL Tools Service (STS) and run the Cake LocalPublish target so the resulting binaries are available under artifacts/publish for local testing. Use when: building STS locally, running LocalPublish, rebuilding STS after code changes, preparing STS for use by another extension/client, "publish STS", "local publish", "build SQL Tools Service", "rebuild STS". This skill only builds; if the goal is to also point vscode-mssql at the output, follow up with the `hook-local-sts-into-mssql` skill.'
---

# Local-publish STS

Build and publish the SQL Tools Service locally via the Cake `LocalPublish` target. Produces a runnable `MicrosoftSqlToolsServiceLayer` host plus all companion service DLLs under `artifacts/publish/`.

## When to use

- Iterating on STS code and wanting a fresh self-contained build.
- Preparing a local STS for vscode-mssql or any other client that consumes it
- The user says "build STS", "publish STS", "local publish", "rebuild STS".

This skill **only builds**. To wire the output into a vscode-mssql repo's environment, run the `hook-local-sts-into-mssql` skill afterward.

## Procedure

### Step 1: Run LocalPublish

From the STS repo root:

Windows:

```pwsh
.\build.ps1 --target=LocalPublish --configuration=Debug
```

On non-Windows:

```bash
./build.sh --target=LocalPublish --configuration=Debug
```

The build typically takes ~1–2 minutes on a warm cache. Use an `initial_wait` of at least 300 seconds when running via tools that need a hint.

### Step 2: Confirm output

The published binaries live at (relative to repo root):

```
artifacts/publish/Microsoft.SqlTools.ServiceLayer/default/net10.0/
```

(.NET version may differ in the rare case that we've bumped to a new version)

Verify with `Test-Path` against `MicrosoftSqlToolsServiceLayer.exe` (Windows) or `MicrosoftSqlToolsServiceLayer` (Linux/macOS).

### Step 3: Report back

Tell the user:

- ✅ Build succeeded.
- The absolute path to the publish folder (they'll need it if they want to wire it into a client).
- Reminder: **re-run this skill after each STS code change**, and **stop any running consumer** (e.g. EDH instances) first to release file locks (see "Handling lock errors" below).

## Build targets

Defined in `build.cake` (search for `Task(...)`). For day-to-day dev, `LocalPublish` is what you want. The others are situational:

| Target                 | What it does                                                     | When to use                               |
| ---------------------- | ---------------------------------------------------------------- | ----------------------------------------- |
| `LocalPublish`         | Build + publish for the current RID only                         | Default dev loop                          |
| `Quick`                | `Cleanup` + `LocalPublish`                                       | After a botched build to start clean      |
| `Install`              | `Cleanup` + `LocalPublish` + copy to `~/.sqltoolsservice/local/` | Sharing with a globally-configured client |
| `Local`                | `Setup` + `Restore` + `TestAll` + `LocalPublish`                 | Pre-PR sanity pass                        |
| `Default`              | Alias for `Local`                                                | Same as above                             |
| `AllPublish`           | Publish for all RIDs in `build.json`                             | Reproducing CI artifacts                  |
| `TestAll` / `TestCore` | Run unit tests, must include --runTests argument                 | Test-only iteration                       |

Configuration flag accepts `Debug` (default for local work) or `Release`.

## Handling lock errors

If `LocalPublish` fails with `MSB3027` / `MSB3021` "file is locked" errors mentioning `MicrosoftSqlToolsServiceLayer.dll`, `Microsoft.SqlTools.Hosting.dll`,
or sibling DLLs, a previously launched consumer process is still running an STS host and holds locks on the publish output.

1. Locate the locking processes:

   ```pwsh
   Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
       Where-Object {
           $_.CommandLine -like "*MicrosoftSqlToolsServiceLayer.dll*" -or
           $_.CommandLine -like "*SqlToolsResourceProviderService.dll*"
       } |
       Select-Object ProcessId, CommandLine
   ```

2. **Ask the user before killing them** — they may be in the middle of an
   active EDH / Azure Data Studio session they'd prefer to close manually.

3. With approval, kill by PID (name-based termination isn't available in
   the agent runtime):

   ```pwsh
   Stop-Process -Id <PID1>,<PID2> -Force
   ```

4. Re-run the publish.

## Notes

- The `net10.0` segment of the publish path tracks the `Frameworks` array in `build.json`. If the framework version changes, read `build.json` to get the new path segment rather than hardcoding it.
- `LocalPublish` writes to the `default` RID slot specifically because it depends on the `RestrictToLocalRuntime` task. Other RIDs only appear after `AllPublish`.
