USE AdventureWorks2008R2;
GO
-- Define the CTE expression name and column list.
WITH
    Sales_CTE
    (
        SalesPersonID,
        SalesOrderID,
        SalesYear
    )
    AS
    -- Define the CTE query.
    (
        SELECT
            SalesPersonID,
            SalesOrderID,
            YEAR(OrderDate) AS SalesYear
        FROM
            Sales.SalesOrderHeader
        WHERE SalesPersonID IS NOT NULL
    )
-- Define the outer query referencing the CTE name.
SELECT
    SalesPersonID,
    COUNT(SalesOrderID) AS TotalSales,
    SalesYear
FROM
    Sales_CTE
GROUP BY SalesYear, SalesPersonID
ORDER BY SalesPersonID, SalesYear;
GO

