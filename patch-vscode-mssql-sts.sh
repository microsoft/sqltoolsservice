#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Debug}"
target_root="${2:-}"

framework="net10.0"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$script_dir"
vscode_mssql_root="$(cd "$repo_root/../vscode-mssql" && pwd)"
config_path="$vscode_mssql_root/extensions/mssql/src/configurations/config.ts"

if [[ ! -f "$config_path" ]]; then
    echo "vscode-mssql configuration was not found: $config_path" >&2
    exit 1
fi

version="$(sed -n '/service: {/,/^[[:space:]]*},/ s/^[[:space:]]*version:[[:space:]]*"\([^"[:space:]]*\)".*/\1/p' "$config_path" | head -n 1)"
if [[ -z "$version" ]]; then
    echo "Could not determine SQL Tools Service version from: $config_path" >&2
    exit 1
fi

if [[ -z "$target_root" ]]; then
    target_root="$vscode_mssql_root/extensions/mssql/sqltoolsservice/$version"
fi

os_name="$(uname -s)"
arch_name="$(uname -m)"
case "$os_name:$arch_name" in
    Darwin:x86_64)
        runtime="osx-x64"
        platform_folder="OSX"
        ;;
    Darwin:arm64)
        runtime="osx-arm64"
        platform_folder="OSX"
        ;;
    Linux:x86_64|Linux:amd64)
        runtime="linux-x64"
        platform_folder="Linux"
        ;;
    Linux:aarch64|Linux:arm64)
        runtime="linux-arm64"
        platform_folder="Linux"
        ;;
    *)
        echo "Unsupported platform: $os_name $arch_name" >&2
        exit 1
        ;;
esac

staging_root="$repo_root/artifacts/sts-vscode-mssql-patch-runs"
staging="$staging_root/run-$(date +%Y%m%d-%H%M%S)-$$"
platform_merged="$staging/merged/$platform_folder"
portable_merged="$staging/merged/Portable"
target_platform="$target_root/$platform_folder"
target_portable="$target_root/Portable"

projects_names=(
    "Credentials"
    "ResourceProvider"
    "ServiceLayer"
)

projects_paths=(
    "src/Microsoft.SqlTools.Credentials/Microsoft.SqlTools.Credentials.csproj"
    "src/Microsoft.SqlTools.ResourceProvider/Microsoft.SqlTools.ResourceProvider.csproj"
    "src/Microsoft.SqlTools.ServiceLayer/Microsoft.SqlTools.ServiceLayer.csproj"
)

ensure_tool() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required tool '$1' was not found on PATH." >&2
        exit 1
    fi
}

check_no_running_sts() {
    if ps ax -o pid= -o command= | grep -F "$target_root" | grep -E "MicrosoftSqlToolsServiceLayer|MicrosoftSqlToolsCredentials|SqlToolsResourceProviderService|dotnet" | grep -v grep >/tmp/sts-patch-running.$$; then
        echo "Close these SQL Tools Service processes before patching:" >&2
        cat /tmp/sts-patch-running.$$ >&2
        rm -f /tmp/sts-patch-running.$$
        exit 1
    fi
    rm -f /tmp/sts-patch-running.$$
}

publish_platform() {
    local name="$1"
    local project="$2"
    local output="$staging/projects/$platform_folder/$name"

    echo
    echo "Publishing $name for $platform_folder ($runtime)..."
    dotnet publish "$repo_root/$project" \
        --framework "$framework" \
        --configuration "$configuration" \
        --runtime "$runtime" \
        --self-contained \
        --output "$output"
}

publish_portable() {
    local name="$1"
    local project="$2"
    local output="$staging/projects/Portable/$name"

    echo
    echo "Publishing $name for Portable..."
    dotnet publish "$repo_root/$project" \
        --framework "$framework" \
        --configuration "$configuration" \
        --output "$output"
}

copy_into() {
    local source="$1"
    local destination="$2"
    rsync -a "$source"/ "$destination"/
}

mirror_into() {
    local source="$1"
    local destination="$2"
    mkdir -p "$destination"
    rsync -a --delete "$source"/ "$destination"/
}

ensure_tool dotnet
ensure_tool rsync

if [[ ! -d "$target_root" ]]; then
    echo "Target root does not exist: $target_root" >&2
    exit 1
fi

check_no_running_sts

cd "$repo_root"

echo "Configuration: $configuration"
echo "Runtime:       $runtime"
echo "Target root:   $target_root"
echo

mkdir -p "$platform_merged" "$portable_merged"

for i in "${!projects_names[@]}"; do
    publish_platform "${projects_names[$i]}" "${projects_paths[$i]}"
done

for i in "${!projects_names[@]}"; do
    publish_portable "${projects_names[$i]}" "${projects_paths[$i]}"
done

echo
echo "Merging publish outputs. ServiceLayer is copied last."
for name in "${projects_names[@]}"; do
    copy_into "$staging/projects/$platform_folder/$name" "$platform_merged"
done

for name in "${projects_names[@]}"; do
    copy_into "$staging/projects/Portable/$name" "$portable_merged"
done

chmod +x "$platform_merged/MicrosoftSqlToolsServiceLayer" 2>/dev/null || true
chmod +x "$platform_merged/MicrosoftSqlToolsCredentials" 2>/dev/null || true
chmod +x "$platform_merged/SqlToolsResourceProviderService" 2>/dev/null || true

echo
echo "Replacing vscode-mssql SQL Tools Service folders..."
mirror_into "$platform_merged" "$target_platform"
mirror_into "$portable_merged" "$target_portable"

echo
echo "Patched vscode-mssql SQL Tools Service."
echo "  $platform_folder: $target_platform"
echo "  Portable: $target_portable"
