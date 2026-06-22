using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Accumulates colored triangles for procedural race ship and station meshes.</summary>
internal sealed class RaceMeshWriter
{
    private readonly List<float> _verts = new(2048);
    private int _triCount;

    public void Tri(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
    {
        _triCount++;
        float shade = 0.9f + (_triCount % 3) * 0.02f;
        _verts.AddRange([ax, ay, az, shade, shade, shade]);
        _verts.AddRange([bx, by, bz, shade * 0.97f, shade * 0.97f, shade * 0.98f]);
        _verts.AddRange([cx, cy, cz, shade * 0.94f, shade * 0.94f, shade * 0.96f]);
    }

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
            Vector3 baseCol = lum > 0.95f ? accent : lum > 0.82f ? primary * 1.1f : lum < 0.55f ? engine : primary;
            if (lum < 0.65f && lum > 0.5f) baseCol = Vector3.Lerp(secondary, engine, 0.35f);

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0f, 1f);
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
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * mod * (0.92f + panel * 0.12f), 0.05f, 1f);
        }
    }

    /// <summary>Bakes directional light into vertex colors for depth without per-pixel normals.</summary>
    public void ApplyBakedLighting(Vector3 lightDir)
    {
        lightDir = Vector3.Normalize(lightDir);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            var approxNormal = Vector3.Normalize(new Vector3(x * 0.15f, 0.65f + y * 0.08f, z * 0.12f));
            float ndl = 0.62f + 0.38f * MathF.Max(Vector3.Dot(approxNormal, lightDir), 0f);
            float rim = 0.03f * MathF.Pow(MathF.Max(y, 0f), 0.5f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * ndl + rim, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * ndl + rim, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * (ndl * 0.95f + rim * 1.2f), 0.05f, 1f);
        }
    }

    public float[] ToArray() => ProceduralMeshes.ClampColors(_verts.ToArray());
}