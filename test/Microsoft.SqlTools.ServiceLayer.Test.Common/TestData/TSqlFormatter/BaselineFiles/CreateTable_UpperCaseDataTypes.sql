
-- this is a comment before create table
CREATE TABLE [SimpleTable]
(

    -- this is a comment before document
    [DocumentID] INT IDENTITY (1, 1) NOT NULL,

    -- this is a comment before Title
    [Title] NVARCHAR (50) NOT NULL,


    -- this is a comment before FileName
    [FileName] NVARCHAR (400) NOT NULL,

    -- this is a comment before FileExtension
    [FileExtension] NVARCHAR(8)
);
