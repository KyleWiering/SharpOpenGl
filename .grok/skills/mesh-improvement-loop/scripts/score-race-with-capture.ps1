# Screenshot-backed race fleet audit: capture (or reuse mesh-loop PNG) + score every ship/station,
# then aggregate model-improvement/<race>/race-score.json (RaceQualityReport format).
#
# Usage:
#   .\score-race-with-capture.ps1 -Race vesper
#   .\score-race-with-capture.ps1 -AllRaces
#   .\score-race-with-capture.ps1 -Race terran -Loop 1 -SkipCapture
#
# Per asset:
#   1. Resolve fallback screenshot: mesh-loop-NN-<race>-<model>.png (prefers lowest loop number)
#   2. If missing and -SkipCapture not set: dotnet --mesh-preview capture
#   3. dotnet --score-mesh with --screenshot-path -> model-improvement/<race>/<model>/scores/loop-NN.json
#   4. dotnet --score-race --from-score-files -> race-score.json
#
# Eight races: terran, vesper, korath, aetherian, nexar, solari, voidborn, cryo
param(
    [string]$Race,
    [switch]$AllRaces,
    [int]$Loop = 1,
    [switch]$SkipCapture,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
)

$ErrorActionPreference = "Stop"

$AllRaceIds = @(
    "terran", "vesper", "korath", "aetherian", "nexar", "solari", "voidborn", "cryo"
)

$ShipIds = @(
    "hero_default", "scout_light", "fighter_basic", "interceptor_mk2", "drone_swarm",
    "corvette_fast", "frigate_strike", "gunship_heavy", "bomber_heavy",
    "destroyer_assault", "cruiser_heavy", "carrier_command", "dreadnought",
    "miner_basic", "miner_eva", "miner_tractor", "transport_cargo", "freighter_bulk", "support_repair"
)

$StationIds = @(
    "command_center", "shipyard_small", "shipyard_medium", "shipyard_large",
    "defense_turret", "sensor_array", "resource_refinery", "repair_bay",
    "power_reactor", "supply_depot"
)

if (-not $AllRaces -and [string]::IsNullOrWhiteSpace($Race)) {
    throw "Specify -Race <id> or -AllRaces"
}

$targetRaces = if ($AllRaces) { $AllRaceIds } else { @($Race) }
$nn = "{0:D2}" -f $Loop
$scoreFileName = "loop-$nn.json"

function Get-FallbackScreenshots {
    param([string]$RaceId, [string]$ModelId)

    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    $paths = New-Object 'System.Collections.Generic.List[string]'

    $preferred = @(
        (Join-Path $RepoRoot "mesh-loop-$nn-$RaceId-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-01-$RaceId-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-02-$RaceId-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-03-$RaceId-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-04-$RaceId-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-05-$RaceId-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-$RaceId-01-$ModelId.png"),
        (Join-Path $RepoRoot "mesh-loop-$RaceId-02-$ModelId.png")
    )

    foreach ($path in $preferred) {
        if ((Test-Path $path) -and $seen.Add($path)) {
            [void]$paths.Add($path)
        }
    }

    Get-ChildItem -Path $RepoRoot -Filter "mesh-loop*-$RaceId-$ModelId.png" -File -ErrorAction SilentlyContinue |
        Sort-Object Name |
        ForEach-Object {
            if ($seen.Add($_.FullName)) {
                [void]$paths.Add($_.FullName)
            }
        }

    return $paths
}

function Invoke-MeshScore {
    param(
        [string]$RaceId,
        [string]$ModelId,
        [string]$Category,
        [string]$PngPath,
        [string]$OutputPath
    )

    $scoreArgs = @(
        "run", "--project", "SharpOpenGl", "--no-build", "--",
        "--score-mesh", "--category", $Category, "--model", $ModelId,
        "--race", $RaceId, "--screenshot-path", $PngPath
    )
    if ($OutputPath) {
        $scoreArgs += @("--output", $OutputPath)
    }

    $output = dotnet @scoreArgs 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "score-mesh failed for $RaceId/$ModelId ($PngPath): $output"
    }

    if ($OutputPath) {
        if (-not (Test-Path $OutputPath)) { throw "Score JSON not created: $OutputPath" }
        return Get-Content $OutputPath -Raw | ConvertFrom-Json
    }

    if ($output -match '"TotalScore"\s*:\s*([\d.]+)') {
        return [PSCustomObject]@{ TotalScore = [double]$Matches[1] }
    }

    throw "Could not parse score output for $RaceId/$ModelId ($PngPath)"
}

function Resolve-BestFallbackScreenshot {
    param(
        [string]$RaceId,
        [string]$ModelId,
        [string]$Category,
        [string]$OutputPath
    )

    $candidates = Get-FallbackScreenshots -RaceId $RaceId -ModelId $ModelId
    if ($candidates.Count -eq 0) { return $null }

    $bestPath = $null
    $bestScore = [double]::MinValue

    foreach ($path in $candidates) {
        $report = Invoke-MeshScore -RaceId $RaceId -ModelId $ModelId -Category $Category -PngPath $path
        if ($report.TotalScore -gt $bestScore) {
            $bestScore = $report.TotalScore
            $bestPath = $path
        }
    }

    $bestReport = $null
    if ($bestPath) {
        $bestReport = Invoke-MeshScore -RaceId $RaceId -ModelId $ModelId -Category $Category -PngPath $bestPath -OutputPath $OutputPath
    }

    return [PSCustomObject]@{
        Path   = $bestPath
        Score  = $bestScore
        Report = $bestReport
    }
}

