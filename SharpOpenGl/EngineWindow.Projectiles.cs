using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private readonly Dictionary<string, (int Vao, int Count)> _projectileMeshes = new();

    private void LoadProjectileMeshes()
    {
        RegisterProjectileMesh("projectile/laser_bolt",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildLaserBolt(new Vector3(1f, 0.5f, 0.35f), 2.6f)));
        RegisterProjectileMesh("projectile/beam",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildBeamStreak(new Vector3(0.55f, 0.95f, 1f), 4.5f)));
        RegisterProjectileMesh("projectile/torpedo",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildTorpedo(new Vector3(0.9f, 0.9f, 0.95f), 1.8f)));
        RegisterProjectileMesh("projectile/rocket",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildRocket(new Vector3(1f, 0.75f, 0.2f), 1.4f)));
        RegisterProjectileMesh("projectile/bomb",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildBomb(new Vector3(1f, 0.55f, 0.15f), 1.6f)));
        RegisterProjectileMesh("projectile/energy_pulse",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildEnergyPulse(new Vector3(0.7f, 0.45f, 1f), 1.5f)));
        RegisterProjectileMesh("projectile/wave",
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildWaveRing(new Vector3(0.4f, 1f, 0.85f)), lines: true));
    }

    private void RegisterProjectileMesh(string key, (int vao, int vbo, int count) mesh, bool lines = false)
    {
        _projectileMeshes[key] = (mesh.vao, mesh.count);
        _assetManager?.RegisterProceduralMesh(key);
    }

    private void ResolveProjectileMeshes()
    {
        if (_world == null) return;

        foreach (var (_, render) in _world.Query<RenderComponent>())
        {
            if (render.MeshId >= 0 || string.IsNullOrEmpty(render.MeshKey)) continue;
            if (!_projectileMeshes.TryGetValue(render.MeshKey, out var mesh)) continue;
            render.MeshId = mesh.Vao;
            render.VertexCount = mesh.Count;
        }
    }

    private Matrix4 BuildProjectileModelMatrix(TransformComponent transform, ProjectileComponent? proj,
        ProjectileVisualComponent? visual)
    {
        Vector3 scale = transform.Scale;
        if (proj != null && visual != null && visual.Visual == WeaponVisualKind.Wave && proj.MaxLifetime > 0f)
        {
            float progress = 1f - MathF.Max(0f, proj.Lifetime / proj.MaxLifetime);
            float ring = visual.Scale + progress * proj.BlastRadius * 0.12f;
            scale = new Vector3(ring, 1f, ring);
        }

        Matrix4 s = Matrix4.CreateScale(scale);
        Matrix4 rx = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(transform.EulerAngles.X));
        Matrix4 ry = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(transform.EulerAngles.Y));
        Matrix4 rz = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(transform.EulerAngles.Z));
        Matrix4 t = Matrix4.CreateTranslation(transform.Position);
        return s * ry * rx * rz * t;
    }

    private void AttachStationWeapons(Entity entity, Stance stationStance,
        params (string type, float damage, float range, float rate)[] weapons)
    {
        if (_world == null) return;
        var wl = new WeaponListComponent();
        for (int i = 0; i < weapons.Length; i++)
        {
            var w = weapons[i];
            wl.Weapons.Add(new WeaponComponent
            {
                Slot = i,
                Type = w.type,
                Damage = w.damage,
                Range = CombatBalance.ScaleRange(w.range),
                FireRate = w.rate,
                ProjectileType = WeaponProfiles.DefaultProjectileTypeKey(w.type),
            });
        }
        _world.AddComponent(entity, wl);
        _world.AddComponent(entity, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest,
            Priority = 50,
        });
        _world.AddComponent(entity, new StanceComponent { CurrentStance = stationStance });
    }
}