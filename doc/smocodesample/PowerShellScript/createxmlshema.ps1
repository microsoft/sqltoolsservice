<#
This code example shows how to create an XML schema by using the XmlSchemaCollection object.
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

#Reference the master database   
$db = $srv.Databases["master"]

#Create a new schema collection  
$xsc = New-Object -TypeName Microsoft.SqlServer.Management.SMO.XmlSchemaCollection -argumentlist $db,"SampleCollection"  
  
#Add the xml  
$dq = '"' # the double quote character  
$xsc.Text = "<schema xmlns=" + $dq + "http://www.w3.org/2001/XMLSchema" + $dq + "  xmlns:ns=" + $dq + "http://ns" + $dq + "><element name=" + $dq + "e" + $dq + " type=" + $dq + "dateTime" + $dq + "/></schema>"  
  
#Create the XML schema collection on the instance of SQL Server.  
$xsc.Create
