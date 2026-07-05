using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>Whether the designer is editing a ship hull or a station building.</summary>
public enum DesignerAssetCategory
{
    Ship,
    Station,
}

/// <summary>
/// Ship-designer screen where players can rotate and recolour their ships and bases.
/// </summary>
/// <remarks>
/// The actual 3D model render is driven by the game layer — this screen provides
/// the control overlay (race/hull pickers, colour pickers, rotation slider, confirm/cancel).
/// </remarks>
public sealed class ShipDesignerScreen : UIScreen
{
    private string _modelId = "fighter_basic";
    private string _raceId = RaceShipMeshes.DefaultRace;
    private int _modelIndex;
    private int _raceIndex;
    private DesignerAssetCategory _category = DesignerAssetCategory.Ship;

    private readonly Panel _controlPanel;
    private readonly Label _raceLabel;
    private readonly Label _modelLabel;
    private readonly Button _categoryBtn;

    // ── Exposed controls ──────────────────────────────────────────────────────

    /// <summary>Currently selected hull primary colour (RGBA, channels 0–1).</summary>
    public Vector4 PrimaryColor { get; private set; } = new Vector4(0.2f, 0.4f, 0.8f, 1f);

    /// <summary>Currently selected hull secondary / accent colour.</summary>
    public Vector4 AccentColor { get; private set; } = new Vector4(0.8f, 0.6f, 0.1f, 1f);

    /// <summary>Current model rotation angle in degrees (0–360).</summary>
    public float RotationDegrees { get; private set; }

    /// <summary>Active faction for mesh resolution.</summary>
    public string RaceId => _raceId;

    /// <summary>Active hull or station definition id.</summary>
    public string ShipId => _modelId;

    /// <summary>Whether the designer is showing ships or stations.</summary>
    public DesignerAssetCategory Category => _category;

    /// <summary>Manifest mesh key for the current race + model selection.</summary>
    public string MeshKey => _category == DesignerAssetCategory.Station
        ? MeshManifest.StationKey(_raceId, _modelId)
        : MeshManifest.ShipKey(_raceId, _modelId);

    /// <inheritdoc/>
    public override string ScreenName => "ShipDesigner";

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when race, category, or model selection changes.</summary>
    public event Action? SelectionChanged;

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

        _raceLabel = new Label
        {
            Name = "RaceLabel",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(68f, 16f),
            Size = new Vector2(224f, 36f),
            FontSize = 16f,
            TextColor = new Vector4(0.9f, 0.92f, 1f, 1f),
        };
        _controlPanel.AddChild(_raceLabel);

        _modelLabel = new Label
        {
            Name = "ModelLabel",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(68f, 68f),
            Size = new Vector2(224f, 36f),
            FontSize = 16f,
            TextColor = new Vector4(0.9f, 0.92f, 1f, 1f),
        };
        _controlPanel.AddChild(_modelLabel);

