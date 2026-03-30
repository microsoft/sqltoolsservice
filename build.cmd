@echo off
for /f %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
echo %ESC%[38;5;208mWARNING: build.cmd is redundant and will be removed in the future. Please use build.ps1 instead.%ESC%[0m
echo.
pwsh -File build.ps1 %*
echo.
echo %ESC%[38;5;208mWARNING: build.cmd is redundant and will be removed in the future. Please use build.ps1 instead.%ESC%[0m
