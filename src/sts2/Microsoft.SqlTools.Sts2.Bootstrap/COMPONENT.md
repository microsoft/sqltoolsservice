# Microsoft.SqlTools.Sts2.Bootstrap

**Role:** Composition root invoked by legacy Program.cs; owns --enable-sts2 / STS_ENABLE_STS2 activation and process wiring.

**Allowed dependencies:** Hosting, Runtime, Multiplexer, Drivers.SqlClient, Drivers.Sqlite, Contracts

**Forbidden:** legacy namespaces

See docs/sts2/SPEC.md SS4 for the authoritative dependency matrix.

