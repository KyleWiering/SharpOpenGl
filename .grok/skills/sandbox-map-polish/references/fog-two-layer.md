# Fog two-layer contract (authoritative)

**This document is law for `/sandbox-map-polish`.** Any change that violates it fails the **FogTwoLayer** gate.

## The three fog states — only two get overlays

`FogState` (`SharpOpenGl.Engine/Grid/FogState.cs`) defines per-cell visibility:

| State | Value | Meaning | Overlay? |
|-------|-------|---------|----------|
| `Unexplored` | 0 | Never seen by player | **YES — dense nebula** |
| `Explored` | 1 | Previously seen, not in current sight | **YES — light memory fog** |
| `Visible` | 2 | In friendly sight range now | **NO — clear view** |

### Visual intent

```
Unexplored  →  "I have no idea what's here"     (high alpha, dense particles)
Explored    →  "I've been here before"          (lower alpha, sparse particles)
Visible     →  "I can see it right now"         (zero veil — full clarity)
```

**There is no fourth state. There is no third overlay tier. Visible is never dimmed.**

---

## Resolution algorithm

`FogNebulaOverlay.ResolveOverlayState` is the **single source of truth** for whether a fog chunk gets an emitter.

```csharp
// Simplified from FogNebulaOverlay.cs
for each cell in chunk:
    if state == Visible → anyVisible = true
    if state != Unexplored → anyExplored = true

if (anyVisible) return null;           // NO OVERLAY
return anyExplored ? Explored : Unexplored;
```

### Truth table (per chunk)

| Cells in chunk | Result | Overlay |
|----------------|--------|---------|
| Any `Visible` | `null` | None |
| All `Unexplored` | `Unexplored` | Dense nebula |
| Mix `Explored` only (no Visible) | `Explored` | Light memory fog |
| Mix `Explored` + `Unexplored` (no Visible) | `Explored` | Light memory fog |

**Mixed Unexplored + Explored without Visible** resolves to `Explored` — the chunk was partially seen before. Do not split into sub-chunk overlays.

---

## Emitter tuning (two layers only)

`FogNebulaOverlay.Config` exposes testable constants:

| Constant | Unexplored | Explored |
|----------|------------|----------|
| `UnexploredEmitRate` | 34f | — |
| `ExploredEmitRate` | — | 13f |
| `UnexploredStartAlpha` | 0.85f | — |
| `ExploredStartAlpha` | — | 0.42f |
| `UnexploredParticleLifetime` | 5f | — |
| `ExploredParticleLifetime` | — | 3.5f |

### Tuning rules

1. **Unexplored must always read denser/darker than Explored.**
   - `UnexploredEmitRate` > `ExploredEmitRate`
   - `UnexploredStartAlpha` > `ExploredStartAlpha`
2. **Never add `VisibleEmitRate` or `VisibleStartAlpha`** — Visible has no emitter.
3. Palette tweaks go through `FogVisualPalette` and `ParticleEffects.CreateFogNebulaChunk` — still only two branches.

---

## Render pipeline order

From `EngineWindow.Map.cs` → `RenderFogOverlay`:

```
1. Terrain / ground quads
2. Entities (ships, stations, resources)
3. Fog nebula particle veil (Unexplored + Explored chunks only)
4. Objective markers, selection rings, UI
```

Visible territory must show terrain and entities **without** nebula particles on top.

---

## State transitions

Managed by `FogOfWar` + `FogOfWarSystem`:

```
Unexplored ──(unit enters sight)──► Visible
Visible ──(unit leaves sight)──────► Explored
Explored ──(never reverts)────────► stays Explored
Unexplored ──(RevealAreaAt)───────► Visible (e.g. initial reveal)
```

- **Explored never returns to Unexplored** — memory persists.
- **Visible** is transient — only while in sight.
- `RevealAreaAt` in `EngineWindow.Map.cs` calls `_fogOfWar.Reveal(0, gx, gy, radius)` at setup.

---

## Forbidden changes

| Forbidden | Why |
|-----------|-----|
| Dim/filter Visible cells | Breaks core RTS readability |
| Add `SemiVisible` or `Shrouded` enum value | Creates third overlay tier |
| Return `FogState.Visible` from `ResolveOverlayState` | Would create Visible emitter |
| Full-screen black for Unexplored | Use nebula particles, not opaque black quad |
| Per-cell fog overlay (not chunked) | Perf regression; breaks emitter pooling |
| Same alpha for Unexplored and Explored | Fails two-layer distinction |

---

## Implementation map

| Concern | File |
|---------|------|
| Enum | `SharpOpenGl.Engine/Grid/FogState.cs` |
| Per-cell state storage | `SharpOpenGl.Engine/Grid/FogOfWar.cs` |
| Sight updates | `SharpOpenGl.Engine/Grid/FogOfWarSystem.cs` |
| Overlay resolution | `SharpOpenGl.Engine/Rendering/FogNebulaOverlay.cs` |
| Particle creation | `SharpOpenGl.Engine/Rendering/ParticleEffects.cs` → `CreateFogNebulaChunk` |
| Colors | `SharpOpenGl.Engine/Rendering/FogVisualPalette.cs` |
| Render pass | `SharpOpenGl/EngineWindow.Map.cs` → `RenderFogOverlay` |
| Initial reveal | `EngineWindow.Map.cs` → `RevealAreaAt`; `sandbox.json` → `initialRevealRadius` |
| Minimap fog | `SharpOpenGl.Engine/UI/Widgets/Minimap.cs` |

---

## Tests that enforce the contract

Run after every fog edit:

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~FogNebulaOverlay|FogVisualPalette"
```

Key assertions (see `FogNebulaOverlayTests.cs`):

- Chunk with any Visible cell → `ResolveOverlayState` returns `null`
- All Unexplored chunk → returns `Unexplored`
- All Explored (no Visible) → returns `Explored`
- `Config` unexplored rates/alphas exceed explored counterparts
- `HasEmitterForChunk` false for fully visible chunks after `Sync`

---

## Correct vs incorrect examples

### Correct: tune Explored to be lighter

```csharp
// FogNebulaOverlay.Config — OK
public const float ExploredStartAlpha = 0.38f;  // still < UnexploredStartAlpha (0.85f)
```

### Incorrect: add Visible haze

```csharp
// NEVER DO THIS
if (anyVisible) return FogState.Visible;  // would spawn emitter — WRONG
```

### Incorrect: third layer via alpha ladder

```csharp
// NEVER DO THIS — three visual tiers
if (mostlyVisible) return FogState.Explored with alpha 0.1f;
```

### Correct: chunk with one visible cell

```
Chunk 10×10: 1 cell Visible, 99 Explored
→ ResolveOverlayState returns null
→ No nebula emitter for entire chunk
```

This is intentional — any current sight line clears the chunk veil completely.

---

## Checklist before merge

- [ ] `ResolveOverlayState` still returns `null` when `anyVisible`
- [ ] No new `FogState` enum values
- [ ] `Config` has only Unexplored/Explored rate and alpha pairs
- [ ] `FogNebulaOverlayTests` green
- [ ] Manual check: move unit into unexplored chunk → veil clears; move away → light memory fog returns
- [ ] Manual check: never-seen chunk stays dense nebula