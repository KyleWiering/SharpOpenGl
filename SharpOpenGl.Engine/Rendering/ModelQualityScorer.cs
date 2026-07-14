using System.Globalization;
using System.Text;
using System.Text.Json;
using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Scores procedural meshes (ships, stations, objects) and optional preview screenshots.</summary>
public static partial class ModelQualityScorer
{
    public sealed record CategoryScore(string Name, float Score, float MaxScore, string Notes);

    public sealed record ModelQualityReport(
        string RaceId,
        string HullId,
        string AssetKind,
        float TotalScore,
        float RaceIdentityScore,
        IReadOnlyList<CategoryScore> Categories,
        IReadOnlyList<string> Suggestions)
    {
        public string ModelId => HullId;

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static ModelQualityReport FromJson(string json) =>
            JsonSerializer.Deserialize<ModelQualityReport>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid score JSON.");
    }

    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Backward-compatible ship scorer.</summary>
    public static ModelQualityReport Score(
        string raceId,
        string hullId,
        string gameDataRoot,
        string? screenshotPath = null)
        => ScoreAsset(ModelMeshSource.KindShip, hullId, gameDataRoot, raceId, screenshotPath);

    public static ModelQualityReport ScoreAsset(
        string assetKind,
        string modelId,
        string gameDataRoot,
        string? raceId = null,
        string? screenshotPath = null)
    {
        string kind = ModelMeshSource.NormalizeKind(assetKind);
        raceId = string.IsNullOrWhiteSpace(raceId) ? RaceShipMeshes.DefaultRace : raceId.Trim();

        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();
        RaceVisualSchema.TryGetRace(raceId, out var race);
        race ??= RaceVisualSchema.AllRaces[0];

        float[] mesh = LoadMesh(kind, modelId, raceId, gameDataRoot);

        var categories = new List<CategoryScore>
        {
            ScoreForm(mesh, race, kind, modelId, raceId),
            ScoreGeometryRichness(mesh, kind, modelId),
            ScoreMaterialContrast(mesh, race, kind),
            ScoreScaleFit(mesh, kind, modelId, raceId),
            ScoreSurfaceDetail(mesh, race, kind),
            ScoreRaceIdentity(mesh, race, kind),
            ScoreScreenshot(screenshotPath, kind),
        };

        float total = categories.Sum(c => c.Score);
        float raceIdentity = categories.First(c => c.Name == "RaceIdentity").Score;
        var suggestions = BuildSuggestions(categories, race, kind, modelId);
        return new ModelQualityReport(raceId, modelId, kind, total, raceIdentity, categories, suggestions);
    }

    public static void WriteReport(string path, ModelQualityReport report)
        => File.WriteAllText(path, report.ToJson());

    private static float[] LoadMesh(string kind, string modelId, string raceId, string gameDataRoot)
    {
        string rel = ModelMeshSource.ResolveObjRelativePath(kind, modelId, raceId);
        string objPath = Path.Combine(gameDataRoot, "Meshes", rel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(objPath))
        {
            var data = ObjMeshLoader.Parse(objPath);
            if (data != null)
                return ModelMeshSource.Build(kind, modelId, raceId);
        }

        return ModelMeshSource.Build(kind, modelId, raceId);
    }

    private static CategoryScore ScoreForm(
        float[] mesh, RaceVisualDefinition race, string kind, string modelId, string raceId)
    {
        var b = MeshBounds.From(mesh);
        float length = MathF.Max(b.MaxZ - b.MinZ, 0.01f);
        float width = MathF.Max(b.MaxAbsX * 2f, 0.01f);
        float height = MathF.Max(b.MaxY - b.MinY, 0.01f);

        if (kind == ModelMeshSource.KindStation)
        {
            float heightToWidth = height / MathF.Max(width, 1f);
            float planRead = 1f - Math.Clamp((heightToWidth - 0.35f) / 1.1f, 0f, 1f);
            float footprint = Math.Clamp(width / MathF.Max(length, 1f), 0.4f, 2.5f);
            float footprintFit = 1f - Math.Clamp(MathF.Abs(footprint - 1.1f) / 1.1f, 0f, 1f);
            float padRead = Math.Clamp(b.MaxAbsX / 8f, 0f, 1f);
            float score = planRead * 7f + footprintFit * 5f + padRead * 5f;
            return new CategoryScore("Massing", Math.Clamp(score, 0f, 17f), 17f,
                $"plan h/w {heightToWidth:F2}, footprint {footprint:F2}, pad {b.MaxAbsX:F1}");
        }

        if (kind == ModelMeshSource.KindObject)
        {
            float compactness = Math.Clamp(height / MathF.Max(length, 0.1f), 0f, 2f) / 2f;
            float centerBias = 1f - Math.Clamp((MathF.Abs(b.MinZ) + MathF.Abs(b.MaxZ)) / MathF.Max(length, 0.1f), 0f, 1f) * 0.35f;
            float iconFill = Math.Clamp((width * height) / 12f, 0f, 1f);
            float score = compactness * 6f + centerBias * 6f + iconFill * 5f;
            return new CategoryScore("IconRead", Math.Clamp(score, 0f, 17f), 17f,
                $"{modelId}: compact {compactness:F2}, fill {iconFill:F2}");
        }

        float aspect = width / length;
        float targetAspect = race.Modifiers.HullWidth / MathF.Max(race.Modifiers.HullLength, 0.1f);
        float aspectFit = 1f - Math.Clamp(MathF.Abs(aspect - targetAspect) / MathF.Max(targetAspect, 0.2f), 0f, 1f);
        float forwardBias = (b.MaxZ > 0 && b.MinZ < 0)
            ? Math.Clamp((b.MaxZ + MathF.Abs(b.MinZ * 0.5f)) / length, 0f, 1f)
            : 0.3f;
        float verticalInterest = Math.Clamp(height / MathF.Max(width, 0.1f), 0f, 1.5f) / 1.5f;
        float shipScore = aspectFit * 7f + forwardBias * 6f + verticalInterest * 4f;
        return new CategoryScore("Silhouette", Math.Clamp(shipScore, 0f, 17f), 17f,
            $"aspect {aspect:F2} (target ~{targetAspect:F2}), fwd {forwardBias:F2}, height {height:F2}");
    }

