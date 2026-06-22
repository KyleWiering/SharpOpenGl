namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Per-design modifiers — only adds geometry that extends the primary silhouette
/// (cargo pods on haulers). All other variation comes from the race hull shape.
/// </summary>
internal static class ShipDesignVariantGeometry
{
    public static void Apply(RaceMeshWriter w, ShipDesignSpec spec, ShipDesignVariant v,
        float len, float wid, float hgt, string raceStyle)
    {
        if (spec.HullClass is "transport" or "freighter" or "miner")
            AddCargoPods(w, len, wid, hgt, Math.Min(v.CargoPodCount, 3), spec.HullClass);
    }

    private static void AddCargoPods(RaceMeshWriter w, float len, float wid, float hgt, int count, string hull)
    {
        if (count <= 0) return;
        float z = len * (hull is "freighter" or "transport" ? -0.05f : 0.08f);
        for (int i = 0; i < count; i++)
        {
            float side = (i - (count - 1) * 0.5f) * wid * 0.22f;
            float podZ = z - i * wid * 0.07f;
            w.Tri(side, hgt * 0.38f, podZ, side - wid * 0.07f, hgt * 0.14f, podZ - wid * 0.06f, side + wid * 0.07f, hgt * 0.14f, podZ - wid * 0.06f);
        }
    }
}