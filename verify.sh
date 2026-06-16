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
    # Unit, multiplexer (incl. single-writer property test), architecture, banned-API,
    # core reducer, coordinator, journal, redaction, and replay tests. Also produces
    # the artifacts/test-journals corpus consumed by the replay gate below.
    # Perf and Engine run in --full only; Simulator has its own gate (heavy background
    # tasks would starve under the unit suite's parallel load).
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo \
        --filter 'Category!=Perf&Category!=Engine&Category!=Simulator'
}

engine_suite() {
    # dialect:tsql tests skip (not fail) when STS2_SQLSERVER_CONNSTRING is unset/unreachable.
    # CI/nightly sets it and these run for real. Locally they pass as skipped no-ops.
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo \
        --filter 'Category=Engine' --logger 'console;verbosity=detailed'
}

perf_smoke() {
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo --filter 'Category=Perf' \
        --logger 'console;verbosity=detailed'
}

mutation_testing() {
    bash scripts/run-sts2-mutation.sh
}

simulator_full() {
    # SPEC §14.4 full run: 10,000 seeds. Deterministic journals per seed (I7).
    STS2_SIMULATOR_SEEDS="${STS2_SIMULATOR_SEEDS:-10000}" \
        dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo --filter 'Category=Simulator'
}

scenario_corpus() {
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo \
        --filter 'FullyQualifiedName~ScenarioCorpusTests|FullyQualifiedName~ActiveScenarioTests'
}

simulator() {
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo --filter 'Category=Simulator'
}

contract_tests_sqlite() {
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo \
        --filter 'FullyQualifiedName~SqliteDriverTests|FullyQualifiedName~SqliteContractTests|FullyQualifiedName~DriverIsolationTests'
}

replay_verify() {
    dotnet run --project tools/sts2-replay --no-build -- verify artifacts/test-journals
}

secret_canary_scan() {
    # I6: produced artifacts and generated docs must never contain a canary value.
    # Canary literals are defined in src/sts2/Microsoft.SqlTools.Sts2.Testing/SecretCanaries.cs.
    local hits
    hits=$(grep -r -l -e 'CANARY-pw-1f9b3c7d2e' -e 'CANARY-at-5a8d0e' artifacts docs/sts2 2>/dev/null || true)
    if [ -n "$hits" ]; then
        echo "    canary found in:"
        printf '%s\n' "$hits" | sed 's/^/      /'
        return 1
    fi
    return 0
}

generated_docs_diff() {
    dotnet test test/sts2/Microsoft.SqlTools.Sts2.UnitTests --no-build --nologo \
        --filter 'FullyQualifiedName~GeneratedDocsTests'
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
# The journal corpus is regenerated by the tests; stale runs must not haunt the replay gate.
rm -rf artifacts/test-journals
gate "unit+multiplexer+architecture tests" unit_tests
gate "scenario tests (Fake, active corpus)" scenario_corpus
gate "contract tests (Sqlite, real I/O)" contract_tests_sqlite
gate "replay verify (sts2-replay)" replay_verify
# The 200-seed simulator is the quick gate; --full runs the 10k variant instead (below),
# so the heavy simulator is never run twice back-to-back.
if [ "$MODE" != "--full" ]; then
    gate "simulator (200 seeds)" simulator
fi
gate "secret canary scan" secret_canary_scan
gate "generated docs diff" generated_docs_diff
gate "legacy diff budget" legacy_diff_budget
gate "build legacy exe (for E2E)" build_legacy_exe
gate "E2E disabled-mode v1 smoke + enabled-mode v1+v2" e2e_tests

if [ "$MODE" = "--full" ]; then
    if [ -n "${STS2_SQLSERVER_CONNSTRING:-}" ]; then
        gate "SQL Server engine suite (dialect:tsql)" engine_suite
    else
        na "SQL Server engine suite (dialect:tsql)" "no STS2_SQLSERVER_CONNSTRING — CI/nightly only"
        engine_suite >/dev/null 2>&1 || true  # exercise the skip path so it stays compiling/green
    fi
    if [ "${STS2_SKIP_STRYKER:-0}" = "1" ]; then
        na "mutation testing (Stryker, ratchet)" "skipped via STS2_SKIP_STRYKER (PR/push tier)"
    elif command -v dotnet-stryker >/dev/null 2>&1 || dotnet tool list --global 2>/dev/null | grep -qi 'dotnet-stryker'; then
        gate "mutation testing (Stryker, ratchet)" mutation_testing
    else
        na "mutation testing (Stryker, ratchet)" "dotnet-stryker not installed (dotnet tool install --global dotnet-stryker)"
    fi
    gate "10k-seed simulator" simulator_full
    gate "perf/memory smoke (1M rows digest mode, >=50k rows/s)" perf_smoke
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
