<#
This script demonstrates iterations through the rows and display collation details for a remote or local instance of SQL Server.
#>

#DLL location needs to be specified
$pathtodll = "" 

Add-Type -Path "$pathtodll\Microsoft.SqlServer.Smo.dll"
Add-Type -Path "$pathtodll\Microsoft.SqlServer.ConnectionInfo.dll"

#Connection information needs to be specified
$connectionString = "Data Source=remote_server_name;User Id=user_id;Password=pwd;Initial Catalog=db_name"
$sqlConn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection = New-Object Microsoft.SqlServer.Management.Common.ServerConnection($sqlConn)
$srv = New-Object Microsoft.SqlServer.Management.Smo.Server($connection)

$d = $srv.EnumCollations()

Foreach ($r in $d.Rows)
{
   Write-Host "============================================"
   Foreach ($c in $r.Table.Columns)
   {
      Write-Host $c.ColumnName "=" $r[$c].ToString()
   }
}