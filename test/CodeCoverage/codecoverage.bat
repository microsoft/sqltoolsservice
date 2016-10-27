SET WORKINGDIR=%~dp0

REM clean-up results from previous run
RMDIR %WORKINGDIR%reports\ /S /Q
DEL %WORKINGDIR%coverage.xml
MKDIR reports

REM backup current project.json
COPY /Y %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK

REM switch PDB type to Full since that is required by OpenCover for now
REM we should remove this step on OpenCover supports portable PDB
cscript /nologo ReplaceText.vbs %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json portable full

REM rebuild the SqlToolsService project
dotnet build %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json 

REM run the tests through OpenCover and generate a report
dotnet build %WORKINGDIR%..\..\test\Microsoft.SqlTools.ServiceLayer.Test\project.json %DOTNETCONFIG% 
"%WORKINGDIR%packages\OpenCover.4.6.519\tools\OpenCover.Console.exe" -register:user -target:dotnet.exe -targetargs:"test %WORKINGDIR%..\Microsoft.SqlTools.ServiceLayer.Test\project.json %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]*" -output:coverage.xml -searchdirs:%WORKINGDIR%..\Microsoft.SqlTools.ServiceLayer.Test\bin\Debug\netcoreapp1.0
"%WORKINGDIR%packages\OpenCoverToCoberturaConverter.0.2.4.0\tools\OpenCoverToCoberturaConverter.exe"  -input:coverage.xml -output:outputCobertura.xml -sources:%WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer
"%WORKINGDIR%packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe"  "-reports:coverage.xml" "-targetdir:%WORKINGDIR%\reports"

REM restore original project.json
COPY /Y %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json
DEL %WORKINGDIR%..\..\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK 
EXIT
