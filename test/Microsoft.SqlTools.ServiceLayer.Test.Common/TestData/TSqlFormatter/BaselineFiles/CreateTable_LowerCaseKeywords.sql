
-- this is a comment before create table
create table [SimpleTable]
(

    -- this is a comment before document
    [DocumentID] INT identity (1, 1) not null,

    -- this is a comment before Title
    [Title] NVARCHAR (50) not null,


    -- this is a comment before FileName
    [FileName] NVARCHAR (400) not null,

    -- this is a comment before FileExtension
    [FileExtension] nvarchar(8)
);
