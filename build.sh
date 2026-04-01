#!/bin/bash
set -e

# Handle too many open files on macOS
[ "$(uname)" = "Darwin" ] && ulimit -n 4096

# Map uname -m values to .NET RID architecture suffixes
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)        ARCH="x64" ;;
    aarch64|arm64) ARCH="arm64" ;;
    armv7l)        ARCH="arm" ;;
    i386|i486|i586|i686)       ARCH="x86"  ;;
esac

case "$(uname -s)" in
    Darwin) export SQLTOOLSSERVICE_PACKAGE_OSNAME="osx-$ARCH" ;;
    Linux)  export SQLTOOLSSERVICE_PACKAGE_OSNAME="linux-$ARCH" ;;
    *)      export SQLTOOLSSERVICE_PACKAGE_OSNAME="win-$ARCH" ;;
esac

dotnet tool restore
dotnet tool run dotnet-cake -- build.cake "$@"
