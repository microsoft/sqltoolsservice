-- Source for the FOREIGN KEY scenario. Two tables — Orders references Customers via
-- a NOT ENFORCED FK. Pair with FabricForeignKey_Target.sql which omits the FK so it
-- appears as an Add child of the Orders table diff. Cross-table dependency ordering
-- must be honoured in the generated script.
CREATE TABLE [dbo].[CustomersFk]
(
    [CustomerId] INT NOT NULL,
    CONSTRAINT [PK_CustomersFk] PRIMARY KEY NONCLUSTERED ([CustomerId]) NOT ENFORCED
);
GO
CREATE TABLE [dbo].[OrdersFk]
(
    [OrderId] INT NOT NULL,
    [CustomerId] INT NOT NULL,
    CONSTRAINT [PK_OrdersFk] PRIMARY KEY NONCLUSTERED ([OrderId]) NOT ENFORCED,
    CONSTRAINT [FK_OrdersFk_CustomersFk] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[CustomersFk] ([CustomerId]) NOT ENFORCED
);
