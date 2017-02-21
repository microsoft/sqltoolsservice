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
go
create view v1
as
    select *
    from [SimpleTable]
go
create procedure p1
as
begin
    select *
    from [SimpleTable]
end
go
insert into t
default values
go

go
insert openquery (OracleSvr, 'SELECT name FROM joe.titles')
values
    ('NewTitle');  
go
insert into myTable
    (FileName, FileType, Document)
select 'Text1.txt' as FileName,
    '.txt' as FileType,
    *
from openrowset(bulk N'C:\Text1.txt', siNGLE_BLOB) as Document;  
go
select *
from openxml (@idoc, '/ROOT/Customers')
exec sp_xml_removedocument @idoc;  