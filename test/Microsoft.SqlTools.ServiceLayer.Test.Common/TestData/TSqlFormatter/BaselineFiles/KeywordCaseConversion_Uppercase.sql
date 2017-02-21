-- Case: common type keywords

CREATE TABLE [SimpleTable]
(

    -- this is a comment before document
    [DocumentID] INT IDENTITY (1, 1) NOT NULL,

    -- this is a comment before Title
    [Title] NVARCHAR (50) NOT NULL,


    -- this is a comment before FileName
    [FileName] NVARCHAR (400) NOT NULL,

    -- this is a comment before FileExtension
    [FileExtension] nvarchar(8)
);
GO
CREATE VIEW v1
AS
    SELECT *
    FROM [SimpleTable]
GO
CREATE PROCEDURE p1
AS
BEGIN
    SELECT *
    FROM [SimpleTable]
END
GO
INSERT INTO t
DEFAULT VALUES
GO

GO
INSERT OPENQUERY (OracleSvr, 'SELECT name FROM joe.titles')
VALUES
    ('NewTitle');  
GO
INSERT INTO myTable
    (FileName, FileType, Document)
SELECT 'Text1.txt' AS FileName,
    '.txt' AS FileType,
    *
FROM OPENROWSET(BULK N'C:\Text1.txt', siNGLE_BLOB) AS Document;  
GO
SELECT *
FROM OPENXML (@idoc, '/ROOT/Customers')
EXEC sp_xml_removedocument @idoc;  