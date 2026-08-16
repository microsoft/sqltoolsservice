param(
    [string]$Configuration = "Debug",
    [string]$TargetRoot = ""
)

$ErrorActionPreference = "Stop"

$framework = "net10.0"
$runtime = "win-x64"
$repoRoot = $PSScriptRoot
$version = "6.0.20260625.1"
if ([string]::IsNullOrWhiteSpace($TargetRoot)) {
    $TargetRoot = Join-Path (Split-Path -Parent $repoRoot) "vscode-mssql\extensions\mssql\sqltoolsservice\$version"
}
$stagingRoot = Join-Path $repoRoot "artifacts\sts-vscode-mssql-patch-runs"
$stagingRunName = "run-{0}-{1}" -f (Get-Date -Format "yyyyMMdd-HHmmss"), $PID
$staging = Join-Path $stagingRoot $stagingRunName
$windowsMerged = Join-Path $staging "merged\Windows"
$portableMerged = Join-Path $staging "merged\Portable"
$targetWindows = Join-Path $TargetRoot "Windows"
$targetPortable = Join-Path $TargetRoot "Portable"

$projects = @(
    @{ Name = "Credentials"; Project = "src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj" },
    @{ Name = "ResourceProvider"; Project = "src\Microsoft.SqlTools.ResourceProvider\Microsoft.SqlTools.ResourceProvider.csproj" },
    @{ Name = "ServiceLayer"; Project = "src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj" }
)

function Get-RunningStsProcesses {
    $roots = @($TargetRoot) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { [System.IO.Path]::GetFullPath($_).TrimEnd('\') }

    Get-CimInstance Win32_Process -Filter "Name='MicrosoftSqlToolsServiceLayer.exe' or Name='MicrosoftSqlToolsCredentials.exe' or Name='SqlToolsResourceProviderService.exe' or Name='dotnet.exe'" |
        Where-Object {
            $processText = @($_.CommandLine, $_.ExecutablePath) |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

            foreach ($root in $roots) {
                foreach ($text in $processText) {
                    if ($text.IndexOf($root, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                        return $true
                    }
                }
            }

            return $false
        }
}

function Invoke-CheckedRobocopy {
    param(
        [string]$Source,
        [string]$Destination,
        [switch]$Mirror
    )

    $mode = if ($Mirror) { "/MIR" } else { "/E" }
    & robocopy $Source $Destination $mode /R:2 /W:1 /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed from '$Source' to '$Destination' with exit code $LASTEXITCODE"
    }
}

function Invoke-DotnetPublish {
    param(
        [hashtable]$ProjectInfo,
        [string]$OutputRoot,
        [switch]$SelfContainedWindows
    )

    $outputPath = Join-Path $OutputRoot $ProjectInfo.Name
    $projectPath = Join-Path $repoRoot $ProjectInfo.Project

    Write-Host ""
    if ($SelfContainedWindows) {
        Write-Host "Publishing $($ProjectInfo.Name) for Windows..."
        & dotnet publish $projectPath --framework $framework --configuration $Configuration --runtime $runtime --self-contained --output $outputPath
    } else {
        Write-Host "Publishing $($ProjectInfo.Name) for Portable..."
        & dotnet publish $projectPath --framework $framework --configuration $Configuration --output $outputPath
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $($ProjectInfo.Name)"
    }
}

Set-Location $repoRoot

if (-not (Test-Path -LiteralPath $TargetRoot -PathType Container)) {
    throw "Target root does not exist: $TargetRoot"
}

$running = @(Get-RunningStsProcesses)
if ($running.Count -gt 0) {
    Write-Host "Close these SQL Tools Service processes before patching:"
    $running | Select-Object ProcessId, Name, CommandLine | Format-Table -Wrap
    throw "Cannot patch while SQL Tools Service processes are running from the target or staging folders."
}

Write-Host "Configuration: $Configuration"
Write-Host "Target root:   $TargetRoot"
Write-Host ""

New-Item -ItemType Directory -Force -Path $windowsMerged, $portableMerged | Out-Null

$windowsProjectRoot = Join-Path $staging "projects\Windows"
$portableProjectRoot = Join-Path $staging "projects\Portable"

foreach ($project in $projects) {
    Invoke-DotnetPublish -ProjectInfo $project -OutputRoot $windowsProjectRoot -SelfContainedWindows
}

foreach ($project in $projects) {
    Invoke-DotnetPublish -ProjectInfo $project -OutputRoot $portableProjectRoot
}

Write-Host ""
Write-Host "Merging publish outputs. ServiceLayer is copied last."
foreach ($project in $projects) {
    Invoke-CheckedRobocopy -Source (Join-Path $windowsProjectRoot $project.Name) -Destination $windowsMerged
}

foreach ($project in $projects) {
    Invoke-CheckedRobocopy -Source (Join-Path $portableProjectRoot $project.Name) -Destination $portableMerged
}

Write-Host ""
Write-Host "Replacing vscode-mssql SQL Tools Service folders..."
New-Item -ItemType Directory -Force -Path $targetWindows, $targetPortable | Out-Null
Invoke-CheckedRobocopy -Source $windowsMerged -Destination $targetWindows -Mirror
Invoke-CheckedRobocopy -Source $portableMerged -Destination $targetPortable -Mirror

Write-Host ""
Write-Host "Patched vscode-mssql SQL Tools Service."
Write-Host "  Windows:  $targetWindows"
Write-Host "  Portable: $targetPortable"
