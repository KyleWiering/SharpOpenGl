using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Ship-designer screen where players can rotate and recolour their ships and bases.
/// </summary>
/// <remarks>
/// The actual 3D model render is driven by the game layer — this screen provides
/// the control overlay (colour pickers, rotation slider, confirm/cancel).
/// </remarks>
public sealed class ShipDesignerScreen : UIScreen
{
    private string _shipId = "fighter_basic";
    private string _raceId = RaceShipMeshes.DefaultRace;
    private int _shipIndex;
    private int _raceIndex;

    // ── Exposed controls ──────────────────────────────────────────────────────

    /// <summary>Currently selected hull primary colour (RGBA, channels 0–1).</summary>
    public Vector4 PrimaryColor { get; private set; } = new Vector4(0.2f, 0.4f, 0.8f, 1f);

    /// <summary>Currently selected hull secondary / accent colour.</summary>
    public Vector4 AccentColor { get; private set; } = new Vector4(0.8f, 0.6f, 0.1f, 1f);

    /// <summary>Current model rotation angle in degrees (0–360).</summary>
    public float RotationDegrees { get; private set; }

    /// <summary>Active faction for mesh resolution.</summary>
    public string RaceId => _raceId;

    /// <summary>Active hull definition id.</summary>
    public string ShipId => _shipId;

    /// <summary>Manifest mesh key for the current race + hull selection.</summary>
    public string MeshKey => MeshManifest.ShipKey(_raceId, _shipId);

    // ── Widgets ───────────────────────────────────────────────────────────────

    private readonly Panel _controlPanel;

    /// <inheritdoc/>
    public override string ScreenName => "ShipDesigner";

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the player confirms the design (ship ID + colours).</summary>
    public event Action<string, Vector4, Vector4>? DesignConfirmed;

    /// <summary>Fired when the player cancels and wants to go back.</summary>
    public event Action? Cancelled;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Build the ship designer UI.</summary>
    public ShipDesignerScreen()
    {
        _controlPanel = new Panel
        {
            Name = "ControlPanel",
            Anchor = Anchor.TopRight,
            Position = new Vector2(-408f, 40f),
            Size = new Vector2(400f, 900f),
        };
        AddWidget(_controlPanel);

        BuildControls();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set which ship definition is being edited.
    /// Call before pushing this screen.
    /// </summary>
    public void LoadShip(string shipId, string? raceId = null)
    {
        _shipId = shipId;
        if (!string.IsNullOrWhiteSpace(raceId))
            _raceId = raceId;

        _shipIndex = Array.IndexOf(FleetGalleryLayout.AllShipIds, _shipId);
        if (_shipIndex < 0) _shipIndex = 0;

        _raceIndex = 0;
        for (int i = 0; i < RaceTextureIndex.AllRaceIds.Count; i++)
        {
            if (RaceTextureIndex.AllRaceIds[i].Equals(_raceId, StringComparison.OrdinalIgnoreCase))
            {
                _raceIndex = i;
                break;
            }
        }
    }

    /// <summary>Cycle to the next playable race and keep the current hull slot.</summary>
    public void CycleRace()
    {
        _raceIndex = (_raceIndex + 1) % RaceTextureIndex.AllRaceIds.Count;
        _raceId = RaceTextureIndex.AllRaceIds[_raceIndex];
    }

    /// <summary>Cycle to the next hull in the fleet roster.</summary>
    public void CycleShip()
    {
        _shipIndex = (_shipIndex + 1) % FleetGalleryLayout.AllShipIds.Length;
        _shipId = FleetGalleryLayout.AllShipIds[_shipIndex];
    }

    /// <summary>Rotate the model preview by <paramref name="degrees"/> (relative delta).</summary>
    public void Rotate(float degrees)
    {
        RotationDegrees = (RotationDegrees + degrees) % 360f;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void BuildControls()
    {
        float y = 16f;
        float btnW = 360f;
        float btnH = 48f;
        float gap = 12f;

        AddPresetButton("Primary: Blue",   new Vector4(0.2f, 0.4f, 0.8f, 1f), true,  ref y, btnW, btnH, gap);
        AddPresetButton("Primary: Red",    new Vector4(0.8f, 0.2f, 0.2f, 1f), true,  ref y, btnW, btnH, gap);
        AddPresetButton("Primary: Green",  new Vector4(0.2f, 0.7f, 0.3f, 1f), true,  ref y, btnW, btnH, gap);
        AddPresetButton("Primary: Black",  new Vector4(0.1f, 0.1f, 0.1f, 1f), true,  ref y, btnW, btnH, gap);

        y += gap * 2f;

        AddPresetButton("Accent: Gold",   new Vector4(0.8f, 0.6f, 0.1f, 1f), false, ref y, btnW, btnH, gap);
        AddPresetButton("Accent: Silver", new Vector4(0.7f, 0.7f, 0.8f, 1f), false, ref y, btnW, btnH, gap);
        AddPresetButton("Accent: Red",    new Vector4(0.9f, 0.2f, 0.2f, 1f), false, ref y, btnW, btnH, gap);
        AddPresetButton("Accent: White",  new Vector4(1.0f, 1.0f, 1.0f, 1f), false, ref y, btnW, btnH, gap);

        y += gap;

        var raceBtn = new Button
        {
            Name = "CycleRace",
            Label = "Next Race",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, y),
            Size = new Vector2(btnW, btnH),
            FontSize = 16f,
        };
        raceBtn.Clicked += CycleRace;
        _controlPanel.AddChild(raceBtn);
        y += btnH + gap;

        var shipBtn = new Button
        {
            Name = "CycleShip",
            Label = "Next Hull",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, y),
            Size = new Vector2(btnW, btnH),
            FontSize = 16f,
        };
        shipBtn.Clicked += CycleShip;
        _controlPanel.AddChild(shipBtn);

        y += gap * 2f;

        var confirmBtn = new Button
        {
            Name = "Confirm",
            Label = "Confirm Design",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, y),
            Size = new Vector2(btnW, btnH),
            FontSize = 18f,
        };
        confirmBtn.Clicked += () => DesignConfirmed?.Invoke(_shipId, PrimaryColor, AccentColor);
        _controlPanel.AddChild(confirmBtn);

        y += btnH + gap;

        var cancelBtn = new Button
        {
            Name = "Cancel",
            Label = "Cancel",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, y),
            Size = new Vector2(btnW, btnH),
            FontSize = 18f,
            NormalColor = new Vector4(0.3f, 0.15f, 0.15f, 1f),
            HoverColor  = new Vector4(0.5f, 0.2f, 0.2f, 1f),
        };
        cancelBtn.Clicked += () => Cancelled?.Invoke();
        _controlPanel.AddChild(cancelBtn);
    }

    private void AddPresetButton(
        string label, Vector4 color, bool isPrimary,
        ref float y, float btnW, float btnH, float gap)
    {
        var btn = new Button
        {
            Name = label.Replace(" ", "").Replace(":", ""),
            Label = label,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, y),
            Size = new Vector2(btnW, btnH),
            NormalColor = new Vector4(color.X * 0.5f, color.Y * 0.5f, color.Z * 0.5f, 1f),
            HoverColor  = new Vector4(color.X * 0.7f, color.Y * 0.7f, color.Z * 0.7f, 1f),
            FontSize = 16f,
        };
        btn.Clicked += () =>
        {
            if (isPrimary)
                PrimaryColor = color;
            else
                AccentColor = color;
        };
        _controlPanel.AddChild(btn);
        y += btnH + gap;
    }
}