# Combat visual rubric

SharpOpenGl scores combat and harvest visuals on a **0–100** scale across five categories. Use when interpreting `score-NN.json` from `/combat-visual-polish`.

**Core constraint:** improvements must raise perceived quality while **lowering or holding** GPU cost. Per-frame mesh uploads and unbounded particle spawn are automatic **PerformanceBudget** failures.

## RTS viewing context

Combat VFX are judged at **oblique top-down RTS zoom** — the same camera as gameplay. Effects must read from above at slight elevation, not only in mesh-preview close-ups.

| Signal | Reward | Penalize |
|--------|--------|----------|
| Projectile trail | Origin and heading obvious at map zoom | Sub-pixel slivers invisible from camera |
| Beam weapon | Instant hit line or streak between muzzle and target | Delayed bolt that looks like a slow projectile |
| Mining beam | Collector→node link visible while `Collecting` | Ore node activity with no ship feedback |
| Hit feedback | Brief flash/ring at impact point | Silent damage with no visual cue |

## Categories (100 total)

| Category | Max | Measures |
|----------|-----|----------|
| **Readability** | 20 | Traceability of shots, beams, pulses, and mining links at RTS zoom |
| **WeaponIdentity** | 20 | Distinct look per `WeaponVisualKind` and weapon family |
| **HarvestFeedback** | 20 | Mode-specific mining visuals (drone shuttle, EVA crew, tractor beam) |
| **PerformanceBudget** | 20 | Mesh reuse, no per-frame uploads, particle/emitter caps |
| **OverlayCraft** | 20 | Intentional overlays — additive flashes, billboards, line primitives |

**Pass threshold:** overall ≥ 80; each category ≥ 16.

---

## Readability (20)

| Score band | Criteria |
|------------|----------|
| 18–20 | Every active weapon fire traceable; mining activity obvious; impact/AoE radius readable |
| 14–17 | Most shots readable; occasional confusion between similar motion types |
| 10–13 | Several weapons blend together; mining only obvious on select |
| 0–9 | Combat unreadable at default zoom; beams invisible or misleading |

### Checklist — weapons

- [ ] Linear projectiles face travel direction (`BuildProjectileModelMatrix` rotation)
- [ ] `ProjectileType.Instant` / beam weapons show hit line or streak, not slow bolt
- [ ] AoE / wave rings expand predictably (`WeaponVisualKind.Wave` scale curve)
- [ ] Homing projectiles remain visible for ≥ 30% of lifetime
- [ ] Muzzle flash or brief streak at fire origin (overlay, not new mesh)

### Checklist — harvest

- [ ] `CollectorState.Collecting` always has at least one visual anchor (beam, drone, or crew)
- [ ] Tractor beam links collector position to `AssignedNode`
- [ ] Drone shuttles readable in round-trip (`DroneShuttleDuration`)
- [ ] EVA crew placement on node surface visible from above

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Weapon type indistinguishable at gameplay zoom | −4 | `readability-blend` |
| Beam looks like slow projectile | −5 | `beam-as-bolt` |
| Mining active with zero VFX | −6 | `silent-harvest` |
| Impact/AoE radius unclear | −3 | `aoe-read` |

---

## WeaponIdentity (20)

Maps to `WeaponProfiles` / `WeaponVisualKind` / `LoadProjectileMeshes`.

| Score band | Criteria |
|------------|----------|
| 18–20 | All seven visual kinds distinct in color, silhouette, and motion |
| 14–17 | Most kinds distinct; 1–2 families share mesh + color |
| 10–13 | Heavy reliance on `laser_bolt` default |
| 0–9 | All weapons appear identical |

### Visual kind expectations

| `WeaponVisualKind` | Mesh key | Identity cues |
|--------------------|----------|---------------|
| `LaserBolt` | `projectile/laser_bolt` | Warm narrow bolt, linear motion |
| `Beam` | `projectile/beam` | Cool elongated streak, instant hit |
| `Torpedo` | `projectile/torpedo` | Bulky hull, homing arc |
| `Rocket` | `projectile/rocket` | Finned body, homing |
| `Bomb` | `projectile/bomb` | Heavy drop, AoE |
| `EnergyPulse` | `projectile/energy_pulse` | Spherical pulse, shorter lifetime |
| `Wave` | `projectile/wave` | Line ring expansion |

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Wrong `MeshKey` for weapon family | −4 | `mesh-mismatch` |
| Color not aligned with `WeaponProfile.Color` | −3 | `color-drift` |
| Default laser bolt used for non-laser | −5 | `default-fallback` |
| `WeaponProfiles` / `LoadProjectileMeshes` key desync | −6 | `registry-desync` |

---

## HarvestFeedback (20)

Maps to `MiningVisualSystem`, `HarvestOrbitSystem`, `ResourceCollectorComponent`.

