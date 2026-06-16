-- Source for the selection-scope scenario. Two tables, each gaining a PK constraint
-- relative to the target. The test will IncludeOnly the OrdersScope table for
-- Generate Script and assert the generated script contains exactly one PRINT header
-- (for OrdersScope), zero ALTER TABLE statements for CustomersScope, and zero
-- PRINT N'Creating Primary Key [PK_CustomersScope]…' lines. This reproduces the
-- regression captured in Prototype 3 — that the original bug emitted 19 unrelated
-- PK statements when scripting a single table — and verifies the DacFx cascade fix
-- in PR 2143938 keeps the script scoped to the selected table.
CREATE TABLE [dbo].[CustomersScope]
(
    [CustomerId] INT NOT NULL,
    CONSTRAINT [PK_CustomersScope] PRIMARY KEY NONCLUSTERED ([CustomerId]) NOT ENFORCED
);
GO
CREATE TABLE [dbo].[OrdersScope]
(
    [OrderId] INT NOT NULL,
    [CustomerId] INT NOT NULL,
    CONSTRAINT [PK_OrdersScope] PRIMARY KEY NONCLUSTERED ([OrderId]) NOT ENFORCED
);
