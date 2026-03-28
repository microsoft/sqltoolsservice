#!/usr/bin/env pwsh
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
if ($IsMacOS) {
    $Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "osx-$arch"
} elseif ($IsLinux) {
    $Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "linux-$arch"
} else {
    $Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "win-$arch"
}
./scripts/cake-bootstrap.ps1 @args
exit $LASTEXITCODE
