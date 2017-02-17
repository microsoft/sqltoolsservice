
INSERT Production.ScrapReason
/*comments like this one*/
OUTPUT
    INSERTED.ScrapReasonID, INSERTED.Name, INSERTED.ModifiedDate
INTO @MyTableVar
VALUES
    (N'Operator error',/*comments like this one*/ GETDATE());
