USE AdventureWorks2008R2;
GO
-- Define the CTE expression name and column list.
WITH Sales_CTE (SalesOrderID)
AS
-- Define the CTE query.
(
    SELECT SalesOrderID
    FROM Sales.SalesOrderHeader
    WHERE SalesPersonID IS NOT NULL
)
-- Define the outer query referencing the CTE name.
SELECT COUNT(SalesOrderID) AS TotalSales
FROM Sales_CTE;
GO

