using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
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
    public ShipControlBar ShipControlBar { get; }
    public ObjectivePanel ObjectivePanel { get; }

    /// <summary>Fired when the pause button is clicked.</summary>
    public event Action? PauseRequested;

    /// <summary>Fired when the minimap is clicked. Argument is normalised 0..1.</summary>
    public event Action<Vector2>? MinimapClicked;

    public GameplayHUD()
    {
        ResourceBar = new ResourceBar
        {
            Name = "ResourceBar",
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(1920f, 48f),
            FontSize = 18f,
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
            Position = new Vector2(-280f, -180f),
            Size = new Vector2(560f, 170f),
            FontSize = 18f,
        };
        AddWidget(UnitInfoPanel);

        BuildPanel = new BuildPanel
        {
            Name = "BuildPanel",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-280f, 56f),
            Size = new Vector2(270f, 500f),
            Visible = false,
        };
        AddWidget(BuildPanel);

        ShipControlBar = new ShipControlBar
        {
            Name = "ShipControlBar",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-200f, -80f),
            Size = new Vector2(400f, 60f),
            Visible = false,
        };
        AddWidget(ShipControlBar);

        var pauseBtn = new Button
        {
            Name = "PauseButton",
            Label = "II",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-72f, 8f),
            Size = new Vector2(56f, 40f),
            FontSize = 20f,
        };
        pauseBtn.Clicked += () => PauseRequested?.Invoke();
        AddWidget(pauseBtn);
    }

    /// <summary>
    /// Only route clicks to interactive HUD widgets so world selection still works.
    /// </summary>
    public override bool HandlePointerTapped(Vector2 screenPoint, int button, Vector2 viewportSize)
    {
        if (!Visible) return false;

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
            if (root is Button btn &&
                btn.HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
                return true;
        }

        return false;
    }

    /// <summary>Update ship control bar visibility from selected units.</summary>
    public void BindShipControlBar(bool hasWeapons, bool hasMovement, Stance? stance, bool anySelected)
    {
        ShipControlBar.Visible = anySelected && (hasWeapons || hasMovement);
        if (ShipControlBar.Visible)
            ShipControlBar.UpdateForShip(hasWeapons, hasMovement, stance);
    }
}