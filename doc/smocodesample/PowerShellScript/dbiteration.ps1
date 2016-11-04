<#
This script demonstrates iterations through the rows and display collation details for a local instance of SQL Server.
#>

#Enter DLL location on local machine
$pathtodll = "" 

Add-Type -Path "$pathtodll\Microsoft.SqlServer.Smo.dll"
Add-Type -Path "$pathtodll\Microsoft.Data.Tools.DataSets.dll"

$srv = New-Object Microsoft.SqlServer.Management.Smo.Server("(local)")

$d = $srv.EnumCollations()

Foreach ($r in $d.Rows)
{
   Write-Host "============================================"
   Foreach ($c in $r.Table.Columns)
   {
      Write-Host $c.ColumnName "=" $r[$c].ToString()
   }
}