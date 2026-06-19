using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Snapshot of stats shown for one selected unit in the info panel.
/// </summary>
public sealed class UnitInfo
{
    /// <summary>Display name (ship/unit class).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Current HP fraction 0–1.</summary>
    public float HPFraction { get; init; }

    /// <summary>Current shield fraction 0–1.</summary>
    public float ShieldFraction { get; init; }

    /// <summary>Current HP value (display only).</summary>
    public float CurrentHP { get; init; }

    /// <summary>Max HP.</summary>
    public float MaxHP { get; init; }

    /// <summary>Current shield value.</summary>
    public float CurrentShields { get; init; }

    /// <summary>Max shields.</summary>
    public float MaxShields { get; init; }

    /// <summary>Armour value.</summary>
    public float Armor { get; init; }

    /// <summary>Build a <see cref="UnitInfo"/> from a <see cref="HealthComponent"/> and entity name.</summary>
    public static UnitInfo FromHealth(string name, HealthComponent health) =>
        new()
        {
            Name = name,
            HPFraction = health.HPFraction,
            ShieldFraction = health.ShieldFraction,
            CurrentHP = health.CurrentHP,
            MaxHP = health.MaxHP,
            CurrentShields = health.CurrentShields,
            MaxShields = health.MaxShields,
            Armor = health.Armor,
        };
}

/// <summary>
/// Displays stats for the currently selected unit(s).
/// Bind <see cref="SelectedUnits"/> each frame.
/// </summary>
public sealed class UnitInfoPanel : Widget
{
    /// <summary>Snapshot data for selected units (up to <see cref="MaxDisplayed"/>).</summary>
    public IReadOnlyList<UnitInfo> SelectedUnits { get; set; } = Array.Empty<UnitInfo>();

    /// <summary>Maximum number of units displayed when multiple are selected.</summary>
    public int MaxDisplayed { get; set; } = 4;

    public Vector4 BackgroundColor { get; set; } = new Vector4(0.05f, 0.06f, 0.12f, 0.92f);
    public Vector4 HPColor { get; set; } = new Vector4(0.2f, 0.9f, 0.2f, 1f);
    public Vector4 ShieldColor { get; set; } = new Vector4(0.3f, 0.6f, 1.0f, 1f);
    public Vector4 BarBgColor { get; set; } = new Vector4(0.15f, 0.15f, 0.15f, 1f);
    public float FontSize { get; set; } = 18f;

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.4f, 0.55f, 0.75f, 1f));

        if (SelectedUnits.Count == 0)
        {
            renderer.DrawText("Click a ship or building to inspect",
                position + new Vector2(10f, 10f), FontSize,
                new Vector4(0.65f, 0.68f, 0.75f, 1f));
            return;
        }

        int count = Math.Min(SelectedUnits.Count, MaxDisplayed);
        float slotH = size.Y / count;
        float padding = 8f;
        float barH = 9f;

        for (int i = 0; i < count; i++)
        {
            UnitInfo unit = SelectedUnits[i];
            float slotY = position.Y + i * slotH;
            float textY = slotY + padding;
            float barY = textY + FontSize * 2f + 4f;
            float barW = size.X - padding * 2f;
            float barX = position.X + padding;

            renderer.DrawText(unit.Name, new Vector2(barX, textY), FontSize + 2f,
                new Vector4(0.55f, 0.85f, 1f, 1f));
            renderer.DrawText(
                $"HP {unit.CurrentHP:0}/{unit.MaxHP:0}   SH {unit.CurrentShields:0}/{unit.MaxShields:0}   AR {unit.Armor:0}",
                new Vector2(barX, textY + FontSize + 2f), FontSize - 1f,
                new Vector4(0.88f, 0.9f, 0.95f, 1f));

            renderer.DrawRect(new Vector2(barX, barY), new Vector2(barW, barH), BarBgColor);
            float hpFill = barW * Math.Clamp(unit.HPFraction, 0f, 1f);
            if (hpFill > 0f)
                renderer.DrawRect(new Vector2(barX, barY), new Vector2(hpFill, barH), HPColor);

            if (unit.MaxShields > 0f)
            {
                float shieldY = barY + barH + 3f;
                renderer.DrawRect(new Vector2(barX, shieldY), new Vector2(barW, barH), BarBgColor);
                float shieldFill = barW * Math.Clamp(unit.ShieldFraction, 0f, 1f);
                if (shieldFill > 0f)
                    renderer.DrawRect(new Vector2(barX, shieldY),
                        new Vector2(shieldFill, barH), ShieldColor);
            }
        }

        if (SelectedUnits.Count > MaxDisplayed)
        {
            string more = $"+ {SelectedUnits.Count - MaxDisplayed} more";
            renderer.DrawText(more,
                new Vector2(position.X + padding, position.Y + size.Y - FontSize - padding),
                FontSize, new Vector4(0.6f, 0.65f, 0.72f, 1f));
        }
    }
}