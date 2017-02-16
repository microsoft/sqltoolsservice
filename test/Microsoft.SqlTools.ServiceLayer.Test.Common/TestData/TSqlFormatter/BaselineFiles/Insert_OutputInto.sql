
INSERT Production.ScrapReason
OUTPUT
    INSERTED.ScrapReasonID, INSERTED.Name, INSERTED.ModifiedDate
INTO @MyTableVar
VALUES
    (N'Operator error', GETDATE());

