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
    CONSTRAINT [PK_OrdersFk] PRIMARY KEY NONCLUSTERED ([OrderId]) NOT ENFORCED
);
