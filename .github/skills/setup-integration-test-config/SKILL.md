---
name: setup-integration-test-config
description: 'Create SQL Tools Service integration test connection settings by generating SQLConnectionInstances XML, then running Microsoft.SqlTools.ServiceLayer.TestEnvConfig to write sqlConnectionSettings.json in the user profile. Use when: setting up local integration tests, configuring SQL test credentials, preparing SQLConnectionInstances xml, generating sqlConnectionSettings.json, or setting up test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig.'
---

# Setup integration test config

Set up local integration-test connection settings for SQL Tools Service by:

1. Preparing an XML file with connection/auth info for required test instances.
2. Running the `Microsoft.SqlTools.ServiceLayer.TestEnvConfig` project with that XML.
3. Producing `sqlConnectionSettings.json` at the user profile root and storing SQL passwords in credential storage.

## When to use

- User asks to set up integration test connection config.
- User needs `sqlConnectionSettings.json` for integration tests.
- User mentions `SQLConnectionInstancesTemplate.xml` or `TestEnvConfig`.

## Ground truth in repo

This workflow is implemented and exercised in:

- `test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/Program.cs`
- `test/Microsoft.SqlTools.ServiceLayer.Test.Common/TestConfigPersistenceHelper.cs`
- `.github/workflows/integration-tests.yml` and `azure-pipelines/integration-tests.yml`

Key behavior:

- XML instances are read from the file passed to `TestEnvConfig`.
- Each `<Instance>` must include `VersionKey` and `DataSource`.
- `UserId` and `Password` are optional.
- If environment variable `<VersionKey>_password` exists, it overrides `<Password>` from XML.
- Output is written to user root as `sqlConnectionSettings.json`.
- Passwords are removed from JSON and stored via test credential storage.

## Required interaction with user

Always ask this first:

- Do you already have credentials you want to use?
- Or do you want me to provide T-SQL to create a test login/user on your test server?

If they already have credentials:

- Ask for server host, auth type, username (if SQL auth), and whether they want one server for both `sqlOnPrem` and `sqlAzure` or separate servers.
- Ask if the user wants to enter a local-only test password via chat, or if it's a real password that needs to be entered by them manually in the XML.

If they want T-SQL:

- Provide SQL to create a login/user suitable for integration tests.
- Then continue with XML setup using that login.

## T-SQL template

Use this when user asks for SQL to create credentials. Replace placeholders first.

```sql
USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'testAccount')
BEGIN
    CREATE LOGIN [testAccount] WITH PASSWORD = N'<StrongPasswordHere>', CHECK_POLICY = OFF;
END
GO

-- CI currently grants sysadmin for broad integration test coverage.
-- If your environment requires least privilege, scope permissions accordingly.
ALTER SERVER ROLE [sysadmin] ADD MEMBER [testAccount];
GO
```

## XML template

Create a local file (for example `test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/SQLConnectionInstances.local.xml`):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Instances>
  <Instance VersionKey="sqlOnPrem">
    <DataSource>localhost</DataSource>
    <UserId>testAccount</UserId>
    <Password></Password>
  </Instance>
  <Instance VersionKey="sqlAzure">
    <DataSource>localhost</DataSource>
    <UserId>testAccount</UserId>
    <Password></Password>
  </Instance>
</Instances>
```

Notes:

- Keep `VersionKey` values as `sqlOnPrem` and `sqlAzure` unless tests are explicitly changed.
- For integrated auth, omit `UserId` and `Password`.
- Prefer env vars (`sqlOnPrem_password`, `sqlAzure_password`) over plaintext passwords.

## Generate the integration test settings

From repo root (all platforms):

```pwsh
dotnet run --project test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/Microsoft.SqlTools.ServiceLayer.TestEnvConfig.csproj -- test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/SQLConnectionInstances.local.xml
```

Optional password override via env vars (recommended):

PowerShell:

```pwsh
$env:sqlOnPrem_password = '<password>'
$env:sqlAzure_password = '<password>'
```

bash/zsh:

```bash
export sqlOnPrem_password='<password>'
export sqlAzure_password='<password>'
```

## Verification

- Confirm command prints completion message.
- Confirm profile-root file exists:
  - Windows: `$env:USERPROFILE/sqlConnectionSettings.json`
  - Linux/macOS: `$HOME/sqlConnectionSettings.json`
- Confirm generated JSON has connection entries and no plaintext password fields.

## Common pitfalls

- Missing `VersionKey` on an `<Instance>`.
- Using wrong env var names (must match `<VersionKey>_password`).
- Assuming password stays in JSON (it is intentionally removed and stored in credential storage).
- Forgetting to pass the XML path argument when running `TestEnvConfig`.

## Expected assistant behavior

- Ask the credentials-vs-T-SQL question first.
- If credentials path is chosen, gather non-secret values and generate XML.
- If T-SQL path is chosen, provide SQL first, then proceed to XML.
- Run `dotnet run ... TestEnvConfig.csproj -- <xml path>`.
- Report resulting `sqlConnectionSettings.json` path and what was generated.
