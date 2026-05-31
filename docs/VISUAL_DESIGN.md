# Visual Design Workflow — SharpOpenGL Space RTS

This document describes the visual design pipeline introduced in **Phase 8**.
It covers how meshes, materials, particles, and LOD work together and how
artists or designers can add or modify visual content.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Mesh Pipeline](#2-mesh-pipeline)
   - [OBJ Mesh Loader](#21-obj-mesh-loader)
   - [Procedural Ship Generator](#22-procedural-ship-generator)
   - [Default Fallback Meshes](#23-default-fallback-meshes)
   - [Mesh Registry](#24-mesh-registry)
3. [Material System](#3-material-system)
4. [Level of Detail (LOD)](#4-level-of-detail-lod)
5. [Billboard Sprite Fallback](#5-billboard-sprite-fallback)
6. [Particle Effects](#6-particle-effects)
   - [Engine Trails](#61-engine-trails)
   - [Explosions](#62-explosions)
   - [Shield Bubbles](#63-shield-bubbles)
   - [Weapon Fire](#64-weapon-fire)
7. [Ship Designer Screen](#7-ship-designer-screen)
8. [Adding a New Ship Mesh](#8-adding-a-new-ship-mesh)
9. [Directory Layout](#9-directory-layout)

---

## 1. Overview

```
GameData/
├── Meshes/
│   ├── default_ship.obj
│   ├── default_base.obj
│   └── default_projectile.obj
└── Ships/
    └── hero_default.json   ← references mesh key "meshes/hero_vanguard.obj"
```

The rendering pipeline follows these steps each frame:

1. **MeshLodSystem** selects the correct LOD mesh for each entity.
2. **BillboardSystem** decides whether an entity is rendered as a 3-D mesh or a flat sprite.
3. **ParticleSystem** advances all particle emitters.
4. **RenderSystem** draws each visible entity via the `IRenderer`.

---

## 2. Mesh Pipeline

### 2.1 OBJ Mesh Loader

`ObjMeshLoader` (in `SharpOpenGl.Engine/Rendering/`) converts a Wavefront `.obj`
file into vertex data (`ObjMeshData`) without touching the GPU.

**Supported OBJ features:**
- `v x y z` — vertex positions
- `vn x y z` — vertex normals
- `f v1[/vt1[/vn1]] v2 v3 [v4…]` — faces (triangles and quads; quads are fan-triangulated)
- `#` comment lines

**Vertex layout:** interleaved `float[6]` per vertex — `{x, y, z, nx, ny, nz}`.
When no normals are present in the file, `(0, 1, 0)` (up) is used as a fallback.

To upload the parsed data to the GPU:
```csharp
ObjMeshData? data = ObjMeshLoader.Parse("GameData/Meshes/my_ship.obj");
if (data != null)
{
    var (vao, vbo, count) = ObjMeshLoader.Upload(data);
    meshRegistry.Register("meshes/my_ship", vao, vbo, count);
}
```

### 2.2 Procedural Ship Generator

`ProceduralShipGenerator.Generate(ShipParameters)` creates a simple triangulated
ship hull from numeric parameters — no art required for prototyping.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Length` | Hull length bow-to-stern | 2.0 |
| `Width` | Maximum hull width | 1.0 |
| `WingAngle` | Wing sweep angle (degrees, positive = swept back) | 30° |
| `WingLength` | Wing tip extension from fuselage edge | 0.5 |
| `EngineCount` | Number of nozzles (0–4) | 2 |

Colours are **not** embedded in the vertex data — the geometry uses normals (pos3 + normal3 layout).
Apply hull/engine colours after spawning via `MaterialComponent` on the entity.

The result is the same `ObjMeshData` format as a loaded `.obj` file.

### 2.3 Default Fallback Meshes

Three placeholder meshes live in `GameData/Meshes/`:

| File | Represents |
|------|-----------|
| `default_ship.obj` | Any ship whose mesh key is missing |
| `default_base.obj` | Any structure/base whose mesh key is missing |
| `default_projectile.obj` | Any projectile whose mesh key is missing |

The engine automatically falls back to these via `MeshRegistry.GetOrFallback` and the
`DefaultAssets.DefaultShipKey` / `DefaultBaseKey` / `DefaultProjectileKey` constants.

### 2.4 Mesh Registry

`MeshRegistry` maps string keys (e.g. `"meshes/hero_vanguard"`) to GPU handles.

```csharp
// At startup, load all default meshes:
registry.Register("default_ship",       ObjMeshLoader.Upload(ObjMeshLoader.Parse(…)));
registry.Register("default_base",       ObjMeshLoader.Upload(ObjMeshLoader.Parse(…)));
registry.Register("default_projectile", ObjMeshLoader.Upload(ObjMeshLoader.Parse(…)));

// Entity's RenderComponent is wired up at spawn time:
var entry = registry.GetOrFallback(ship.Mesh, "default_ship");
render.MeshId      = entry!.Vao;
render.VertexCount = entry.VertexCount;
```

---

## 3. Material System

`Material` stores three visual properties:

| Property | Type | Description |
|----------|------|-------------|
| `DiffuseColor` | `Vector3` | Base surface colour (RGB, 0–1) |
| `EmissiveColor` | `Vector3` | Self-glow colour added on top |
| `Opacity` | `float` | 1 = opaque, 0 = invisible |

Attach a `MaterialComponent` to an entity to override default colours:
```csharp
world.AddComponent(entity, new MaterialComponent
{
    Material = Material.Emissive(
        diffuse: new Vector3(0.1f, 0.1f, 0.2f),
        emissive: new Vector3(0.0f, 0.8f, 1.0f))
});
```

Convenience factory methods: `Material.Solid(color)`, `Material.Emissive(diffuse, emissive)`,
`Material.Translucent(diffuse, opacity)`.

---

## 4. Level of Detail (LOD)

`MeshLod` holds three mesh handles at different detail levels:

| Level | Distance | Usage |
|-------|----------|-------|
| Detail | < `SimpleDistance` (default 80 u) | Full geometry |
| Simple | 80–250 u | Reduced polygon count |
| Icon | > `IconDistance` (default 250 u) | Minimal silhouette |

Attach a `MeshLodComponent` to an entity and the `MeshLodSystem` will automatically
swap `RenderComponent.MeshId` each frame:

```csharp
world.AddComponent(entity, new MeshLodComponent
{
    Lod = new MeshLod
    {
        DetailMesh = (detailVao, detailCount),
        SimpleMesh = (simpleVao, simpleCount),
        IconMesh   = (iconVao,   iconCount),
        SimpleDistance = 80f,
        IconDistance   = 250f,
    }
});
```

Missing levels (vao == 0) fall back to the next-lower detail level automatically.

---

## 5. Billboard Sprite Fallback

For entities beyond `BillboardComponent.FarThreshold` (default 400 u), the 3-D mesh
is hidden and replaced by a camera-facing flat quad.

```csharp
world.AddComponent(entity, new BillboardComponent
{
    Color        = new Vector4(0.3f, 0.4f, 0.9f, 1f),
    Width        = 1f,
    Height       = 1f,
    FarThreshold = 400f,
});
```

`BillboardSystem` sets `BillboardComponent.IsActive` and toggles `RenderComponent.Visible`.
The platform renderer checks `IsActive` to draw the billboard quad.

---

## 6. Particle Effects

All particle effects use `ParticleEmitter`. Add a `ParticleEmitterComponent` to any entity.
`ParticleSystem` advances all emitters each frame.

### 6.1 Engine Trails

```csharp
var emitter = ParticleEffects.CreateEngineTrail(
    origin:     engineNozzlePosition,
    exhaustDir: -ship.ForwardVector);

world.AddComponent(engineEntity, new ParticleEmitterComponent { Emitter = emitter });
```

### 6.2 Explosions

```csharp
var emitter = ParticleEffects.CreateExplosion(explosionOrigin, radius: 3f);
// Stop emitting after one frame to get a burst:
emitter.IsEmitting = false;
```

### 6.3 Shield Bubbles

```csharp
var emitter = ParticleEffects.CreateShieldBubble(shipPosition, shieldRadius: 2f);
// Attach to the ship entity so it follows movement
```

### 6.4 Weapon Fire

```csharp
// Laser bolt
var laser = ParticleEffects.CreateWeaponFire(muzzlePos, forward, isLaser: true);

// Missile trail
var trail = ParticleEffects.CreateWeaponFire(muzzlePos, forward, isLaser: false);
```

---

## 7. Ship Designer Screen

`ShipDesignerScreen` provides colour-picker buttons and a rotation control.
`ShipDesignerRenderer` draws the 3-D ship preview using those values:

```csharp
var renderer = new ShipDesignerRenderer(meshRegistry);

// Each frame while the designer is open:
renderer.AutoRotate(designerScreen, deltaTime);
renderer.Render(
    screen:      designerScreen,
    meshKey:     "meshes/hero_vanguard",
    fallbackKey: DefaultAssets.DefaultShipKey,
    renderer:    platformRenderer,
    projection:  projMatrix,
    view:        viewMatrix);
```

`DesignConfirmed` fires when the player clicks **Confirm Design**, passing the
`shipId`, `PrimaryColor`, and `AccentColor`.

---

## 8. Adding a New Ship Mesh

1. **Create the OBJ file** in `GameData/Meshes/` (e.g. `scout_razor.obj`).
2. **Update the ship JSON** in `GameData/Ships/` to set `"mesh": "meshes/scout_razor"`.
3. **Load and register** at game startup:
   ```csharp
   var data = ObjMeshLoader.Parse("GameData/Meshes/scout_razor.obj");
   if (data != null)
       registry.Register("meshes/scout_razor", ObjMeshLoader.Upload(data));
   ```
4. The mesh is now available. If the OBJ is missing at runtime, `GetOrFallback`
   will return `default_ship` automatically.

---

## 9. Directory Layout

```
SharpOpenGl.Engine/
├── ECS/
│   ├── MaterialComponent.cs
│   ├── ParticleEmitterComponent.cs
│   ├── ParticleSystem.cs
│   ├── MeshLodComponent.cs
│   ├── MeshLodSystem.cs
│   ├── BillboardComponent.cs
│   └── BillboardSystem.cs
└── Rendering/
    ├── Material.cs
    ├── ObjMeshData.cs
    ├── ObjMeshLoader.cs
    ├── ProceduralShipGenerator.cs
    ├── Particle.cs
    ├── ParticleEmitter.cs
    ├── ParticleEffects.cs
    ├── MeshLod.cs
    ├── MeshRegistry.cs
    └── ShipDesignerRenderer.cs

GameData/
└── Meshes/
    ├── default_ship.obj
    ├── default_base.obj
    └── default_projectile.obj
```
