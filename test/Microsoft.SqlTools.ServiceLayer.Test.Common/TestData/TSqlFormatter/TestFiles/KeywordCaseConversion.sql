-- Case: common type keywords

cReaTe tAbLe [SimpleTable]
(

    -- this is a comment before document
    [DocumentID] INT iDentIty (1, 1) NOT NULL,

    -- this is a comment before Title
    [Title] NVARCHAR (50) NOT NULL,


    -- this is a comment before FileName
    [FileName] NVARCHAR (400) NOT NULL,

    -- this is a comment before FileExtension
    [FileExtension] nvarchar(8)
);
GO
CREATE vIEw v1 aS select * from [SimpleTable]
GO
CREATE pRoceduRe p1 aS 
bEgIn
select * from [SimpleTable]
eNd
GO
iNsert iNto t dEfault vAlues
GO

GO
iNsert oPenQUERY (OracleSvr, 'SELECT name FROM joe.titles')  
VALUES ('NewTitle');  
GO
INSERT INTO myTable(FileName, FileType, Document)   
   SELECT 'Text1.txt' AS FileName,   
      '.txt' AS FileType,   
      * FROM openROWSET(bUlK N'C:\Text1.txt', siNGLE_BLOB) AS Document;  
GO
sELECT    *  
FROM       openXML (@idoc, '/ROOT/Customers')   
exEC sp_xml_removedocument @idoc;  