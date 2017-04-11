-- create signature
ADD SIGNATURE TO [Procedure1] 
    BY CERTIFICATE [Certificate1] 
    WITH PASSWORD = 'pGFD4bb925DGvbd2439587y' ;
GO
--Create a queue to receive messages.
CREATE QUEUE NotifyQueue ;
GO
--Create a service on the queue that references
--the event notifications contract.
CREATE SERVICE NotifyService
ON QUEUE NotifyQueue
([http://schemas.microsoft.com/SQL/Notifications/PostEventNotification]);
GO
--Create the event notification on queue.
CREATE EVENT NOTIFICATION Notify_ALTER_T1
ON QUEUE notifyqueue
FOR QUEUE_ACTIVATION
TO SERVICE 'NotifyService',
'8140a771-3c4b-4479-8ac0-81008ab17984';
GO
--Create the event notification on database
CREATE EVENT NOTIFICATION Notify_ALTER_T1
ON DATABASE
FOR ALTER_TABLE
TO SERVICE 'NotifyService',
    '8140a771-3c4b-4479-8ac0-81008ab17984';
GO

CREATE FUNCTION [dbo].[TableFunctionWithComputedColumns] 
(
    -- Add the parameters for the function here
    @p1 int = 2, 
    @p2 nchar(10) = NUll
)
RETURNS 
@Table_Var TABLE 
(
    -- Add the column definitions for the TABLE variable here
    c1 int, 
    c2 nchar(10),
    c3 AS 1 * 3
)
AS
BEGIN
    -- Fill the table variable with the rows for your result set
    INSERT INTO @Table_Var
    SELECT a.column_1, a.column_2
    FROM Table_1 a
    WHERE a.column_1 > 5

    INSERT INTO @Table_Var
    SELECT column_1, 'From 2'
    FROM Table_2 
    WHERE @p1 > column_1

    RETURN 
END
GO

CREATE FUNCTION [dbo].[TableFunctionWithComputedColumnsEncrypted] 
(
    -- Add the parameters for the function here
    @p1 int = 2, 
    @p2 nchar(10)
)
RETURNS 
@Table_Var TABLE 
(
    -- Add the column definitions for the TABLE variable here
    c1 int, 
    c2 nchar(10),
    c3 AS 1 * 3
)
WITH ENCRYPTION
AS
BEGIN
    -- Fill the table variable with the rows for your result set
    INSERT INTO @Table_Var
    SELECT a.column_1, a.column_2
    FROM Table_1 a
    WHERE a.column_1 > 5

    INSERT INTO @Table_Var
    SELECT column_1, 'From 2'
    FROM Table_2 
    WHERE @p1 > column_1

    RETURN 
END
GO

Create table [dbo].[referenced_table] (C1 int, C2 int);

GO

CREATE PROCEDURE GetReferenedTable
AS
BEGIN
SELECT * from [dbo].[referenced_table];
END
GO
exec sp_addextendedproperty  N'microsoft_database_tools_support', 'GetReferenedTable',  N'SCHEMA',  'dbo', N'PROCEDURE' ,'GetReferenedTable'
GO
DISABLE TRIGGER [Trigger_1]
    ON DATABASE;
GO

CREATE VIEW [dbo].[View_2] (c1)
AS
SELECT     column_1 as c1
FROM         dbo.Table_1

GO

exec sp_addextendedproperty 'prop_ex', 'Table_1', 'SCHEMA', 'dbo', 'TABLE', 'Table_1'

GO

exec sp_addextendedproperty 'prop_ex', 'column_1', 'SCHEMA', 'dbo', 'TABLE', 'Table_1', 'COLUMN', 'column_1'
GO

CREATE TABLE dbo.MultipleIndexTable
( [c1] INT NOT NULL CHECK (c1 > 0),
[c2] int default 10 null,
PRIMARY KEY NONCLUSTERED (c1 ASC),
UNIQUE CLUSTERED (c1 ASC, c2 DESC)
)

GO

CREATE TRIGGER [Trigger_2] 
ON DATABASE 
FOR DROP_TABLE
AS 
   SELECT COUNT(column_1) from dbo.Table_1
   RAISERROR ('You must disable Trigger "Trigger_1" to drop synonyms!',10, 1)
   ROLLBACK

GO

SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO

DISABLE TRIGGER [Trigger_1] ON DATABASE
GO

GO

CREATE TABLE dbo.Table_3
(
  c1 int,
  c2 int,
) ON PartitionScheme(c1)

GO
CREATE TABLE [dbo].[Different_WithAppend_Table](
[Id] [int] IDENTITY(1,1) NOT NULL,
[Col] [char](1) NULL
) ON [PRIMARY]

GO

CREATE FUNCTION [dbo].[EncryptedFunctionWithConstraints]
(@p1 INT)
RETURNS 
    @GeneratedTableName TABLE (
        [c0] INT NOT NULL PRIMARY KEY,
        [c1] INT DEFAULT ((1)) NULL,
        [c2] NCHAR (10) NULL,
        [c3] INT UNIQUE ,
        CHECK ([c1]>(0)))
WITH ENCRYPTION
AS
BEGIN
    insert into @GeneratedTableName values (1,1, 'abc',1);
    RETURN
END
GO
CREATE TABLE [[] (c1 int)
GO
CREATE TABLE []]] (c1 int)
GO  
CREATE TABLE [asdf'[] (c1 int)
GO
CREATE TABLE [噂構申表5] (c1 int)
GO
-- Casing of NULL is explicit 'NUll'
CREATE PROC CasingOnDefaultValue @param1 int = NUll, @param2 nvarchar(123) = N'abc'
AS
BEGIN
  select 1 as a
END
-- permissions 
GO
CREATE USER nologon4 without login
GO
GRANT VIEW DEFINITION ON CasingOnDefaultValue to nologon4
GO
CREATE USER granter without login
GO
GRANT CONNECT TO granter WITH GRANT OPTION;
GO
DENY CONNECT TO nologon4 CASCADE AS granter;
GO
GRANT VIEW DEFINITION ON [噂構申表5] to nologon4
GO
GRANT VIEW DEFINITION ON [[] TO nologon4
GO
GRANT VIEW DEFINITION ON []]] TO nologon4
GO
GRANT VIEW DEFINITION ON [asdf'[] TO nologon4
GO
GRANT SELECT ON dbo.Table_1 to nologon4
GO
GRANT SELECT ON dbo.Table_2 to nologon4
GO
REVOKE SELECT ON dbo.Table_2(column_2) TO nologon4
GO
GRANT SELECT ON dbo.View_1 to nologon4
GO
GRANT SELECT ON dbo.EncryptedView(A) to nologon4
GO
GRANT EXECUTE ON dbo.Procedure1 TO nologon4
GO
GRANT EXECUTE ON dbo.CLR_SimpleResultsetProcedure TO nologon4
GO
GRANT EXECUTE ON dbo.EncryptedProcedure TO nologon4
GO
GRANT VIEW DEFINITION ON CERTIFICATE :: Certificate1 TO nologon4
GO
GRANT EXECUTE ON dbo.ScalarFunction1 TO nologon4
GO
GRANT EXECUTE ON dbo.EncryptedFunction TO nologon4
GO
GRANT SELECT ON dbo.InlineFunction_1 TO nologon4
GO
GRANT SELECT ON dbo.TableFunction1 TO nologon4
GO
GRANT SELECT ON dbo.CLRTableValueFunction TO nologon4
GO
GRANT VIEW DEFINITION ON TYPE::dbo.dataType To nologon4
GO
GRANT VIEW DEFINITION ON FULLTEXT CATALOG ::FullTextCatalog1 To nologon4
GO
GRANT VIEW DEFINITION ON  XML SCHEMA COLLECTION :: dbo.XmlSchemaCollection To nologon4
GO
GRANT VIEW DEFINITION ON  ASSEMBLY :: [Geometry] To nologon4
GO
GRANT VIEW DEFINITION ON   TYPE:: dbo.Angle To nologon4
GO
GRANT VIEW DEFINITION ON  dbo.[Concat] To nologon4
GO
GRANT VIEW DEFINITION ON  dbo.Synonym_1 To nologon4
GO
GRANT VIEW DEFINITION ON  SCHEMA :: Schema1 To nologon4
GO
GRANT VIEW DEFINITION ON  SYMMETRIC  KEY :: SymKey1 To nologon4
GO
GRANT VIEW DEFINITION ON  ASYMMETRIC KEY :: AsmKey1 To nologon4
GO
GRANT VIEW DEFINITION ON  dbo.Queue1 To nologon4
GO
GRANT VIEW DEFINITION ON  dbo.NotifyQueue To nologon4
GO
GRANT VIEW DEFINITION ON  SERVICE :: Service1 To nologon4
GO
GRANT VIEW DEFINITION ON  SERVICE :: NotifyService To nologon4
GO
GRANT VIEW DEFINITION ON  CONTRACT :: Contract1 To nologon4
GO
GRANT VIEW DEFINITION ON  MESSAGE TYPE :: MessageType1 To nologon4
GO
GRANT VIEW DEFINITION ON  ROUTE :: AutoCreatedLocal To nologon4
GO
GRANT VIEW DEFINITION ON  ROUTE :: Route1 To nologon4
GO
GRANT VIEW DEFINITION ON  REMOTE  SERVICE BINDING :: ServiceBinding1 To nologon4
GO
GRANT SELECT ON  dbo.referenced_table To  nologon4
GO 
GRANT SELECT ON  dbo.TableFunctionWithComputedColumns  To nologon4 
GO
GRANT SELECT ON  dbo.TableFunctionWithComputedColumnsEncrypted   To nologon4 
GO
GRANT SELECT ON dbo.View_2 TO nologon4
GO
GRANT SELECT ON dbo.MultipleIndexTable TO nologon4
GO
GRANT SELECT ON dbo.Table_3 TO nologon4
GO
GRANT SELECT ON dbo.Different_WithAppend_Table TO nologon4
GO
GRANT SELECT ON dbo.[EncryptedFunctionWithConstraints] TO nologon4
GO