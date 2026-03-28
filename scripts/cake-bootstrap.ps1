#!/usr/bin/env pwsh
<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
This Powershell script will restore local dotnet tools (including Cake)
and execute your Cake build script with the parameters you provide.

.PARAMETER Script
The build script to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.LINK
http://cakebuild.net

#>

[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Verbose",
    [switch]$SkipToolPackageRestore,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

Write-Host "Preparing to run build script..."

$PS_SCRIPT_ROOT = split-path -parent $MyInvocation.MyCommand.Definition;
$TOOLS_DIR = Join-Path $PSScriptRoot "..\.tools"

function Get-ToolPath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ToolName
    )

    # $IsWindows is only available in PowerShell Core (6+).
    # Fall back to $env:OS for Windows PowerShell 5.x compatibility.
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        return Join-Path $TOOLS_DIR "$ToolName.exe"
    }

    return Join-Path $TOOLS_DIR $ToolName
}

function InstallOrUpdate-Tool {
    param(
        [Parameter(Mandatory=$true)]
        [string]$PackageId
    )

    $installArgs = @("tool", "install", "--tool-path", $TOOLS_DIR, $PackageId)
    & dotnet @installArgs

    if ($LASTEXITCODE -eq 0) {
        return
    }

    $updateArgs = @("tool", "update", "--tool-path", $TOOLS_DIR, $PackageId)
    & dotnet @updateArgs

    if ($LASTEXITCODE -ne 0) {
        Throw "Could not install or update dotnet tool '$PackageId'."
    }
}

$CAKE_EXE = Get-ToolPath -ToolName "dotnet-cake"
$T4_EXE = Get-ToolPath -ToolName "t4"

# Make sure tools folder exists
if ((Test-Path $PSScriptRoot) -and !(Test-Path $TOOLS_DIR)) {
    Write-Host "Creating tools directory..."
    New-Item -Path $TOOLS_DIR -Type directory | out-null
}

# Restore local dotnet tools?
if(-Not $SkipToolPackageRestore.IsPresent)
{
    Write-Host "Restoring local dotnet tools..."
    InstallOrUpdate-Tool -PackageId "Cake.Tool"
    InstallOrUpdate-Tool -PackageId "dotnet-t4"
}

# Make sure that Cake has been installed.
if (!(Test-Path $CAKE_EXE)) {
    Throw "Could not find dotnet-cake at $CAKE_EXE"
}

if (!(Test-Path $T4_EXE)) {
    Throw "Could not find t4 at $T4_EXE"
}

# Start Cake
Write-Host "Running build script..."
$cakeArgs = @(
    $Script,
    "--verbosity",
    $Verbosity
)

if ($ScriptArgs) {
    $cakeArgs += $ScriptArgs
}

Write-Host "& `"$CAKE_EXE`" $($cakeArgs -join ' ')"
& $CAKE_EXE @cakeArgs
exit $LASTEXITCODE