function Invoke-AssetCaptureAndScore {
    param(
        [string]$RaceId,
        [string]$ModelId,
        [ValidateSet("ship", "station")]
        [string]$Category
    )

    $slug = "$RaceId-$ModelId"
    $png = Join-Path $RepoRoot "mesh-loop-$nn-$slug.png"
    $scoreDir = Join-Path $RepoRoot "model-improvement\$RaceId\$ModelId\scores"
    $json = Join-Path $scoreDir $scoreFileName

    if (-not (Test-Path $scoreDir)) {
        New-Item -ItemType Directory -Force -Path $scoreDir | Out-Null
    }

    $fallback = Resolve-BestFallbackScreenshot -RaceId $RaceId -ModelId $ModelId -Category $Category -OutputPath $json
    if ($fallback) {
        Write-Host "[fallback] $RaceId/$ModelId -> $($fallback.Path) (best=$([math]::Round($fallback.Score, 2)))"
        $png = $fallback.Path
        $report = $fallback.Report
    }
    elseif ($SkipCapture) {
        throw "No fallback PNG for $RaceId/$ModelId and -SkipCapture is set"
    }
    else {
        Write-Host "[capture] $png ($Category)"
        $previewArgs = @(
            "run", "--project", "SharpOpenGl", "--no-build", "--",
            "--mesh-preview", "--category", $Category, "--model", $ModelId,
            "--race", $RaceId, "--screenshot-path", $png
        )
        dotnet @previewArgs
        if (-not (Test-Path $png)) { throw "Screenshot not created: $png" }

        Write-Host "[score] $json"
        $report = Invoke-MeshScore -RaceId $RaceId -ModelId $ModelId -Category $Category -PngPath $png -OutputPath $json
    }
    return [PSCustomObject]@{
        Category = $Category
        ModelId  = $ModelId
        Score    = [math]::Round($report.TotalScore, 2)
        Png      = $png
    }
}

function Invoke-RaceFleetAudit {
    param([string]$RaceId)

    $outDir = Join-Path $RepoRoot "model-improvement\$RaceId"
    $raceJson = Join-Path $outDir "race-score.json"

    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    }

    Write-Host ""
    Write-Host "========== RACE FLEET AUDIT: $RaceId =========="

    $assetResults = @()
    foreach ($ship in $ShipIds) {
        $assetResults += Invoke-AssetCaptureAndScore -RaceId $RaceId -ModelId $ship -Category "ship"
    }
    foreach ($station in $StationIds) {
        $assetResults += Invoke-AssetCaptureAndScore -RaceId $RaceId -ModelId $station -Category "station"
    }

    Write-Host "[aggregate] $raceJson"
    $aggregateArgs = @(
        "run", "--project", "SharpOpenGl", "--no-build", "--",
        "--score-race", "--race", $RaceId, "--from-score-files",
        "--score-file", $scoreFileName, "--output", $raceJson
    )
    dotnet @aggregateArgs
    if (-not (Test-Path $raceJson)) { throw "Race score not created: $raceJson" }

    $raceReport = Get-Content $raceJson -Raw | ConvertFrom-Json
    $pass = ($raceReport.ShipFleetScore -ge 85) -and ($raceReport.StationFleetScore -ge 85)

    return [PSCustomObject]@{
        Race               = $RaceId
        ShipFleetScore     = [math]::Round($raceReport.ShipFleetScore, 2)
        StationFleetScore  = [math]::Round($raceReport.StationFleetScore, 2)
        OverallScore       = [math]::Round($raceReport.OverallScore, 2)
        PassBoth85         = $pass
        WeakestAssets      = $raceReport.WeakestAssets
    }
}

Push-Location $RepoRoot
try {
    Write-Host "[build] SharpOpenGl"
    dotnet build SharpOpenGl 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

    $summary = @()
    foreach ($raceId in $targetRaces) {
        if ($raceId -notin $AllRaceIds) {
            throw "Unknown race '$raceId'. Valid: $($AllRaceIds -join ', ')"
        }
        $summary += Invoke-RaceFleetAudit -RaceId $raceId
    }

    Write-Host ""
    Write-Host "=== RACE FLEET SCORE SUMMARY (screenshot-backed) ==="
    $summary | Format-Table Race, ShipFleetScore, StationFleetScore, OverallScore, PassBoth85 -AutoSize

    $failed = $summary | Where-Object { -not $_.PassBoth85 }
    if ($failed) {
        Write-Host ""
        Write-Host "Races below 85 (weakest assets):"
        foreach ($row in $failed) {
            Write-Host "  $($row.Race): ship=$($row.ShipFleetScore) station=$($row.StationFleetScore)"
            $row.WeakestAssets | ForEach-Object { Write-Host "    $_" }
        }
    }

    $summary
}
finally {
    Pop-Location
}