| Score band | Criteria |
|------------|----------|
| 18–20 | All three harvest modes visually distinct and informative |
| 14–17 | Two modes strong; one weak or generic |
| 10–13 | Only tractor or only drones polished |
| 0–9 | Mining looks identical across modes or absent |

### Mode expectations

| `HarvestMode` | Low-cost visual |
|---------------|-----------------|
| `TractorBeam` | Line streak / beam flash between ship and node; `TractorPulseInterval` pulses |
| `Drones` | 1–3 shuttle drones (`desiredDrones` clamp); ore visible on return |
| `Eva` | Crew entities on node surface; no tractor component |

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Tractor mode missing beam component | −6 | `tractor-missing` |
| Drones not visible during collect | −5 | `drone-invisible` |
| EVA uses projectile mesh | −4 | `eva-wrong-mesh` |
| Visuals persist after `Collecting` ends | −3 | `orphan-vfx` |

---

## PerformanceBudget (20)

**Gate category** — scores below 16 block sign-off regardless of visual flair.

| Score band | Criteria |
|------------|----------|
| 18–20 | All meshes pre-uploaded; particles capped; color/scale-only runtime updates |
| 14–17 | Minor per-spawn allocations; caps documented |
| 10–13 | Occasional per-frame geometry rebuild |
| 0–9 | Per-frame `UploadMesh`/`UploadProcedural`; unbounded emitters |

### Hard rules

| Rule | Pass | Fail |
|------|------|------|
| Projectile VAO registration | Once in `LoadProjectileMeshes` | Per-shot upload |
| Mining beam geometry | Reused streak / line draw | New mesh per `TractorPulseInterval` |
| Particle emitters | Pooled with max active count | Spawn without prune |
| `RenderComponent` updates | Color, scale, transform only | MeshId rebuild each frame |

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Per-frame procedural upload | −8 | `per-frame-upload` |
| Unbounded particle spawn | −6 | `particle-unbounded` |
| Duplicate VAO per weapon instance | −5 | `vao-duplication` |
| GC pressure from per-shot float[] | −3 | `alloc-per-shot` |

---

## OverlayCraft (20)

Rewards intentional low-cost layering — the same craft as `FogNebulaOverlay` but for combat.

| Score band | Criteria |
|------------|----------|
| 18–20 | Additive flashes, billboards, and line overlays feel cohesive and timed |
| 14–17 | Good flashes; some overlays too long or muddy |
| 10–13 | Flat untextured meshes only; no overlay pass |
| 0–9 | Harsh pop-in/out; z-fighting; additive blowout |

### Techniques (see `render-tricks.md`)

- Streak billboards aligned to shot vector
- Short-lived additive color flash on `RenderComponent`
- Line-mode `BuildWaveRing` for AoE
- Beam flash quad at muzzle (single quad, no upload)
- Pooled impact particles (≤ 32 per event)

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Overlay persists too long (> 0.5s for hit flash) | −3 | `overlay-linger` |
| Additive saturation blows out scene | −4 | `additive-blowout` |
| No depth bias — overlays z-fight terrain | −3 | `z-fight` |
| Missing muzzle/impact flash entirely | −5 | `no-flash` |

---

## Area-scoped scoring notes

When `area` ≠ `all`, still fill all five category objects:

| area | Primary categories | Others |
|------|-------------------|--------|
| `weapons` | Readability, WeaponIdentity, OverlayCraft | HarvestFeedback: note `N/A — weapons scope`; PerformanceBudget: still audit projectile path |
| `harvest` | HarvestFeedback, Readability, OverlayCraft | WeaponIdentity: note `N/A — harvest scope` |
| `combat-feedback` | Readability, OverlayCraft, PerformanceBudget | Shield/attack hover pulses, ability VFX |
| `all` | All five weighted equally |

Do **not** award 20/20 on N/A categories — use 16–18 with explicit `N/A` notes if genuinely unexamined, or score honestly if cross-cutting.

## Suggested next actions by lowest category

| Lowest category | First files to open |
|-----------------|---------------------|
| Readability | `EngineWindow.Projectiles.cs`, `WeaponProfiles.cs`, `MiningVisualSystem.cs` |
| WeaponIdentity | `WeaponProfiles.cs`, `ProceduralMeshes.cs`, `EngineWindow.Projectiles.cs` |
| HarvestFeedback | `MiningVisualSystem.cs`, `EngineWindow.MiningVfx.cs`, `HarvestOrbitSystem.cs` |
| PerformanceBudget | `EngineWindow.Projectiles.cs`, `ParticleEffects.cs`, render loop upload sites |
| OverlayCraft | `EngineWindow.MiningVfx.cs`, `EngineWindow.cs` (pulse timers), `ParticleEffects.cs` |