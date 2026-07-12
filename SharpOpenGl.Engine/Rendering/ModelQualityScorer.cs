using System.Globalization;
using System.Text;
using System.Text.Json;
using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Scores ship mesh geometry and optional in-game preview screenshots for iterative improvement.</summary>
public static class ModelQualityScorer
{
    public sealed record CategoryScore(string Name, float Score, float MaxScore, string Notes);

    public sealed record ModelQualityReport(
        string RaceId,
        string HullId,
        float TotalScore,
        IReadOnlyList<CategoryScore> Categories,
        IReadOnlyList<string> Suggestions)
    {
        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static ModelQualityReport FromJson(string json) =>
            JsonSerializer.Deserialize<ModelQualityReport>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid score JSON.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static ModelQualityReport Score(
        string raceId,
        string hullId,
        string gameDataRoot,
        string? screenshotPath = null)
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();
        RaceVisualSchema.TryGetRace(raceId, out var race);
        race ??= RaceVisualSchema.AllRaces[0];

        string objPath = Path.Combine(gameDataRoot, "Meshes", "Ships", raceId, $"{hullId}.obj");
        float[] mesh = File.Exists(objPath)
            ? LoadProceduralColorsFromObj(objPath) ?? RaceShipMeshes.Build(raceId, hullId)
            : RaceShipMeshes.Build(raceId, hullId);

        var categories = new List<CategoryScore>
        {
            ScoreSilhouette(mesh, race, hullId),
            ScoreGeometryRichness(mesh, hullId),
            ScoreMaterialContrast(mesh, race),
            ScoreProportions(mesh, raceId, hullId),
            ScoreSurfaceDetail(mesh, race),
            ScoreScreenshot(screenshotPath),
        };

        float total = categories.Sum(c => c.Score);
        var suggestions = BuildSuggestions(categories, race, hullId);
        return new ModelQualityReport(raceId, hullId, total, categories, suggestions);
    }

    public static void WriteReport(string path, ModelQualityReport report)
        => File.WriteAllText(path, report.ToJson());

    private static float[]? LoadProceduralColorsFromObj(string objPath)
    {
        var data = ObjMeshLoader.Parse(objPath);
        if (data == null) return null;

        // OBJ stores normals in attr slot; rebuild with procedural colors for material scoring.
        return RaceShipMeshes.Build(
            Path.GetFileName(Path.GetDirectoryName(objPath)) ?? RaceShipMeshes.DefaultRace,
            Path.GetFileNameWithoutExtension(objPath));
    }

    private static CategoryScore ScoreSilhouette(float[] mesh, RaceVisualDefinition race, string hullId)
    {
        var b = MeshBounds.From(mesh);
        float length = b.MaxZ - b.MinZ;
        float width = b.MaxAbsX * 2f;
        float height = b.MaxY - b.MinY;

        if (length < 0.01f) length = 0.01f;

        float aspect = width / length;
        float targetAspect = race.Modifiers.HullWidth / Math.Max(race.Modifiers.HullLength, 0.1f);
        float aspectFit = 1f - Math.Clamp(MathF.Abs(aspect - targetAspect) / Math.Max(targetAspect, 0.2f), 0f, 1f);

        float forwardBias = (b.MaxZ > 0 && b.MinZ < 0)
            ? Math.Clamp((b.MaxZ + MathF.Abs(b.MinZ * 0.5f)) / length, 0f, 1f)
            : 0.3f;

        float verticalInterest = Math.Clamp(height / Math.Max(width, 0.1f), 0f, 1.5f) / 1.5f;
        float score = (aspectFit * 8f) + (forwardBias * 7f) + (verticalInterest * 5f);

        string notes = $"aspect {aspect:F2} (target ~{targetAspect:F2}), fwd {forwardBias:F2}, height {height:F2}";
        return new CategoryScore("Silhouette", Math.Clamp(score, 0f, 20f), 20f, notes);
    }

