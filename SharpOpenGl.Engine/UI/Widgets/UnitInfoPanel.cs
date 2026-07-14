using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Displays stats for the currently selected unit(s).
/// Bind <see cref="SelectedUnits"/> each frame.
/// </summary>
public sealed class UnitInfoPanel : Widget
{
    /// <summary>Left-column header glyph size per menu icon pattern spec.</summary>
    public const float HeaderIconSize = 32f;

    /// <summary>Compact header glyph when the panel height budget is tight (1024×768 + build map open).</summary>
    public const float CompactHeaderIconSize = 26f;

    private const float CompactLayoutHeightThreshold = 148f;

    private const float HeaderIconGap = 6f;
    private const float StatMicroIconSize = 15f;
    private const float StatIconGap = 4f;
    private const float StatSegmentGap = 10f;
    private const float EmptyStateIconSize = 24f;
    private const float EmptyStateIconGap = 8f;

    /// <summary>Snapshot data for selected units (up to <see cref="MaxDisplayed"/>).</summary>
    public IReadOnlyList<UnitInfo> SelectedUnits { get; set; } = Array.Empty<UnitInfo>();

    /// <summary>Maximum number of units displayed when multiple are selected.</summary>
    public int MaxDisplayed { get; set; } = 4;

    public Vector4 BackgroundColor { get; set; } = MenuTheme.PanelBackground;
    public Vector4 HPColor { get; set; } = new Vector4(0.2f, 0.9f, 0.2f, 1f);
    public Vector4 ShieldColor { get; set; } = new Vector4(0.3f, 0.6f, 1.0f, 1f);
    public Vector4 CargoColor { get; set; } = new Vector4(0.95f, 0.75f, 0.2f, 1f);
    public Vector4 BarBgColor { get; set; } = new Vector4(0.15f, 0.15f, 0.15f, 1f);
    public float FontSize { get; set; } = 18f;

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, MenuTheme.PanelBorder);

        bool compact = IsCompactLayout(size);
        float padding = compact ? 6f : 8f;
        float headerIconSize = compact ? CompactHeaderIconSize : HeaderIconSize;
        float contentW = size.X - padding * 2f;

        if (SelectedUnits.Count == 0)
        {
            DrawEmptyState(renderer, position, size, padding, contentW);
            return;
        }

        float footerReserve = 0f;
        if (SelectedUnits.Count > MaxDisplayed)
        {
            string morePreview = $"+ {SelectedUnits.Count - MaxDisplayed} more";
            float moreSize = UIFontMetrics.FitFontSize(morePreview, FontSize, contentW, 10f);
            footerReserve = moreSize * UITextDrawing.LineHeightFactor + padding * 2f;
        }

        int count = Math.Min(SelectedUnits.Count, MaxDisplayed);
        float slotH = (size.Y - footerReserve) / count;
        float barH = compact ? 7f : 9f;
        bool tightSlots = count >= MaxDisplayed || slotH < 48f;

        for (int i = 0; i < count; i++)
        {
            UnitInfo unit = SelectedUnits[i];
            float slotTop = position.Y + i * slotH;
            float slotBottom = slotTop + slotH;
            float barX = position.X + padding;
            float barW = contentW;
            float textColumnX = barX + headerIconSize + HeaderIconGap;
            float textColumnW = Math.Max(0f, barW - headerIconSize - HeaderIconGap);
            float barY = slotBottom - padding;

            if (unit.CargoCapacity > 0f)
            {
                barY -= barH;
                renderer.DrawRect(new Vector2(barX, barY), new Vector2(barW, barH), BarBgColor);
                float cargoFill = barW * Math.Clamp(unit.CargoFraction, 0f, 1f);
                if (cargoFill > 0f)
                    renderer.DrawRect(new Vector2(barX, barY), new Vector2(cargoFill, barH), CargoColor);

                if (!string.IsNullOrEmpty(unit.HarvestMode))
                {
                    float cargoSize;
                    float cargoLabelHeight;
                    if (tightSlots)
                    {
                        string cargoLabel =
                            $"Harvest: {unit.HarvestMode}   Cargo {unit.CargoAmount:0}/{unit.CargoCapacity:0}";
                        cargoSize = UIFontMetrics.FitFontSize(cargoLabel, FontSize - 3f, textColumnW, 9f);
                        cargoLabelHeight = cargoSize * UITextDrawing.LineHeightFactor;
                        float labelY = barY - cargoLabelHeight - 2f;
                        UITextDrawing.DrawTextBlock(renderer, cargoLabel, new Vector2(textColumnX, labelY),
                            cargoSize, new Vector4(0.82f, 0.78f, 0.55f, 1f), textColumnW, maxLines: 1);
                    }
                    else
                    {
                        cargoSize = FontSize - 3f;
                        cargoLabelHeight = DrawCargoHarvestRow(renderer, unit, new Vector2(textColumnX, 0f),
                            textColumnW, cargoSize, barY - 2f);
                    }

                    barY -= cargoLabelHeight + 2f;
                }
            }

            if (unit.MaxShields > 0f)
            {
                barY -= barH + 3f;
                renderer.DrawRect(new Vector2(barX, barY), new Vector2(barW, barH), BarBgColor);
                float shieldFill = barW * Math.Clamp(unit.ShieldFraction, 0f, 1f);
                if (shieldFill > 0f)
                {
                    Vector4 shieldColor = unit.ShieldBarColor ?? ShieldColor;
                    renderer.DrawRect(new Vector2(barX, barY),
                        new Vector2(shieldFill, barH), shieldColor);
                }
            }

            if (unit.MaxHP > 0f)
            {
                barY -= barH + 4f;
                renderer.DrawRect(new Vector2(barX, barY), new Vector2(barW, barH), BarBgColor);
                float hpFill = barW * Math.Clamp(unit.HPFraction, 0f, 1f);
                if (hpFill > 0f)
                {
                    Vector4 hpColor = ResolveHpBarColor(unit.HpBarPulse);
                    float pulseBoost = unit.HpBarPulse * 0.06f;
                    float drawFill = Math.Min(barW, hpFill * (1f + pulseBoost));
                    if (unit.HpBarPulse > 0.25f)
                    {
                        float glowH = barH + unit.HpBarPulse * 2f;
                        float glowY = barY - (glowH - barH) * 0.5f;
                        var glowColor = hpColor with { W = unit.HpBarPulse * 0.35f };
                        renderer.DrawRect(new Vector2(barX, glowY), new Vector2(drawFill, glowH), glowColor);
                    }

                    renderer.DrawRect(new Vector2(barX, barY), new Vector2(drawFill, barH), hpColor);
                }
            }

            float textBottom = barY - 4f;
            float textY = slotTop + padding;

            Vector4 nameColor = GameplayEntityDisplay.LabelColor(unit.DisplayKind);
            float nameSize = UIFontMetrics.FitFontSize(unit.Name, FontSize + 2f, textColumnW);
            string name = UITextDrawing.TruncateWithEllipsis(unit.Name, textColumnW, nameSize);
            DrawHeaderGlyph(renderer, unit, new Vector2(barX, textY), headerIconSize);
            renderer.DrawText(name, new Vector2(textColumnX, textY), nameSize, nameColor);
            textY += nameSize * UITextDrawing.LineHeightFactor + 2f;

            List<StatSegment> statSegments = BuildPrimaryStatSegments(unit);
            float statsSize = statSegments.Count > 0
                ? FitStatRowFontSize(statSegments, FontSize - 2f, textColumnW, 10f)
                : 0f;
            float statsHeight = statsSize > 0f
                ? statsSize * UITextDrawing.LineHeightFactor + 2f
                : 0f;

            float subtitleBudget = Math.Max(0f, textBottom - textY - statsHeight);
            if (!string.IsNullOrEmpty(unit.Subtitle) && subtitleBudget > 0f)
            {
                float subtitleSize = FontSize - 1f;
                int subtitleMaxLines = UITextDrawing.MaxLinesFromHeight(subtitleBudget, subtitleSize);
                UITextDrawing.DrawTextBlock(renderer, unit.Subtitle, new Vector2(textColumnX, textY),
                    subtitleSize, new Vector4(0.75f, 0.8f, 0.9f, 1f), textColumnW, subtitleMaxLines, subtitleBudget);
                textY += Math.Min(
                    UITextDrawing.MeasureTextBlockHeight(unit.Subtitle, subtitleSize, textColumnW),
                    subtitleBudget) + 2f;
            }

            if (statSegments.Count > 0)
            {
                Vector4 textColor = new(0.88f, 0.9f, 0.95f, 1f);
                DrawStatSegments(renderer, statSegments, new Vector2(textColumnX, textY), textColumnW, statsSize, textColor);
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

    private void DrawEmptyState(IUIRenderer renderer, Vector2 position, Vector2 size, float padding, float contentW)
    {
        const string prompt = "Select a unit to inspect";
        float textY = position.Y + padding + 2f;
        Vector2 iconPos = position + new Vector2(padding, textY);
        MenuIconDrawing.DrawEntityKind(renderer, EntityDisplayKind.Scenery, iconPos, EmptyStateIconSize);

        float textX = iconPos.X + EmptyStateIconSize + EmptyStateIconGap;
        float textW = Math.Max(0f, contentW - EmptyStateIconSize - EmptyStateIconGap);
        float promptSize = UIFontMetrics.FitFontSize(prompt, FontSize, textW, 12f);
        UITextDrawing.DrawTextBlock(renderer, prompt, new Vector2(textX, textY), promptSize,
            new Vector4(0.65f, 0.68f, 0.75f, 1f), textW, maxLines: 1);
    }

    private static void DrawHeaderGlyph(IUIRenderer renderer, UnitInfo unit, Vector2 position, float size)
    {
        if (unit.HeaderIcon is { } headerIcon)
        {
            var (primary, accent) = HeaderIconTints(unit, headerIcon);
            MenuIconDrawing.Draw(renderer, headerIcon, position, size, primary, accent);
            return;
        }

        if (unit.Role is { } role)
        {
            MenuIconDrawing.DrawShipRole(renderer, role, position, size);
            return;
        }

        MenuIconDrawing.DrawEntityKind(renderer, unit.DisplayKind, position, size);
    }

    private static (Vector4 Primary, Vector4 Accent) HeaderIconTints(UnitInfo unit, MenuIconKind icon)
    {
        if (icon is MenuIconKind.HullMilitary or MenuIconKind.HullEngineering or MenuIconKind.HullPolitical
            && unit.Role is { } role)
        {
            return role switch
            {
                ShipRole.Military => (new Vector4(0.62f, 0.14f, 0.14f, 0.95f), new Vector4(0.98f, 0.88f, 0.88f, 1f)),
                ShipRole.Engineering => (new Vector4(0.62f, 0.42f, 0.08f, 0.95f), new Vector4(0.98f, 0.92f, 0.72f, 1f)),
                ShipRole.Political => (new Vector4(0.62f, 0.50f, 0.10f, 0.95f), new Vector4(1f, 0.94f, 0.55f, 1f)),
                _ => (new Vector4(0.3f, 0.3f, 0.3f, 0.95f), new Vector4(1f, 1f, 1f, 1f)),
            };
        }

        Vector4 label = GameplayEntityDisplay.LabelColor(unit.DisplayKind);
        return (Darken(label, 0.72f), label);
    }

    private float DrawCargoHarvestRow(
        IUIRenderer renderer, UnitInfo unit, Vector2 position, float maxWidth,
        float preferredSize, float baselineY)
    {
        var segments = new List<StatSegment>(2);
        if (!string.IsNullOrEmpty(unit.HarvestMode))
            segments.Add(new StatSegment(MenuIconKind.StatHarvest, unit.HarvestMode));
        segments.Add(new StatSegment(MenuIconKind.StatCargo,
            $"{unit.CargoAmount:0}/{unit.CargoCapacity:0}"));

        float size = FitStatRowFontSize(segments, preferredSize, maxWidth, 9f);
        float rowHeight = size * UITextDrawing.LineHeightFactor;
        float rowY = baselineY - rowHeight;
        Vector4 textColor = new(0.82f, 0.78f, 0.55f, 1f);
        DrawStatSegments(renderer, segments, new Vector2(position.X, rowY), maxWidth, size, textColor);
        return rowHeight;
    }

    private void DrawStatSegments(
        IUIRenderer renderer, IReadOnlyList<StatSegment> segments,
        Vector2 position, float maxWidth, float fontSize, Vector4 textColor)
    {
        float lineHeight = fontSize * UITextDrawing.LineHeightFactor;
        float iconY = position.Y + Math.Max(0f, (lineHeight - StatMicroIconSize) * 0.5f);
        float x = position.X;

        foreach (StatSegment segment in segments)
        {
            if (x > position.X + 0.5f)
                x += StatSegmentGap;

            var (primary, accent) = StatIconTints(segment.Icon);
            MenuIconDrawing.Draw(renderer, segment.Icon, new Vector2(x, iconY), StatMicroIconSize, primary, accent);
            x += StatMicroIconSize + StatIconGap;

            renderer.DrawText(segment.Text, new Vector2(x, position.Y), fontSize, textColor);
            x += UIFontMetrics.MeasureTextWidth(segment.Text, fontSize);
        }
    }

    private static List<StatSegment> BuildPrimaryStatSegments(UnitInfo unit)
    {
        var segments = new List<StatSegment>(3);
        if (unit.MaxHP > 0f)
            segments.Add(new StatSegment(MenuIconKind.StatHP, $"{unit.CurrentHP:0}/{unit.MaxHP:0}"));
        if (unit.MaxShields > 0f)
            segments.Add(new StatSegment(MenuIconKind.StatShield, $"{unit.CurrentShields:0}/{unit.MaxShields:0}"));
        if (unit.Armor > 0f)
            segments.Add(new StatSegment(MenuIconKind.StatArmor, $"{unit.Armor:0}"));
        return segments;
    }

    private static float FitStatRowFontSize(
        IReadOnlyList<StatSegment> segments, float preferredSize, float maxWidth, float minSize)
    {
        float size = preferredSize;
        while (size > minSize && MeasureStatRowWidth(segments, size) > maxWidth)
            size -= 0.5f;
        return size;
    }

    private static float MeasureStatRowWidth(IReadOnlyList<StatSegment> segments, float fontSize)
    {
        if (segments.Count == 0)
            return 0f;

        float width = 0f;
        for (int i = 0; i < segments.Count; i++)
        {
            if (i > 0)
                width += StatSegmentGap;
            width += StatMicroIconSize + StatIconGap;
            width += UIFontMetrics.MeasureTextWidth(segments[i].Text, fontSize);
        }

        return width;
    }

    private (Vector4 Primary, Vector4 Accent) StatIconTints(MenuIconKind icon) => icon switch
    {
        MenuIconKind.StatHP => (Darken(HPColor, 0.55f), HPColor),
        MenuIconKind.StatShield => (Darken(ShieldColor, 0.55f), ShieldColor),
        MenuIconKind.StatArmor => (new Vector4(0.45f, 0.48f, 0.55f, 1f), new Vector4(0.82f, 0.86f, 0.92f, 1f)),
        MenuIconKind.StatCargo => (Darken(CargoColor, 0.55f), CargoColor),
        MenuIconKind.StatHarvest => (new Vector4(0.50f, 0.46f, 0.30f, 1f), new Vector4(0.92f, 0.86f, 0.62f, 1f)),
        _ => (new Vector4(0.35f, 0.38f, 0.45f, 1f), new Vector4(0.88f, 0.9f, 0.95f, 1f)),
    };

    private static Vector4 Darken(Vector4 color, float factor) =>
        new(color.X * factor, color.Y * factor, color.Z * factor, color.W);

    private Vector4 ResolveHpBarColor(float pulse)
    {
        if (pulse <= 0f) return HPColor;

        float boost = pulse * 0.55f;
        return new Vector4(
            MathF.Min(1f, HPColor.X + boost),
            MathF.Min(1f, HPColor.Y + boost * 0.35f),
            MathF.Max(0f, HPColor.Z - boost * 0.25f),
            1f);
    }

    internal static bool IsCompactLayout(Vector2 size) =>
        size.Y <= CompactLayoutHeightThreshold;

    private readonly record struct StatSegment(MenuIconKind Icon, string Text);
}