SET WORKINGDIR=%~dp0
SET _TargetLocation=%1
SET _BuildConfiguration=%2
IF [%_BuildConfiguration%] NEQ []  GOTO Start
SET _BuildConfiguration=Debug

:Start
SET _PerfTestSourceLocation="%WORKINGDIR%\test\Microsoft.SqlTools.ServiceLayer.PerfTests\bin\%_BuildConfiguration%\netcoreapp2.1\win7-x64\publish"
SET _ServiceSourceLocation="%WORKINGDIR%\src\Microsoft.SqlTools.ServiceLayer\bin\%_BuildConfiguration%\netcoreapp2.1\win7-x64\publish"



dotnet publish %WORKINGDIR%test\Microsoft.SqlTools.ServiceLayer.PerfTests -c %_BuildConfiguration% -r win7-x64
dotnet publish %WORKINGDIR%src\Microsoft.SqlTools.ServiceLayer -c %_BuildConfiguration% -r win7-x64
		
XCOPY /i /E /y %_PerfTestSourceLocation% "%_TargetLocation%\Tests"
XCOPY /i /E /y %_ServiceSourceLocation% "%_TargetLocation%\Microsoft.SqlTools.ServiceLayer"
