using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Interactive galactic star map: hyperlanes, planet nodes, hover glow, and click handling.
/// </summary>
public sealed class StarMapCanvas : Widget
{
    private const float PlanetVisualRadius = 22f;
    private const float DoubleClickSeconds = 0.35f;

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
        IReadOnlyList<StarMapHyperlane> lanes = StarMapLogic.BuildHyperlanes(_nodes);
        var laneColor = new Vector4(0.35f, 0.55f, 0.95f, 0.45f);

        foreach (StarMapHyperlane lane in lanes)
        {
            Vector2 from = StarMapLogic.ToPlanetCenter(lane.From, position, size);
            Vector2 to = StarMapLogic.ToPlanetCenter(lane.To, position, size);
            DrawLine(renderer, from, to, laneColor, 2f);
        }
    }

    private void DrawPlanets(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        foreach (StarMapNode node in _nodes)
        {
            Vector2 center = StarMapLogic.ToPlanetCenter(node.Position, position, size);
            bool isHovered = node.Id == _hoveredId;
            bool isSelected = node.Id == _selectedId;
            float alphaScale = node.IsUnlocked ? 1f : 0.35f;

            if (isHovered && node.IsUnlocked)
            {
                float pulse = 0.55f + 0.45f * MathF.Sin(_time * 5f);
                var glow = node.PlanetColor * new Vector4(1f, 1f, 1f, 0.22f * pulse);
                DrawFilledCircle(renderer, center, PlanetVisualRadius + 10f + pulse * 8f, glow);
            }
            else if (isSelected && node.IsUnlocked)
            {
                var selectionGlow = node.PlanetColor * new Vector4(1f, 1f, 1f, 0.3f);
                DrawFilledCircle(renderer, center, PlanetVisualRadius + 12f, selectionGlow);
            }

            if (node.IsUnlocked)
            {
                DrawOrbitRing(renderer, center, PlanetVisualRadius + 16f, node.PlanetColor * new Vector4(1f, 1f, 1f, 0.35f));
            }

            var bodyColor = node.PlanetColor * new Vector4(1f, 1f, 1f, alphaScale);
            DrawFilledCircle(renderer, center, PlanetVisualRadius, bodyColor);
            DrawFilledCircle(renderer, center, PlanetVisualRadius * 0.55f, bodyColor * new Vector4(1.15f, 1.15f, 1.15f, alphaScale));

            if (node.IsCompleted)
            {
                DrawVictoryMarker(renderer, center, PlanetVisualRadius);
            }

            string label = node.PlanetName;
            if (!node.IsUnlocked)
                label += " [LOCKED]";

            renderer.DrawText(
                label,
                new Vector2(center.X - 80f, center.Y + PlanetVisualRadius + 10f),
                14f,
                (node.IsUnlocked ? MenuTheme.BodyTextColor : MenuTheme.MutedTextColor) * new Vector4(1f, 1f, 1f, alphaScale));
        }
    }

    private static void DrawVictoryMarker(IUIRenderer renderer, Vector2 center, float planetRadius)
    {
        var gold = new Vector4(1f, 0.85f, 0.25f, 1f);
        Vector2 markerPos = center + new Vector2(planetRadius * 0.55f, -planetRadius * 0.75f);
        renderer.DrawText("✓", markerPos, 20f, gold);
        DrawFilledCircle(renderer, markerPos + new Vector2(6f, 8f), 10f, gold * new Vector4(1f, 1f, 1f, 0.25f));
    }

    private static void DrawOrbitRing(IUIRenderer renderer, Vector2 center, float radius, Vector4 color)
    {
        const int segments = 48;
        for (int i = 0; i < segments; i++)
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