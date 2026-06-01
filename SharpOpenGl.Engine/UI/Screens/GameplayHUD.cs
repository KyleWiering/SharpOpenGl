using OpenTK.Mathematics;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// In-game heads-up display.
/// Hosts the <see cref="ResourceBar"/> (top), <see cref="Minimap"/> (bottom-left),
/// <see cref="UnitInfoPanel"/> (bottom-centre), and a pause button (top-right).
/// </summary>
public sealed class GameplayHUD : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "GameplayHUD";

    // ── Widgets exposed for data binding ──────────────────────────────────────

    /// <summary>Top resource bar.  Bind <see cref="ResourceBar.Resources"/> each frame.</summary>
    public ResourceBar ResourceBar { get; }

    /// <summary>Bottom-left minimap.  Bind fog-of-war and unit positions each frame.</summary>
    public Minimap Minimap { get; }

    /// <summary>Bottom-centre unit info panel.  Bind selected units each frame.</summary>
    public UnitInfoPanel UnitInfoPanel { get; }

    /// <summary>Right-side build panel. Bind building data when a building is selected.</summary>
    public BuildPanel BuildPanel { get; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the pause button is clicked.</summary>
    public event Action? PauseRequested;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initialise and wire up the HUD layout.</summary>
    public GameplayHUD()
    {
        // ── Resource bar (top, full-width, 48 px tall) ──────────────────────
        ResourceBar = new ResourceBar
        {
            Name = "ResourceBar",
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(1920f, 48f),
        };
        AddWidget(ResourceBar);

        // ── Minimap (bottom-left, 240 × 240) ────────────────────────────────
        Minimap = new Minimap
        {
            Name = "Minimap",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(8f, -248f),
            Size = new Vector2(240f, 240f),
        };
        AddWidget(Minimap);

        // ── Unit info panel (bottom-centre, 480 × 160) ──────────────────────
        UnitInfoPanel = new UnitInfoPanel
        {
            Name = "UnitInfoPanel",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-240f, -168f),
            Size = new Vector2(480f, 160f),
        };
        AddWidget(UnitInfoPanel);

        // ── Build panel (right side, shown when building is selected) ────────
        BuildPanel = new BuildPanel
        {
            Name = "BuildPanel",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-280f, 56f),
            Size = new Vector2(270f, 500f),
            Visible = false,
        };
        AddWidget(BuildPanel);

        // ── Pause button (top-right) ─────────────────────────────────────────
        var pauseBtn = new Button
        {
            Name = "PauseButton",
            Label = "II",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-72f, 8f),
            Size = new Vector2(56f, 40f),
            FontSize = 18f,
        };
        pauseBtn.Clicked += () => PauseRequested?.Invoke();
        AddWidget(pauseBtn);
    }
}
