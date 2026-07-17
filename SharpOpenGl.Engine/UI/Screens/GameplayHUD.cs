using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// In-game heads-up display.
/// </summary>
public sealed class GameplayHUD : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "GameplayHUD";

    public ResourceBar ResourceBar { get; }
    public Minimap Minimap { get; }
    public UnitInfoPanel UnitInfoPanel { get; }
    public BuildPanel BuildPanel { get; }
    public BuildMapPanel BuildMapPanel { get; }
    public ShipControlBar ShipControlBar { get; }
    public ObjectivePanel ObjectivePanel { get; }

    /// <summary>Optional on-screen camera joystick for touch layouts.</summary>
    public VirtualJoystick? CameraJoystick { get; private set; }

    /// <summary>Fired when the pause button is clicked.</summary>
    public event Action? PauseRequested;

    /// <summary>Fired when the build-map button is clicked.</summary>
    public event Action? BuildMapRequested;

    /// <summary>Fired when the minimap is clicked. Argument is normalised 0..1.</summary>
    public event Action<Vector2>? MinimapClicked;

    /// <summary>Optional session subtitle (e.g. sandbox seed snippet).</summary>
    public string SessionSubtitle { get; set; } = string.Empty;

    /// <summary>Placement-mode hint shown above the minimap (empty when not placing).</summary>
    public string PlacementHint { get; set; } = string.Empty;

    /// <summary>When true, the placement hint uses valid (green) styling.</summary>
    public bool PlacementHintIsValid { get; set; }

    /// <summary>Brief flash intensity after a failed placement click (0..1).</summary>
    public float PlacementHintFlash { get; set; }

    /// <summary>When true, shows a one-time coach mark for the build flow entry points.</summary>
    public bool ShowBuildFlowHint { get; set; } = true;

    /// <summary>When true, shows a contextual B-key hint while a builder ship is selected.</summary>
    public bool ShowBuilderShortcutHint { get; set; }

    /// <summary>When true, shows a one-time onboarding banner on the first training mission.</summary>
    public bool ShowFirstMissionOnboardingHint { get; set; }

    private const float PlacementHintFontSize = 20f;
    private const float PlacementHintMinFontSize = 12f;
    private const float PlacementHintPadX = 16f;
    private const float PlacementHintPadY = 8f;

    public GameplayHUD()
    {
        ResourceBar = new ResourceBar
        {
            Name = "ResourceBar",
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(UIScaler.ReferenceSize.X, GameplayHudLayout.ResourceBarHeight),
            FontSize = GameplayHudLayout.ResourceBarFontSize,
        };
        AddWidget(ResourceBar);

        ObjectivePanel = new ObjectivePanel
        {
            Name = "ObjectivePanel",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-360f, 56f),
            Size = new Vector2(720f, 200f),
            Visible = false,
        };
        AddWidget(ObjectivePanel);

        Minimap = new Minimap
        {
            Name = "Minimap",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(8f, -248f),
            Size = new Vector2(240f, 240f),
        };
        Minimap.Clicked += norm => MinimapClicked?.Invoke(norm);
        AddWidget(Minimap);

        UnitInfoPanel = new UnitInfoPanel
        {
            Name = "UnitInfoPanel",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(GameplayHudLayout.UnitInfoOffsetX, GameplayHudLayout.UnitInfoOffsetY),
            Size = new Vector2(GameplayHudLayout.UnitInfoStandardWidth, GameplayHudLayout.UnitInfoStandardHeight),
            FontSize = 17f,
        };
        AddWidget(UnitInfoPanel);

        // Layout audit (1024x768 logical viewport via uniform scale): BuildPanel bottom
        // y=556 clears Minimap top y=592 by 36px; no overlap with ShipControlBar,
        // BuildMapPanel, UnitInfoPanel, or PlacementHint band — anchor unchanged.
        BuildPanel = new BuildPanel
        {
            Name = "BuildPanel",
            Anchor = Anchor.TopRight,
            Position = new Vector2(GameplayHudLayout.BuildPanelOffsetX, GameplayHudLayout.BuildPanelOffsetY),
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            Visible = false,
        };
        AddWidget(BuildPanel);

        BuildMapPanel = new BuildMapPanel
        {
            Name = "BuildMapPanel",
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = false,
        };
        AddWidget(BuildMapPanel);

        var buildMapBtn = new Button
        {
            Name = "BuildMapButton",
            Label = "Build",
            TooltipHint = "Open structure build menu (B) — pick icon, then place on map",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-120f, 8f),
            Size = new Vector2(112f, 44f),
            FontSize = 16f,
        };
        MenuTheme.ApplyNavButton(buildMapBtn, showGlow: false);
        AddWidget(buildMapBtn);

        ShipControlBar = new ShipControlBar
        {
            Name = "ShipControlBar",
            Visible = false,
        };
        AddWidget(ShipControlBar);

        var pauseBtn = new Button
        {
            Name = "PauseButton",
            Label = "Pause",
            TooltipHint = "Pause (P)",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-8f, 8f),
            Size = new Vector2(112f, 44f),
            FontSize = 16f,
        };
        MenuTheme.ApplyNavButton(pauseBtn, showGlow: false);
        pauseBtn.Clicked += () => PauseRequested?.Invoke();
        AddWidget(pauseBtn);

        ApplyDensityLayout();
    }

    /// <summary>Recompute HUD panel sizes/anchors for the default 1024×768 window density budget.</summary>
    public void ApplyDensityLayout() => GameplayHudLayout.ApplyDensityLayout(this);

    /// <summary>Show or hide the virtual camera joystick for touch-first layouts.</summary>
    public void ConfigureTouchLayout(LayoutProfile profile)
    {
        bool touch = AdaptiveLayout.IsTouchDevice(profile);
        if (touch && CameraJoystick == null)
        {
            CameraJoystick = new VirtualJoystick
            {
                Name = "CameraJoystick",
                Anchor = Anchor.BottomLeft,
                Position = new Vector2(24f, -200f),
                Size = new Vector2(140f, 140f),
                BaseRadius = 56f,
                ThumbRadius = 22f,
            };
            AddWidget(CameraJoystick);
        }
        else if (!touch && CameraJoystick != null)
        {
            RemoveWidget(CameraJoystick);
            CameraJoystick = null;
        }
    }

    /// <summary>Feed active touch points into the virtual joystick each frame.</summary>
    public void UpdateTouchJoystick(IReadOnlyList<TouchPoint> touches)
    {
        if (CameraJoystick == null || !CameraJoystick.Visible) return;
        CameraJoystick.UpdateTouches(touches, Vector2.Zero, UIScaler.ReferenceSize);
    }

    /// <summary>Normalised camera pan axis from the virtual joystick.</summary>
    public Vector2 ReadCameraJoystickAxis() => CameraJoystick?.Axis ?? Vector2.Zero;

    /// <summary>Dismiss the first-run build flow coach mark for this session.</summary>
    public void DismissBuildFlowHint()
    {
        ShowBuildFlowHint = false;
        ShowBuilderShortcutHint = false;
    }

    /// <summary>Dismiss the first training mission onboarding banner for this session.</summary>
    public void DismissFirstMissionOnboardingHint() => ShowFirstMissionOnboardingHint = false;

    /// <inheritdoc/>
    public override void Draw(IUIRenderer renderer)
    {
        ApplyDensityLayout();
        base.Draw(renderer);
        DrawSessionSubtitle(renderer);
        DrawFirstMissionOnboardingHint(renderer);
        DrawBuildFlowHint(renderer);
        DrawBuilderShortcutHint(renderer);
        DrawPlacementHint(renderer);
    }

    private void DrawSessionSubtitle(IUIRenderer renderer)
    {
        if (string.IsNullOrWhiteSpace(SessionSubtitle))
            return;

        const float fontSize = 16f;
        float textWidth = UIFontMetrics.MeasureTextWidth(SessionSubtitle, fontSize);
        float textHeight = UIFontMetrics.GetGlyphHeight(fontSize);
        float panelX = (UIScaler.ReferenceSize.X - textWidth) * 0.5f;
        float panelY = 52f;

        renderer.DrawText(
            SessionSubtitle,
            new Vector2(panelX, panelY),
            fontSize,
            new Vector4(0.72f, 0.82f, 0.95f, 0.9f));
    }

    private void DrawFirstMissionOnboardingHint(IUIRenderer renderer)
    {
        if (!ShowFirstMissionOnboardingHint)
            return;

        const string hint =
            "Training mission: follow objectives above — hostile arrives shortly; select ship (LClick), attack (RClick)";
        const float fontSize = 14f;
        const float padX = 14f;
        const float padY = 7f;

        float maxWidth = 640f;
        float fittedSize = UIFontMetrics.FitFontSize(hint, fontSize, maxWidth, 11f);
        string text = UITextDrawing.TruncateWithEllipsis(hint, maxWidth, fittedSize);
        float textWidth = UIFontMetrics.MeasureTextWidth(text, fittedSize);
        float textHeight = UIFontMetrics.GetGlyphHeight(fittedSize);

        float panelW = textWidth + padX * 2f;
        float panelH = textHeight + padY * 2f;
        float panelX = (UIScaler.ReferenceSize.X - panelW) * 0.5f;
        float panelY = 268f;

        renderer.DrawRect(
            new Vector2(panelX, panelY),
            new Vector2(panelW, panelH),
            new Vector4(0.12f, 0.1f, 0.06f, 0.9f));
        renderer.DrawRectOutline(
            new Vector2(panelX, panelY),
            new Vector2(panelW, panelH),
            new Vector4(1f, 0.82f, 0.35f, 0.9f));
        renderer.DrawText(
            text,
            new Vector2(panelX + padX, panelY + padY),
            fittedSize,
            new Vector4(1f, 0.92f, 0.65f, 1f));
    }

    private void DrawBuildFlowHint(IUIRenderer renderer)
    {
        if (!ShowBuildFlowHint || ShowBuilderShortcutHint)
            return;

        const string hint = "Tip: select builder, press Build (B)";
        const float fontSize = 13f;
        const float padX = 12f;
        const float padY = 6f;
        const float rightMargin = 16f;

        float maxWidth = 300f;
        float fittedSize = UIFontMetrics.FitFontSize(hint, fontSize, maxWidth, 10f);
        string text = UITextDrawing.TruncateWithEllipsis(hint, maxWidth, fittedSize);
        float textWidth = UIFontMetrics.MeasureTextWidth(text, fittedSize);
        float textHeight = UIFontMetrics.GetGlyphHeight(fittedSize);

        float panelW = textWidth + padX * 2f;
        float panelH = textHeight + padY * 2f;
        float panelX = UIScaler.ReferenceSize.X - panelW - rightMargin;
        float panelY = 58f;

        renderer.DrawRect(
            new Vector2(panelX, panelY),
            new Vector2(panelW, panelH),
            new Vector4(0.1f, 0.16f, 0.28f, 0.92f));
        renderer.DrawRectOutline(
            new Vector2(panelX, panelY),
            new Vector2(panelW, panelH),
            new Vector4(0.45f, 0.72f, 1f, 0.85f));
        renderer.DrawText(
            text,
            new Vector2(panelX + padX, panelY + padY),
            fittedSize,
            new Vector4(0.82f, 0.9f, 1f, 1f));
    }

    private void DrawBuilderShortcutHint(IUIRenderer renderer)
    {
        if (!ShowBuilderShortcutHint)
            return;

        const string hint = "Press B — open build menu";
        const float fontSize = 13f;
        const float padX = 12f;
        const float padY = 6f;

        float maxWidth = 260f;
        float fittedSize = UIFontMetrics.FitFontSize(hint, fontSize, maxWidth, 10f);
        string text = UITextDrawing.TruncateWithEllipsis(hint, maxWidth, fittedSize);
        float textWidth = UIFontMetrics.MeasureTextWidth(text, fittedSize);
        float textHeight = UIFontMetrics.GetGlyphHeight(fittedSize);

        float panelW = textWidth + padX * 2f;
        float panelH = textHeight + padY * 2f;
        float panelX = (UIScaler.ReferenceSize.X - panelW) * 0.5f;
        float panelY = UIScaler.ReferenceSize.Y - 118f;

        renderer.DrawRect(
            new Vector2(panelX, panelY),
            new Vector2(panelW, panelH),
            new Vector4(0.08f, 0.14f, 0.22f, 0.92f));
        renderer.DrawRectOutline(
            new Vector2(panelX, panelY),
            new Vector2(panelW, panelH),
            new Vector4(0.4f, 0.68f, 0.95f, 0.85f));
        renderer.DrawText(
            text,
            new Vector2(panelX + padX, panelY + padY),
            fittedSize,
            new Vector4(0.78f, 0.88f, 1f, 1f));
    }

    private void DrawPlacementHint(IUIRenderer renderer)
    {
        if (string.IsNullOrWhiteSpace(PlacementHint))
            return;

        Vector2 viewport = UIScaler.ReferenceSize;
        float maxLogicalWidth = viewport.X - 280f;
        var (text, fontSize) = FitHintLabel(
            renderer, PlacementHint, maxLogicalWidth, PlacementHintFontSize, PlacementHintMinFontSize);
        float textWidth = UIFontMetrics.MeasureTextWidth(text, fontSize);
        float textHeight = UIFontMetrics.GetGlyphHeight(fontSize);

        float panelW = textWidth + PlacementHintPadX * 2f;
        float panelH = textHeight + PlacementHintPadY * 2f;
        float panelX = (viewport.X - panelW) * 0.5f;
        float panelY = viewport.Y - 292f;

        float flash = Math.Clamp(PlacementHintFlash, 0f, 1f);
        Vector4 bgColor = PlacementHintIsValid
            ? Vector4.Lerp(new Vector4(0.08f, 0.22f, 0.12f, 0.88f),
                new Vector4(0.14f, 0.42f, 0.2f, 0.95f), flash)
            : Vector4.Lerp(new Vector4(0.22f, 0.08f, 0.08f, 0.88f),
                new Vector4(0.38f, 0.1f, 0.1f, 0.95f), flash);

        Vector4 textColor = PlacementHintIsValid
            ? Vector4.Lerp(new Vector4(0.55f, 0.98f, 0.65f, 1f),
                new Vector4(0.85f, 1f, 0.9f, 1f), flash)
            : Vector4.Lerp(new Vector4(0.98f, 0.55f, 0.55f, 1f),
                new Vector4(1f, 0.85f, 0.85f, 1f), flash);

        renderer.DrawRect(
            new Vector2(panelX, panelY), new Vector2(panelW, panelH), bgColor);
        renderer.DrawText(
            text,
            new Vector2(panelX + PlacementHintPadX, panelY + PlacementHintPadY),
            fontSize,
            textColor);
    }

    private static (string Text, float LogicalDrawSize) FitHintLabel(
        IUIRenderer renderer, string text, float maxLogicalWidth,
        float preferredLogicalSize, float minLogicalSize)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float maxPhysicalWidth = MathF.Max(0f, renderer.ScaleToPhysical(maxLogicalWidth) - 2f);
        float viewportMargin = renderer.ScaleToPhysical(32f);
        maxPhysicalWidth = MathF.Min(
            maxPhysicalWidth,
            MathF.Max(0f, renderer.ViewportSize.X - viewportMargin));

        float preferredPhysical = renderer.ResolveFontSize(preferredLogicalSize);
        float minPhysical = MathF.Max(
            ScaledUIRenderer.MinPhysicalFontSize,
            renderer.ResolveFontSize(minLogicalSize));
        float fittedPhysical = UIFontMetrics.FitFontSize(
            text, preferredPhysical, maxPhysicalWidth, minPhysical);
        string display = UITextDrawing.TruncateWithEllipsis(text, maxPhysicalWidth, fittedPhysical);
        float logicalDrawSize = fittedPhysical / physicalScale;
        return (display, logicalDrawSize);
    }

    /// <inheritdoc/>
    public override bool HandleScroll(Vector2 screenPoint, float deltaY, Vector2 viewportSize)
    {
        if (!Visible) return false;

        if (BuildMapPanel.Visible &&
            BuildMapPanel.HandleScroll(screenPoint, deltaY, Vector2.Zero, viewportSize))
            return true;

        if (BuildPanel.Visible &&
            BuildPanel.HandleScroll(screenPoint, deltaY, Vector2.Zero, viewportSize))
            return true;

        if (ObjectivePanel.Visible &&
            ObjectivePanel.HandleScroll(screenPoint, deltaY, Vector2.Zero, viewportSize))
            return true;

        return base.HandleScroll(screenPoint, deltaY, viewportSize);
    }

    /// <summary>
    /// Only route clicks to interactive HUD widgets so world selection still works.
    /// </summary>
    public override bool HandlePointerTapped(Vector2 screenPoint, int button, Vector2 viewportSize)
    {
        if (!Visible) return false;

        if (BuildMapPanel.Visible &&
            BuildMapPanel.HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
            return true;

        if (BuildPanel.Visible &&
            BuildPanel.HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
            return true;

        if (ShipControlBar.Visible &&
            ShipControlBar.HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
            return true;

        if (Minimap.HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
            return true;

        foreach (var root in Roots)
        {
            if (root is not Button btn) continue;
            if (btn.HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
            {
                if (btn.Name == "BuildMapButton")
                    BuildMapRequested?.Invoke();
                return true;
            }
        }

        return false;
    }

    /// <summary>Update ship control bar visibility from selected units.</summary>
    public void BindShipControlBar(
        bool hasWeapons,
        bool hasMovement,
        bool hasResourceCollector,
        bool hasStructureBuilder,
        Stance? stance,
        bool anySelected,
        FormationType? formation,
        bool showFormation)
    {
        ShipControlBar.Visible = anySelected &&
            (hasWeapons || hasMovement || hasResourceCollector || hasStructureBuilder);
        if (ShipControlBar.Visible)
            ShipControlBar.UpdateForShip(
                hasWeapons, hasMovement, hasResourceCollector, hasStructureBuilder,
                stance, formation, showFormation);

        ShowBuilderShortcutHint = ShowBuildFlowHint && hasStructureBuilder && anySelected;
    }
}