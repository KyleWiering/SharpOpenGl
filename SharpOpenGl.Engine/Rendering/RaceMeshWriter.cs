using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Accumulates colored triangles for procedural race ship and station meshes.</summary>
internal sealed class RaceMeshWriter
{
    internal enum HullMaterial { Hull, Truss, Solar, Radiator, Engine, Weapon, ShieldGen }

    private readonly List<float> _verts = new(4096);
    private int _triCount;

    public void Tri(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
        => TriMat(HullMaterial.Hull, ax, ay, az, bx, by, bz, cx, cy, cz);

    public void TriMat(HullMaterial mat, float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
    {
        _triCount++;
        float lum = MaterialLuminance(mat) + (_triCount % 3) * 0.008f;
        _verts.AddRange([ax, ay, az, lum, lum, lum]);
        _verts.AddRange([bx, by, bz, lum * 0.985f, lum * 0.985f, lum * 0.99f]);
        _verts.AddRange([cx, cy, cz, lum * 0.97f, lum * 0.97f, lum * 0.975f]);
    }

    private static float MaterialLuminance(HullMaterial mat) => mat switch
    {
        HullMaterial.Hull => 0.9f,
        HullMaterial.Truss => 0.78f,
        HullMaterial.Solar => 0.97f,
        HullMaterial.Radiator => 0.62f,
        HullMaterial.Engine => 0.48f,
        HullMaterial.Weapon => 0.36f,
        HullMaterial.ShieldGen => 0.30f,
        _ => 0.88f,
    };

    public void TriColored(Vector3 a, Vector3 b, Vector3 c, Vector3 color)
    {
        _triCount++;
        Vector3 bCol = color * (0.88f + (_triCount % 3) * 0.04f);
        Vector3 cCol = color * (0.82f + (_triCount % 4) * 0.03f);
        _verts.AddRange([a.X, a.Y, a.Z, color.X, color.Y, color.Z]);
        _verts.AddRange([b.X, b.Y, b.Z, bCol.X, bCol.Y, bCol.Z]);
        _verts.AddRange([c.X, c.Y, c.Z, cCol.X, cCol.Y, cCol.Z]);
    }

    public void RecolorPrimary(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum > 0.95f
                ? Vector3.ComponentMin(accent * 1.35f + new Vector3(0.18f, 0.22f, 0.28f), Vector3.One)
                : lum > 0.82f ? primary * 1.1f : lum < 0.55f ? engine * 0.82f : primary;
            if (lum < 0.65f && lum > 0.5f) baseCol = Vector3.Lerp(secondary, engine, 0.35f);

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0f, 1f);
        }
    }

    /// <summary>NASA truss material bands — white hull, grey truss, gold solar, charcoal radiators, orange engines.</summary>
    public void RecolorTrussNasa(Vector3 hull, Vector3 truss, Vector3 solar, Vector3 radiator, Vector3 engineGlow)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => solar,
                > 0.84f => hull,
                > 0.72f => truss,
                > 0.56f => radiator,
                _ => engineGlow,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Position-encoded micro-variation so shader substrates read with more depth.</summary>
    public void ApplySubstrateVariation(RaceSubstrateProfile profile)
    {
        float freq = profile.MicroFrequency;
        float grit = profile.Grit;
        float accent = profile.AccentBoost;
        float panelDepth = profile.PanelDepth;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float panel = 0.5f + 0.5f * MathF.Sin(x * freq) * MathF.Cos(z * freq * 0.73f);
            float rivet = MathF.Abs(MathF.Sin(x * freq * 2.1f) * MathF.Cos(z * freq * 1.9f));
            float seam = MathF.Abs(MathF.Sin((x + z) * freq * 0.55f));
            float heightBand = 0.5f + 0.5f * MathF.Sin(y * freq * 1.4f);
            float mod = 1f - grit * 0.22f + panel * grit * 0.18f + rivet * grit * 0.08f + seam * panelDepth * 0.8f;

            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum > 0.88f)
                mod += accent * 0.06f * panel;
            if (y > 0.05f)
                mod += heightBand * panelDepth * 0.12f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * mod, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * mod, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * mod * (0.98f + panel * 0.04f), 0.05f, 1f);
        }
    }

    /// <summary>Bakes directional light into vertex colors for depth without per-pixel normals.</summary>
    public void ApplyBakedLighting(Vector3 lightDir, float contrast = 1f)
    {
        lightDir = Vector3.Normalize(lightDir);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            var approxNormal = Vector3.Normalize(new Vector3(x * 0.18f, 0.72f + y * 0.1f, z * 0.14f));
            float ndl = 0.42f + 0.58f * MathF.Max(Vector3.Dot(approxNormal, lightDir), 0f);
            float bellyShadow = y < 0.14f ? 0.68f : 1f;
            float crestBoost = y > 0.16f ? 1.1f + 0.08f * MathF.Min(y, 0.55f) : 1f;
            float rim = 0.1f * MathF.Pow(MathF.Max(y, 0f), 0.38f);
            float shade = ndl * bellyShadow * crestBoost;
            shade = 0.5f + (shade - 0.5f) * contrast;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * shade + rim, 0.03f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * shade + rim, 0.03f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * (shade * 0.94f + rim * 1.12f), 0.03f, 1f);
        }
    }

    /// <summary>Vesper vasudan palette — darker bays, brighter accent crest bands, distinct engine/weapon bands.</summary>
    public void RecolorVasudan(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponBay = Vector3.Lerp(secondary * 0.42f, primary * 0.28f, 0.5f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.35f, 0.22f);
        Vector3 shieldEmit = Vector3.Lerp(accent * 0.55f, engine * 0.42f, 0.38f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => Vector3.Lerp(accent, new Vector3(0.90f, 0.98f, 1.0f), 0.78f),
                > 0.86f => Vector3.Lerp(primary, accent, 0.32f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.55f),
                > 0.58f => secondary * 0.92f,
                > 0.42f => engineGlow,
                > 0.34f => weaponBay,
                > 0.26f => shieldEmit,
                _ => secondary * 0.38f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    public float[] ToArray() => ProceduralMeshes.ClampColors(_verts.ToArray());
}