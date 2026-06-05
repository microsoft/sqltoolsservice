---
name: hook-local-sts-into-mssql
description: 'Wire a locally-published SQL Tools Service (STS) into the vscode-mssql Extension Development Host by setting MSSQL_SQLTOOLSSERVICE in launch.json. Use when: testing local STS changes in vscode-mssql, hooking up locally-built STS to MSSQL, debugging STS from VS Code F5, "use my local STS in MSSQL", "point MSSQL at local STS", "hook up STS to MSSQL", "set MSSQL_SQLTOOLSSERVICE". Assumes STS has already been local-published (see the `local-publish-sts` skill).'
---

# Hook local STS into vscode-mssql

Update vscode-mssql's `launch.json` to point its Extension Development Host
at a locally-published STS via the `MSSQL_SQLTOOLSSERVICE` environment
variable. After this, hitting F5 in the MSSQL extension uses your local
STS instead of the bundled one.

## When to use

- The user just built STS (or asks you to) and wants to test it in the
  MSSQL extension.
- The user says "use my local STS in MSSQL", "point MSSQL at local STS",
  "hook up local STS", "set `MSSQL_SQLTOOLSSERVICE`".
- The MSSQL repo may or may not be in the same workspace as STS — this
  skill handles both cases.

## Prerequisite

The STS publish folder must exist. If it doesn't, run the
`local-publish-sts` skill first (or instruct the user to). The path you
will need is:

```
<sts-repo-root>/artifacts/publish/Microsoft.SqlTools.ServiceLayer/default/net10.0
```

## Procedure

### Step 1 — Locate the STS publish folder

If the current working directory is an STS clone, derive the path from
there. Otherwise ask the user for the STS repo root via `ask_user`.

Confirm the folder exists and contains
`MicrosoftSqlToolsServiceLayer.exe` (Windows) or
`MicrosoftSqlToolsServiceLayer` (Linux/macOS). If missing, stop and tell
the user they need to local-publish first.

### Step 2 — Locate the vscode-mssql repo

The agent may not have direct access to the MSSQL repo. Check, in this
order:

1. **Sibling directory** of the STS repo (`<sts-repo-parent>/vscode-mssql/`).
   This is the most common layout.
2. **Open editor workspace folders** — check any `.code-workspace` file
   or `${workspaceFolder}` references that point at a vscode-mssql clone.
3. **Common defaults**: `~/Source/vscode-mssql/`,
   `~/source/repos/vscode-mssql/`, `~/code/vscode-mssql/`,
   `~/repos/vscode-mssql/`.

If none match, **use `ask_user`** to prompt for the absolute path to
their vscode-mssql repo root. Do not guess silently.

Validate it's a real vscode-mssql clone before continuing — the root
should contain **both** `extensions/mssql/package.json` and
`.vscode/launch.json`.

### Step 3 — Update both launch.json files

vscode-mssql has **two** launch.json files. Update both so the env var
is set whether the user opens the repo root or the `extensions/mssql`
folder as their workspace:

| Path (relative to vscode-mssql root) | Active when… | Configurations |
|---|---|---|
| `.vscode/launch.json` | Repo root is the workspace folder | "Run All Extensions" |
| `extensions/mssql/.vscode/launch.json` | `extensions/mssql` is the workspace folder | "Launch Extension", "Launch Extension (With Other Extensions Disabled)" |

For **every** launch configuration in **both** files, set
`MSSQL_SQLTOOLSSERVICE` inside the `env` block. The shipped template
typically has a commented-out placeholder:

```jsonc
// "MSSQL_SQLTOOLSSERVICE": "<Path to STS>"
```

Replace that line (uncommented) with the absolute STS publish path. If
no placeholder exists, add the entry to the existing `env` object.

```jsonc
"env": {
    "MSSQL_SQLTOOLSSERVICE": "c:\\path\\to\\sqltoolsservice\\artifacts\\publish\\Microsoft.SqlTools.ServiceLayer\\default\\net10.0"
    // Other env entries (e.g. MSSQL_SQLTOOLS_MCP) — preserve any that are present
}
```

**Path formatting rules:**
- Use an **absolute path**. `${workspaceFolder}`-relative paths break
  whenever the STS repo lives outside the launched workspace (which is
  always when the two repos are sibling directories).
- On Windows, JSON requires either `\\` or `/` as the path separator
  (single `\` is an invalid escape). Either works for VS Code.
- Preserve any other entries already in the `env` object (e.g.
  `MSSQL_SQLTOOLS_MCP`, telemetry flags) — only touch
  `MSSQL_SQLTOOLSSERVICE`.

Reference for the env var:
<https://github.com/microsoft/vscode-mssql/blob/main/DEVELOPMENT.md#using-mssql_sqltoolsservice-environment-variable>

### Step 4 — Verify and report

1. Resolve the exact path you wrote and confirm it exists:
   ```pwsh
   Test-Path "<the configured absolute path>\MicrosoftSqlToolsServiceLayer.exe"
   ```
2. Re-read both launch.json files and confirm `MSSQL_SQLTOOLSSERVICE`
   shows up with the right value in every config's `env` block.
3. Tell the user:
   - ✅ Both launch.json files updated with `MSSQL_SQLTOOLSSERVICE`.
   - Which configurations now use the local STS.
   - That they need to **re-run the `local-publish-sts` skill after each
     STS code change** and **stop any running EDH first** to release
     file locks before re-publishing.

## Common pitfalls

- **Only updating one launch.json**. The repo-root file and the
  `extensions/mssql/.vscode/` file each ship with different launch
  configurations. If you only update one, F5 will silently fall back to
  the bundled STS when the user opens the "wrong" workspace folder.
- **Forgetting to escape backslashes** in the JSON string. `"c:\Users\…"`
  is invalid JSON and VS Code will silently ignore the env block.
- **Pointing at `…/Debug/net10.0/` instead of `…/publish/…/default/net10.0/`**.
  The `bin/Debug` output isn't a published runnable; only the
  `artifacts/publish/Microsoft.SqlTools.ServiceLayer/default/net10.0`
  folder is.
- **Pointing at a stale path** if the user has multiple STS clones. Always
  confirm which clone the user means before writing.
