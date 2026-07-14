# Key files — combat visual polish

Paths relative to repo root (`REPO`). Read ≤3 files per evaluation pass.

## Weapons VFX

| File | Role |
|------|------|
| `SharpOpenGl.Engine/Combat/WeaponProfiles.cs` | `WeaponVisualKind`, colors, speeds, `MeshKey()` mapping |
| `SharpOpenGl.Engine/ECS/ProjectileSystem.cs` | Spawns projectiles, motion types, lifetime |
| `SharpOpenGl.Engine/ECS/CombatSystem.cs` | Weapon fire, targeting, damage application |
| `SharpOpenGl.Engine/ECS/AbilitySystem.cs` | Special abilities and non-standard shots |
| `SharpOpenGl/EngineWindow.Projectiles.cs` | `LoadProjectileMeshes`, `ResolveProjectileMeshes`, `BuildProjectileModelMatrix` |
| `SharpOpenGl.Engine/Rendering/ProceduralMeshes.cs` | `BuildLaserBolt`, `BuildBeamStreak`, `BuildEnergyPulse`, `BuildWaveRing` |
| `SharpOpenGl.Engine/Rendering/ModelMeshSource.cs` | Fallback procedural builders for migration |

### Weapon flow

```
WeaponComponent → WeaponProfiles.Resolve() → ProjectileSystem spawn
    → RenderComponent.MeshKey → LoadProjectileMeshes VAO
    → BuildProjectileModelMatrix (scale/rotate)
```

## Harvest / mining VFX

| File | Role |
|------|------|
| `SharpOpenGl.Engine/ECS/MiningVisualSystem.cs` | Drone/EVA/tractor visuals while `Collecting` |
| `SharpOpenGl.Engine/ECS/MiningVisualComponent.cs` | Per-collector visual state tags |
| `SharpOpenGl.Engine/ECS/ResourceCollectorComponent.cs` | `HarvestMode`, `CollectorState`, assigned node |
| `SharpOpenGl.Engine/ECS/HarvestOrbitSystem.cs` | Orbit positioning around resource nodes |
| `SharpOpenGl.Engine/ECS/HarvestOrbitHelper.cs` | Orbit math helpers |
| `SharpOpenGl/EngineWindow.MiningVfx.cs` | Render pass for mining beams and drones |
| `SharpOpenGl.Engine/ECS/ResourceSystem.cs` | Tractor pulse timer gate (`TractorPulseInterval`) |

### Harvest mode entry

```
ResourceCollectorComponent.State == Collecting
    → MiningVisualSystem.UpdateCollecting()
    → Drones | Eva | TractorBeam branch
    → TractorBeamVisualComponent (tractor only)
```

## Combat feedback (non-projectile)

| File | Role |
|------|------|
| `SharpOpenGl/EngineWindow.cs` | `_attackHoverPulse`, `_shieldRingPulse`, render order |
| `SharpOpenGl.Engine/ECS/CombatSystem.cs` | Hit resolution triggers |
| `SharpOpenGl.Engine/Grid/CombatFogGate.cs` | LOS — affects what player sees firing |

## Shared rendering infrastructure

| File | Role |
|------|------|
| `SharpOpenGl.Engine/Rendering/ParticleEffects.cs` | Emitter creation, fog/combat particles |
| `SharpOpenGl.Engine/Rendering/FogNebulaOverlay.cs` | **Overlay pattern reference** — pooled emitters, chunk caps |
| `SharpOpenGl.Engine/Rendering/MeshBuilder.cs` | `UploadProcedural`, `BuildGroundQuad` |

## Config & balance

| File | Role |
|------|------|
| `GameData/Config/combat_balance.json` | Projectile scale, damage tuning |
| `SharpOpenGl.Engine/Combat/CombatBalance.cs` | `ScaleProjectile` and balance helpers |

## Tests

| Test class | Evidence for |
|------------|--------------|
| `SharpOpenGl.Tests/Combat/CombatSystemTests.cs` | Weapon fire, damage, combat loop |
| `SharpOpenGl.Tests/Config/CombatBalanceTests.cs` | Balance config integrity |
| `SharpOpenGl.Tests/Economy/MiningVisualTests.cs` | Drone shuttle, tractor tagging |
| `SharpOpenGl.Tests/ECS/UtilityArticulationTests.cs` | `MiningVisualSystem_tractor_mode_still_tags_beam_visual` |
| `SharpOpenGl.Tests/Economy/ResourceSystemTests.cs` | Tractor pulse integration |

### Test command

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~CombatSystem|CombatBalance|MiningVisual|UtilityArticulation|Projectile"
```

## Edit priority by area

| area | Edit first | Edit second | Rarely |
|------|------------|-------------|--------|
| `weapons` | `WeaponProfiles.cs`, `EngineWindow.Projectiles.cs` | `ProceduralMeshes.cs` | `CombatSystem.cs` |
| `harvest` | `MiningVisualSystem.cs`, `EngineWindow.MiningVfx.cs` | `MiningVisualComponent.cs` | `HarvestOrbitSystem.cs` |
| `combat-feedback` | `EngineWindow.cs` (pulses) | `AbilitySystem.cs` | `CombatSystem.cs` |
| `all` | Above by lowest rubric category | — | — |