# Render tricks — low-cost combat & harvest VFX

Authoritative techniques for `/combat-visual-polish`. Every trick here is **cheaper** than spawning new procedural meshes per frame.

## Golden rules

1. **Upload once, tint forever** — register VAOs in `LoadProjectileMeshes`; change `RenderComponent` color/scale at runtime.
2. **Lines over solids for beams** — `BuildBeamStreak` and line-mode `BuildWaveRing` are fewer verts than hull meshes.
3. **Billboards for flashes** — camera-facing quads with additive blend; lifetime < 0.3s.
4. **Pool particles** — reuse `ParticleEmitter` instances; cap active emitters (see `FogNebulaOverlay.MaxActiveChunks` pattern).
5. **No per-frame `UploadMesh`** — if you need dynamic geometry, write to a pre-allocated vertex buffer and update sub-range only.

---

## Projectile mesh reuse

**Entry point:** `SharpOpenGl/EngineWindow.Projectiles.cs` → `LoadProjectileMeshes()`

```csharp
// GOOD — once at startup
RegisterProjectileMesh("projectile/beam",
    MeshBuilder.UploadProcedural(ProceduralMeshes.BuildBeamStreak(color, 3f)));

// BAD — every shot
MeshBuilder.UploadProcedural(ProceduralMeshes.BuildBeamStreak(color, length));
```

**Runtime binding:** `ResolveProjectileMeshes()` assigns `render.MeshId` from `_projectileMeshes` dictionary by `render.MeshKey`.

**Color-only variation:** override `RenderComponent` tint from `WeaponProfile.Color` — do not rebuild `BuildLaserBolt` per weapon instance.

| Builder | Cost profile | Best for |
|---------|--------------|----------|
| `BuildLaserBolt` | Low tri count | Linear kinetic shots |
| `BuildBeamStreak` | Thin quad strip | Beams, tractor links |
| `BuildEnergyPulse` | Small sphere-ish | Cannon pulses |
| `BuildWaveRing` | Line loop | AoE expansion (`lines: true`) |
| `BuildTorpedo` / `BuildRocket` | Medium | Homing only — don't duplicate per race |

---

## Streak billboards

For instant beams and tractor mining links:

1. Use pre-uploaded `projectile/beam` mesh.
2. Scale Y or length via `TransformComponent.Scale` along shot vector.
3. Orient with `BuildProjectileModelMatrix` — align `rotation.Y` to `atan2(dx, dz)`.
4. Optional: spawn **additive** flash quad at muzzle (separate pooled entity, 0.15s lifetime).

**Tractor beam (`MiningVisualSystem` / `EngineWindow.MiningVfx.cs`):**
- Tag collector with `TractorBeamVisualComponent` in `TractorBeam` mode.
- Draw line streak between collector and node — reuse beam VAO, scale to distance.
- Pulse alpha on `TractorPulseInterval` (0.65s) — color lerp only, no geometry rebuild.

---

## Beam flashes

Short additive overlays at fire and impact:

| Event | Technique | Duration |
|-------|-----------|----------|
| Muzzle fire | Scale up `energy_pulse` 1.5×, alpha fade | 0.1–0.2s |
| Beam impact | Line cross or ring flash at target | 0.15–0.25s |
| Shield hit | Existing `_shieldRingPulse` — don't add mesh | reuse pulse timer |
| Attack hover | `_attackHoverPulse` tint — `RenderComponent` color | reuse |

Avoid spawning new entities for every bullet — batch impact flashes into pooled particle burst (≤ 12 points).

---

## Additive blend overlays

Follow `FogNebulaOverlay` overlay discipline:

- Draw combat overlays **after** opaque terrain/entities, **before** UI rings.
- Use point/line particle pass or dedicated additive shader pass.
- Keep alpha ≤ 0.85 for unexplored-tier density; combat flashes typically 0.4–0.7 peak.
- **Never** stack more than 2 additive layers on same pixel (blowout → `overlayCraft` deduction).

---

## Particle caps

| System | Suggested cap | Reference |
|--------|---------------|-----------|
| Fog nebula chunks | 400 active emitters | `FogNebulaOverlay.MaxActiveChunks` |
| Combat impact burst | 32 points per event | pool + prune |
| Mining node dust | 16 points per active collector | `ParticleEffects` |
| Muzzle flash | 8 points | single emitter reuse |

**Prune pattern:** remove emitters when `Lifetime <= 0` or parent entity dead (`MiningVisualSystem.CleanupCollectorVisuals`).

---

## Color-only `RenderComponent` tricks

```csharp
// Scale wave ring without new mesh
float progress = 1f - proj.Lifetime / proj.MaxLifetime;
scale = new Vector3(ring, 1f, ring);  // BuildProjectileModelMatrix

// Weapon color from profile — no mesh rebuild
render.Color = new Vector4(profile.Color.X, profile.Color.Y, profile.Color.Z, 1f);
```

Supported runtime fields (prefer these over mesh rebuild):
- `TransformComponent.Position / Scale / EulerAngles`
- `RenderComponent` color/tint if exposed
- `ProjectileVisualComponent.Scale` for wave rings

---

## Mining mode low-cost patterns

| Mode | Implementation | Avoid |
|------|----------------|-------|
| `TractorBeam` | `TractorBeamVisualComponent` + beam streak line | Spawning torpedo mesh as beam |
| `Drones` | Reuse small drone mesh handles via `MiningVisualMeshHandles` | New drone mesh per collector |
| `Eva` | Crew entity on node — position snap, no animation mesh | Full EVA rig per frame |

**Drone count:** `Math.Clamp((int)(collector.HarvestRate / 8f) + 1, 1, 3)` — never exceed 3 for perf.

---

## Anti-patterns (auto-fail PerformanceBudget)

| Anti-pattern | Why it hurts | Fix |
|--------------|--------------|-----|
| `UploadProcedural` in `ProjectileSystem.Update` | GPU upload every frame | Pre-register mesh keys |
| New `float[]` per shot in hot loop | GC + upload pressure | Static buffer pool |
| Unique mesh per weapon color | VAO explosion | Tint registered mesh |
| Permanent impact decals | Unbounded entity count | Timed particle burst |
| Full-screen flash on every hit | Additive blowout + fill rate | Localized ring only |

---

## Verification commands

```powershell
# Grep for per-frame upload smells
rg "UploadProcedural|UploadMesh" SharpOpenGl/ SharpOpenGl.Engine/ --glob "*.cs"

# Confirm projectile registration is startup-only
rg "LoadProjectileMeshes|RegisterProjectileMesh" SharpOpenGl/

# Mining visual tests
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~MiningVisual"
```

Any `UploadProcedural` hit inside a `Update(` loop or per-shot spawn path → file **PerformanceBudget** work order before other polish.