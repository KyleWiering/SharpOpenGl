using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

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
    public Vector4 CargoColor { get; set; } = new Vector4(0.95f, 0.75f, 0.2f, 1f);
    public Vector4 BarBgColor { get; set; } = new Vector4(0.15f, 0.15f, 0.15f, 1f);
    public float FontSize { get; set; } = 18f;

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.4f, 0.55f, 0.75f, 1f));

        if (SelectedUnits.Count == 0)
        {
            UITextDrawing.DrawTextBlock(renderer, "Click a ship, node, or enemy to inspect",
                position + new Vector2(10f, 10f), FontSize,
                new Vector4(0.65f, 0.68f, 0.75f, 1f), size.X - 20f);
            return;
        }

        int count = Math.Min(SelectedUnits.Count, MaxDisplayed);
        float slotH = size.Y / count;
        float padding = 8f;
        float barH = 9f;
        float contentW = size.X - padding * 2f;

        for (int i = 0; i < count; i++)
        {
            UnitInfo unit = SelectedUnits[i];
            float slotY = position.Y + i * slotH;
            float textY = slotY + padding;
            float barY = textY + FontSize * 2f + 4f;
            float barW = size.X - padding * 2f;
            float barX = position.X + padding;

            Vector4 nameColor = GameplayEntityDisplay.LabelColor(unit.DisplayKind);
            float nameSize = UIFontMetrics.FitFontSize(unit.Name, FontSize + 2f, barW);
            string name = UITextDrawing.TruncateWithEllipsis(unit.Name, barW, nameSize);
            renderer.DrawText(name, new Vector2(barX, textY), nameSize, nameColor);

            float detailY = textY + nameSize * UITextDrawing.LineHeightFactor + 2f;
            if (!string.IsNullOrEmpty(unit.Subtitle))
            {
                UITextDrawing.DrawTextBlock(renderer, unit.Subtitle, new Vector2(barX, detailY),
                    FontSize - 1f, new Vector4(0.75f, 0.8f, 0.9f, 1f), barW);
                detailY += (FontSize - 1f) * UITextDrawing.LineHeightFactor + 2f;
            }

            if (unit.MaxHP > 0f)
            {
                string stats = $"HP {unit.CurrentHP:0}/{unit.MaxHP:0}";
                if (unit.MaxShields > 0f)
                    stats += $"   SH {unit.CurrentShields:0}/{unit.MaxShields:0}";
                if (unit.Armor > 0f)
                    stats += $"   AR {unit.Armor:0}";
                float statsSize = UIFontMetrics.FitFontSize(stats, FontSize - 2f, barW, 10f);
                UITextDrawing.DrawTextBlock(renderer, stats, new Vector2(barX, detailY),
                    statsSize, new Vector4(0.88f, 0.9f, 0.95f, 1f), barW, maxLines: 1);
                barY = detailY + statsSize * UITextDrawing.LineHeightFactor + 4f;
            }

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
                {
                    Vector4 shieldColor = unit.ShieldBarColor ?? ShieldColor;
                    renderer.DrawRect(new Vector2(barX, shieldY),
                        new Vector2(shieldFill, barH), shieldColor);
                }
                barY = shieldY + barH + 3f;
            }

            if (unit.CargoCapacity > 0f)
            {
                if (!string.IsNullOrEmpty(unit.HarvestMode))
                {
                    string cargoLabel =
                        $"Harvest: {unit.HarvestMode}   Cargo {unit.CargoAmount:0}/{unit.CargoCapacity:0}";
                    float labelY = barY + 2f;
                    float cargoSize = UIFontMetrics.FitFontSize(cargoLabel, FontSize - 3f, barW, 9f);
                    UITextDrawing.DrawTextBlock(renderer, cargoLabel, new Vector2(barX, labelY),
                        cargoSize, new Vector4(0.82f, 0.78f, 0.55f, 1f), barW, maxLines: 1);
                    barY = labelY + cargoSize * UITextDrawing.LineHeightFactor + 3f;
                }

                renderer.DrawRect(new Vector2(barX, barY), new Vector2(barW, barH), BarBgColor);
                float cargoFill = barW * Math.Clamp(unit.CargoFraction, 0f, 1f);
                if (cargoFill > 0f)
                    renderer.DrawRect(new Vector2(barX, barY), new Vector2(cargoFill, barH), CargoColor);
            }
        }

        if (SelectedUnits.Count > MaxDisplayed)
        {
            string more = $"+ {SelectedUnits.Count - MaxDisplayed} more";
            float moreSize = UIFontMetrics.FitFontSize(more, FontSize, contentW, 10f);
            more = UITextDrawing.TruncateWithEllipsis(more, contentW, moreSize);
            renderer.DrawText(more,
                new Vector2(position.X + padding, position.Y + size.Y - moreSize - padding),
                moreSize, new Vector4(0.6f, 0.65f, 0.72f, 1f));
        }
    }
}