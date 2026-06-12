# Harvests RS0016 (missing PublicAPI declaration) diagnostics from an STS2 build and
# appends the missing entries to each project's PublicAPI.Unshipped.txt, sorted.
# Intentional-use tool: review the resulting diff before committing (SPEC §4.4).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$output = & dotnet build (Join-Path $root 'sqltoolsservice-sts2.slnf') -v q --nologo 2>&1 | Out-String
$pattern = [regex]"(?m)^(?<file>[^(\r\n]+)\(\d+,\d+\): error RS0016: Symbol '(?<symbol>.+?)' is not part of the declared public API"
$additions = @{}
foreach ($m in $pattern.Matches(($output -replace "\x1b\[[0-9;]*m", ''))) {
    $projectDir = Split-Path $m.Groups['file'].Value.Trim() -Parent
    while ($projectDir -and -not (Test-Path (Join-Path $projectDir 'PublicAPI.Unshipped.txt'))) {
        $projectDir = Split-Path $projectDir -Parent
    }
    if (-not $projectDir) { continue }
    $apiFile = Join-Path $projectDir 'PublicAPI.Unshipped.txt'
    $additions[$apiFile] = @($additions[$apiFile]) + $m.Groups['symbol'].Value
}

if ($additions.Count -eq 0) { Write-Host 'No RS0016 diagnostics found.'; exit 0 }

foreach ($apiFile in $additions.Keys) {
    $existing = @(Get-Content $apiFile | Where-Object { $_ -ne '' })
    $header = @($existing | Where-Object { $_.StartsWith('#') })
    $entries = @($existing | Where-Object { -not $_.StartsWith('#') })
    $merged = @($entries + $additions[$apiFile] | Where-Object { $_ } | Sort-Object -CaseSensitive -Unique)
    Set-Content $apiFile ($header + $merged)
    Write-Host "$apiFile : $($merged.Count - $entries.Count) new entries"
}