    private static CategoryScore ScoreGeometryRichness(float[] mesh, string kind, string modelId)
    {
        int tris = mesh.Length / ProceduralMeshes.Stride / 3;
        int sweetMin = kind switch
        {
            ModelMeshSource.KindObject => modelId is "laser" or "beam" or "wave" ? 6 : 12,
            ModelMeshSource.KindStation => 48,
            _ => modelId is "drone_swarm" or "scout_light" ? 18 : 36,
        };
        int sweetMax = kind switch
        {
            ModelMeshSource.KindObject => 96,
            ModelMeshSource.KindStation => 420,
            _ => modelId is "dreadnought" or "carrier_command" ? 320 : 180,
        };

        float richness;
        if (tris < sweetMin)
            richness = tris / (float)sweetMin * 0.55f;
        else if (tris <= sweetMax)
            richness = 0.55f + (tris - sweetMin) / (float)(sweetMax - sweetMin) * 0.45f;
        else
            richness = 1f - Math.Clamp((tris - sweetMax) / (float)sweetMax, 0f, 0.35f);

        var triPattern = AnalyzeTrianglePatterns(mesh);
        float triPatternPenalty = triPattern.Severity * 8f;
        float score = richness * 17f - triPatternPenalty;
        string geomNotes = triPattern.Severity > 0.08f
            ? $"{tris} triangles, tri-pattern -{triPatternPenalty:F1} (slivers {triPattern.SliverCount}, fishbone {triPattern.ZigzagChains}, facet-seams {triPattern.FacetSeams}, micro {triPattern.MicroFacets})"
            : $"{tris} triangles";
        return new CategoryScore("Geometry", Math.Clamp(score, 0f, 17f), 17f, geomNotes);
    }

    private readonly record struct TrianglePatternAnalysis(
        float Severity,
        int SliverCount,
        int ZigzagChains,
        int FacetSeams,
        int MicroFacets);

    private static TrianglePatternAnalysis AnalyzeTrianglePatterns(float[] mesh)
    {
        int stride = ProceduralMeshes.Stride;
        int triCount = mesh.Length / stride / 3;
        if (triCount < 6)
            return new TrianglePatternAnalysis(0f, 0, 0, 0, 0);

        var b = MeshBounds.From(mesh);
        float span = MathF.Max(MathF.Max(b.MaxAbsX * 2f, b.MaxY - b.MinY), b.MaxZ - b.MinZ);
        float microAreaThreshold = span * span * 0.00008f;

        var centroids = new List<(float X, float Z, float Thin)>(triCount);
        int sliverCount = 0;
        int facetSeams = 0;
        int microFacets = 0;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = t * 3 * stride;
            int i1 = i0 + stride;
            int i2 = i0 + stride * 2;
            var v0 = new Vector3(mesh[i0], mesh[i0 + 1], mesh[i0 + 2]);
            var v1 = new Vector3(mesh[i1], mesh[i1 + 1], mesh[i1 + 2]);
            var v2 = new Vector3(mesh[i2], mesh[i2 + 1], mesh[i2 + 2]);

            float e0 = Vector3.Distance(v0, v1);
            float e1 = Vector3.Distance(v1, v2);
            float e2 = Vector3.Distance(v2, v0);
            float maxE = MathF.Max(e0, MathF.Max(e1, e2));
            float minE = MathF.Min(e0, MathF.Min(e1, e2));
            float thin = maxE > 0.001f ? minE / maxE : 1f;
            if (thin < 0.18f)
                sliverCount++;

            float lum0 = (mesh[i0 + 3] + mesh[i0 + 4] + mesh[i0 + 5]) / 3f;
            float lum1 = (mesh[i1 + 3] + mesh[i1 + 4] + mesh[i1 + 5]) / 3f;
            float lum2 = (mesh[i2 + 3] + mesh[i2 + 4] + mesh[i2 + 5]) / 3f;
            float lumSpread = MathF.Max(lum0, MathF.Max(lum1, lum2)) - MathF.Min(lum0, MathF.Min(lum1, lum2));
            if (lumSpread > 0.13f)
                facetSeams++;

            float area = Vector3.Cross(v1 - v0, v2 - v0).Length * 0.5f;
            if (area < microAreaThreshold && thin < 0.42f)
                microFacets++;

            var centroid = (v0 + v1 + v2) / 3f;
            centroids.Add((centroid.X, centroid.Z, thin));
        }

