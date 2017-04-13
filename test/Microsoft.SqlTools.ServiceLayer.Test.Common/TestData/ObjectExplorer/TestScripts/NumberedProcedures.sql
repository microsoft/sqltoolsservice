CREATE PROCEDURE [dbo].[Encrypted_Additional_Numbered_Procedure]
( @param1 int)
WITH ENCRYPTION
AS
SELECT 1 AS Col1

GO


CREATE PROCEDURE [dbo].[Encrypted_Additional_Numbered_Procedure];2
( @param1 int)
WITH ENCRYPTION
AS
SELECT 2 AS Col1


GO

CREATE PROCEDURE [dbo].[Encrypted_Additional_Numbered_Procedure];3
( @param1 int)
WITH ENCRYPTION
AS
SELECT 3 AS Col1


GO

CREATE PROCEDURE [dbo].[Encrypted_Additional_Numbered_Procedure];4
( @param1 int)
WITH ENCRYPTION
AS
SELECT 4 AS Col1

GO

CREATE PROCEDURE [dbo].[Additional_Numbered_Procedure]
(@param1 int)
AS
SELECT 1 AS Col1

GO

CREATE PROCEDURE [dbo].[Additional_Numbered_Procedure];2
(@param1 int)
AS
SELECT 2 AS Col1

GO

CREATE PROCEDURE [dbo].[Additional_Numbered_Procedure];3
(@param1 int)
AS
SELECT 3 AS Col1

GO

CREATE PROCEDURE [dbo].[Additional_Numbered_Procedure];4
(@param1 int)
AS
SELECT 4 AS Col1


