using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Data for a single buildable item displayed in the <see cref="BuildPanel"/>.
/// </summary>
public sealed class BuildableItem
{
    /// <summary>Entity definition ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Display name shown to the player.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Energy cost.</summary>
    public int EnergyCost { get; init; }

    /// <summary>Minerals cost.</summary>
    public int MineralsCost { get; init; }

    /// <summary>Data cost.</summary>
    public int DataCost { get; init; }

    /// <summary>Crew cost.</summary>
    public int CrewCost { get; init; }

    /// <summary>Build time in seconds.</summary>
    public float BuildTime { get; init; }

    /// <summary>Whether the player can afford this item.</summary>
    public bool CanAfford { get; set; } = true;
}

/// <summary>
/// Data for items currently in the build queue.
/// </summary>
public sealed class QueuedItem
{
    /// <summary>Display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Build progress fraction 0–1.</summary>
    public float Progress { get; init; }
}

/// <summary>
/// Shows buildable items for a selected building and the current production queue.
/// Raises <see cref="BuildRequested"/> when the player clicks a build button.
/// </summary>
public sealed class BuildPanel : Widget
{
    /// <summary>Buildable items available at the selected building.</summary>
    public IReadOnlyList<BuildableItem> AvailableItems { get; set; } = Array.Empty<BuildableItem>();

    /// <summary>Current production queue.</summary>
    public IReadOnlyList<QueuedItem> Queue { get; set; } = Array.Empty<QueuedItem>();

    /// <summary>Building type name shown at the top of the panel.</summary>
    public string BuildingName { get; set; } = string.Empty;

    /// <summary>Supply info: "Used / Cap".</summary>
    public string SupplyText { get; set; } = string.Empty;

    /// <summary>Fired when the player clicks to build an item. Argument is the definition ID.</summary>
    public event Action<string>? BuildRequested;

    /// <summary>Background colour.</summary>
    public Vector4 BackgroundColor { get; set; } = new(0.05f, 0.05f, 0.12f, 0.92f);

    /// <summary>Button colour for affordable items.</summary>
    public Vector4 ButtonColor { get; set; } = new(0.15f, 0.35f, 0.15f, 1f);

    /// <summary>Button colour for unaffordable items.</summary>
    public Vector4 DisabledColor { get; set; } = new(0.3f, 0.15f, 0.15f, 1f);

    private float FontSize { get; } = 12f;

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.5f, 0.4f, 0.2f, 1f));

        float y = position.Y + 6f;
        float x = position.X + 8f;

        // Title
        renderer.DrawText(BuildingName, new Vector2(x, y), FontSize + 2,
            new Vector4(0.9f, 0.8f, 0.4f, 1f));
        y += FontSize + 10f;

        // Supply
        if (!string.IsNullOrEmpty(SupplyText))
        {
            renderer.DrawText($"Supply: {SupplyText}", new Vector2(x, y), FontSize,
                new Vector4(0.7f, 0.7f, 0.9f, 1f));
            y += FontSize + 6f;
        }

        // Queue
        if (Queue.Count > 0)
        {
            renderer.DrawText("Building:", new Vector2(x, y), FontSize,
                new Vector4(0.8f, 0.8f, 0.8f, 1f));
            y += FontSize + 2f;
            foreach (var item in Queue)
            {
                float barW = size.X - 16f;
                renderer.DrawRect(new Vector2(x, y), new Vector2(barW, 10f),
                    new Vector4(0.15f, 0.15f, 0.15f, 1f));
                float fill = barW * Math.Clamp(item.Progress, 0f, 1f);
                if (fill > 0f)
                    renderer.DrawRect(new Vector2(x, y), new Vector2(fill, 10f),
                        new Vector4(0.3f, 0.8f, 0.3f, 1f));
                renderer.DrawText($"{item.Name} ({item.Progress * 100:0}%)",
                    new Vector2(x + 2f, y - 1f), FontSize - 2,
                    new Vector4(1f, 1f, 1f, 1f));
                y += 14f;
            }
            y += 4f;
        }

        // Available items as buttons
        float btnH = 28f;
        float btnW = size.X - 16f;
        for (int i = 0; i < AvailableItems.Count && y + btnH < position.Y + size.Y; i++)
        {
            var item = AvailableItems[i];
            var btnPos = new Vector2(x, y);
            var btnSize = new Vector2(btnW, btnH);
            var color = item.CanAfford ? ButtonColor : DisabledColor;

            renderer.DrawRect(btnPos, btnSize, color);
            renderer.DrawRectOutline(btnPos, btnSize, new Vector4(0.6f, 0.6f, 0.6f, 0.5f));

            string label = $"{item.Name}  E:{item.EnergyCost} M:{item.MineralsCost} " +
                           $"D:{item.DataCost} C:{item.CrewCost}  ({item.BuildTime:0}s)";
            renderer.DrawText(label, btnPos + new Vector2(4f, 6f), FontSize,
                item.CanAfford ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(0.6f, 0.4f, 0.4f, 1f));

            y += btnH + 4f;
        }
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible || button != 0) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);

        // Check if click is within this panel
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        Vector2 localPos = screenPoint - pos;

        // Determine which button was clicked
        float y = 6f + (FontSize + 10f); // title
        if (!string.IsNullOrEmpty(SupplyText)) y += FontSize + 6f;
        if (Queue.Count > 0) y += FontSize + 2f + Queue.Count * 14f + 4f;

        float btnH = 28f;
        for (int i = 0; i < AvailableItems.Count; i++)
        {
            if (localPos.Y >= y && localPos.Y < y + btnH)
            {
                var item = AvailableItems[i];
                if (item.CanAfford)
                    BuildRequested?.Invoke(item.Id);
                return true;
            }
            y += btnH + 4f;
        }

        return true; // consume click even if no button hit
    }
}
