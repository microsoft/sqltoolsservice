SET WORKINGDIR=%~dp0
SET REPOROOT=%WORKINGDIR%..\..

REM clean-up results from previous run
RMDIR %WORKINGDIR%reports\ /S /Q
DEL %WORKINGDIR%coverage.xml
MKDIR reports

REM Setup repo base path

REM backup current CSPROJ files
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj.BAK

REM switch PDB type to Full since that is required by OpenCover for now
REM we should remove this step on OpenCover supports portable PDB
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj portable full

REM rebuild the SqlToolsService project
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj %DOTNETCONFIG%
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj  %DOTNETCONFIG%
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj %DOTNETCONFIG% -r win7-x64
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj %DOTNETCONFIG% -r win7-x64
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj %DOTNETCONFIG% -r win7-x64
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj %DOTNETCONFIG% -r win7-x64
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj %DOTNETCONFIG% -r win7-x64

REM run the tests through OpenCover and generate a report
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver\Microsoft.SqlTools.ServiceLayer.TestDriver.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver\Microsoft.SqlTools.ServiceLayer.TestDriver.csproj %DOTNETCONFIG% -r win7-x64
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.Test.Common\Microsoft.SqlTools.ServiceLayer.Test.Common.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.Test.Common\Microsoft.SqlTools.ServiceLayer.Test.Common.csproj %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\Microsoft.SqlTools.ServiceLayer.UnitTests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\Microsoft.SqlTools.ServiceLayer.UnitTests.csproj %DOTNETCONFIG%
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.csproj %DOTNETCONFIG% 

SET TEST_SERVER=localhost
SET SQLTOOLSSERVICE_EXE=%REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\bin\Integration\netcoreapp2.2\win7-x64\MicrosoftSqlToolsServiceLayer.exe
SET SERVICECODECOVERAGE=True
SET CODECOVERAGETOOL="%WORKINGDIR%packages\OpenCover.4.6.684\tools\OpenCover.Console.exe"
SET CODECOVERAGEOUTPUT=coverage.xml

REM dotnet.exe test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj %DOTNETCONFIG%"

SET SERVICECODECOVERAGE=FALSE

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* +[MicrosoftSqlToolsServiceLayer*]* +[MicrosoftSqlToolsCredentials*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]* -[Microsoft.SqlTools.ServiceLayer.*Test*]* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Agent.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.DisasterRecovery.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Profiler.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Admin.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Management.*" -output:coverage.xml -hideskipped:All -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\bin\Debug\netcoreapp2.2

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\Microsoft.SqlTools.ServiceLayer.UnitTests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* +[MicrosoftSqlToolsServiceLayer*]* +[MicrosoftSqlToolsCredentials*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.*Test*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*  -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Agent.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.DisasterRecovery.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Profiler.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Admin.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Management.*" -output:coverage.xml -hideskipped:All -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\bin\Debug\netcoreapp2.2

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* +[MicrosoftSqlToolsServiceLayer*]* +[MicrosoftSqlToolsCredentials*]* [Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]* -[Microsoft.SqlTools.ServiceLayer.*Test*]*  -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Agent.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.DisasterRecovery.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Profiler.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Admin.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Management.*" -output:coverage.xml -hideskipped:All -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\bin\Debug\netcoreapp2.2

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* +[MicrosoftSqlToolsServiceLayer*]* +[MicrosoftSqlToolsCredentials*]* [Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]* -[Microsoft.SqlTools.ServiceLayer.*Test*]*  -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Agent.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.DisasterRecovery.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Profiler.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Admin.* -[MicrosoftSqlToolsServiceLayer]Microsoft.SqlTools.ServiceLayer.Management.*" -output:coverage.xml -hideskipped:All -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ManagedBatchParser.IntegrationTests\bin\Debug\netcoreapp2.2

REM Generate the report
"%WORKINGDIR%packages\OpenCoverToCoberturaConverter.0.2.4.0\tools\OpenCoverToCoberturaConverter.exe"  -input:coverage.xml -output:outputCobertura.xml -sources:%REPOROOT%\src\Microsoft.SqlTools.ServiceLayer
"%WORKINGDIR%packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe"  "-reports:coverage.xml" "-targetdir:%WORKINGDIR%\reports"

REM restore original project.json

COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.ManagedBatchParser\Microsoft.SqlTools.ManagedBatchParser.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.Core\Microsoft.SqlTools.ResourceProvider.Core.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.ResourceProvider.DefaultImpl\Microsoft.SqlTools.ResourceProvider.DefaultImpl.csproj.BAK 

EXIT