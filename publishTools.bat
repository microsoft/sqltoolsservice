REM --configuration=retail to build retail bits
dotnet publish --runtime=win7-x64  src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj
dotnet publish --runtime=win7-x64  src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj
dotnet publish --runtime=win7-x64  src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj
REM D:\src\sqltoolsservice\src\Microsoft.SqlTools.ResourceProvider\bin\Debug\netcoreapp2.1\win7-x64\publish\
REM D:\src\sqltoolsservice\src\Microsoft.SqlTools.Credentials\bin\Debug\netcoreapp2.2\win7-x64\publish\
rem D:\src\sqltoolsservice\src\Microsoft.SqlTools.ServiceLayer\bin\Debug\netcoreapp2.2\win7-x64\publish\
rem D:\src\sqltoolsservice\src\Microsoft.SqlTools.ServiceLayer\bin\Debug\netcoreapp2.2\win7-x64\publish\
