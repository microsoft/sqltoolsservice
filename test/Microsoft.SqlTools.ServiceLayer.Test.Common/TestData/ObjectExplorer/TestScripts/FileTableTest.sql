
CREATE DATABASE [$(DatabaseName)]
  
GO
ALTER DATABASE [$(DatabaseName)] SET TARGET_RECOVERY_TIME = 1 MINUTES
GO
USE [$(DatabaseName)]

GO
ALTER DATABASE [$(DatabaseName)] 
SET FILESTREAM( 
	NON_TRANSACTED_ACCESS = FULL, 
	DIRECTORY_NAME = N'$(DatabaseName)' 
 ) WITH NO_WAIT

GO

ALTER DATABASE $(DatabaseName)
	ADD FILEGROUP [FileGroup1]
  CONTAINS FILESTREAM

GO

ALTER DATABASE $(DatabaseName)
ADD FILE
  (NAME = 'FileTableFile'
   , FILENAME = '$(DefaultDataPath)$(DatabaseName)_FT'
   )
TO FILEGROUP [FileGroup1]

GO



CREATE TABLE [dbo].[FileTablePass] AS FILETABLE WITH(
FileTable_Directory = 'docs',
FILETABLE_PRIMARY_KEY_CONSTRAINT_NAME=MyPk,
FILETABLE_STREAMID_UNIQUE_CONSTRAINT_NAME=MyStreamUQ,
FILETABLE_FULLPATH_UNIQUE_CONSTRAINT_NAME=MyPathUQ)

GO
ALTER TABLE [dbo].[FileTablePass]  WITH CHECK ADD  CONSTRAINT [MyCheck] CHECK   ((stream_id IS NOT NULL))
GO
ALTER TABLE [dbo].[FileTablePass] ADD  CONSTRAINT [MyDefault]  DEFAULT ((NULL)) FOR [name]
GO
ALTER TABLE [dbo].[FileTablePass] ADD  CONSTRAINT [MyQU] UNIQUE NONCLUSTERED ([name] ASC)
GO
ALTER TABLE [dbo].[FileTablePass]  WITH CHECK ADD  CONSTRAINT [MyFk] FOREIGN KEY([parent_path_locator])
REFERENCES [dbo].[FileTablePass] ([path_locator])
GO
CREATE TABLE [dbo].[t2] (
    [c1] INT NOT NULL,
    [c2] INT DEFAULT ((1)) NULL,
	[c3] INT DEFAULT ((1)) NOT NULL,
    [path_locator] hierarchyid,
    PRIMARY KEY CLUSTERED ([c1] ASC),
    UNIQUE NONCLUSTERED ([c2] ASC),
    CHECK ([c2] > (0))
);

GO
CREATE STATISTICS stat1
    ON dbo.[FileTablePass](stream_id)
WITH SAMPLE 50 PERCENT;
GO
CREATE INDEX IX_FileTablePass_Stream_id
    ON dbo.FileTablePass(stream_id); 
GO

CREATE TRIGGER FileTableTrigger
ON dbo.FileTablePass
AFTER INSERT 
AS RAISERROR ('Block insert', 16, 10);
GO
CREATE INDEX IX_T2_C3 
    ON [dbo].[t2](c3); 

GO

CREATE TRIGGER reminder2
ON dbo.t2
AFTER INSERT, UPDATE, DELETE 
AS
PRINT 'reminder trigger';

GO

ALTER TABLE dbo.t2 
ADD CONSTRAINT FK_TO_FILETABLE FOREIGN KEY (path_locator)
    REFERENCES FileTablePass (path_locator) ;
GO
exec sp_addextendedproperty 'prop_ex', 'FileTable', 'SCHEMA', 'dbo', 'TABLE', 'FileTablePass'
exec sp_addextendedproperty 'prop_ex', 'MyPk', 'SCHEMA', 'dbo', 'TABLE', 'FileTablePass', 'CONSTRAINT', 'MyPk'
exec sp_addextendedproperty 'prop_ex', 'MyStreamUQ', 'SCHEMA', 'dbo', 'TABLE', 'FileTablePass', 'CONSTRAINT', 'MyStreamUQ'
exec sp_addextendedproperty 'prop_ex', 'MyPathUQ', 'SCHEMA', 'dbo', 'TABLE', 'FileTablePass', 'CONSTRAINT', 'MyPathUQ'

GO

CREATE USER test_user WITHOUT LOGIN
GO

GRANT SELECT ON dbo.FileTablePass TO test_user
GO