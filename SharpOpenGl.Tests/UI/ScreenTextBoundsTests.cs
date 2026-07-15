using System.Reflection;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ScreenTextBoundsTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 CompactViewport = new(1024f, 768f);

    private const float UnitInfoPanelWidth = 560f;
    private const float UnitInfoHeaderIconReserve = UnitInfoPanel.HeaderIconSize + 6f;
    private const float UnitInfoContentWidth = UnitInfoPanelWidth - 16f - UnitInfoHeaderIconReserve;

    private const float SaveSlotButtonInner = 420f - Button.TextPadding;
    private const float LoadEntryButtonInner = 700f - Button.TextPadding;

    private const float ShipDesignerPickerWrap = 224f - 8f;
    private const float ShipDesignerPreviewWrap = 720f - 8f;
    private const float ShipDesignerControlButtonInner = 360f - Button.TextPadding;

    private const float BriefingPanelTop = 120f + 72f;
    private const float BriefingPanelBottom = 120f + 500f;

    private const float MainMenuTitleWidth = 900f;
    private const float MainMenuButtonInner = 400f - 20f - IconButton.TitleNavIconColumnWidth;
    private const float MainMenuSubtitleWrap = 900f - 8f;

    private const float SandboxSeedFieldInner = 520f - 24f;
    private const float SandboxStartButtonInner = 340f - 20f;
    private const float SandboxRandomizeButtonInner = 180f - 20f;

    private const float MissionPreviewWrap = 500f - 40f;
    private const float MissionHeadingWidth = 900f;
    private const float MissionStartButtonInner = 340f - 20f - IconButton.TitleNavIconColumnWidth;
    private const float MissionBackButtonInner = 220f - 20f - IconButton.TitleNavIconColumnWidth;

    private const float MpMapLabelWrap = 640f - 20f;
    private const float MpRaceLabelWrap = 300f - 20f;
    private const float MpValidationWrap = 900f - 20f;
    private const float MpStartButtonInner = 340f - 20f;
    private const float MpKindButtonInner = 150f - 20f;

    private const float BriefingWrap = 1920f - 160f - 32f;
    private const float PauseButtonInner = 280f - 20f;
    private const float SettingsButtonInner = 400f - 20f;

    [Fact]
    public void MainMenuScreen_text_fits_label_and_button_bounds_at_1920x1080()
    {
        var screen = new MainMenuScreen(hasSave: true);
        var inner = new RecordingRenderer(ReferenceViewport);

        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.FontSize >= 50f
                ? MainMenuTitleWidth
                : draw.FontSize > 20f
                    ? MainMenuSubtitleWrap
                    : MainMenuButtonInner;
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Fact]
    public void MainMenuScreen_nav_buttons_reserve_icon_column()
    {
        var screen = new MainMenuScreen(hasSave: true);
        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        var navLabels = new HashSet<string>(StringComparer.Ordinal)
        {
            "New Game", "Sandbox", "Multiplayer", "Continue", "Load Game", "Ship Designer", "Settings", "Quit",
        };

        foreach (var draw in inner.TextDraws.Where(d => navLabels.Contains(d.Text)))
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= MainMenuButtonInner + 1f, $"[{draw.Text}] width {width} > {MainMenuButtonInner}");
        }

        foreach (IconButton button in screen.NavButtons)
        {
            var (buttonPos, buttonSize) = button.Resolve(Vector2.Zero, ReferenceViewport);
            float iconColumnRight = buttonPos.X + IconButton.TitleNavIconColumnWidth;
            float labelLeft = buttonPos.X + IconButton.TitleNavIconColumnWidth;

            int iconColumnRects = inner.RectDraws.Count(rect =>
                rect.Position.X >= buttonPos.X - 1f
                && rect.Position.X + rect.Size.X <= iconColumnRight + 1f
                && rect.Position.Y >= buttonPos.Y - 1f
                && rect.Position.Y + rect.Size.Y <= buttonPos.Y + buttonSize.Y + 1f);

            Assert.True(iconColumnRects >= 1, $"[{button.Label}] should draw nav glyph rects in icon column.");

            var labelDraw = inner.TextDraws.FirstOrDefault(d => d.Text == button.Label);
            if (labelDraw.Text != null)
                Assert.True(labelDraw.Position.X >= labelLeft - 1f, $"{button.Label} label should start after icon column.");
        }
    }

    [Fact]
    public void MainMenuScreen_subtitle_has_contrast_scrim_on_starfield()
    {
        var screen = new MainMenuScreen();
        var scrim = FindWidget<Panel>(screen, "SubtitleScrim");
        var subtitle = FindWidget<Label>(screen, "Subtitle");

        Assert.NotNull(scrim);
        Assert.NotNull(subtitle);
        Assert.True(scrim!.BackgroundColor.W >= 0.5f, "Subtitle scrim should be semi-opaque over starfield.");
        Assert.Equal(MenuTheme.SubtitleScrimColor, scrim.BackgroundColor);
        Assert.True(subtitle!.TextColor.X >= 0.8f, "Subtitle text should be high-contrast on starfield.");
        Assert.Equal(MenuTheme.SubtitleColor, subtitle.TextColor);
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    [InlineData(2560f, 1440f)]
    public void MainMenuScreen_text_fits_at_reference_viewports(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var inner = new RecordingRenderer(viewport);
        var screen = new MainMenuScreen(hasSave: true);
        ScaledUIRenderer? scaledRenderer = null;

        if (viewport == ReferenceViewport)
            screen.Draw(inner);
        else
        {
            scaledRenderer = new ScaledUIRenderer(inner, new UIScaler(viewport));
            screen.Draw(scaledRenderer);
        }

        AssertScreenTextWithinViewport(inner.TextDraws, viewport);

        float scale = scaledRenderer?.ScaleToPhysical(1f) ?? 1f;
        float titleFontThreshold = 50f * scale;
        float subtitleFontThreshold = 20f * scale;
        float subtitleWrapPhysical = (scaledRenderer?.ScaleToPhysical(MainMenuSubtitleWrap) ?? MainMenuSubtitleWrap);

        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            if (draw.FontSize >= titleFontThreshold)
            {
                float physicalMax = scaledRenderer?.ScaleToPhysical(MainMenuTitleWidth) ?? MainMenuTitleWidth;
                Assert.True(width <= physicalMax + 1f, $"[{draw.Text}] width {width} > {physicalMax}");
            }
            else if (draw.FontSize > subtitleFontThreshold)
            {
                Assert.True(width <= subtitleWrapPhysical + 1f, $"[{draw.Text}] width {width} > {subtitleWrapPhysical}");
            }
            else
            {
                float physicalMax = scaledRenderer?.ScaleToPhysical(MainMenuButtonInner) ?? MainMenuButtonInner;
                Assert.True(width <= physicalMax + 1f, $"[{draw.Text}] width {width} > {physicalMax}");
            }
        }
    }

    [Fact]
    public void MainMenuScreen_long_subtitle_wraps_within_panel_width()
    {
        var screen = new MainMenuScreen();
        var subtitle = FindWidget<Label>(screen, "Subtitle");
        subtitle!.Text =
            "Command the void across multiple sectors with extended fleet operations and strategic deployment";
        subtitle.WrapWidth = UITextDrawing.ContentWrapWidth(900f, 4f);

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        Assert.All(inner.TextDraws.Where(d => d.FontSize < 30f), draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= subtitle.WrapWidth + 1f, draw.Text);
        });
    }

    [Fact]
    public void MissionSelectScreen_long_mission_preview_text_fits_panel_bounds()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry
            {
                Id = "long_mission",
                Title = "Operation Vanguard: Deep Space Reconnaissance and Extended Territorial Control",
                Description = "A very long summary that should wrap inside the preview panel without overflowing the mission select layout.",
                MapId = "sector_alpha_extended_operations_zone",
                BriefingText =
                    "Commander, hostile forces have established fortified positions across the outer rim. " +
                    "Your orders are to neutralize their command infrastructure and secure all relay stations " +
                    "before the enemy fleet completes its warp assembly sequence.",
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim",
                    "Protect the civilian evacuation corridor for at least fifteen minutes under continuous fire",
                ],
                PlanetName = "Helios Prime Extended Colony Zone Alpha Seven",
                StarMapPosition = new Vector2(0.35f, 0.45f),
            },
        ]);

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.FontSize switch
            {
                >= 30f => MissionHeadingWidth,
                >= 18f when draw.Text is "Start Mission" => MissionStartButtonInner,
                >= 18f when draw.Text is "Back" => MissionBackButtonInner,
                >= 16f => MissionPreviewWrap,
                _ => MissionPreviewWrap,
            };
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Theory]
    [InlineData(1920f, 1080f)]
    [InlineData(1024f, 768f)]
    public void MainMenuScreen_eight_buttons_fit_reference_and_compact_viewports(float viewportX, float viewportY)
    {
        var viewport = new Vector2(viewportX, viewportY);
        var screen = new MainMenuScreen(hasSave: true);
        var inner = new RecordingRenderer(viewport);

        if (viewport == CompactViewport)
        {
            var renderer = new ScaledUIRenderer(inner, new UIScaler(viewport));
            screen.Draw(renderer);
            AssertNoHorizontalViewportBleed(inner.TextDraws, viewport);
        }
        else
        {
            screen.Draw(inner);
            AssertScreenTextWithinViewport(inner.TextDraws, viewport);
        }

        Assert.NotNull(screen.FindButton("Sandbox"));
    }

    [Fact]
    public void SandboxSetupScreen_labels_and_buttons_fit_bounds()
    {
        var screen = new SandboxSetupScreen();
        screen.SeedField.Value = "frontier-alpha-42";

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.Text switch
            {
                "Start Sandbox" => SandboxStartButtonInner,
                "Back" => 200f - Button.TextPadding,
                "Randomize" => SandboxRandomizeButtonInner,
                "World seed" => 520f,
                _ when draw.FontSize >= 30f => 900f,
                _ when draw.FontSize >= 18f && draw.Text.StartsWith("Same seed", StringComparison.Ordinal) => 900f,
                _ when draw.FontSize >= 18f => SandboxSeedFieldInner,
                _ => SandboxSeedFieldInner,
            };
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Fact]
    public void MultiplayerSetupScreen_labels_and_buttons_fit_bounds_with_stress_data()
    {
        var screen = new MultiplayerSetupScreen();

        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot);
            screen.CycleSlotKind(slot);
        }

        while (screen.GetSelectedMapId() != "duel_frontier")
            screen.CycleMap(1);

        for (int raceStep = 0; raceStep < 12; raceStep++)
            screen.CycleSlotRace(0, 1);

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = ResolveMultiplayerMaxWidth(draw);
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Fact]
    public void MultiplayerSetupScreen_compact_viewport_avoids_horizontal_bleed_under_stress()
    {
        var screen = new MultiplayerSetupScreen();
        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot);
            screen.CycleSlotKind(slot);
        }

        var inner = new RecordingRenderer(CompactViewport);
        var renderer = new ScaledUIRenderer(inner, new UIScaler(CompactViewport));
        screen.Draw(renderer);

        AssertNoHorizontalViewportBleed(inner.TextDraws, CompactViewport);
    }

    [Fact]
    public void MultiplayerSetupScreen_validation_message_wraps_when_map_too_small()
    {
        var screen = new MultiplayerSetupScreen();
        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot);
            screen.CycleSlotKind(slot);
        }

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        var validationLines = inner.TextDraws
            .Where(d => d.FontSize <= 16f && d.Text.Contains("supports up to", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(validationLines);
        Assert.All(validationLines, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= MpValidationWrap + 1f, draw.Text);
        });
    }

    [Theory]
    [InlineData(1920f, 1080f)]
    [InlineData(1024f, 768f)]
    public void UnitInfoPanel_long_unit_names_and_subtitles_fit_slot_bounds(float viewportX, float viewportY)
    {
        var viewport = new Vector2(viewportX, viewportY);
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(UnitInfoPanelWidth, 170f),
            FontSize = 18f,
            SelectedUnits = CreateStressUnitInfos(),
        };

        var inner = new RecordingRenderer(viewport);
        if (viewport == CompactViewport)
        {
            panel.Anchor = Anchor.BottomCenter;
            panel.Position = new Vector2(-280f, -180f);

            var scaler = new UIScaler(viewport);
            var renderer = new ScaledUIRenderer(inner, scaler);
            var (panelPos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
            panel.Draw(renderer, panelPos, ReferenceViewport);
            AssertNoHorizontalViewportBleed(inner.TextDraws, viewport);
        }
        else
        {
            panel.Draw(inner, Vector2.Zero, ReferenceViewport);
            Assert.All(inner.TextDraws, draw =>
            {
                float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
                Assert.True(width <= UnitInfoContentWidth + 1f, draw.Text);
            });
            AssertScreenTextWithinViewport(inner.TextDraws, viewport);
        }
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    [InlineData(2560f, 1440f)]
    public void SaveGameScreen_slot_labels_fit_bounds_with_long_names(float viewportWidth, float viewportHeight)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_bounds_{Guid.NewGuid():N}");
        try
        {
            var mgr = new SaveManager(dir);
            string longMission =
                "Operation Vanguard Deep Space Reconnaissance Extended Territorial Control Campaign Alpha Seven " +
                "Sector Frontier Relay Corridor";

            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = longMission,
                ElapsedMissionTime = 3600f,
                Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
            });
            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.Autosave,
                MissionId = longMission + "_autosave_variant",
                ElapsedMissionTime = 7200f,
                Entities = [new EntitySaveRecord { EntityId = 2, TemplateId = "hero_default", Health = 50f }],
            });

            var viewport = new Vector2(viewportWidth, viewportHeight);
            var inner = new RecordingRenderer(viewport);
            var screen = new SaveGameScreen(mgr);
            ScaledUIRenderer? scaledRenderer = null;

            if (viewport == ReferenceViewport)
                screen.Draw(inner);
            else
            {
                scaledRenderer = new ScaledUIRenderer(inner, new UIScaler(viewport));
                screen.Draw(scaledRenderer);
            }

            AssertScreenTextWithinViewport(inner.TextDraws, viewport);

            float titleFontSize = scaledRenderer?.ScaleToPhysical(26f) ?? 26f;
            var slotDraws = inner.TextDraws
                .Where(d => d.Text is not "Save Game" and not "Back")
                .Where(d => Math.Abs(d.FontSize - titleFontSize) > 1f)
                .ToList();

            Assert.NotEmpty(slotDraws);
            float slotInnerPhysical = scaledRenderer?.ScaleToPhysical(SaveSlotButtonInner) ?? SaveSlotButtonInner;
            Assert.All(slotDraws, draw =>
            {
                float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
                Assert.True(width <= slotInnerPhysical + 1f, $"[{draw.Text}] width {width} > {slotInnerPhysical}");
            });
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    [InlineData(2560f, 1440f)]
    public void LoadGameScreen_entry_labels_fit_bounds_with_long_metadata(float viewportWidth, float viewportHeight)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"load_bounds_{Guid.NewGuid():N}");
        try
        {
            var mgr = new SaveManager(dir);
            string longMission =
                "Operation Vanguard Deep Space Reconnaissance Extended Territorial Control Campaign Alpha Seven " +
                "Relay Corridor Staging Area";

            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[1],
                MissionId = longMission,
                ElapsedMissionTime = 5432f,
                Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
            });
            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[2],
                MissionId = longMission + "_secondary_front",
                ElapsedMissionTime = 12_345f,
                Entities = [new EntitySaveRecord { EntityId = 2, TemplateId = "hero_default", Health = 80f }],
            });

            var viewport = new Vector2(viewportWidth, viewportHeight);
            var inner = new RecordingRenderer(viewport);
            var screen = new LoadGameScreen(mgr);
            ScaledUIRenderer? scaledRenderer = null;

            if (viewport == ReferenceViewport)
                screen.Draw(inner);
            else
            {
                scaledRenderer = new ScaledUIRenderer(inner, new UIScaler(viewport));
                screen.Draw(scaledRenderer);
            }

            AssertScreenTextWithinViewport(inner.TextDraws, viewport);
            var entryDraws = inner.TextDraws
                .Where(d => d.Text.Contains(" | ", StringComparison.Ordinal))
                .ToList();

            Assert.NotEmpty(entryDraws);
            float entryInnerPhysical = scaledRenderer?.ScaleToPhysical(LoadEntryButtonInner) ?? LoadEntryButtonInner;
            Assert.All(entryDraws, draw =>
            {
                float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
                Assert.True(width <= entryInnerPhysical + 1f, $"[{draw.Text}] width {width} > {entryInnerPhysical}");
            });
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveGameScreen_overwrite_dialog_text_fits_bounds_with_long_metadata()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_confirm_{Guid.NewGuid():N}");
        try
        {
            var mgr = new SaveManager(dir);
            string longMission =
                "Operation Vanguard Deep Space Reconnaissance Extended Territorial Control Campaign Alpha Seven " +
                "Relay Corridor Staging Area";

            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = longMission,
                ElapsedMissionTime = 3600f,
                Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
            });

            var screen = new SaveGameScreen(mgr);
            screen.RequestSave(SaveSlotNames.ManualSlots[0], () => new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = longMission,
                Entities = [new EntitySaveRecord { EntityId = 2, TemplateId = "hero_default", Health = 100f }],
            });

            var inner = new RecordingRenderer(ReferenceViewport);
            screen.Draw(inner);

            AssertScreenTextWithinViewport(inner.TextDraws);
            const float confirmWrap = 420f - 8f;
            const float confirmButtonInner = 160f - Button.TextPadding;
            foreach (var draw in inner.TextDraws)
            {
                float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
                float maxWidth = draw.Text switch
                {
                    "Overwrite Save?" => 420f,
                    "Existing data will be replaced." => 400f,
                    "Overwrite" or "Cancel" => confirmButtonInner,
                    _ when draw.Text.Contains('—', StringComparison.Ordinal) => confirmWrap,
                    _ => 520f,
                };
                Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
            }
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ShipDesignerScreen_race_and_model_labels_fit_control_panel()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip(FleetGalleryLayout.AllShipIds[0], RaceTextureIndex.AllRaceIds[0]);

        int raceCount = RaceTextureIndex.AllRaceIds.Count;
        int shipCount = FleetGalleryLayout.AllShipIds.Length;
        int stationCount = FleetGalleryLayout.AllBaseIds.Length;

        for (int r = 0; r < raceCount; r++)
        {
            for (int m = 0; m < shipCount; m++)
            {
                AssertShipDesignerTextFits(screen);
                screen.CycleModel();
            }

            screen.CycleRace();
        }

        screen.ToggleCategory();
        for (int r = 0; r < raceCount; r++)
        {
            for (int m = 0; m < stationCount; m++)
            {
                AssertShipDesignerTextFits(screen);
                screen.CycleModel();
            }

            screen.CycleRace();
        }
    }

    [Fact]
    public void BriefingScreen_briefing_body_scrolls_without_viewport_bleed()
    {
        var screen = CreateLongBriefingScreen(out string longBody);
        Assert.True(longBody.Length >= 2000, "Briefing body must stress scroll height.");

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        var chromeDraws = inner.TextDraws.Where(IsBriefingChromeDraw).ToList();
        AssertScreenTextWithinViewport(chromeDraws);

        Assert.All(inner.TextDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.Text is "Start Mission" or "Back"
                ? 280f - Button.TextPadding
                : BriefingWrap;
            Assert.True(width <= maxWidth + 1f, draw.Text);
        });

        var bodyDraws = inner.TextDraws.Where(IsBriefingBodyDraw).ToList();
        Assert.NotEmpty(bodyDraws);

        var visibleBodyDraws = bodyDraws
            .Where(d => d.Position.Y + d.FontSize > BriefingPanelTop && d.Position.Y < BriefingPanelBottom)
            .ToList();
        Assert.NotEmpty(visibleBodyDraws);
        Assert.All(visibleBodyDraws, draw =>
        {
            Assert.True(draw.Position.Y >= BriefingPanelTop - 1f, draw.Text);
            Assert.True(draw.Position.Y + draw.FontSize <= BriefingPanelBottom + 1f, draw.Text);
        });

        var scroll = FindWidget<ScrollPanel>(screen, "BriefingBodyScroll");
        Assert.NotNull(scroll);
        Assert.True(scroll!.ContentHeight > scroll.Size.Y);
        Assert.True(scroll.MaxScrollOffset(scroll.Size) > 0f);
    }

    [Fact]
    public void BriefingScreen_compact_viewport_chrome_avoids_horizontal_bleed()
    {
        var screen = CreateLongBriefingScreen(out _);
        var inner = new RecordingRenderer(CompactViewport);
        var renderer = new ScaledUIRenderer(inner, new UIScaler(CompactViewport));
        screen.Draw(renderer);

        var chromeDraws = inner.TextDraws.Where(IsBriefingChromeDraw).ToList();
        AssertNoHorizontalViewportBleed(chromeDraws, CompactViewport);
    }

    [Fact]
    public void BriefingScreen_long_objectives_fit_content_width()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            DisplayName = "Operation Vanguard: Extended Deep Space Reconnaissance Campaign",
            Description = "Long fallback briefing body for missions without dedicated narrative text blocks.",
            Briefing = new BriefingDefinition
            {
                Text =
                    "Commander, reconnaissance probes report multiple hostile staging areas along the frontier. " +
                    "Establish forward bases, interdict enemy supply lines, and hold the relay corridor until reinforcements arrive.",
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector",
                    "Protect the civilian evacuation corridor for fifteen minutes under continuous weapons fire",
                ],
            },
        });

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        Assert.All(inner.TextDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.Text is "Start Mission" or "Back"
                ? 280f - 20f
                : BriefingWrap;
            Assert.True(width <= maxWidth + 1f, draw.Text);
        });
    }

    [Fact]
    public void SettingsScreen_and_pause_screen_short_labels_fit_buttons()
    {
        string settingsDir = Path.Combine(Path.GetTempPath(), $"sg_screen_bounds_{Guid.NewGuid():N}");
        Directory.CreateDirectory(settingsDir);
        try
        {
            var settings = new SettingsManager(Path.Combine(settingsDir, "settings.json"));
            var settingsScreen = new SettingsScreen(settings);
            var pauseScreen = new PauseScreen();

            var inner = new RecordingRenderer(ReferenceViewport);
            settingsScreen.Draw(inner);
            pauseScreen.Draw(inner);

            Assert.All(inner.TextDraws, draw =>
            {
                float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
                float maxWidth = draw.Text is "Resume" or "Save Game" or "Load Game" or "Settings" or "Quit to Menu" or "Paused"
                    ? PauseButtonInner
                    : draw.Text is "+" or "−"
                        ? 72f - Button.TextPadding
                        : IsSettingsValueLabel(draw.Text)
                            ? 380f - 72f * 2f - 16f - Button.TextPadding
                            : SettingsButtonInner;
                Assert.True(width <= maxWidth + 1f, draw.Text);
            });
        }
        finally
        {
            if (Directory.Exists(settingsDir))
                Directory.Delete(settingsDir, recursive: true);
        }
    }

    private static bool IsSettingsValueLabel(string text) =>
        text.StartsWith("Master:", StringComparison.Ordinal)
        || text.StartsWith("Music:", StringComparison.Ordinal)
        || text.StartsWith("SFX:", StringComparison.Ordinal)
        || text.StartsWith("Pan:", StringComparison.Ordinal)
        || text.StartsWith("Zoom:", StringComparison.Ordinal)
        || text.StartsWith("Font:", StringComparison.Ordinal);

    private static float ResolveMultiplayerMaxWidth((string Text, float FontSize, Vector2 Position) draw)
    {
        if (draw.Text is "<" or ">" or "—")
            return 44f;

        if (draw.Text is "Start Match" or "Back")
            return MpStartButtonInner;

        if (draw.Text is "Human" or "AI" or "Empty")
            return MpKindButtonInner;

        if (draw.Text.StartsWith("Map:", StringComparison.Ordinal))
            return MpMapLabelWrap;

        if (draw.Text.StartsWith("Player ", StringComparison.Ordinal))
            return 120f;

        if (draw.Text.StartsWith("Up to ", StringComparison.Ordinal))
            return 1100f - 20f;

        if (draw.FontSize >= 30f)
            return 900f;

        if (draw.FontSize >= 18f)
            return MpRaceLabelWrap;

        if (draw.FontSize <= 16f)
            return MpValidationWrap;

        return MpRaceLabelWrap;
    }

    private static void AssertScreenTextWithinViewport(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws,
        Vector2? viewport = null)
    {
        Vector2 bounds = viewport ?? ReferenceViewport;
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= bounds.X + 1f, draw.Text);
            Assert.True(draw.Position.Y >= -1f, draw.Text);
            Assert.True(draw.Position.Y + draw.FontSize <= bounds.Y + 1f, draw.Text);
        }
    }

    private static BriefingScreen CreateLongBriefingScreen(out string longBody)
    {
        longBody = string.Join(
            " ",
            Enumerable.Repeat(
                "Commander, reconnaissance probes report multiple hostile staging areas along the contested frontier corridor.",
                40));

        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            DisplayName = "Operation Vanguard: Extended Deep Space Reconnaissance Campaign",
            Description = "Fallback briefing body.",
            Briefing = new BriefingDefinition
            {
                Text = longBody,
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector",
                    "Protect the civilian evacuation corridor for fifteen minutes under continuous weapons fire",
                ],
            },
        });
        return screen;
    }

    private static bool IsBriefingChromeDraw((string Text, float FontSize, Vector2 Position) draw) =>
        draw.Text is "Start Mission" or "Back" or "OBJECTIVES"
        || draw.Text.StartsWith("Operation Vanguard", StringComparison.Ordinal)
        || draw.Text.StartsWith("• ", StringComparison.Ordinal);

    private static bool IsBriefingBodyDraw((string Text, float FontSize, Vector2 Position) draw) =>
        draw.Text is not "Start Mission" and not "Back" && !IsBriefingChromeDraw(draw);

    private static void AssertNoHorizontalViewportBleed(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws,
        Vector2 viewport)
    {
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= viewport.X + 1f, draw.Text);
        }
    }

    private static void AssertShipDesignerTextFits(ShipDesignerScreen screen)
    {
        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.Text switch
            {
                "<" or ">" => 40f - Button.TextPadding,
                "Load Model" or "Reload Model" or "Ships" or "Stations" or "Confirm Design" or "Cancel" =>
                    ShipDesignerControlButtonInner,
                _ when draw.FontSize >= 18f && draw.Text.StartsWith("Primary:", StringComparison.Ordinal) =>
                    ShipDesignerControlButtonInner,
                _ when draw.FontSize >= 18f && draw.Text.StartsWith("Accent:", StringComparison.Ordinal) =>
                    ShipDesignerControlButtonInner,
                _ when draw.FontSize == 16f && draw.Text is not "<" and not ">" =>
                    ShipDesignerPickerWrap,
                _ when draw.FontSize == 18f =>
                    ShipDesignerPreviewWrap,
                _ => ShipDesignerPreviewWrap,
            };
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    private static UnitInfo[] CreateStressUnitInfos() =>
    [
        new UnitInfo
        {
            Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
            Subtitle = "Terran Battlecruiser — Elite Strike Wing — Long-Range Artillery Support Platform",
            CurrentHP = 4200,
            MaxHP = 5000,
            CurrentShields = 1800,
            MaxShields = 2000,
            Armor = 12,
            HPFraction = 0.84f,
            ShieldFraction = 0.9f,
            DisplayKind = EntityDisplayKind.Friendly,
        },
        new UnitInfo
        {
            Name = "Aetherian Phase Interceptor Squadron Alpha Extended",
            Subtitle = "Ability: Chronoshift Field Projection — reduces incoming kinetic damage across the wing",
            CurrentHP = 900,
            MaxHP = 1000,
            HPFraction = 0.9f,
            DisplayKind = EntityDisplayKind.Friendly,
        },
        new UnitInfo
        {
            Name = "Korath Siege Dreadnought Ultra-Heavy Assault Platform",
            Subtitle = "Harvest: Tractor Beam   Cargo 240/300",
            CurrentHP = 8000,
            MaxHP = 9000,
            HPFraction = 0.88f,
            HarvestMode = "Tractor Beam",
            CargoAmount = 240f,
            CargoCapacity = 300f,
            DisplayKind = EntityDisplayKind.Friendly,
        },
        new UnitInfo
        {
            Name = "Voidborn Corrupted Node Relay Stabilizer Array",
            Subtitle = "Hostile structure — long-range disruption field emitter",
            CurrentHP = 1500,
            MaxHP = 2000,
            HPFraction = 0.75f,
            DisplayKind = EntityDisplayKind.Hostile,
        },
    ];

    private static TWidget? FindWidget<TWidget>(UIScreen screen, string name)
        where TWidget : Widget
    {
        foreach (Widget root in GetRoots(screen))
        {
            TWidget? match = FindWidgetInTree<TWidget>(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static TWidget? FindWidgetInTree<TWidget>(Widget widget, string name)
        where TWidget : Widget
    {
        if (widget is TWidget match && widget.Name == name)
            return match;

        foreach (Widget child in widget.Children)
        {
            TWidget? childMatch = FindWidgetInTree<TWidget>(child, name);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    private static IEnumerable<Widget> GetRoots(UIScreen screen)
    {
        FieldInfo? field = typeof(UIScreen).GetField("_roots", BindingFlags.Instance | BindingFlags.NonPublic);
        return (IEnumerable<Widget>)field!.GetValue(screen)!;
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> RectDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            RectDraws.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}