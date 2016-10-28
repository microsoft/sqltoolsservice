Add-Type -Path "..\packages\SharedManagementObjects.140.1.9\lib\netcoreapp1.0\Microsoft.SqlServer.Smo.dll"
Add-Type -Path "..\packages\SharedManagementObjects.140.1.9\lib\netcoreapp1.0\Microsoft.Data.Tools.DataSets.dll"

$srv = New-Object Microsoft.SqlServer.Management.Smo.Server("(local)")

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