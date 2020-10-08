//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    static class SqlConnectionHelperScripts
    {
        public const string EngineEdition = "SELECT SERVERPROPERTY('EngineEdition'), SERVERPROPERTY('productversion'), SERVERPROPERTY ('productlevel'), SERVERPROPERTY ('edition'), SERVERPROPERTY ('MachineName'), SERVERPROPERTY ('ServerName'), (SELECT CASE WHEN EXISTS (SELECT TOP 1 1 from [sys].[all_columns] WITH (NOLOCK) WHERE name = N'xml_index_type' AND OBJECT_ID(N'sys.xml_indexes') = object_id) THEN 1 ELSE 0 END AS SXI_PRESENT)";
        public const string EngineEditionWithLock = "SELECT SERVERPROPERTY('EngineEdition'), SERVERPROPERTY('productversion'), SERVERPROPERTY ('productlevel'), SERVERPROPERTY ('edition'), SERVERPROPERTY ('MachineName'), SERVERPROPERTY ('ServerName'), (SELECT CASE WHEN EXISTS (SELECT TOP 1 1 from [sys].[all_columns] WHERE name = N'xml_index_type' AND OBJECT_ID(N'sys.xml_indexes') = object_id) THEN 1 ELSE 0 END AS SXI_PRESENT)";

        public const string CheckDatabaseReadonly = @"EXEC sp_dboption '{0}', 'read only'";

        public const string GetDatabaseFilePathAndName = @"
DECLARE @filepath		nvarchar(260),
		@rc 			int

EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',N'Software\Microsoft\MSSQLServer\MSSQLServer',N'DefaultData', @filepath output, 'no_output' 

IF ((@filepath IS NOT NULL) AND (CHARINDEX(N'\', @filepath, len(@filepath)) = 0))
    SELECT @filepath = @filepath + N'\'

IF (@filepath IS NULL)
    SELECT	@filepath = [sdf].[physical_name]
	FROM	[master].[sys].[database_files] AS [sdf]
	WHERE	[file_id] = 1

SELECT @filepath AS FilePath
";

        public const string GetDatabaseLogPathAndName = @"
DECLARE @filepath		nvarchar(260),
		@rc 			int

EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',N'Software\Microsoft\MSSQLServer\MSSQLServer',N'DefaultLog', @filepath output, 'no_output' 

IF ((@filepath IS NOT NULL) AND (CHARINDEX(N'\', @filepath, len(@filepath)) = 0))
    SELECT @filepath = @filepath + N'\'

IF (@filepath IS NULL)
    SELECT	@filepath = [ldf].[physical_name]
	FROM	[master].[sys].[database_files] AS [ldf]
	WHERE	[file_id] = 2

SELECT @filepath AS FilePath
";
        /**
         * Query to get the server endpoints. We first try to query the dm_cluster_endpoints DMV since that will contain all
         * of the endpoints. But if that fails (such as if the user doesn't have VIEW SERVER STATE permissions) then we'll
         * fall back to just querying the ControllerEndpoint server property to at least get the endpoint of the controller
         * and rely on the caller to connect to the controller to query for any of the other endpoints it needs.
         */
        public const string GetClusterEndpoints = @"BEGIN TRY
SELECT [name], [description], [endpoint], [protocol_desc] FROM .[sys].[dm_cluster_endpoints]
END TRY
BEGIN CATCH
DECLARE @endpoint VARCHAR(max)
select @endpoint = CONVERT(VARCHAR(max),SERVERPROPERTY('ControllerEndpoint'))
SELECT 'controller' AS name, 'Cluster Management Service' AS description, @endpoint as endpoint, SUBSTRING(@endpoint, 0, CHARINDEX(':', @endpoint))
END CATCH
";
        public const string GetHostInfo = @"SELECT [host_platform], [host_distribution], [host_release], [host_service_pack_level], [host_sku], [os_language_version] FROM sys.dm_os_host_info";
        public const string GetHostWindowsVersion = @"SELECT windows_release FROM sys.dm_os_windows_info";
    }
}
