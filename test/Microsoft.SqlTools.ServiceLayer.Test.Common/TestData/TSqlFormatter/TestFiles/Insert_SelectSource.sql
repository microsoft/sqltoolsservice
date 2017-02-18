
INSERT INTO myTable(FileName, FileType, Document) --comment
   SELECT 'Text1.txt' AS FileName,       '.txt' AS FileType, 
      * FROM OPENROWSET(BULK N'C:\Text1.txt', SINGLE_BLOB) AS Document;
