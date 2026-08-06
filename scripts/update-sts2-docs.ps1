# Regenerates the STS2 review docs (docs/sts2/CONTRACT.md etc.) from their generators.
# Review the diff before committing: generated docs are the human review surface (SPEC §12.3).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:STS2_UPDATE_DOCS = '1'
try {
    dotnet test (Join-Path $root 'test/sts2/Microsoft.SqlTools.Sts2.UnitTests') --nologo --filter 'FullyQualifiedName~GeneratedDocsTests.CommittedDocsMatchGenerators'
}
finally {
    Remove-Item Env:STS2_UPDATE_DOCS -ErrorAction SilentlyContinue
}
git -C $root status --short docs/sts2
