using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Pure logic for galactic star map hit-testing, unlock rules, and hyperlane graph.
/// </summary>
public static class StarMapLogic
{
    public const float DefaultPlanetRadius = 28f;

    /// <summary>Return whether a screen point lies within a planet's hit radius.</summary>
    public static bool IsPlanetHit(Vector2 point, Vector2 planetCenter, float radius = DefaultPlanetRadius)
    {
        Vector2 delta = point - planetCenter;
        return delta.LengthSquared <= radius * radius;
    }

    /// <summary>
    /// A mission is unlocked when it has no prerequisite or the prerequisite is completed.
    /// </summary>
    public static bool IsMissionUnlocked(string? prerequisiteMissionId, IReadOnlySet<string> completedMissionIds)
    {
        if (string.IsNullOrWhiteSpace(prerequisiteMissionId))
            return true;

        return completedMissionIds.Contains(prerequisiteMissionId);
    }

    /// <summary>Resolve a planet colour from a hex string; falls back to <paramref name="fallback"/>.</summary>
    public static Vector4 ParsePlanetColor(string? hex, Vector4 fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        string value = hex.Trim();
        if (value.StartsWith('#'))
            value = value[1..];

        if (value.Length != 6 && value.Length != 8)
            return fallback;

        if (!int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out int packed))
            return fallback;

        float r = ((packed >> 16) & 0xFF) / 255f;
        float g = ((packed >> 8) & 0xFF) / 255f;
        float b = (packed & 0xFF) / 255f;
        float a = value.Length == 8 ? ((packed >> 24) & 0xFF) / 255f : 1f;
        return new Vector4(r, g, b, a);
    }

    /// <summary>Convert normalized star-map coordinates to pixel centre inside a canvas.</summary>
    public static Vector2 ToPlanetCenter(Vector2 normalizedPosition, Vector2 canvasPosition, Vector2 canvasSize)
    {
        return canvasPosition + new Vector2(
            normalizedPosition.X * canvasSize.X,
            normalizedPosition.Y * canvasSize.Y);
    }

    /// <summary>
    /// Build hyperlane segments from prerequisite links where the destination system is unlocked.
    /// </summary>
    public static IReadOnlyList<StarMapHyperlane> BuildHyperlanes(IReadOnlyList<StarMapNode> nodes)
    {
        var byId = nodes.ToDictionary(n => n.Id);
        var lanes = new List<StarMapHyperlane>();

        foreach (StarMapNode node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.PrerequisiteMissionId))
                continue;

            if (!node.IsUnlocked)
                continue;

            if (!byId.TryGetValue(node.PrerequisiteMissionId, out StarMapNode? prerequisite))
                continue;

            lanes.Add(new StarMapHyperlane(prerequisite.Position, node.Position));
        }

        return lanes;
    }

    /// <summary>Find the topmost unlocked planet under a screen point.</summary>
    public static StarMapNode? HitTestPlanets(
        Vector2 point,
        IReadOnlyList<StarMapNode> nodes,
        Vector2 canvasPosition,
        Vector2 canvasSize,
        float radius = DefaultPlanetRadius)
    {
        StarMapNode? hit = null;
        float bestDepth = float.MinValue;

        foreach (StarMapNode node in nodes)
        {
            if (!node.IsUnlocked)
                continue;

            Vector2 center = ToPlanetCenter(node.Position, canvasPosition, canvasSize);
            if (!IsPlanetHit(point, center, radius))
                continue;

            float depth = node.Position.X + node.Position.Y;
            if (depth >= bestDepth)
            {
                bestDepth = depth;
                hit = node;
            }
        }

        return hit;
    }
}

/// <summary>Runtime data for one planet node on the star map.</summary>
public sealed record StarMapNode(
    string Id,
    string PlanetName,
    Vector2 Position,
    Vector4 PlanetColor,
    string? PrerequisiteMissionId,
    bool IsUnlocked,
    bool IsCompleted);

/// <summary>A hyperlane segment between two normalized map positions.</summary>
public sealed record StarMapHyperlane(Vector2 From, Vector2 To);