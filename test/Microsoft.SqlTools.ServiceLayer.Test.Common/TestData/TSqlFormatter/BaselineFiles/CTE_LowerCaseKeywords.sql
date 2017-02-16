use AdventureWorks2008R2;
go
-- Define the CTE expression name and column list.
with
    Sales_CTE
    (
        SalesPersonID,
        SalesOrderID,
        SalesYear
    )
    as
    -- Define the CTE query.
    (
        select
            SalesPersonID,
            SalesOrderID,
            YEAR(OrderDate) as SalesYear
        from
            Sales.SalesOrderHeader
        where SalesPersonID is not null
    )
-- Define the outer query referencing the CTE name.
select
    SalesPersonID,
    COUNT(SalesOrderID) as TotalSales,
    SalesYear
from
    Sales_CTE
group by SalesYear, SalesPersonID
order by SalesPersonID, SalesYear;
go

