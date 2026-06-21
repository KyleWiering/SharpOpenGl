using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Accumulates colored triangles for procedural race ship meshes.</summary>
internal sealed class RaceMeshWriter
{
    private readonly List<float> _verts = new(768);
    private int _triCount;

    public void Tri(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
    {
        _triCount++;
        float shade = 0.72f + (_triCount % 5) * 0.06f;
        _verts.AddRange([ax, ay, az, shade, shade, shade]);
        _verts.AddRange([bx, by, bz, shade * 0.88f, shade * 0.88f, shade * 0.9f]);
        _verts.AddRange([cx, cy, cz, shade * 0.8f, shade * 0.8f, shade * 0.85f]);
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

    public float[] ToArray() => _verts.ToArray();
}