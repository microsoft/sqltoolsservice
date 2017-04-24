SET WORKINGDIR=%~dp0
SET REPOROOT=%WORKINGDIR%..\..

REM clean-up results from previous run
RMDIR %WORKINGDIR%reports\ /S /Q
DEL %WORKINGDIR%coverage.xml
MKDIR reports

REM Setup repo base path

REM backup current project.json
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK

REM switch PDB type to Full since that is required by OpenCover for now
REM we should remove this step on OpenCover supports portable PDB
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json portable full

REM rebuild the SqlToolsService project
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json
dotnet build %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json %DOTNETCONFIG%
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json
dotnet build %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json %DOTNETCONFIG%
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json %DOTNETCONFIG% 

REM run the tests through OpenCover and generate a report
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver\project.json
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver\project.json %DOTNETCONFIG%
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.Test.Common\project.json
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.Test.Common\project.json %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\project.json
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\project.json %DOTNETCONFIG%
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\project.json
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\project.json %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\project.json
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\project.json %DOTNETCONFIG% 

SET TEST_SERVER=localhost
SET SQLTOOLSSERVICE_EXE=%REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\bin\Integration\netcoreapp1.0\win7-x64\Microsoft.SqlTools.ServiceLayer.exe
SET SERVICECODECOVERAGE=True
SET CODECOVERAGETOOL="%WORKINGDIR%packages\OpenCover.4.6.684\tools\OpenCover.Console.exe"
SET CODECOVERAGEOUTPUT=coverage.xml

dotnet.exe test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\project.json %DOTNETCONFIG%"

SET SERVICECODECOVERAGE=FALSE

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\project.json %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*" -output:coverage.xml -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\bin\Debug\netcoreapp1.0

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\project.json %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*" -output:coverage.xml -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\bin\Debug\netcoreapp1.0

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\project.json %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*" -output:coverage.xml -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\bin\Debug\netcoreapp1.0

REM Generate the report
"%WORKINGDIR%packages\OpenCoverToCoberturaConverter.0.2.4.0\tools\OpenCoverToCoberturaConverter.exe"  -input:coverage.xml -output:outputCobertura.xml -sources:%REPOROOT%\src\Microsoft.SqlTools.ServiceLayer
"%WORKINGDIR%packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe"  "-reports:coverage.xml" "-targetdir:%WORKINGDIR%\reports"

REM restore original project.json
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json
DEL %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\project.json.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json.BAK %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json
DEL %REPOROOT%\src\Microsoft.SqlTools.Credentials\project.json.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json.BAK %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json
DEL %REPOROOT%\src\Microsoft.SqlTools.Hosting\project.json.BAK 

EXIT
