$Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "win-x64"
$Env:MSBuildEmitSolution=1
.\scripts\cake-bootstrap.ps1 -experimental @args
exit $LASTEXITCODE
