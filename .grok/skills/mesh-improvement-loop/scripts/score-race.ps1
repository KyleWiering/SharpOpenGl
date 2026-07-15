# Score all ships + stations for one race (8-race game fleet audit).
# Usage: .\score-race.ps1 -Race vesper
param(
    [Parameter(Mandatory = $true)]
    [string]$Race,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
)

$ErrorActionPreference = "Stop"
$outDir = Join-Path $RepoRoot "model-improvement\$Race"
$json = Join-Path $outDir "race-score.json"

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

Push-Location $RepoRoot
try {
    Write-Host "[score-race] $Race -> $json"
    dotnet run --project SharpOpenGl -- --score-race --race $Race --output $json
    if (-not (Test-Path $json)) { throw "Race score not created: $json" }
    Get-Content $json -Raw
}
finally {
    Pop-Location
}