        centroids.Sort((a, b) => a.Z.CompareTo(b.Z));

        int zigzagChains = 0;
        int alternations = 0;
        for (int i = 1; i < centroids.Count; i++)
        {
            float dz = centroids[i].Z - centroids[i - 1].Z;
            if (dz > 0.08f)
            {
                if (alternations >= 4)
                    zigzagChains++;
                alternations = 0;
                continue;
            }

            if (centroids[i].X * centroids[i - 1].X < 0f
                && MathF.Abs(centroids[i].X) > 0.02f
                && MathF.Abs(centroids[i - 1].X) > 0.02f)
            {
                alternations++;
            }
        }

        if (alternations >= 4)
            zigzagChains++;

        float n = MathF.Max(triCount, 1);
        float sliverRatio = sliverCount / n;
        float facetSeamRatio = facetSeams / n;
        float microFacetRatio = microFacets / n;
        float severity = Math.Clamp(
            sliverRatio * 1.1f
            + zigzagChains * 0.14f
            + facetSeamRatio * 0.95f
            + microFacetRatio * 0.75f,
            0f,
            1f);
        return new TrianglePatternAnalysis(severity, sliverCount, zigzagChains, facetSeams, microFacets);
    }

    private static CategoryScore ScoreMaterialContrast(float[] mesh, RaceVisualDefinition race, string kind)
    {
        var lumBuckets = new HashSet<int>();
        float minLum = 1f, maxLum = 0f, variance = 0f;
        int count = mesh.Length / ProceduralMeshes.Stride;

        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            lumBuckets.Add((int)(lum * 24f));
            minLum = MathF.Min(minLum, lum);
            maxLum = MathF.Max(maxLum, lum);
            variance += lum * lum;
        }

        variance = variance / count - MathF.Pow((minLum + maxLum) * 0.5f, 2f);
        float bandTarget = kind == ModelMeshSource.KindObject ? 4f : 6f;
        float bandScore = Math.Clamp(lumBuckets.Count / bandTarget, 0f, 1f);
        float rangeScore = Math.Clamp((maxLum - minLum) / 0.45f, 0f, 1f);
        float gritBonus = (race.Substrate?.Grit ?? 0f) > 0.05f && kind != ModelMeshSource.KindObject ? 0.12f : 0f;

        var triPattern = AnalyzeTrianglePatterns(mesh);
        float facetSeamRatio = triPattern.FacetSeams / (float)MathF.Max(count / 3, 1);
        float wrapPenalty = facetSeamRatio > 0.18f
            ? Math.Clamp((facetSeamRatio - 0.18f) * 10f, 0f, 3f)
            : 0f;

        float score = bandScore * 8f + rangeScore * 6f + MathF.Sqrt(MathF.Max(variance, 0f)) * 5f + gritBonus * 2f - wrapPenalty;
        string matNotes = wrapPenalty > 0.1f
            ? $"{lumBuckets.Count} luminance bands, range {maxLum - minLum:F2}, texture-wrap -{wrapPenalty:F1}"
            : $"{lumBuckets.Count} luminance bands, range {maxLum - minLum:F2}";
        return new CategoryScore("Materials", Math.Clamp(score, 0f, 16f), 16f, matNotes);
    }

    private static CategoryScore ScoreScaleFit(float[] mesh, string kind, string modelId, string raceId)
    {
        var b = MeshBounds.From(mesh);

        if (kind == ModelMeshSource.KindObject)
        {
            float span = MathF.Max(MathF.Max(b.MaxAbsX, b.MaxY), b.MaxZ - b.MinZ);
            float target = modelId is "neutral_planet" or "harvestable_planet" ? 8f : 4f;
            float fit = 1f - Math.Clamp(MathF.Abs(span - target) / target, 0f, 1f);
            return new CategoryScore("Scale", Math.Clamp(fit * 12f, 0f, 12f), 12f, $"span {span:F1} (target ~{target:F0})");
        }

        if (kind == ModelMeshSource.KindStation)
        {
            float styleScale = 0.85f + raceId.Length * 0f;
            RaceVisualSchema.TryGetRace(raceId, out var race);
            styleScale = 0.85f + (race?.Modifiers.Superstructure ?? 0.3f) * 0.3f;
            float targetRadius = 7f * styleScale;
            float radiusFit = 1f - Math.Clamp(MathF.Abs(b.MaxAbsX - targetRadius) / targetRadius, 0f, 1f);
            float heightFit = 1f - Math.Clamp(MathF.Abs(b.MaxY - targetRadius * 1.1f) / (targetRadius * 1.1f), 0f, 1f);
            float score = ((radiusFit + heightFit) / 2f) * 12f;
            return new CategoryScore("Scale", Math.Clamp(score, 0f, 12f), 12f,
                $"pad r~{b.MaxAbsX:F1} h~{b.MaxY:F1} (target ~{targetRadius:F0})");
        }

        var (len, wid, hgt) = ResolveHullDimensions(raceId, modelId);
        float xFit = 1f - Math.Clamp(MathF.Abs(b.MaxAbsX - wid * 0.5f) / MathF.Max(wid, 0.1f), 0f, 1f);
        float yFit = 1f - Math.Clamp(MathF.Abs(b.MaxY - hgt) / MathF.Max(hgt, 0.1f), 0f, 1f);
        float zFit = 1f - Math.Clamp(MathF.Abs(b.MaxZ - len) / MathF.Max(len, 0.1f), 0f, 1f);
        float shipScore = ((xFit + yFit + zFit) / 3f) * 12f;
        return new CategoryScore("Proportions", Math.Clamp(shipScore, 0f, 12f), 12f,
            $"envelope {wid:F1}×{hgt:F1}×{len:F1}");
    }

    private static CategoryScore ScoreSurfaceDetail(float[] mesh, RaceVisualDefinition race, string kind)
    {
        var b = MeshBounds.From(mesh);
        int tris = mesh.Length / ProceduralMeshes.Stride / 3;

        float styleDetail = kind == ModelMeshSource.KindStation
            ? Math.Clamp(b.MaxY / MathF.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 4f
            : race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase)
                || race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase)
                ? Math.Clamp(b.MaxY / MathF.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 5f
                : race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase)
                    ? 3f + Math.Clamp(b.MaxY / MathF.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 2f
                : race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase)
                    ? 3f + Math.Clamp(b.MaxY / MathF.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 2f
                : race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase)
                    || race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase)
                    ? 3f + Math.Clamp(b.MaxY / MathF.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 2f
                : race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase)
                    || race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase)
                    ? 3f + Math.Clamp(b.MaxY / MathF.Max(b.MaxAbsX, 0.1f), 0f, 1f) * 2f
                : 3f;

        float accentVerts = 0;
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            if (lum > 0.88f) accentVerts++;
        }

        float accentRatio = accentVerts / MathF.Max(tris * 3f, 1f);
        float accentTarget = kind == ModelMeshSource.KindObject ? 0.06f : 0.10f;
        float accentScore = Math.Clamp(accentRatio / accentTarget, 0f, 1f) * 5f;
        float triDetail = Math.Clamp(tris / (kind == ModelMeshSource.KindStation ? 120f : 80f), 0f, 1f) * 3f;

        float score = styleDetail + accentScore + triDetail;
        return new CategoryScore("SurfaceDetail", Math.Clamp(score, 0f, 12f), 12f,
            $"detail {styleDetail:F1}, accent {accentRatio:P0}");
    }

    private static CategoryScore ScoreRaceIdentity(float[] mesh, RaceVisualDefinition race, string kind)
    {
        if (kind == ModelMeshSource.KindObject)
        {
            float colorPop = ScoreMaterialContrast(mesh, race, kind).Score / 16f;
            return new CategoryScore("RaceIdentity", Math.Clamp(colorPop * 6f + 2f, 0f, 10f), 10f,
                "neutral object — clarity baseline");
        }

        var primary = PaletteVector(race.Palette.Primary);
        var secondary = PaletteVector(race.Palette.Secondary);
        var accent = PaletteVector(race.Palette.Accent);

        int nearPrimary = 0, nearSecondary = 0, nearAccent = 0, total = 0;
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            var c = new Vector3(mesh[i + 3], mesh[i + 4], mesh[i + 5]);
            total++;
            if (ColorDistance(c, primary) < 0.22f) nearPrimary++;
            if (ColorDistance(c, secondary) < 0.22f) nearSecondary++;
            if (ColorDistance(c, accent) < 0.28f) nearAccent++;
        }

        float paletteCoverage = Math.Clamp((nearPrimary + nearSecondary) / (float)MathF.Max(total, 1) * 2.5f, 0f, 1f);
        float accentPresence = Math.Clamp(nearAccent / (float)MathF.Max(total, 1) * 18f, 0f, 1f);
        float styleBonus = race.Style.Length > 0 ? 0.15f : 0f;
        float grit = (race.Substrate?.Grit ?? 0f) > 0.04f ? 0.12f : 0f;

        float score = paletteCoverage * 5f + accentPresence * 3f + (styleBonus + grit) * 10f;
        return new CategoryScore("RaceIdentity", Math.Clamp(score, 0f, 10f), 10f,
            $"{race.DisplayName} palette {paletteCoverage:P0}, accent {accentPresence:P0}, style={race.Style}");
    }

    private static CategoryScore ScoreScreenshot(string? screenshotPath, string kind)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath) || !File.Exists(screenshotPath))
            return new CategoryScore("Screenshot", 0f, 16f, "no capture");

        if (!PngMetrics.TryAnalyze(screenshotPath, out var metrics))
            return new CategoryScore("Screenshot", 0f, 16f, "unreadable PNG");

        float fgTarget = kind switch
        {
            ModelMeshSource.KindStation => 0.05f,
            ModelMeshSource.KindObject => 0.03f,
            _ => metrics.PanelCount >= 3 ? 0.04f : 0.08f,
        };

        float score;
        string notes;
        if (metrics.PanelMetrics is { Count: >= 3 })
        {
            var panelScores = metrics.PanelMetrics
                .Select(p => ScoreScreenshotPanel(p, fgTarget))
                .ToList();
            ReadOnlySpan<float> panelWeights = stackalloc float[] { 0.55f, 0.25f, 0.20f };
            score = 0f;
            for (int i = 0; i < panelScores.Count && i < panelWeights.Length; i++)
                score += panelScores[i] * panelWeights[i];
            float minFg = metrics.PanelMetrics.Min(p => p.ForegroundRatio);
            float maxFg = metrics.PanelMetrics.Max(p => p.ForegroundRatio);
            float balance = maxFg > 0.001f ? Math.Clamp(minFg / maxFg, 0f, 1f) : 0f;
            if (metrics.PanelMetrics.Count(p => p.ForegroundRatio >= 0.006f) < 3)
                balance *= 0.5f;
            score = Math.Clamp(score + balance * 2f, 0f, 16f);

            float primaryFg = metrics.PanelMetrics[0].ForegroundRatio;
            float avgContrast = metrics.PanelMetrics.Average(p => p.LuminanceStdDev);
            float avgEdge = metrics.PanelMetrics.Average(p => p.EdgeDensity);
            notes = $"3-view p1 fg {primaryFg:P1} (55%), balance {balance:F2}, contrast {avgContrast:F2}, edges {avgEdge:F3}";
        }
        else
        {
            score = Math.Clamp(ScoreScreenshotPanel(metrics, fgTarget), 0f, 14f);
            notes = $"fg {metrics.ForegroundRatio:P1}, contrast {metrics.LuminanceStdDev:F2}, edges {metrics.EdgeDensity:F3}";
        }

        return new CategoryScore("Screenshot", Math.Clamp(score, 0f, 16f), 16f, notes);
    }

    private static float ScoreScreenshotPanel(PngMetrics.Metrics metrics, float fgTarget)
    {
        float coverage = Math.Clamp(metrics.ForegroundRatio / fgTarget, 0f, 1f);
        float contrast = Math.Clamp(metrics.LuminanceStdDev / 0.16f, 0f, 1f);
        float edge = Math.Clamp(metrics.EdgeDensity / 0.075f, 0f, 1f);
        float color = Math.Clamp(metrics.ColorDiversity / 20f, 0f, 1f);
        return coverage * 4f + contrast * 4f + edge * 3f + color * 3f;
    }

    private static List<string> BuildSuggestions(
        IReadOnlyList<CategoryScore> categories,
        RaceVisualDefinition race,
        string kind,
        string modelId)
    {
        var suggestions = new List<string>();
        var byName = categories.ToDictionary(c => c.Name, c => c);

        string formKey = kind switch
        {
            ModelMeshSource.KindStation => "Massing",
            ModelMeshSource.KindObject => "IconRead",
            _ => "Silhouette",
        };

        if (byName.TryGetValue(formKey, out var form) && form.Score < form.MaxScore * 0.72f)
        {
            suggestions.Add(kind switch
            {
                ModelMeshSource.KindStation => $"Strengthen {modelId} plan massing: widen pad footprint, cluster deck superstructure, shorten lone vertical spires — readable from oblique top-down.",
                ModelMeshSource.KindObject => $"Improve {modelId} icon readability: bolder silhouette, higher contrast, centered composition for HUD/map scale.",
                _ => $"Strengthen {race.DisplayName} dorsal silhouette on {modelId}: elongate bow (+Z), deck mass readable from oblique top-down, narrow wings (style={race.Style}).",
            });
        }

        if (byName["Geometry"].Notes.Contains("tri-pattern", StringComparison.OrdinalIgnoreCase))
            suggestions.Add("Visible triangle patterns — incomplete mesh definition or bad texture wrap. Replace facet strips with flush boxes/panels, merge coplanar faces, and smooth vertex luminance across flat surfaces.");

        if (byName["Materials"].Notes.Contains("texture-wrap", StringComparison.OrdinalIgnoreCase))
            suggestions.Add("Fix texture wrap: align vertex luminance bands per material zone; avoid per-triangle color gradients on surfaces meant to read as flat panels.");
        if (byName["Geometry"].Score < 11f)
        {
            int tris = int.Parse(byName["Geometry"].Notes.Split(' ')[0], CultureInfo.InvariantCulture);
            suggestions.Add(tris < 36
                ? "Add more structural sections, panel lines, and kit-bash detail — mesh is too primitive."
                : "Simplify overlapping triangles or merge coplanar faces — mesh may be too dense/noisy.");
        }

        if (byName["Materials"].Score < 10f)
            suggestions.Add("Increase material contrast: darker bays, brighter accent bands, stronger substrate grit variation.");

        string scaleKey = kind == ModelMeshSource.KindShip ? "Proportions" : "Scale";
        if (byName.TryGetValue(scaleKey, out var scale) && scale.Score < scale.MaxScore * 0.65f)
            suggestions.Add($"Re-anchor {modelId} to gameplay envelope; verify bounds against {kind} scale targets.");

        if (byName["SurfaceDetail"].Score < 8f)
        {
            if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
                suggestions.Add("Add vasudan surface detail: segmented belly plates, dorsal spine ridges, lateral intake scoops.");
            else
                suggestions.Add("Add flush surface detail: panel seams, light strips, and recessed glow wells.");
        }

        if (byName["RaceIdentity"].Score < 7f && kind != ModelMeshSource.KindObject)
            suggestions.Add($"Push {race.DisplayName} identity: align vertex bands to primary/secondary/accent palette in race_visuals.json.");

        if (byName["Screenshot"].Score < 9f)
            suggestions.Add("Improve panel-1 RTS readability (55% weight): boost dorsal/plan lighting and contrast; verify all 3 oblique top-down panels show the asset.");

        if (suggestions.Count == 0)
            suggestions.Add("Polish micro-details: sharper facet transitions, accent tips on leading edges, engine glow gradients.");

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

        string hullKey = hullId;
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "fighter_basic")
            {
                wid *= 1.60f;
                len *= 0.66f;
            }
            else if (hullKey is "hero_default")
            {
                wid *= 1.68f;
                len *= 0.66f;
            }
            else if (hullKey is "scout_light")
            {
                wid *= 1.72f;
                len *= 0.66f;
            }
            else if (hullKey is "interceptor_mk2")
            {
                wid *= 1.68f;
                len *= 0.70f;
            }
            else if (hullKey is "drone_swarm")
            {
                wid *= 1.18f;
                len *= 0.86f;
            }
            else if (hullKey is "corvette_fast")
            {
                wid *= 1.62f;
                len *= 0.68f;
            }
            else if (hullKey is "frigate_strike")
            {
                wid *= 1.56f;
                len *= 0.72f;
            }
            else if (hullKey is "gunship_heavy" or "bomber_heavy")
            {
                wid *= 1.42f;
                len *= 0.74f;
            }
            else if (hullKey is "destroyer_assault")
            {
                wid *= 2.24f;
                len *= 0.48f;
                hgt *= 0.94f;
            }
            else if (hullKey is "cruiser_heavy")
            {
                wid *= 1.22f;
                len *= 0.74f;
                hgt *= 0.92f;
            }
            else if (hullKey is "carrier_command")
            {
                wid *= 1.46f;
                len *= 0.66f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.44f;
                len *= 0.64f;
                hgt *= 0.90f;
            }
            else if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor")
            {
                wid *= 1.48f;
                len *= 0.74f;
            }
            else if (hullKey is "transport_cargo" or "freighter_bulk")
            {
                wid *= 1.42f;
                len *= 0.72f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.46f;
                len *= 0.70f;
            }
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "fighter_basic")
            {
                wid *= 1.32f;
                len *= 0.76f;
            }
            else if (hullKey is "scout_light")
            {
                wid *= 1.42f;
                len *= 0.72f;
            }
            else if (hullKey is "interceptor_mk2")
            {
                wid *= 1.30f;
                len *= 0.76f;
                hgt *= 1.08f;
            }
            else if (hullKey is "drone_swarm")
            {
                wid *= 1.24f;
                len *= 0.80f;
            }
            else if (hullKey is "hero_default")
            {
                wid *= 1.06f;
                len *= 0.82f;
            }
            else if (hullKey is "corvette_fast")
            {
                wid *= 1.38f;
                len *= 0.72f;
            }
            else if (hullKey is "frigate_strike")
            {
                wid *= 1.38f;
                len *= 0.72f;
            }
            else if (hullKey is "gunship_heavy")
            {
                wid *= 1.04f;
                len *= 0.82f;
            }
            else if (hullKey is "bomber_heavy")
            {
                wid *= 1.02f;
                len *= 0.84f;
            }
            else if (hullKey is "destroyer_assault")
            {
                wid *= 1.48f;
                len *= 0.62f;
                hgt *= 0.94f;
            }
            else if (hullKey is "cruiser_heavy")
            {
                wid *= 1.26f;
                len *= 0.72f;
                hgt *= 0.92f;
            }
            else if (hullKey is "carrier_command")
            {
                wid *= 1.32f;
                len *= 0.68f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.30f;
                len *= 0.66f;
                hgt *= 0.90f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.24f;
                len *= 0.76f;
            }
            else if (hullKey is "miner_eva" or "miner_tractor")
            {
                wid *= 1.18f;
                len *= 0.78f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.42f;
                len *= 0.68f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.22f;
                len *= 0.74f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.12f;
                len *= 0.78f;
            }
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "fighter_basic")
            {
                wid *= 1.76f;
                len *= 0.73f;
            }
            else if (hullKey is "scout_light")
            {
                wid *= 1.68f;
                len *= 0.68f;
            }
            else if (hullKey is "interceptor_mk2")
            {
                wid *= 1.52f;
                len *= 0.74f;
                hgt *= 1.06f;
            }
            else if (hullKey is "drone_swarm")
            {
                wid *= 1.48f;
                len *= 0.76f;
            }
            else if (hullKey is "hero_default")
            {
                wid *= 1.58f;
                len *= 0.72f;
            }
            else if (hullKey is "corvette_fast")
            {
                wid *= 1.52f;
                len *= 0.70f;
            }
            else if (hullKey is "frigate_strike")
            {
                wid *= 1.55f;
                len *= 0.68f;
            }
            else if (hullKey is "gunship_heavy")
            {
                wid *= 1.08f;
                len *= 0.84f;
            }
            else if (hullKey is "bomber_heavy")
            {
                wid *= 1.12f;
                len *= 0.82f;
            }
            else if (hullKey is "destroyer_assault")
            {
                wid *= 1.78f;
                len *= 0.56f;
                hgt *= 0.94f;
            }
            else if (hullKey is "cruiser_heavy")
            {
                wid *= 1.38f;
                len *= 0.68f;
                hgt *= 0.92f;
            }
            else if (hullKey is "carrier_command")
            {
                wid *= 1.28f;
                len *= 0.66f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.42f;
                len *= 0.62f;
                hgt *= 0.90f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.22f;
                len *= 0.76f;
            }
            else if (hullKey is "miner_eva" or "miner_tractor")
            {
                wid *= 1.18f;
                len *= 0.78f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.18f;
                len *= 0.74f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.08f;
                len *= 0.82f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.22f;
                len *= 0.76f;
            }
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "scout_light") { wid *= 2.12f; len *= 0.58f; }
            else if (hullKey is "interceptor_mk2") { wid *= 1.92f; len *= 0.62f; }
            else if (hullKey is "drone_swarm") { wid *= 1.40f; len *= 0.80f; }
            else if (hullKey is "fighter_basic") { wid *= 1.68f; len *= 0.80f; }
            else if (hullKey is "hero_default") { wid *= 1.62f; len *= 0.82f; hgt *= 0.84f; }
            else if (hullKey is "corvette_fast") { wid *= 1.48f; len *= 0.74f; }
            else if (hullKey is "frigate_strike") { wid *= 1.35f; len *= 0.78f; }
            else if (hullKey is "gunship_heavy") { wid *= 1.12f; len *= 0.94f; hgt *= 0.92f; }
            else if (hullKey is "bomber_heavy") { wid *= 0.92f; len *= 1.02f; }
            else if (hullKey is "destroyer_assault") { wid *= 1.72f; len *= 0.68f; hgt *= 0.88f; }
            else if (hullKey is "cruiser_heavy") { wid *= 1.38f; len *= 0.86f; hgt *= 0.82f; }
            else if (hullKey is "carrier_command") { wid *= 0.90f; len *= 1.04f; hgt *= 0.78f; }
            else if (hullKey is "dreadnought") { wid *= 1.22f; len *= 0.88f; hgt *= 0.74f; }
            else if (hullKey is "miner_basic") { wid *= 1.58f; len *= 0.82f; }
            else if (hullKey is "miner_eva" or "miner_tractor") { wid *= 1.52f; len *= 0.84f; }
            else if (hullKey is "transport_cargo") { wid *= 1.55f; len *= 0.82f; }
            else if (hullKey is "freighter_bulk") { wid *= 1.58f; len *= 0.80f; }
            else if (hullKey is "support_repair") { wid *= 1.55f; len *= 0.82f; }
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "scout_light") { wid *= 2.48f; len *= 0.58f; }
            else if (hullKey is "interceptor_mk2") { wid *= 2.38f; len *= 0.60f; }
            else if (hullKey is "drone_swarm") { wid *= 1.72f; len *= 0.78f; }
            else if (hullKey is "fighter_basic") { wid *= 2.22f; len *= 0.62f; }
            else if (hullKey is "hero_default") { wid *= 2.05f; len *= 0.64f; hgt *= 0.88f; }
            else if (hullKey is "corvette_fast") { wid *= 1.92f; len *= 0.68f; }
            else if (hullKey is "frigate_strike") { wid *= 1.85f; len *= 0.70f; }
            else if (hullKey is "gunship_heavy") { wid *= 1.06f; len *= 0.92f; }
            else if (hullKey is "bomber_heavy") { wid *= 1.08f; len *= 0.90f; }
            else if (hullKey is "destroyer_assault") { wid *= 2.05f; len *= 0.58f; hgt *= 0.90f; }
            else if (hullKey is "cruiser_heavy") { wid *= 1.72f; len *= 0.66f; hgt *= 0.86f; }
            else if (hullKey is "carrier_command") { wid *= 1.48f; len *= 0.64f; hgt *= 0.82f; }
            else if (hullKey is "dreadnought") { wid *= 1.55f; len *= 0.62f; hgt *= 0.84f; }
            else if (hullKey is "transport_cargo") { wid *= 1.42f; len *= 0.70f; }
            else if (hullKey is "support_repair") { wid *= 1.38f; len *= 0.72f; }
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "scout_light") { wid *= 2.85f; len *= 0.56f; }
            else if (hullKey is "interceptor_mk2") { wid *= 3.05f; len *= 0.52f; }
            else if (hullKey is "interceptor") { wid *= 2.72f; len *= 0.58f; }
            else if (hullKey is "drone_swarm") { wid *= 1.95f; len *= 0.76f; }
            else if (hullKey is "fighter_basic") { wid *= 2.55f; len *= 0.60f; }
            else if (hullKey is "hero_default") { wid *= 2.35f; len *= 0.62f; hgt *= 0.86f; }
            else if (hullKey is "corvette_fast") { wid *= 2.18f; len *= 0.64f; }
            else if (hullKey is "frigate_strike") { wid *= 2.05f; len *= 0.66f; }
            else if (hullKey is "gunship_heavy") { wid *= 1.72f; len *= 0.72f; }
            else if (hullKey is "bomber_heavy") { wid *= 1.68f; len *= 0.74f; }
            else if (hullKey is "destroyer_assault") { wid *= 2.22f; len *= 0.56f; hgt *= 0.88f; }
            else if (hullKey is "cruiser_heavy") { wid *= 1.88f; len *= 0.64f; hgt *= 0.84f; }
            else if (hullKey is "carrier_command") { wid *= 1.62f; len *= 0.62f; hgt *= 0.80f; }
            else if (hullKey is "dreadnought") { wid *= 1.72f; len *= 0.60f; hgt *= 0.82f; }
            else if (hullKey is "transport_cargo") { wid *= 2.05f; len *= 0.64f; }
            else if (hullKey is "freighter_bulk") { wid *= 1.78f; len *= 0.68f; }
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            if (hullKey is "scout_light") { wid *= 2.05f; len *= 0.58f; }
            else if (hullKey is "interceptor_mk2") { wid *= 1.95f; len *= 0.60f; }
            else if (hullKey is "drone_swarm") { wid *= 1.38f; len *= 0.86f; }
            else if (hullKey is "fighter_basic") { wid *= 1.50f; len *= 0.78f; }
            else if (hullKey is "hero_default") { wid *= 1.62f; len *= 0.76f; hgt *= 0.88f; }
            else if (hullKey is "corvette_fast") { wid *= 1.72f; len *= 0.70f; }
            else if (hullKey is "frigate_strike") { wid *= 1.85f; len *= 0.66f; }
            else if (hullKey is "gunship_heavy") { wid *= 0.90f; len *= 1.02f; hgt *= 0.94f; }
            else if (hullKey is "bomber_heavy") { wid *= 0.92f; len *= 1.00f; }
            else if (hullKey is "destroyer_assault") { wid *= 1.48f; len *= 0.72f; hgt *= 0.86f; }
            else if (hullKey is "cruiser_heavy") { wid *= 1.45f; len *= 0.78f; hgt *= 0.74f; }
            else if (hullKey is "carrier_command") { wid *= 1.02f; len *= 0.98f; hgt *= 0.82f; }
            else if (hullKey is "dreadnought") { wid *= 1.38f; len *= 0.90f; hgt *= 0.70f; }
            else if (hullKey is "miner_basic") { wid *= 1.18f; len *= 0.86f; }
            else if (hullKey is "miner_eva" or "miner_tractor") { wid *= 1.26f; len *= 0.88f; }
            else if (hullKey is "transport_cargo") { wid *= 1.22f; len *= 0.88f; }
            else if (hullKey is "freighter_bulk") { wid *= 1.38f; len *= 0.86f; }
            else if (hullKey is "support_repair") { wid *= 1.20f; len *= 0.88f; }
        }

        return (len, wid, hgt);
    }

    private static Vector3 PaletteVector(float[] rgb) =>
        new(rgb[0], rgb.Length > 1 ? rgb[1] : rgb[0], rgb.Length > 2 ? rgb[2] : rgb[0]);

    private static float ColorDistance(Vector3 a, Vector3 b) => Vector3.Distance(a, b);

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
    internal sealed record Metrics(
        float ForegroundRatio,
        float LuminanceStdDev,
        float EdgeDensity,
        int ColorDiversity,
        int PanelCount = 1,
        IReadOnlyList<Metrics>? PanelMetrics = null);

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
        var full = new Metrics(
            fg / (float)total,
            (float)Math.Sqrt(Math.Max(variance, 0)),
            edges / (float)total,
            colors.Count);

        if (w < 96) return full;

        const int panelCount = 3;
        int panelW = w / panelCount;
        var panels = new List<Metrics>(panelCount);
        for (int p = 0; p < panelCount; p++)
        {
            int x0 = p * panelW;
            int x1 = p == panelCount - 1 ? w : x0 + panelW;
            panels.Add(AnalyzeRegion(rgba, w, h, x0, 0, x1, h, panelCount));
        }

        if (!LooksLikeTriView(panels))
            return full;

        return full with
        {
            PanelCount = panelCount,
            PanelMetrics = panels,
        };
    }

    private static bool LooksLikeTriView(IReadOnlyList<Metrics> panels)
    {
        if (panels.Count < 3) return false;

        float maxFg = panels.Max(p => p.ForegroundRatio);
        float minFg = panels.Min(p => p.ForegroundRatio);
        float avgFg = panels.Average(p => p.ForegroundRatio);
        return avgFg is >= 0.008f and <= 0.22f
            && maxFg - minFg <= 0.14f
            && panels.Count(p => p.ForegroundRatio >= 0.006f) >= 2;
    }

    private static Metrics AnalyzeRegion(
        byte[] rgba, int w, int h, int x0, int y0, int x1, int y1, int panelCount)
    {
        int fg = 0;
        double sum = 0, sumSq = 0;
        var colors = new HashSet<int>();
        int edges = 0;
        int total = 0;

        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                int i = (y * w + x) * 4;
                float r = rgba[i] / 255f, g = rgba[i + 1] / 255f, b = rgba[i + 2] / 255f;
                float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                bool isBg = lum < 0.06f && r < 0.08f && g < 0.08f && b < 0.15f;
                if (!isBg) fg++;

                sum += lum;
                sumSq += lum * lum;
                colors.Add((rgba[i] >> 4) << 8 | (rgba[i + 1] >> 4) << 4 | (rgba[i + 2] >> 4));

                if (x > x0 && y > y0)
                {
                    int j = (y * w + (x - 1)) * 4;
                    float lumPrev = 0.2126f * rgba[j] + 0.7152f * rgba[j + 1] + 0.0722f * rgba[j + 2];
                    if (MathF.Abs(lum - lumPrev) > 0.08f) edges++;
                }

                total++;
            }
        }

        if (total == 0)
            return new Metrics(0f, 0f, 0f, 0, panelCount);

        double mean = sum / total;
        double variance = sumSq / total - mean * mean;
        return new Metrics(
            fg / (float)total,
            (float)Math.Sqrt(Math.Max(variance, 0)),
            edges / (float)total,
            colors.Count,
            panelCount);
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
