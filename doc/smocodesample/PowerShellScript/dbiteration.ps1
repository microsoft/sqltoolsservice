<#
This script demonstrates iterations through the rows and display collation details for a remote or local instance of SQL Server.
#>

#DLL location needs to be specified
$pathtodll = "" 

Add-Type -Path "$pathtodll\Microsoft.SqlServer.Smo.dll"
Add-Type -Path "$pathtodll\Microsoft.SqlServer.ConnectionInfo.dll"

#Connection context need to be specified
$srv = New-Object Microsoft.SqlServer.Management.Smo.Server()
$srv.ConnectionContext.LoginSecure = $false
$srv.ConnectionContext.ServerInstance = "instance_name"
$srv.ConnectionContext.Login = "user_id"
$srv.ConnectionContext.Password = "pwd"

$datatable = $srv.EnumCollations()

Foreach ($row in $datatable.Rows)
{
   Write-Host "============================================"
   Foreach ($column in $row.Table.Columns)
   {
      Write-Host $column.ColumnName "=" $row[$column].ToString()
   }
}