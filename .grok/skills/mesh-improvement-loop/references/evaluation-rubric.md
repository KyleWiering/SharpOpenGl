# Mesh evaluation rubric

SharpOpenGl scores procedural assets on a **0–100** scale plus a separate **RaceIdentity (0–10)** sub-score embedded in the total. Use this when interpreting `loop-NN.json` or `race-score.json`.

## RTS viewing priority

Gameplay camera is **oblique top-down** (elevated, looking down at the battlefield). Scoring and mesh edits must optimize what players actually see.

| View | Yaw (ships) | Yaw (stations) | Weight | Purpose |
|------|-------------|----------------|--------|---------|
| **Primary** | ~35° | ~18° | **55%** | RTS gameplay angle — dorsal/plan silhouette |
| Secondary | ~125° | ~105° | 25% | Quartering top-down — stern/port mass |
| Tertiary | ~215° | ~198° | 20% | Opposite quartering — avoid belly/underside focus |

Pure 90° side elevation and underside-heavy angles are **not** primary score drivers.

## Asset kinds

| Kind | CLI `--category` | Examples | Builder entry |
|------|------------------|----------|---------------|
| **Spaceship** | `ship` | `fighter_basic`, `dreadnought` | `RaceShipMeshes.Build(race, hull)` |
| **Station** | `station` | `command_center`, `shipyard_large` | `RaceStationMeshes.Build(type, race)` |
| **Object** | `object` | `shield_generator`, `resource_node`, `laser` | `ModelMeshSource.Build("object", id)` |

Eight playable races (from `race_visuals.json`): **terran**, **vesper**, **korath**, **aetherian**, **nexar**, **solari**, **voidborn**, **cryo**.

## Triangle pattern anti-pattern (Geometry + Materials penalty)

**Visible triangle patterns** on hull or deck surfaces indicate **incomplete mesh definition** or **bad texture wrap** (per-triangle vertex luminance instead of uniform material zones). These read as noisy facets in the primary top-down panel and are scored negatively.

| Signal | Meaning | Detection |
|--------|---------|-----------|
| **Slivers** | Narrow elongation tris | min-edge / max-edge &lt; 0.18 |
| **Fishbone** | Alternating chevron strips along spine | 4+ ±X zigzag chains along +Z |
| **Facet seams** | Bad texture wrap on flat panels | Per-triangle vertex lum spread &gt; 0.13 |
| **Micro-facets** | Incomplete surfacing (should be boxes) | Tiny area + thin aspect |

| Penalty | Max deduction | Notes keyword |
|---------|---------------|---------------|
| Combined tri-pattern severity | **−8 pts** Geometry | `tri-pattern` in Geometry notes |
| Facet seam / wrap ratio | **−3 pts** Materials | `texture-wrap` in Materials notes |

**Fix guidance:** Replace facet strips with `AddBox`/`TriColored` flush panels, merge coplanar faces, assign **uniform vertex luminance per material zone** on surfaces meant to read flat.

## Per-asset categories (100 total)

| Category | Max | Ships | Stations | Objects |
|----------|-----|-------|----------|---------|
| Silhouette / Massing / IconRead | 17 | Dorsal aspect, forward bias, low profile height | **Plan footprint**, pad radius, deck clusters (not tall spires) | Compact icon, center bias |
| Geometry | 17 | Triangle sweet spot by hull class; tri-pattern penalty | Higher tri budget; tri-pattern penalty | Small-icon budget |
| Materials | 16 | Luminance bands, range, grit; texture-wrap penalty | Same + station deck panels | Color pop |
| Proportions / Scale | 12 | Hull envelope fit | Pad radius / moderate height | World span vs gameplay |
| SurfaceDetail | 12 | Keel, accents, tri detail | Deck tiers, ring structures | Accent ratio |
| RaceIdentity | 10 | Palette + accent presence | Full race scoring | Clarity baseline (~6+) |
| Screenshot | 16 | 3 oblique top-down panels; panel 1 = 55% | Same; plan mass must read in panel 1 | Tighter fg target |

### Station massing (plan-view first)

Stations are judged like **cities from above**, not skyscrapers in space.

| Reward | Penalize |
|--------|----------|
| Wide pad footprint, ring/hub structures visible in plan | Lone vertical towers with empty space around them |
| Clustered superstructure on deck | Height ≫ width (spire landmark scoring) |
| Readable build-pad silhouette at RTS zoom | Pure side-elevation landmark mass |

Massing formula favors **low height/width ratio** and strong footprint fit.

### Screenshot expectations

- **3-panel composite** in one PNG — all oblique top-down quadrants (ships: 35° / 125° / 215°; stations: 18° / 105° / 198°)
- **2× camera pullback** — full asset visible in every panel
- **Weighted average:** panel 1 = 55%, panel 2 = 25%, panel 3 = 20%, then +balance bonus (up to +2)
- Scorer penalizes empty panels and rewards balanced foreground across all three views
- Without PNG, Screenshot = 0 (geometry-only score caps at 84)

## Race-level score (`--score-race`)

Aggregates **19 ships + 10 stations** per race (no objects in race rollup).

| Field | Meaning |
|-------|---------|
| `OverallScore` | Mean of all 29 race-tied assets |
| `ShipFleetScore` | Mean of 19 hulls |
| `StationFleetScore` | Mean of 10 bases |
| `RaceIdentityAverage` | Mean RaceIdentity sub-score |
| `WeakestAssets` | Bottom 5 `kind/model (score)` for prioritization |

Run all eight races:

```powershell
dotnet run --project SharpOpenGl -- --score-all-races --output model-improvement/race-leaderboard.json
```

## Improvement thresholds (scorer → do-better)

| Signal | Action |
|--------|--------|
| Form category &lt; 72% of max | Shape/massing/icon pass — optimize **dorsal/plan** silhouette |
| Geometry notes contain `tri-pattern` | Remove visible facet triangles; merge to flush panels/boxes |
| Materials notes contain `texture-wrap` | Align vertex luminance per material zone; no per-triangle gradients on flat panels |
| RaceIdentity &lt; 7 | Push `race_visuals.json` palette into vertex bands |
| Screenshot &lt; 9 | Relight panel 1 (RTS primary); verify all 3 oblique panels |
| Station Massing &lt; 12 | Widen pad, cluster deck mass, shorten spires |
| Race `ShipFleetScore` &lt; `StationFleetScore` − 5 | Prioritize fleet silhouettes |
| Asset in `WeakestAssets` | Targeted loop for that hull/station |

## Capture commands

```powershell
# Ship (legacy --hull still works)
dotnet run --project SharpOpenGl -- --mesh-preview --race vesper --category ship --model fighter_basic --screenshot-path mesh-loop-01.png
dotnet run --project SharpOpenGl -- --score-mesh --race vesper --category ship --model fighter_basic --screenshot-path mesh-loop-01.png --output model-improvement/vesper/fighter_basic/scores/loop-01.json

# Station
dotnet run --project SharpOpenGl -- --mesh-preview --race korath --category station --model command_center --screenshot-path mesh-korath-cc.png

# Object (race-neutral)
dotnet run --project SharpOpenGl -- --mesh-preview --category object --model shield_generator --screenshot-path mesh-shield.png

# Race audit
dotnet run --project SharpOpenGl -- --score-race --race vesper --output model-improvement/vesper/race-score.json
```