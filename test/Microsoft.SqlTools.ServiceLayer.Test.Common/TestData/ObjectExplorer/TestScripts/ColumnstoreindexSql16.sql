CREATE TABLE CS_Delay_Table1
(ProductKey [int] NOT NULL, 
OrderDateKey [int] NOT NULL, 
DueDateKey [int] NOT NULL, 
ShipDateKey [int] NOT NULL);
GO

CREATE CLUSTERED COLUMNSTORE INDEX CSI_1 ON CS_Delay_Table1
WITH (COMPRESSION_DELAY = 100 minutes);
GO

CREATE TABLE CS_Delay_Table2
(ProductKey [int] NOT NULL, 
OrderDateKey [int] NOT NULL, 
DueDateKey [int] NOT NULL, 
ShipDateKey [int] NOT NULL);
GO

CREATE CLUSTERED INDEX CI_Table2 ON CS_Delay_Table2 (ProductKey);
GO

CREATE NONCLUSTERED COLUMNSTORE INDEX CSI_2
ON CS_Delay_Table2
(OrderDateKey, DueDateKey, ShipDateKey)
WITH (COMPRESSION_DELAY = 200);
GO

CREATE TABLE CS_Delay_Table3
(ProductKey [int] NOT NULL, 
OrderDateKey [int] NOT NULL, 
DueDateKey [int] NOT NULL, 
ShipDateKey [int] NOT NULL,
INDEX CSI_3 CLUSTERED COLUMNSTORE WITH(COMPRESSION_DELAY = 50 minute));
GO