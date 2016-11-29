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

$d = $srv.EnumCollations()

Foreach ($r in $d.Rows)
{
   Write-Host "============================================"
   Foreach ($c in $r.Table.Columns)
   {
      Write-Host $c.ColumnName "=" $r[$c].ToString()
   }
}