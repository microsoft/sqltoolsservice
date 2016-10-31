<#
This script demonstrates iterations through the rows and display collation details for a remote instance of SQL Server.
For remote connection, System.Data.SqlClient.dll is required, you can find and download it from nuget.org.
substitute %PathToDLL% as DLL location on local machine
#>

Add-Type -Path "%PathToDLL%\Microsoft.SqlServer.Smo.dll"
Add-Type -Path "%PathToDLL%\Microsoft.Data.Tools.DataSets.dll"
Add-Type -Path "%PathToDLL%\System.Data.SqlClient.dll"
Add-Type -Path "%PathToDLL%\Microsoft.SqlServer.ConnectionInfo.dll"

$connectionString = "Data Source=remote_server_name;User Id=user_id;Password=pwd;Initial Catalog=database_name"

$connectionBuilder = New-Object -TypeName System.Data.SqlClient.SqlConnectionStringBuilder -argumentlist $connectionString

$sqlConn = New-Object -TypeName System.Data.SqlClient.SqlConnection -argumentlist $connectionBuilder.ToString()

$connection = New-Object -TypeName Microsoft.SqlServer.Management.Common.ServerConnection -argumentlist $sqlConn

$srv = New-Object -TypeName Microsoft.SqlServer.Management.Smo.Server -argumentlist $connection

$d = New-Object Microsoft.Data.Tools.DataSets.DataTable
$d = $srv.EnumCollations

Foreach ($r in $d.Rows)
{
   Write-Host "============================================"
   Foreach ($c in $d.Columns)
   {
      Write-Host $c.ColumnName "=" $r[$c]
   }
}