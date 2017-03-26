use WideWorldImporters;
go

alter database current collate Latin1_General_100_CI_AS;
go

alter database current set RECOVERY SIMPLE;
go

alter database current set AUTO_UPDATE_STATISTICS_ASYNC on;
go
