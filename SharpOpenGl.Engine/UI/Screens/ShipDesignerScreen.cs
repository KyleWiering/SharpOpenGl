using OpenTK.Mathematics;
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
    private string _shipId = "default";

    // ── Exposed controls ──────────────────────────────────────────────────────

    /// <summary>Currently selected hull primary colour (RGBA, channels 0–1).</summary>
    public Vector4 PrimaryColor { get; private set; } = new Vector4(0.2f, 0.4f, 0.8f, 1f);

    /// <summary>Currently selected hull secondary / accent colour.</summary>
    public Vector4 AccentColor { get; private set; } = new Vector4(0.8f, 0.6f, 0.1f, 1f);

    /// <summary>Current model rotation angle in degrees (0–360).</summary>
    public float RotationDegrees { get; private set; }

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
        // Right-hand control panel (width 400, full height)
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
    public void LoadShip(string shipId)
    {
        _shipId = shipId;
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

        // Primary colour presets
        AddPresetButton("Primary: Blue",   new Vector4(0.2f, 0.4f, 0.8f, 1f), true,  ref y, btnW, btnH, gap);
        AddPresetButton("Primary: Red",    new Vector4(0.8f, 0.2f, 0.2f, 1f), true,  ref y, btnW, btnH, gap);
        AddPresetButton("Primary: Green",  new Vector4(0.2f, 0.7f, 0.3f, 1f), true,  ref y, btnW, btnH, gap);
        AddPresetButton("Primary: Black",  new Vector4(0.1f, 0.1f, 0.1f, 1f), true,  ref y, btnW, btnH, gap);

        y += gap * 2f;

        // Accent colour presets
        AddPresetButton("Accent: Gold",   new Vector4(0.8f, 0.6f, 0.1f, 1f), false, ref y, btnW, btnH, gap);
        AddPresetButton("Accent: Silver", new Vector4(0.7f, 0.7f, 0.8f, 1f), false, ref y, btnW, btnH, gap);
        AddPresetButton("Accent: Red",    new Vector4(0.9f, 0.2f, 0.2f, 1f), false, ref y, btnW, btnH, gap);
        AddPresetButton("Accent: White",  new Vector4(1.0f, 1.0f, 1.0f, 1f), false, ref y, btnW, btnH, gap);

        y += gap * 3f;

        // Confirm / Cancel
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
