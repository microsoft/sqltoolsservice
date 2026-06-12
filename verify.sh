#!/usr/bin/env bash
#
# STS2 verification gates (docs/sts2/SPEC.md §15).
#   ./verify.sh --quick   fast loop: build, tests, architecture gates, E2E, legacy diff budget
#   ./verify.sh --full    quick gates plus engine/mutation/perf suites (grow in M5+)
#
# Gates that arrive in later milestones print "n/a at M<n>" so the report format is
# stable from M0 on. Never weaken a gate to go green (SPEC §2.12).

set -uo pipefail

MODE="${1:---quick}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

LEGACY_DIFF_FILE_BUDGET=3
LEGACY_DIFF_LINE_BUDGET=60
BASE_REF="${STS2_VERIFY_BASE:-main}"

declare -a GATE_NAMES=()
declare -a GATE_RESULTS=()
FAILED=0

gate() {
    local name="$1"; shift
    echo "==> $name"
    if "$@"; then
        GATE_NAMES+=("$name"); GATE_RESULTS+=("ok")
        echo "    $name: ok"
    else
        GATE_NAMES+=("$name"); GATE_RESULTS+=("FAIL")
        echo "    $name: FAIL"
        FAILED=1
    fi
}

na() {
    GATE_NAMES+=("$1"); GATE_RESULTS+=("n/a ($2)")
    echo "==> $1: n/a ($2)"
}

build_sts2() {
    dotnet build sqltoolsservice-sts2.slnf -v q --nologo
}

unit_tests() {
    # Unit, multiplexer (incl. single-writer property test), architecture, and
    # banned-API wiring tests all live in the UnitTests project at M0.
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo
}

build_legacy_exe() {
    dotnet build src/Microsoft.SqlTools.ServiceLayer -v q --nologo
}

e2e_tests() {
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.E2ETests --no-build --nologo
}

legacy_diff_budget() {
    # Legacy = src/ and test/ excluding the STS2 subtrees (DECISIONS.md D-0004 scope).
    # Repo-level build plumbing (sln, slnf, Packages.props, docs, tools) is not legacy code.
    local stats files lines
    stats=$(git diff --numstat "${BASE_REF}...HEAD" -- 'src' 'test' ':(exclude)src/sts2' ':(exclude)test/sts2')
    files=$(printf '%s' "$stats" | grep -c . || true)
    lines=$(printf '%s' "$stats" | awk '{ added+=$1; deleted+=$2 } END { print added+deleted+0 }')
    echo "    legacy diff vs ${BASE_REF}: ${lines} lines across ${files} files (budget: <${LEGACY_DIFF_LINE_BUDGET} lines, <=${LEGACY_DIFF_FILE_BUDGET} files)"
    printf '%s\n' "$stats" | sed 's/^/      /'
    [ "${files:-0}" -le "$LEGACY_DIFF_FILE_BUDGET" ] && [ "${lines:-0}" -lt "$LEGACY_DIFF_LINE_BUDGET" ]
}

echo "STS2 verify ($MODE) @ $(git rev-parse --short HEAD)"
echo

gate "build (sts2 slnf, warnings as errors)" build_sts2
gate "unit+multiplexer+architecture tests" unit_tests
na "scenario tests (Fake)" "M1"
na "contract tests (Fake+Sqlite)" "M2+"
na "replay verify" "M1"
na "simulator" "M1"
na "secret canary scan" "M1"
na "generated docs diff" "M1"
gate "legacy diff budget" legacy_diff_budget
gate "build legacy exe (for E2E)" build_legacy_exe
gate "E2E disabled-mode v1 smoke + enabled-mode v1+v2" e2e_tests

if [ "$MODE" = "--full" ]; then
    na "SQL Server container suite" "M5"
    na "mutation testing" "M7"
    na "10k-seed simulator" "M7"
    na "perf/memory smoke" "M3"
fi

echo
SUMMARY="$ROOT/artifacts/verify-latest.md"
mkdir -p "$ROOT/artifacts"
{
    echo "## verify.sh $MODE - $(date -u +%Y-%m-%dT%H:%M:%SZ) - $(git rev-parse --short HEAD)"
    for i in "${!GATE_NAMES[@]}"; do
        echo "- ${GATE_NAMES[$i]}: ${GATE_RESULTS[$i]}"
    done
} > "$SUMMARY"
cat "$SUMMARY"

if [ "$FAILED" -ne 0 ]; then
    echo
    echo "verify: FAILED"
    exit 1
fi
echo
echo "verify: green"
