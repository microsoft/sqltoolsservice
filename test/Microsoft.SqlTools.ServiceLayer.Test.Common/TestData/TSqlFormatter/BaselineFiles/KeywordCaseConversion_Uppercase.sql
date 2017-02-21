-- Case: common type keywords

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
GO
create view v1
as
    select *
    from [SimpleTable]
GO
create procedure p1
as
bEgIn
    select *
    from [SimpleTable]
eNd
GO
insert into t
default values
GO


