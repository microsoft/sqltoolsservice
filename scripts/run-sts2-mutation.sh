#!/usr/bin/env bash
#
# Runs Stryker.NET mutation testing per STS2 project (SPEC §14.6). Stryker mutates one
# project-under-test per run; the UnitTests project references many source projects, so
# we drive each target project separately with its own ratchet threshold:
#   - Core, Contracts: 70% (stryker-config.json)
#   - Runtime pure units (canonical digest, redaction, envelope codec, journal manifest,
#                         WireValueEncoder): 60% (stryker-config-runtime.json + mutate glob)
#
# Uses a real .sln (sqltoolsservice-sts2-stryker.sln) scoped to the STS2 projects, because
# Stryker cannot read .slnf filters and the full solution's MSBuild-variable TFMs break its
# analysis. Pass a single project name as $1 to run just that one (default: all three).

set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
export PATH="$PATH:$HOME/.dotnet/tools"

# On Windows, Stryker's Buildalyzer picks Visual Studio's MSBuild by default, but the .NET
# SDK pinned in global.json requires MSBuild >= 18 (VS 2022 ships 17.x), which fails to
# resolve Microsoft.NET.Sdk. Force the .NET SDK's own MSBuild (18.x). On Linux/CI there is
# no VS to mis-pick, so Buildalyzer uses the SDK MSBuild naturally and we leave it alone.
case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*|*NT*)
        if [ -z "${MSBUILD_EXE_PATH:-}" ]; then
            sdk_version="$(dotnet --version)"
            sdk_dir="$(dotnet --list-sdks | grep -F "$sdk_version " | sed -E 's/^[^[]*\[(.+)\]$/\1/')"
            if [ -n "$sdk_dir" ]; then
                export MSBUILD_EXE_PATH="${sdk_dir}\\${sdk_version}\\MSBuild.dll"
                echo "Using SDK MSBuild: $MSBUILD_EXE_PATH"
            fi
        fi
        ;;
esac

ONLY="${1:-all}"

run_one() {
    local project="$1" config="$2" mutate="$3"
    if [ "$ONLY" != "all" ] && [ "$ONLY" != "$project" ]; then
        return 0
    fi
    echo "==> Stryker: $project"
    dotnet stryker --config-file "$config" --project "$project" --mutate "$mutate"
    local code=$?
    echo "    $project: exit $code"
    return $code
}

failed=0
run_one "Microsoft.SqlTools.Sts2.Core.csproj"     stryker-config.json         '**/*.cs' || failed=1
run_one "Microsoft.SqlTools.Sts2.Contracts.csproj" stryker-config.json         '**/*.cs' || failed=1
run_one "Microsoft.SqlTools.Sts2.Runtime.csproj"   stryker-config-runtime.json '{**/Journaling/CanonicalJson.cs,**/Journaling/JournalManifest.cs,**/Envelopes/*.cs,**/Redaction/*.cs,**/Effects/WireValueEncoder.cs}' || failed=1

exit $failed
