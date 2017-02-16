
-- this is a comment before create table
CREATE TABLE [SimpleTable]
(

    -- this is a comment before document
    [DocumentID] int IDENTITY (1, 1) NOT NULL,

    -- this is a comment before Title
    [Title] nvarchar (50) NOT NULL,


    -- this is a comment before FileName
    [FileName] nvarchar (400) NOT NULL,

    -- this is a comment before FileExtension
    [FileExtension] nvarchar(8)
);
