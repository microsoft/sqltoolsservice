USE master
GO

DECLARE @dbname nvarchar(128)
SET @dbname = N'#DatabaseName#'

IF NOT(EXISTS (SELECT name 
FROM master.dbo.sysdatabases 
WHERE ('[' + name + ']' = @dbname 
OR name = @dbname)))
BEGIN
	CREATE DATABASE #DatabaseName#
END