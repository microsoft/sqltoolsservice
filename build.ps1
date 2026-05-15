#!/usr/bin/env pwsh
$architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
if ($null -ne $architecture) {
    $arch = $architecture.ToString().ToLower()
} else {
    switch ($Env:PROCESSOR_ARCHITECTURE) {
        "AMD64" { $arch = "x64"; break }
        "ARM64" { $arch = "arm64"; break }
        "x86" { $arch = "x86"; break }
        default { $arch = $Env:PROCESSOR_ARCHITECTURE.ToLower(); break }
    }
}

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
