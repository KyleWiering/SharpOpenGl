using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Parameters for generating a procedural spaceship hull.
/// All length units are in world-space metres.
/// </summary>
public sealed class ShipParameters
{
    /// <summary>Total hull length (bow to stern).</summary>
    public float Length { get; set; } = 2f;

    /// <summary>Maximum hull width at the widest point.</summary>
    public float Width { get; set; } = 1f;

    /// <summary>Wing sweep angle in degrees (0 = straight, positive = swept back).</summary>
    public float WingAngle { get; set; } = 30f;

    /// <summary>Length of each wing tip from the fuselage edge.</summary>
    public float WingLength { get; set; } = 0.5f;

    /// <summary>Number of engine nozzles (0–4).</summary>
    public int EngineCount { get; set; } = 2;

    /// <summary>Hull primary colour.</summary>
    public Vector3 HullColor { get; set; } = new Vector3(0.3f, 0.4f, 0.8f);

    /// <summary>Engine glow colour.</summary>
    public Vector3 EngineColor { get; set; } = new Vector3(0.3f, 0.8f, 1f);
}

/// <summary>
/// Generates simple procedural spaceship meshes from <see cref="ShipParameters"/>.
/// Output uses the same float layout as <see cref="ObjMeshData"/> (pos3 + normal3, stride 6).
/// </summary>
public static class ProceduralShipGenerator
{
    /// <summary>
    /// Build a triangulated ship hull from the given parameters.
    /// Returns <see cref="ObjMeshData"/> that can be uploaded to the GPU.
    /// </summary>
    public static ObjMeshData Generate(ShipParameters p)
    {
        var verts = new List<float>();

        float hl = p.Length * 0.5f;   // half-length
        float hw = p.Width  * 0.5f;   // half-width

        // ── Fuselage (8 triangles forming a pointed diamond body) ─────────────
        Vector3 bow     = new( hl,   0f,  0f);  // front tip
        Vector3 stern   = new(-hl,   0f,  0f);  // rear centre
        Vector3 portFwd = new( hw * 0.2f, 0f, -hw); // port forward edge
        Vector3 stbdFwd = new( hw * 0.2f, 0f,  hw); // starboard forward edge
        Vector3 portAft = new(-hw * 0.2f, 0f, -hw); // port rear edge
        Vector3 stbdAft = new(-hw * 0.2f, 0f,  hw); // starboard rear edge
        Vector3 top     = new( 0f,  hw * 0.25f, 0f);
        Vector3 bot     = new( 0f, -hw * 0.25f, 0f);

        // Top faces
        AddTri(verts, bow, stbdFwd, top, p.HullColor);
        AddTri(verts, bow, top,     portFwd, p.HullColor);
        AddTri(verts, stbdFwd, stbdAft, top, p.HullColor);
        AddTri(verts, portFwd, top,   portAft, p.HullColor);
        AddTri(verts, stbdAft, stern, top, p.HullColor);
        AddTri(verts, portAft, top,   stern, p.HullColor);

        // Bottom faces (mirrored)
        AddTri(verts, bow,     bot,     stbdFwd, p.HullColor);
        AddTri(verts, bow,     portFwd, bot,     p.HullColor);
        AddTri(verts, stbdFwd, bot,     stbdAft, p.HullColor);
        AddTri(verts, portFwd, portAft, bot,     p.HullColor);
        AddTri(verts, stbdAft, bot,     stern,   p.HullColor);
        AddTri(verts, portAft, stern,   bot,     p.HullColor);

        // ── Wings ─────────────────────────────────────────────────────────────
        float wingAngleRad = MathHelper.DegreesToRadians(p.WingAngle);
        float wingSweep    = MathF.Sin(wingAngleRad) * p.WingLength;
        float wingReach    = MathF.Cos(wingAngleRad) * p.WingLength;

        if (p.WingLength > 0f)
        {
            // Starboard wing
            Vector3 sbRoot  = stbdFwd;
            Vector3 sbTip   = new(sbRoot.X - wingSweep, 0f, sbRoot.Z + wingReach);
            Vector3 sbBase  = stbdAft;
            AddTri(verts, sbRoot, sbTip, sbBase, p.HullColor);
            AddTri(verts, sbBase, sbTip, sbRoot, p.HullColor); // underside

            // Port wing (mirrored)
            Vector3 pbRoot = portFwd;
            Vector3 pbTip  = new(pbRoot.X - wingSweep, 0f, pbRoot.Z - wingReach);
            Vector3 pbBase = portAft;
            AddTri(verts, pbRoot, pbBase, pbTip, p.HullColor);
            AddTri(verts, pbBase, pbRoot, pbTip, p.HullColor); // underside
        }

        // ── Engine nozzles ────────────────────────────────────────────────────
        int eng = Math.Clamp(p.EngineCount, 0, 4);
        float[] engOffsets = GetEngineOffsets(eng, hw);
        for (int i = 0; i < eng; i++)
        {
            Vector3 engCenter = new(stern.X - 0.05f, 0f, engOffsets[i]);
            AddEngineNozzle(verts, engCenter, 0.08f, p.EngineColor);
        }

        return new ObjMeshData(verts.ToArray(), "<procedural>", "procedural_ship");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float[] GetEngineOffsets(int count, float hw) =>
        count switch
        {
            1 => new[] { 0f },
            2 => new[] { -hw * 0.4f, hw * 0.4f },
            3 => new[] { -hw * 0.5f, 0f, hw * 0.5f },
            4 => new[] { -hw * 0.55f, -hw * 0.2f, hw * 0.2f, hw * 0.55f },
            _ => Array.Empty<float>(),
        };

    private static void AddTri(
        List<float> verts, Vector3 a, Vector3 b, Vector3 c, Vector3 color)
    {
        Vector3 edge1  = b - a;
        Vector3 edge2  = c - a;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

        Emit(verts, a, normal, color);
        Emit(verts, b, normal, color);
        Emit(verts, c, normal, color);
    }

    private static void AddEngineNozzle(
        List<float> verts, Vector3 center, float radius, Vector3 color)
    {
        const int segments = 6;
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathHelper.TwoPi * i / segments;
            float a1 = MathHelper.TwoPi * (i + 1) / segments;
            Vector3 p0 = center + new Vector3(0f, MathF.Sin(a0) * radius, MathF.Cos(a0) * radius);
            Vector3 p1 = center + new Vector3(0f, MathF.Sin(a1) * radius, MathF.Cos(a1) * radius);
            AddTri(verts, center, p0, p1, color);
        }
    }

    private static void Emit(List<float> verts, Vector3 pos, Vector3 normal, Vector3 _)
    {
        verts.Add(pos.X);    verts.Add(pos.Y);    verts.Add(pos.Z);
        verts.Add(normal.X); verts.Add(normal.Y); verts.Add(normal.Z);
    }
}
