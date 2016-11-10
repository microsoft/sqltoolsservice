<#
This code example shows how to create an XML schema by using the XmlSchemaCollection object.
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

#Reference the AdventureWorks database   
$db = $srv.Databases["AdventureWorks"]

#Create a new schema collection  
$xsc = New-Object -TypeName Microsoft.SqlServer.Management.SMO.XmlSchemaCollection -argumentlist $db,"SampleCollection"  
  
#Add the xml  
$dq = '"' # the double quote character  
$xsc.Text = "<schema xmlns=" + $dq + "http://www.w3.org/2001/XMLSchema" + $dq + "  xmlns:ns=" + $dq + "http://ns" + $dq + "><element name=" + $dq + "e" + $dq + " type=" + $dq + "dateTime" + $dq + "/></schema>"  
  
#Create the XML schema collection on the instance of SQL Server.  
$xsc.Create