    private static CategoryScore ScoreGeometryRichness(float[] mesh, string hullId)
    {
        int tris = mesh.Length / ProceduralMeshes.Stride / 3;
        int sweetMin = hullId is "drone_swarm" or "scout_light" ? 18 : 36;
        int sweetMax = hullId is "dreadnought" or "carrier_command" ? 320 : 180;

        float richness;
        if (tris < sweetMin)
            richness = tris / (float)sweetMin * 0.55f;
        else if (tris <= sweetMax)
            richness = 0.55f + (tris - sweetMin) / (float)(sweetMax - sweetMin) * 0.45f;
        else
            richness = 1f - Math.Clamp((tris - sweetMax) / (float)sweetMax, 0f, 0.35f);

        float score = richness * 20f;
        return new CategoryScore("Geometry", Math.Clamp(score, 0f, 20f), 20f, $"{tris} triangles");
    }

    private static CategoryScore ScoreMaterialContrast(float[] mesh, RaceVisualDefinition race)
    {
        var lumBuckets = new HashSet<int>();
        float minLum = 1f, maxLum = 0f, variance = 0f;
        int count = mesh.Length / ProceduralMeshes.Stride;

        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            lumBuckets.Add((int)(lum * 20f));
            minLum = MathF.Min(minLum, lum);
            maxLum = MathF.Max(maxLum, lum);
            variance += lum * lum;
        }

        variance = variance / count - MathF.Pow((minLum + maxLum) * 0.5f, 2f);
        float bandScore = Math.Clamp(lumBuckets.Count / 5f, 0f, 1f);
        float rangeScore = Math.Clamp((maxLum - minLum) / 0.45f, 0f, 1f);
        float gritBonus = (race.Substrate?.Grit ?? 0f) > 0.05f ? 0.15f : 0f;

