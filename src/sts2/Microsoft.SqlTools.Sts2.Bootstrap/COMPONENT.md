# Microsoft.SqlTools.Sts2.Bootstrap

**Role:** Composition root invoked by legacy Program.cs; owns --enable-sts2 / STS_ENABLE_STS2 activation and process wiring.

**Allowed dependencies:** Hosting, Runtime, Multiplexer, Drivers.SqlClient, Contracts; Drivers.Sqlite only when `IncludeSts2SqliteDriver=true`

**Forbidden:** legacy namespaces

See docs/sts2/SPEC.md SS4 for the authoritative dependency matrix.

The default and production publish graph excludes Drivers.Sqlite. Local and E2E builds may opt in with `-p:IncludeSts2SqliteDriver=true`.
