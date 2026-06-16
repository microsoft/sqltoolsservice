-- Source for the UNIQUE constraint scenario. Pair with FabricUnique_Target.sql: target
-- omits the UQ so the constraint appears as an Add child of the parent table's diff.
-- Unique constraints in Fabric Warehouse must be NONCLUSTERED NOT ENFORCED.
CREATE TABLE [dbo].[ProductsUq]
(
    [ProductId] INT NOT NULL,
    [Sku] VARCHAR(50) NOT NULL,
    CONSTRAINT [UQ_ProductsUq_Sku] UNIQUE NONCLUSTERED ([Sku]) NOT ENFORCED
);