        float score = (bandScore * 10f) + (rangeScore * 8f) + (MathF.Sqrt(MathF.Max(variance, 0f)) * 6f) + gritBonus * 2f;
        return new CategoryScore("Materials", Math.Clamp(score, 0f, 20f), 20f,
            $"{lumBuckets.Count} luminance bands, range {maxLum - minLum:F2}");
    }

    private static CategoryScore ScoreProportions(float[] mesh, string raceId, string hullId)
    {
        var b = MeshBounds.From(mesh);
        var (len, wid, hgt) = ResolveHullDimensions(raceId, hullId);

        float xFit = 1f - Math.Clamp(MathF.Abs(b.MaxAbsX - wid * 0.5f) / Math.Max(wid, 0.1f), 0f, 1f);
        float yFit = 1f - Math.Clamp(MathF.Abs(b.MaxY - hgt) / Math.Max(hgt, 0.1f), 0f, 1f);
        float zFit = 1f - Math.Clamp(MathF.Abs(b.MaxZ - len) / Math.Max(len, 0.1f), 0f, 1f);

        float score = ((xFit + yFit + zFit) / 3f) * 15f;
        return new CategoryScore("Proportions", Math.Clamp(score, 0f, 15f), 15f,
            $"envelope {wid:F1}×{hgt:F1}×{len:F1}");
    }

    private static CategoryScore ScoreSurfaceDetail(float[] mesh, RaceVisualDefinition race)
    {
        var b = MeshBounds.From(mesh);
        int tris = mesh.Length / ProceduralMeshes.Stride / 3;

        float keelScore = race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase)
            ? Math.Clamp(b.MaxY / Math.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 6f
            : 3f;

        float accentVerts = 0;
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            if (lum > 0.9f) accentVerts++;
        }

        float accentRatio = accentVerts / Math.Max(tris * 3f, 1f);
        float accentScore = Math.Clamp(accentRatio * 40f, 0f, 6f);
        float triDetail = Math.Clamp(tris / 80f, 0f, 1f) * 3f;

        float score = keelScore + accentScore + triDetail;
        return new CategoryScore("SurfaceDetail", Math.Clamp(score, 0f, 15f), 15f,
            $"keel {keelScore:F1}, accent {accentRatio:P0}");
    }

    private static CategoryScore ScoreScreenshot(string? screenshotPath)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath) || !File.Exists(screenshotPath))
            return new CategoryScore("Screenshot", 0f, 10f, "no capture");

        if (!PngMetrics.TryAnalyze(screenshotPath, out var metrics))
            return new CategoryScore("Screenshot", 0f, 10f, "unreadable PNG");

        float coverage = Math.Clamp(metrics.ForegroundRatio / 0.12f, 0f, 1f);
        float contrast = Math.Clamp(metrics.LuminanceStdDev / 0.18f, 0f, 1f);
        float edge = Math.Clamp(metrics.EdgeDensity / 0.08f, 0f, 1f);
        float color = Math.Clamp(metrics.ColorDiversity / 24f, 0f, 1f);

        float score = coverage * 3f + contrast * 3f + edge * 2f + color * 2f;
        return new CategoryScore("Screenshot", Math.Clamp(score, 0f, 10f), 10f,
            $"fg {metrics.ForegroundRatio:P1}, contrast {metrics.LuminanceStdDev:F2}, edges {metrics.EdgeDensity:F3}");
    }

    private static List<string> BuildSuggestions(
        IReadOnlyList<CategoryScore> categories,
        RaceVisualDefinition race,
        string hullId)
    {
        var suggestions = new List<string>();
        var byName = categories.ToDictionary(c => c.Name, c => c);

        if (byName["Silhouette"].Score < 14f)
        {
            suggestions.Add($"Strengthen {race.DisplayName} silhouette: elongate bow (+Z), add dorsal keel, keep wings narrow (style={race.Style}).");
        }

        if (byName["Geometry"].Score < 12f)
        {
            int tris = int.Parse(byName["Geometry"].Notes.Split(' ')[0], CultureInfo.InvariantCulture);
            suggestions.Add(tris < 36
                ? "Add more hull sections, panel lines, and engine nacelles — mesh is too primitive."
                : "Simplify overlapping triangles or merge coplanar faces — mesh may be too dense/noisy.");
        }

        if (byName["Materials"].Score < 12f)
        {
            suggestions.Add("Increase material contrast: darker engine bays, brighter accent bands, stronger substrate grit variation.");
        }

        if (byName["Proportions"].Score < 10f)
        {
            suggestions.Add($"Re-anchor geometry to hull envelope for {hullId}; nose should reach +Z bound, stern near -Z.");
        }

        if (byName["SurfaceDetail"].Score < 9f)
        {
            if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
                suggestions.Add("Add vasudan surface detail: segmented belly plates, dorsal spine ridges, lateral intake scoops.");
            else
                suggestions.Add("Add flush surface detail: panel seams, light strips, and recessed engine glow wells.");
        }

        if (byName["Screenshot"].Score < 6f)
        {
            suggestions.Add("Improve in-game readability: boost directional lighting/shadow separation and race texture contrast.");
        }

        if (suggestions.Count == 0)
            suggestions.Add("Polish micro-details: sharper facet transitions, accent tips on wing leading edges, engine glow gradients.");

        return suggestions;
    }

    private static (float len, float wid, float hgt) ResolveHullDimensions(string raceId, string hullId)
    {
        var design = ShipDesignCatalog.Resolve(hullId, raceId);
        var hull = RaceVisualSchema.ResolveHullProfile(design.HullClass);
        var variant = ShipDesignVariant.FromSpec(design);
        RaceVisualSchema.TryGetRace(raceId, out var race);
        race ??= RaceVisualSchema.AllRaces[0];

        float s = hull.Size * variant.LengthScale;
        float len = s * hull.LengthRatio * race.Modifiers.HullLength * variant.LengthScale;
        float wid = s * hull.WidthRatio * race.Modifiers.HullWidth * variant.WidthScale;
        float hgt = s * hull.HeightRatio * variant.HeightScale;
        len += s * variant.NoseExtension * 0.5f;
        len += s * variant.SternExtension * 0.35f;
        return (len, wid, hgt);
    }

    private readonly struct MeshBounds
    {
        public float MaxAbsX { get; init; }
        public float MaxY { get; init; }
        public float MinY { get; init; }
        public float MaxZ { get; init; }
        public float MinZ { get; init; }

        public static MeshBounds From(float[] mesh)
        {
            float maxAbsX = 0f, maxY = float.MinValue, minY = float.MaxValue;
            float maxZ = float.MinValue, minZ = float.MaxValue;
            for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
            {
                maxAbsX = MathF.Max(maxAbsX, MathF.Abs(mesh[i]));
                maxY = MathF.Max(maxY, mesh[i + 1]);
                minY = MathF.Min(minY, mesh[i + 1]);
                maxZ = MathF.Max(maxZ, mesh[i + 2]);
                minZ = MathF.Min(minZ, mesh[i + 2]);
            }

            return new MeshBounds { MaxAbsX = maxAbsX, MaxY = maxY, MinY = minY, MaxZ = maxZ, MinZ = minZ };
        }
    }
}

