# Microsoft.SqlTools.Sts2.Drivers.Sqlite

**Role:** Non-production real-I/O test driver adapter over Microsoft.Data.Sqlite.

**Allowed dependencies:** Abstractions, Contracts, Microsoft.Data.Sqlite, SQLitePCLRaw.lib.e_sqlite3

**Forbidden:** Core, Runtime, Hosting, legacy namespaces

The enforced dependency matrix is in [DependencyMatrixTests](../../../test/sts2/Microsoft.SqlTools.Sts2.UnitTests/Architecture/DependencyMatrixTests.cs) (I11); see the generated [component graph](../../../docs/sts2/COMPONENTS.md) for the references on disk.

This project is tested directly. It enters the ServiceLayer graph only when a local or test build sets `IncludeSts2SqliteDriver=true`.
