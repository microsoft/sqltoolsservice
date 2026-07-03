@echo off
setlocal

where pwsh >nul 2>nul
if errorlevel 1 (
    echo This script requires PowerShell 7+ ^(pwsh^).
    exit /b 1
)

pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0patch-vscode-mssql-sts.ps1" %*
exit /b %ERRORLEVEL%
