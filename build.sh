#!/bin/bash
# Handle too many files on osx
[ "$(uname)" == "Darwin" ] && ulimit -n 4096

if ! command -v pwsh &>/dev/null; then
    echo "Error: pwsh (PowerShell) is required but not found. Install it from https://aka.ms/powershell"
    exit 1
fi

pwsh ./build.ps1 "$@"
