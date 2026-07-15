using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UnitInfoPanelShieldTests
{
    private const float PanelPadding = 8f;
    private const float HeaderIconGap = 6f;
    private const float StatMicroIconSize = 15f;
    private const float EmptyStateIconSize = 24f;
    private const int MinimumHeaderIconRectCount = 3;
    private const int MinimumStatIconRectCount = 3;
    [Fact]
    public void UnitInfo_hides_shield_bar_color_when_no_shields()
    {
        RaceShieldSchema.ResetForTests();
        var health = new HealthComponent
        {
            MaxHP = 100f, CurrentHP = 100f,
            MaxShields = 0f, CurrentShields = 0f,
        };

        var info = UnitInfo.FromHealth("Fighter", health, EntityDisplayKind.Friendly, "terran");
        Assert.Equal(0f, info.MaxShields);
        Assert.Null(info.ShieldBarColor);
    }

    [Fact]
    public void UnitInfo_uses_race_tint_for_shielded_units()
    {
        RaceShieldSchema.ResetForTests();
        var health = new HealthComponent
        {
            MaxHP = 100f, CurrentHP = 100f,
            MaxShields = 50f, CurrentShields = 50f,
        };

        var solari = UnitInfo.FromHealth("Gunship", health, EntityDisplayKind.Friendly, "solari");
        Assert.NotNull(solari.ShieldBarColor);
        Assert.Equal(RaceShieldSchema.ResolveShieldTint("solari"), solari.ShieldBarColor!.Value);
    }

    [Fact]
    public void UnitInfoPanel_skips_shield_bar_when_max_shields_zero()
    {
        var panel = new UnitInfoPanel
        {
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Korath Raider",
                    MaxHP = 200f,
                    CurrentHP = 200f,
                    MaxShields = 0f,
                    HPFraction = 1f,
                },
            ],
        };

        Assert.Equal(0f, panel.SelectedUnits[0].MaxShields);
    }

    [Fact]
    public void UnitInfoPanel_selected_unit_draws_header_icon()
    {
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(320f, 140f),
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Gunship",
                    MaxHP = 100f,
                    CurrentHP = 80f,
                    MaxShields = 50f,
                    CurrentShields = 40f,
                    HPFraction = 0.8f,
                    ShieldFraction = 0.8f,
                    DisplayKind = EntityDisplayKind.Friendly,
                },
            ],
        };

        var renderer = new RectRecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, panel.Size);

        int headerIconRects = renderer.Rects.Count(rect =>
            IsInHeaderIconColumn(rect.Position) && rect.Size.X <= UnitInfoPanel.HeaderIconSize);
        Assert.True(headerIconRects >= MinimumHeaderIconRectCount,
            $"Expected header icon column rects, found {headerIconRects}.");
        Assert.Contains(renderer.Texts, text => text.Text == "Gunship");
    }

    [Fact]
    public void UnitInfoPanel_empty_state_shows_inspect_glyph()
    {
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(320f, 120f),
            SelectedUnits = [],
        };

        var renderer = new RectRecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, panel.Size);

        int emptyIconRects = renderer.Rects.Count(rect =>
            IsInEmptyStateIconColumn(rect.Position) && rect.Size.X <= EmptyStateIconSize);
        Assert.True(emptyIconRects >= MinimumHeaderIconRectCount,
            $"Expected empty-state inspect glyph rects, found {emptyIconRects}.");
        Assert.Contains(renderer.Texts, text => text.Text.Contains("Select a unit to inspect", StringComparison.Ordinal));
    }

    [Fact]
    public void UnitInfoPanel_hp_bar_pulse_draws_brightened_fill()
    {
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(560f, 170f),
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Cruiser",
                    MaxHP = 1000f,
                    CurrentHP = 600f,
                    HPFraction = 0.6f,
                    HpBarPulse = 0.85f,
                    DisplayKind = EntityDisplayKind.Friendly,
                },
            ],
        };

        var renderer = new RectRecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, panel.Size);

        Assert.Contains(renderer.Rects, r =>
            r.Color.X > panel.HPColor.X && r.Size.Y >= 7f);
    }

    [Fact]
    public void UnitInfoPanel_stat_row_uses_micro_glyph_layout()
    {
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(320f, 140f),
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Battlecruiser",
                    MaxHP = 5000f,
                    CurrentHP = 4200f,
                    MaxShields = 2000f,
                    CurrentShields = 1800f,
                    Armor = 12f,
                    HPFraction = 0.84f,
                    ShieldFraction = 0.9f,
                    DisplayKind = EntityDisplayKind.Friendly,
                },
            ],
        };

        var renderer = new RectRecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, panel.Size);

        float headerIcon = UnitInfoPanel.IsCompactLayout(panel.Size)
            ? UnitInfoPanel.CompactHeaderIconSize
            : UnitInfoPanel.HeaderIconSize;
        float statColumnX = PanelPadding + headerIcon + HeaderIconGap;
        int statIconRects = renderer.Rects.Count(rect =>
            rect.Position.X >= statColumnX - 0.5f
            && rect.Position.X <= statColumnX + StatMicroIconSize + 0.5f
            && rect.Size.X <= StatMicroIconSize + 1f
            && rect.Size.Y <= StatMicroIconSize + 1f);
        Assert.True(statIconRects >= MinimumStatIconRectCount,
            $"Expected HP/SH/AR micro-glyphs, found {statIconRects} icon-slot rects.");
        Assert.Contains(renderer.Texts, text => text.Text == "4200/5000");
        Assert.Contains(renderer.Texts, text => text.Text == "1800/2000");
        Assert.Contains(renderer.Texts, text => text.Text == "12");
    }

    private static bool IsInHeaderIconColumn(Vector2 position) =>
        position.X >= PanelPadding - 0.5f
        && position.X <= PanelPadding + UnitInfoPanel.HeaderIconSize + 0.5f;

    private static bool IsInEmptyStateIconColumn(Vector2 position) =>
        position.X >= PanelPadding - 0.5f
        && position.X <= PanelPadding + EmptyStateIconSize + 0.5f;

    private sealed class RectRecordingRenderer : IUIRenderer
    {
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();
        public List<(string Text, Vector2 Position, float FontSize, Vector4 Color)> Texts { get; } = new();

        public Vector2 ViewportSize => new(320f, 140f);

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            Texts.Add((text, position, fontSize, color));
    }
}