CREATE TABLE [dbo].[CustomersScope]
(
    [CustomerId] INT NOT NULL
);
GO
CREATE TABLE [dbo].[OrdersScope]
(
    [OrderId] INT NOT NULL,
    [CustomerId] INT NOT NULL
);
