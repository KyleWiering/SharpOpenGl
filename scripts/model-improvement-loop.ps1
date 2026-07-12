# Iterative mesh improvement loop for one race/hull.
# Usage: .\scripts\model-improvement-loop.ps1 -Race vesper -Hull fighter_basic -MaxLoop 5

param(
    [string]$Race = "vesper",
    [string]$Hull = "fighter_basic",
    [int]$MaxLoop = 10,
    [int]$PauseAt = 5,
    [int]$StartLoop = 1
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $RepoRoot

$ImproveDir = Join-Path $RepoRoot "model-improvement\$Race\$Hull"
$BriefPath = Join-Path $ImproveDir "do-better.md"
$ScoreDir = Join-Path $ImproveDir "scores"
New-Item -ItemType Directory -Force -Path $ImproveDir, $ScoreDir | Out-Null

function Invoke-Build {
    dotnet build SharpOpenGl --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

function Export-Ship {
    dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~Export_and_score_${Race}_fighter" --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~Export_and_score_vesper_fighter" --verbosity quiet
    }
}

function Capture-Preview([string]$OutPng) {
    $full = Join-Path $RepoRoot $OutPng
    dotnet run --project SharpOpenGl --no-build -- --mesh-preview --race $Race --hull $Hull --screenshot-path $full
    if ($LASTEXITCODE -ne 0) { throw "Mesh preview failed" }
    if (-not (Test-Path $full)) { throw "Screenshot not created: $full" }
}

function Score-Mesh([string]$Png, [string]$JsonOut) {
    $scoreArgs = @("run", "--project", "SharpOpenGl", "--no-build", "--", "--score-mesh", "--race", $Race, "--hull", $Hull, "--output", $JsonOut)
    if ($Png) { $scoreArgs += @("--screenshot-path", $Png) }
    dotnet @scoreArgs
    if ($LASTEXITCODE -ne 0) { throw "Score failed" }
}

Write-Host "Model improvement: $Race / $Hull (loops $StartLoop..$MaxLoop)" -ForegroundColor Cyan
Invoke-Build

for ($loop = $StartLoop; $loop -le $MaxLoop; $loop++) {
    $pngName = "mesh-loop-{0:D2}.png" -f $loop
    $pngPath = Join-Path $RepoRoot $pngName
    $scorePath = Join-Path $ScoreDir ("loop-{0:D2}.json" -f $loop)

    Write-Host "`n=== Loop $loop ===" -ForegroundColor Cyan
    Write-Host "Brief: $BriefPath"
    Export-Ship
    Capture-Preview $pngName
    Score-Mesh $pngPath $scorePath

    $reportJson = Get-Content $scorePath -Raw | ConvertFrom-Json
    Write-Host ("Score: {0:N1} / 100" -f $reportJson.TotalScore) -ForegroundColor Green
    foreach ($cat in $reportJson.Categories) {
        Write-Host ("  {0,-14} {1,4:N1}/{2,4:N0}  {3}" -f $cat.Name, $cat.Score, $cat.MaxScore, $cat.Notes)
    }

    if ($loop -eq $PauseAt -and $MaxLoop -gt $PauseAt) {
        Write-Host "`n*** Paused after loop $PauseAt — review $pngName and $BriefPath ***" -ForegroundColor Yellow
        Write-Host "Provide feedback, then: .\scripts\model-improvement-loop.ps1 -StartLoop $($PauseAt+1)" -ForegroundColor Yellow
        break
    }
}

Write-Host "`nScreenshots saved to repo root: mesh-loop-*.png"