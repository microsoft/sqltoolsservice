#!/usr/bin/env pwsh
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
if ($IsMacOS) {
    $Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "osx-$arch"
} elseif ($IsLinux) {
    $Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "linux-$arch"
} else {
    $Env:SQLTOOLSSERVICE_PACKAGE_OSNAME = "win-$arch"
}

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool run dotnet-cake -- build.cake @args
exit $LASTEXITCODE
