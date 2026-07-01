-- Source for the PRIMARY KEY scenario. Pair with FabricPrimaryKey_Target.sql which
-- has no PK so the PK appears as an Add child of the parent table's diff. PKs in
-- Fabric Warehouse must be NONCLUSTERED NOT ENFORCED (the only supported form).
CREATE TABLE [dbo].[CustomersPk]
(
    [CustomerId] INT NOT NULL,
    [Email] VARCHAR(100) NOT NULL,
    CONSTRAINT [PK_CustomersPk] PRIMARY KEY NONCLUSTERED ([CustomerId]) NOT ENFORCED
);