        _categoryBtn = new Button
        {
            Name = "CategoryToggle",
            Label = "Ships",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, 120f),
            Size = new Vector2(360f, 40f),
            FontSize = 16f,
        };
        _categoryBtn.Clicked += ToggleCategory;
        _controlPanel.AddChild(_categoryBtn);

        BuildPickerRow("RacePrev", "RaceNext", 16f, CycleRace);
        BuildPickerRow("ModelPrev", "ModelNext", 68f, CycleModel);

        BuildControls();
        RefreshSelectionLabels();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set which ship definition is being edited.
    /// Call before pushing this screen.
    /// </summary>
    public void LoadShip(string shipId, string? raceId = null)
    {
        _category = DesignerAssetCategory.Ship;
        LoadModel(shipId, raceId);
    }

    /// <summary>Set which station definition is being edited.</summary>
    public void LoadStation(string stationId, string? raceId = null)
    {
        _category = DesignerAssetCategory.Station;
        LoadModel(stationId, raceId);
    }

    /// <summary>Cycle to the next playable race and keep the current model slot.</summary>
    public void CycleRace(int delta = 1) => SelectRace(_raceIndex + delta);

    /// <summary>Cycle to the next hull or station in the roster.</summary>
    public void CycleModel(int delta = 1) => SelectModel(_modelIndex + delta);

    /// <summary>Switch between ship hulls and station buildings.</summary>
    public void ToggleCategory()
    {
        _category = _category == DesignerAssetCategory.Ship
            ? DesignerAssetCategory.Station
            : DesignerAssetCategory.Ship;
        _modelIndex = 0;
        _modelId = ActiveModelIds[0];
        RefreshSelectionLabels();
        SelectionChanged?.Invoke();
    }

    /// <summary>Rotate the model preview by <paramref name="degrees"/> (relative delta).</summary>
    public void Rotate(float degrees)
    {
        RotationDegrees = (RotationDegrees + degrees) % 360f;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string[] ActiveModelIds => _category == DesignerAssetCategory.Station
        ? FleetGalleryLayout.AllBaseIds
        : FleetGalleryLayout.AllShipIds;

    private void LoadModel(string modelId, string? raceId)
    {
        _modelId = modelId;
        if (!string.IsNullOrWhiteSpace(raceId))
            _raceId = raceId;

        _modelIndex = Array.IndexOf(ActiveModelIds, _modelId);
        if (_modelIndex < 0) _modelIndex = 0;

        _raceIndex = 0;
        for (int i = 0; i < RaceTextureIndex.AllRaceIds.Count; i++)
        {
            if (RaceTextureIndex.AllRaceIds[i].Equals(_raceId, StringComparison.OrdinalIgnoreCase))
            {
                _raceIndex = i;
                break;
            }
        }

        if (_modelIndex >= 0 && _modelIndex < ActiveModelIds.Length)
            _modelId = ActiveModelIds[_modelIndex];

        RefreshSelectionLabels();
    }

    private void SelectRace(int index)
    {
        int count = RaceTextureIndex.AllRaceIds.Count;
        if (count == 0) return;

        _raceIndex = ((index % count) + count) % count;
        _raceId = RaceTextureIndex.AllRaceIds[_raceIndex];
        RefreshSelectionLabels();
        SelectionChanged?.Invoke();
    }

    private void SelectModel(int index)
    {
        string[] models = ActiveModelIds;
        if (models.Length == 0) return;

        _modelIndex = ((index % models.Length) + models.Length) % models.Length;
        _modelId = models[_modelIndex];
        RefreshSelectionLabels();
        SelectionChanged?.Invoke();
    }

    private void RefreshSelectionLabels()
    {
        _raceLabel.Text = FormatDisplayName(_raceId);
        _modelLabel.Text = FormatDisplayName(_modelId);
        _categoryBtn.Label = _category == DesignerAssetCategory.Ship ? "Ships" : "Stations";
    }

    private static string FormatDisplayName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return string.Empty;

        string[] parts = id.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            ReadOnlySpan<char> part = parts[i];
            parts[i] = char.ToUpperInvariant(part[0]) + part[1..].ToString();
        }

        return string.Join(' ', parts);
    }

    private void BuildPickerRow(string prevName, string nextName, float y, Action<int> onCycle)
    {
        var prevBtn = new Button
        {
            Name = prevName,
            Label = "<",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, y),
            Size = new Vector2(40f, 36f),
            FontSize = 18f,
        };
        prevBtn.Clicked += () => onCycle(-1);
        _controlPanel.AddChild(prevBtn);

        var nextBtn = new Button
        {
            Name = nextName,
            Label = ">",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(300f, y),
            Size = new Vector2(40f, 36f),
            FontSize = 18f,
        };
        nextBtn.Clicked += () => onCycle(1);
        _controlPanel.AddChild(nextBtn);
    }

    private void BuildControls()
    {
        float y = 176f;
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
        confirmBtn.Clicked += () => DesignConfirmed?.Invoke(_modelId, PrimaryColor, AccentColor);
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