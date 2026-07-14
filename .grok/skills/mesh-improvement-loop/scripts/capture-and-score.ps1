# Capture mesh preview and write quality score JSON.
# Usage: .\capture-and-score.ps1 -Race vesper -Model fighter_basic -Category ship -Loop 1
param(
    [string]$Race = "vesper",
    [Parameter(Mandatory = $true)]
    [string]$Model,
    [ValidateSet("ship", "station", "object")]
    [string]$Category = "ship",
    [Parameter(Mandatory = $true)]
    [int]$Loop,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
)

$ErrorActionPreference = "Stop"
$nn = "{0:D2}" -f $Loop
$slug = if ($Category -eq "object") { "shared-$Model" } else { "$Race-$Model" }
$png = Join-Path $RepoRoot "mesh-loop-$nn-$slug.png"

if ($Category -eq "object") {
    $scoreDir = Join-Path $RepoRoot "model-improvement\shared\objects\$Model\scores"
} else {
    $scoreDir = Join-Path $RepoRoot "model-improvement\$Race\$Model\scores"
}
$json = Join-Path $scoreDir "loop-$nn.json"

if (-not (Test-Path $scoreDir)) {
    New-Item -ItemType Directory -Force -Path $scoreDir | Out-Null
}

$raceArgs = if ($Category -eq "object") { @() } else { @("--race", $Race) }

Push-Location $RepoRoot
try {
    Write-Host "[capture] $png ($Category)"
    $previewArgs = @(
        "run", "--project", "SharpOpenGl", "--",
        "--mesh-preview", "--category", $Category, "--model", $Model
    ) + $raceArgs + @("--screenshot-path", $png)
    dotnet @previewArgs
    if (-not (Test-Path $png)) { throw "Screenshot not created: $png" }

    Write-Host "[score] $json"
    $scoreArgs = @(
        "run", "--project", "SharpOpenGl", "--",
        "--score-mesh", "--category", $Category, "--model", $Model
    ) + $raceArgs + @("--screenshot-path", $png, "--output", $json)
    dotnet @scoreArgs
    if (-not (Test-Path $json)) { throw "Score JSON not created: $json" }

    Write-Host "[test] ModelQualityScorer"
    dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~ModelQualityScorer" --no-build

    Write-Host "OK loop-$nn total score in $json"
    Get-Content $json -Raw
}
finally {
    Pop-Location
}