/// <summary>Minimal PNG RGBA8 analyzer for mesh preview screenshots.</summary>
internal static class PngMetrics
{
    internal sealed record Metrics(float ForegroundRatio, float LuminanceStdDev, float EdgeDensity, int ColorDiversity);

    public static bool TryAnalyze(string path, out Metrics metrics)
    {
        metrics = default!;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (!TryDecodeRgba8(bytes, out int w, out int h, out byte[] rgba))
                return false;

            metrics = Analyze(w, h, rgba);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Metrics Analyze(int w, int h, byte[] rgba)
    {
        int fg = 0;
        double sum = 0, sumSq = 0;
        var colors = new HashSet<int>();
        int edges = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 4;
                float r = rgba[i] / 255f, g = rgba[i + 1] / 255f, b = rgba[i + 2] / 255f;
                float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                bool isBg = lum < 0.06f && r < 0.08f && g < 0.08f && b < 0.15f;
                if (!isBg) fg++;

                sum += lum;
                sumSq += lum * lum;
                colors.Add((rgba[i] >> 4) << 8 | (rgba[i + 1] >> 4) << 4 | (rgba[i + 2] >> 4));

                if (x > 0 && y > 0)
                {
                    int j = ((y - 1) * w + (x - 1)) * 4;
                    float lumPrev = 0.2126f * rgba[j] + 0.7152f * rgba[j + 1] + 0.0722f * rgba[j + 2];
                    if (MathF.Abs(lum - lumPrev) > 0.08f) edges++;
                }
            }
        }

        int total = w * h;
        double mean = sum / total;
        double variance = sumSq / total - mean * mean;
        return new Metrics(
            fg / (float)total,
            (float)Math.Sqrt(Math.Max(variance, 0)),
            edges / (float)total,
            colors.Count);
    }

    private static bool TryDecodeRgba8(byte[] data, out int width, out int height, out byte[] rgba)
    {
        width = height = 0;
        rgba = Array.Empty<byte>();
        if (data.Length < 24 || data[0] != 137 || data[1] != 80) return false;

        int offset = 8;
        byte bitDepth = 0, colorType = 0;
        var idat = new List<byte>();

        while (offset + 8 <= data.Length)
        {
            int len = (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
            string type = Encoding.ASCII.GetString(data, offset + 4, 4);
            offset += 8;
            if (offset + len > data.Length) return false;

            if (type == "IHDR")
            {
                width = (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
                height = (data[offset + 4] << 24) | (data[offset + 5] << 16) | (data[offset + 6] << 8) | data[offset + 7];
                bitDepth = data[offset + 8];
                colorType = data[offset + 9];
            }
            else if (type == "IDAT")
            {
                idat.AddRange(data.AsSpan(offset, len));
            }

            offset += len + 4;
        }

        if (width <= 0 || height <= 0 || bitDepth != 8 || colorType != 6) return false;

        byte[] inflated = Inflate(idat.ToArray());
        rgba = new byte[width * height * 4];
        int stride = width * 4;
        int src = 0;
        for (int y = 0; y < height; y++)
        {
            if (src >= inflated.Length) return false;
            int filter = inflated[src++];
            for (int x = 0; x < stride; x++)
            {
                if (src >= inflated.Length) return false;
                byte raw = inflated[src++];
                int dst = y * stride + x;
                byte left = x >= 4 ? rgba[dst - 4] : (byte)0;
                byte up = y > 0 ? rgba[dst - stride] : (byte)0;
                byte upLeft = x >= 4 && y > 0 ? rgba[dst - stride - 4] : (byte)0;
                rgba[dst] = Unfilter(filter, raw, left, up, upLeft);
            }
        }

        return true;
    }

    private static byte Unfilter(int filter, byte raw, byte left, byte up, byte upLeft) => filter switch
    {
        0 => raw,
        1 => (byte)(raw + left),
        2 => (byte)(raw + up),
        3 => (byte)(raw + ((left + up) >> 1)),
        4 => (byte)(raw + Paeth(left, up, upLeft)),
        _ => raw,
    };

    private static byte Paeth(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        int pr = pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        return (byte)pr;
    }

    private static byte[] Inflate(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }
}