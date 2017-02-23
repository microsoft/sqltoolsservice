select *
from myTable
where ProcessDate = '20161009'
    and LoanChargeoffDate is not null --charged-off loans
    --and LoandChargeoffDate is null and LoanCloseDate IS NOT null
    and LoanCode IN (0,1) -- open and close ended loans only
    and LoanBalloonDate IS NULL -- exclude balloon loans
    and LoanType = 1 -- LoanType NOT IN (10, 51, 97) -- no lines of credit 
    -- and LoanCreditScore > 0
    and LoanTermMonths > 0
order by AddrChanged
