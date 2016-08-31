SET WORKINGDIR=%~dp0
rmdir %WORKINGDIR%reports\ /S /Q
del %WORKINGDIR%coverage.xml
mkdir reports
COPY /Y %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK
cscript /nologo ReplaceText.vbs %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json portable full
dotnet build %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json
"%WORKINGDIR%packages\OpenCover.4.6.519\tools\OpenCover.Console.exe" -register:user -target:dotnet.exe -targetargs:"test %WORKINGDIR%..\Microsoft.SqlTools.ServiceLayer.Test\project.json" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]*" -output:coverage.xml -searchdirs:%WORKINGDIR%..\Microsoft.SqlTools.ServiceLayer.Test\bin\Debug\netcoreapp1.0
"%WORKINGDIR%packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe"  "-reports:coverage.xml" "-targetdir:%WORKINGDIR%\reports"
COPY /Y %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json
EXIT
