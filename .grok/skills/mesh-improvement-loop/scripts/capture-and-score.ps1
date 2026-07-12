# Capture full-ship mesh preview and write quality score JSON.
# Usage: .\capture-and-score.ps1 -Race vesper -Hull fighter_basic -Loop 1
param(
    [Parameter(Mandatory = $true)]
    [string]$Race,
    [Parameter(Mandatory = $true)]
    [string]$Hull,
    [Parameter(Mandatory = $true)]
    [int]$Loop,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
)

$ErrorActionPreference = "Stop"
$nn = "{0:D2}" -f $Loop
$png = Join-Path $RepoRoot "mesh-loop-$nn.png"
$scoreDir = Join-Path $RepoRoot "model-improvement\$Race\$Hull\scores"
$json = Join-Path $scoreDir "loop-$nn.json"

if (-not (Test-Path $scoreDir)) {
    New-Item -ItemType Directory -Force -Path $scoreDir | Out-Null
}

Push-Location $RepoRoot
try {
    Write-Host "[capture] $png"
    dotnet run --project SharpOpenGl -- --mesh-preview --race $Race --hull $Hull --screenshot-path $png
    if (-not (Test-Path $png)) { throw "Screenshot not created: $png" }

    Write-Host "[score] $json"
    dotnet run --project SharpOpenGl -- --score-mesh --race $Race --hull $Hull --screenshot-path $png --output $json
    if (-not (Test-Path $json)) { throw "Score JSON not created: $json" }

    Write-Host "[test] Export_and_score"
    dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~Export_and_score" --no-build

    Write-Host "OK loop-$nn total score in $json"
    Get-Content $json -Raw
}
finally {
    Pop-Location
}