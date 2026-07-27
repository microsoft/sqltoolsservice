#!/usr/bin/env pwsh
if ($PSVersionTable.PSEdition -ne 'Core') {
    throw "This script requires PowerShell Core (v7+). You are running Windows PowerShell $($PSVersionTable.PSVersion). Please run pwsh.exe or install PowerShell from https://aka.ms/powershell."
}

$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()

# Set correct environment variable in case non-Windows users are running cross-plat `pwsh`
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
