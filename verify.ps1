# Thin PowerShell shim over verify.sh (docs/sts2/SPEC.md §15). Requires Git Bash on Windows.
param([string]$Mode = "--quick")
& bash "$PSScriptRoot/verify.sh" $Mode
exit $LASTEXITCODE
