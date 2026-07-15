using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Interactive galactic star map: hyperlanes, planet nodes, hover glow, and click handling.
/// </summary>
public sealed class StarMapCanvas : Widget
{
    private const float PlanetVisualRadius = 22f;
    private const float DoubleClickSeconds = 0.35f;
    private const float PlanetLabelFontSize = 15f;
    private const float PlanetLabelPaddingX = 8f;
    private const float PlanetLabelPaddingY = 4f;
    private const float LockBadgeFontSize = 10f;

    private readonly List<StarMapNode> _nodes = new();
    private float _time;
    private string? _hoveredId;
    private string? _selectedId;
    private float _lastClickTime = -10f;
    private string? _lastClickId;

    /// <summary>Fired when the player selects an unlocked planet.</summary>
    public event Action<string>? PlanetSelected;

    /// <summary>Fired when the player double-clicks an unlocked planet.</summary>
    public event Action<string>? PlanetActivated;

    /// <summary>Replace all planet nodes on the map.</summary>
    public void SetNodes(IEnumerable<StarMapNode> nodes)
    {
        _nodes.Clear();
        _nodes.AddRange(nodes);
        _hoveredId = null;
    }

    /// <summary>Highlight the currently selected mission planet.</summary>
    public void SetSelectedMission(string? missionId) => _selectedId = missionId;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        _time += deltaTime;
        base.Update(deltaTime);
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible)
            return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (!Contains(screenPoint, containerPosition, containerSize))
            return false;

        StarMapNode? hit = StarMapLogic.HitTestPlanets(screenPoint, _nodes, pos, size);
        if (hit == null)
            return false;

        PlanetSelected?.Invoke(hit.Id);

        float now = _time;
        if (_lastClickId == hit.Id && now - _lastClickTime <= DoubleClickSeconds)
        {
            PlanetActivated?.Invoke(hit.Id);
            _lastClickId = null;
            _lastClickTime = -10f;
        }
        else
        {
            _lastClickId = hit.Id;
            _lastClickTime = now;
        }

        return true;
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible)
            return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        StarMapNode? hit = StarMapLogic.HitTestPlanets(pointerPosition, _nodes, pos, size);
        _hoveredId = hit?.Id;

        base.UpdatePointerState(pointerPosition, isPointerDown, containerPosition, containerSize);
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        DrawHyperlanes(renderer, position, size);
        DrawPlanets(renderer, position, size);
    }

    private void DrawHyperlanes(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        var byId = _nodes.ToDictionary(n => n.Id);
        var unlockedLaneColor = new Vector4(0.35f, 0.55f, 0.95f, 0.45f);

        foreach (StarMapNode node in _nodes)
        {
            if (string.IsNullOrWhiteSpace(node.PrerequisiteMissionId))
                continue;

            if (!byId.TryGetValue(node.PrerequisiteMissionId, out StarMapNode? prerequisite))
                continue;

            Vector2 from = StarMapLogic.ToPlanetCenter(prerequisite.Position, position, size);
            Vector2 to = StarMapLogic.ToPlanetCenter(node.Position, position, size);

            if (node.IsUnlocked)
                DrawLine(renderer, from, to, unlockedLaneColor, 2f);
            else
                DrawDashedLine(renderer, from, to, MenuTheme.StarMapLockedLaneColor, 2f);
        }
    }

    private void DrawPlanets(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        foreach (StarMapNode node in _nodes)
        {
            Vector2 center = StarMapLogic.ToPlanetCenter(node.Position, position, size);
            bool isHovered = node.Id == _hoveredId;
            bool isSelected = node.Id == _selectedId;

            if (isSelected)
            {
                float pulse = 0.65f + 0.35f * MathF.Sin(_time * 4f);
                var outerRing = MenuTheme.StarMapSelectionRingColor * new Vector4(1f, 1f, 1f, 0.35f * pulse);
                DrawOrbitRing(renderer, center, PlanetVisualRadius + 28f, outerRing, segments: 56, dotSize: 3f);
                var innerRing = MenuTheme.StarMapSelectionRingColor * new Vector4(1f, 1f, 1f, 0.75f);
                DrawOrbitRing(renderer, center, PlanetVisualRadius + 18f, innerRing, segments: 48, dotSize: 2.5f);
                var selectionGlow = node.PlanetColor * new Vector4(1f, 1f, 1f, node.IsUnlocked ? 0.42f : 0.18f);
                DrawFilledCircle(renderer, center, PlanetVisualRadius + 14f, selectionGlow);
            }
            else if (isHovered && node.IsUnlocked)
            {
                float pulse = 0.55f + 0.45f * MathF.Sin(_time * 5f);
                var glow = node.PlanetColor * new Vector4(1f, 1f, 1f, 0.22f * pulse);
                DrawFilledCircle(renderer, center, PlanetVisualRadius + 10f + pulse * 8f, glow);
            }

            if (node.IsUnlocked)
            {
                DrawOrbitRing(renderer, center, PlanetVisualRadius + 16f, node.PlanetColor * new Vector4(1f, 1f, 1f, 0.35f));
            }
            else
            {
                DrawDashedOrbitRing(renderer, center, PlanetVisualRadius + 16f,
                    MenuTheme.StarMapLockedLabelColor * new Vector4(1f, 1f, 1f, 0.45f));
            }

            Vector4 bodyColor = node.IsUnlocked
                ? node.PlanetColor
                : node.PlanetColor * MenuTheme.StarMapLockedBodyTint;
            DrawFilledCircle(renderer, center, PlanetVisualRadius, bodyColor);
            DrawFilledCircle(renderer, center, PlanetVisualRadius * 0.55f,
                bodyColor * new Vector4(node.IsUnlocked ? 1.15f : 0.95f, node.IsUnlocked ? 1.15f : 0.95f, node.IsUnlocked ? 1.15f : 0.95f, 1f));

            if (!node.IsUnlocked)
                DrawLockBadge(renderer, center);

            if (node.IsCompleted)
                DrawVictoryMarker(renderer, center, PlanetVisualRadius);

            DrawPlanetLabel(renderer, center, node.PlanetName, node.IsUnlocked, isSelected);
        }
    }

    private static void DrawPlanetLabel(
        IUIRenderer renderer, Vector2 center, string planetName, bool isUnlocked, bool isSelected)
    {
        if (string.IsNullOrWhiteSpace(planetName))
            return;

        float textWidth = UIFontMetrics.MeasureTextWidth(planetName, PlanetLabelFontSize);
        float scrimWidth = textWidth + PlanetLabelPaddingX * 2f;
        float scrimHeight = PlanetLabelFontSize + PlanetLabelPaddingY * 2f;
        var scrimPos = new Vector2(center.X - scrimWidth * 0.5f, center.Y + PlanetVisualRadius + 8f);

        renderer.DrawRect(scrimPos, new Vector2(scrimWidth, scrimHeight), MenuTheme.StarMapLabelScrimColor);

        Vector4 textColor = isUnlocked
            ? isSelected ? MenuTheme.StarMapSelectionRingColor : MenuTheme.StarMapLabelColor
            : MenuTheme.StarMapLockedLabelColor;

        var textPos = scrimPos + new Vector2(PlanetLabelPaddingX, PlanetLabelPaddingY);
        renderer.DrawText(planetName, textPos, PlanetLabelFontSize, textColor);
    }

    private static void DrawLockBadge(IUIRenderer renderer, Vector2 center)
    {
        const string lockLabel = "LOCK";
        float badgeWidth = UIFontMetrics.MeasureTextWidth(lockLabel, LockBadgeFontSize) + 8f;
        float badgeHeight = LockBadgeFontSize + 6f;
        var badgePos = center + new Vector2(-badgeWidth * 0.5f, -PlanetVisualRadius - badgeHeight - 4f);

        renderer.DrawRect(badgePos, new Vector2(badgeWidth, badgeHeight),
            new Vector4(0.08f, 0.1f, 0.16f, 0.92f));

        float labelWidth = UIFontMetrics.MeasureTextWidth(lockLabel, LockBadgeFontSize);
        var labelPos = badgePos + new Vector2((badgeWidth - labelWidth) * 0.5f, 3f);
        renderer.DrawText(lockLabel, labelPos, LockBadgeFontSize, MenuTheme.StarMapLockedLabelColor);

        DrawPadlockGlyph(renderer, center + new Vector2(0f, -2f), 12f);
    }

    private static void DrawPadlockGlyph(IUIRenderer renderer, Vector2 center, float size)
    {
        var lockColor = new Vector4(0.82f, 0.86f, 0.92f, 0.95f);
        float bodyW = size * 0.7f;
        float bodyH = size * 0.55f;
        var bodyPos = center + new Vector2(-bodyW * 0.5f, size * 0.05f);
        renderer.DrawRect(bodyPos, new Vector2(bodyW, bodyH), lockColor);

        float shackleW = bodyW * 0.55f;
        float shackleH = size * 0.45f;
        var shacklePos = center + new Vector2(-shackleW * 0.5f, -shackleH + size * 0.08f);
        renderer.DrawRect(shacklePos, new Vector2(shackleW, shackleH), lockColor * new Vector4(1f, 1f, 1f, 0.85f));
        renderer.DrawRect(shacklePos + new Vector2(shackleW * 0.2f, shackleH * 0.35f),
            new Vector2(shackleW * 0.6f, shackleH * 0.45f),
            MenuTheme.StarMapLockedBodyTint * new Vector4(0.2f, 0.22f, 0.28f, 1f));
    }

    private static void DrawVictoryMarker(IUIRenderer renderer, Vector2 center, float planetRadius)
    {
        var gold = new Vector4(1f, 0.85f, 0.25f, 1f);
        Vector2 markerPos = center + new Vector2(planetRadius * 0.55f, -planetRadius * 0.75f);
        renderer.DrawText("✓", markerPos, 20f, gold);
        DrawFilledCircle(renderer, markerPos + new Vector2(6f, 8f), 10f, gold * new Vector4(1f, 1f, 1f, 0.25f));
    }

    private static void DrawOrbitRing(
        IUIRenderer renderer, Vector2 center, float radius, Vector4 color,
        int segments = 48, float dotSize = 2f)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * MathF.Tau;
            float px = center.X + MathF.Cos(angle) * radius;
            float py = center.Y + MathF.Sin(angle) * radius;
            renderer.DrawRect(new Vector2(px, py), new Vector2(dotSize, dotSize), color);
        }
    }

    private static void DrawDashedOrbitRing(IUIRenderer renderer, Vector2 center, float radius, Vector4 color)
    {
        const int segments = 48;
        for (int i = 0; i < segments; i += 2)
        {
            float angle = i / (float)segments * MathF.Tau;
            float px = center.X + MathF.Cos(angle) * radius;
            float py = center.Y + MathF.Sin(angle) * radius;
            renderer.DrawRect(new Vector2(px, py), new Vector2(2f, 2f), color);
        }
    }

    private static void DrawLine(IUIRenderer renderer, Vector2 from, Vector2 to, Vector4 color, float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.Length;
        if (length < 1f)
            return;

        Vector2 direction = delta / length;
        Vector2 normal = new(-direction.Y, direction.X);
        int steps = (int)(length / 4f) + 1;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            renderer.DrawRect(
                point - normal * thickness * 0.5f,
                new Vector2(thickness, thickness),
                color);
        }
    }

    private static void DrawDashedLine(IUIRenderer renderer, Vector2 from, Vector2 to, Vector4 color, float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.Length;
        if (length < 1f)
            return;

        Vector2 direction = delta / length;
        Vector2 normal = new(-direction.Y, direction.X);
        int steps = (int)(length / 6f) + 1;

        for (int i = 0; i <= steps; i += 2)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            renderer.DrawRect(
                point - normal * thickness * 0.5f,
                new Vector2(thickness, thickness),
                color);
        }
    }

    internal static void DrawFilledCircle(IUIRenderer renderer, Vector2 center, float radius, Vector4 color)
    {
        int diameter = Math.Max(2, (int)(radius * 2f));
        float topLeftX = center.X - radius;
        float topLeftY = center.Y - radius;
        float radiusSq = radius * radius;

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    renderer.DrawRect(
                        new Vector2(topLeftX + x, topLeftY + y),
                        Vector2.One,
                        color);
                }
            }
        }
    }
}