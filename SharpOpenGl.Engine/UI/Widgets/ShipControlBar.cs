using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Standard RTS command grid for selected ships: move, stop, patrol, attack, attack-move, stance, formation.
/// </summary>
public sealed class ShipControlBar : Widget
{
    public const float ButtonWidth = 120f;
    public const float ButtonHeight = 80f;
    public const float ButtonGap = 12f;
    public const float PanelPadding = 14f;
    public const int GridColumns = 3;

    /// <summary>Invisible hit padding added beyond each command button's visual bounds.</summary>
    public const float CommandHitPadding = 4f;

    /// <summary>Minimum touch target extent for build/harvest priority buttons.</summary>
    public const float MinimumCommandHitExtent = IconButton.MinimumHitExtent;

    private readonly IconButton _moveButton;
    private readonly IconButton _stopButton;
    private readonly IconButton _patrolButton;
    private readonly IconButton _attackButton;
    private readonly IconButton _attackMoveButton;
    private readonly IconButton _stanceButton;
    private readonly IconButton _formationButton;
    private readonly IconButton _buildButton;
    private readonly IconButton _harvestButton;

    /// <summary>Currently active command mode, or null if none.</summary>
    public string? ActiveCommand { get; private set; }

    /// <summary>Raised when a command button is clicked.</summary>
    public event Action<string>? CommandActivated;

    /// <summary>Raised when the stance button is toggled.</summary>
    public event Action? StanceToggled;

    /// <summary>Raised when the formation button is clicked or G is pressed.</summary>
    public event Action? FormationCycled;

    public ShipControlBar()
    {
        Name = "ShipControlBar";
        Anchor = Anchor.BottomRight;
        Size = new Vector2(ComputePanelWidth(), ComputePanelHeight());
        Position = new Vector2(-12f, -12f);

        _moveButton = CreateCommandButton(MenuIconKind.Move, "Move", "Move (M)", 0, 0);
        _stopButton = CreateCommandButton(MenuIconKind.Stop, "Stop", "Stop (S)", 1, 0);
        _patrolButton = CreateCommandButton(MenuIconKind.Patrol, "Patrol", "Patrol (P)", 2, 0);
        _attackButton = CreateCommandButton(MenuIconKind.Attack, "Attack", "Attack (T)", 0, 1);
        _attackMoveButton = CreateCommandButton(MenuIconKind.AttackMove, "Attack Move", "Attack Move (A)", 1, 1);
        _stanceButton = CreateCommandButton(MenuIconKind.StanceDefensive, "Stance", "Cycle Stance", 2, 1);
        _formationButton = CreateCommandButton(MenuIconKind.FormationLine, "Formations", "Formation: Line", 0, 2);
        _buildButton = CreateCommandButton(
            MenuIconKind.Build, "Build", "Build structures (B)", 1, 2, requireMinimumHitExtent: true);
        _harvestButton = CreateCommandButton(MenuIconKind.Harvest, "Harvest", "Harvest (H)", 2, 2, requireMinimumHitExtent: true);

        _moveButton.Clicked += () => ActivateCommand("move");
        _stopButton.Clicked += () => ActivateCommand("stop");
        _patrolButton.Clicked += () => ActivateCommand("patrol");
        _attackButton.Clicked += () => ActivateCommand("attack");
        _attackMoveButton.Clicked += () => ActivateCommand("attack_move");
        _stanceButton.Clicked += () => StanceToggled?.Invoke();
        _formationButton.Clicked += () => FormationCycled?.Invoke();
        _buildButton.Clicked += () => ActivateCommand("build");
        _harvestButton.Clicked += () => ActivateCommand("harvest");

        AddChild(_moveButton);
        AddChild(_stopButton);
        AddChild(_patrolButton);
        AddChild(_attackButton);
        AddChild(_attackMoveButton);
        AddChild(_stanceButton);
        AddChild(_formationButton);
        AddChild(_buildButton);
        AddChild(_harvestButton);
    }

    /// <summary>Update button visibility based on ship capabilities.</summary>
    public void UpdateForShip(
        bool hasWeapons,
        bool hasMovement,
        bool hasResourceCollector,
        bool hasStructureBuilder,
        Stance? stance,
        FormationType? formation,
        bool showFormation)
    {
        _moveButton.Visible = hasMovement;
        _stopButton.Visible = hasMovement;
        _patrolButton.Visible = hasMovement;
        _attackButton.Visible = hasWeapons;
        _attackMoveButton.Visible = hasWeapons && hasMovement;
        _stanceButton.Visible = hasWeapons;
        _formationButton.Visible = showFormation;
        _buildButton.Visible = hasStructureBuilder;
        _harvestButton.Visible = hasResourceCollector;

        if (stance.HasValue)
        {
            (_stanceButton.Icon, _stanceButton.Label, _stanceButton.TooltipHint, _stanceButton.ActiveNormalColor) =
                GetStancePresentation(stance.Value);
            _stanceButton.IsActive = true;
        }
        else
        {
            _stanceButton.Icon = MenuIconKind.StanceDefensive;
            _stanceButton.Label = "Stance";
            _stanceButton.TooltipHint = "Cycle Stance";
            _stanceButton.ActiveNormalColor = new Vector4(0.3f, 0.5f, 0.3f, 1f);
            _stanceButton.IsActive = false;
        }

        if (formation.HasValue)
        {
            string formationDetail = FormationLayout.GetLabel(formation.Value);
            _formationButton.Icon = GetFormationIcon(formation.Value);
            _formationButton.Label = formationDetail;
            _formationButton.TooltipHint = $"Formation: {formationDetail} (G)";
            _formationButton.IsActive = true;
        }
        else if (showFormation)
        {
            _formationButton.Icon = MenuIconKind.FormationLine;
            _formationButton.Label = "Line";
            _formationButton.TooltipHint = "Formation: Line (G)";
            _formationButton.IsActive = false;
        }
        else
        {
            _formationButton.IsActive = false;
        }
    }

    /// <summary>Handle keyboard shortcuts for commands.</summary>
    public bool HandleKeyShortcut(char key)
    {
        switch (char.ToLowerInvariant(key))
        {
            case 'm': ActivateCommand("move"); return true;
            case 's': ActivateCommand("stop"); return true;
            case 'p': ActivateCommand("patrol"); return true;
            case 't': ActivateCommand("attack"); return true;
            case 'a': ActivateCommand("attack_move"); return true;
            case 'h':
                if (!_harvestButton.Visible) return false;
                ActivateCommand("harvest");
                return true;
            case 'b':
                if (!_buildButton.Visible) return false;
                ActivateCommand("build");
                return true;
            case 'g': FormationCycled?.Invoke(); return true;
            default: return false;
        }
    }

    /// <summary>Set the active command highlight without raising <see cref="CommandActivated"/>.</summary>
    public void SetActiveCommand(string? command)
    {
        ActiveCommand = command;
        UpdateCommandHighlights(command);
    }

    private void ActivateCommand(string command)
    {
        SetActiveCommand(command);
        CommandActivated?.Invoke(command);
    }

    /// <summary>Clear the active command highlight.</summary>
    public void ClearActiveCommand()
    {
        ActiveCommand = null;
        UpdateCommandHighlights(null);
    }

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, MenuTheme.PanelBackground);
        renderer.DrawRectOutline(position, size, MenuTheme.PanelBorder);
        base.OnDraw(renderer, position, size);
    }

    private void UpdateCommandHighlights(string? command)
    {
        Highlight(_moveButton, command == "move");
        Highlight(_patrolButton, command == "patrol");
        Highlight(_attackButton, command == "attack");
        Highlight(_attackMoveButton, command == "attack_move");
        Highlight(_buildButton, command == "build");
        Highlight(_harvestButton, command == "harvest");
    }

    private static void Highlight(IconButton button, bool active) =>
        button.IsActive = active;

    private static MenuIconKind GetFormationIcon(FormationType formation) => formation switch
    {
        FormationType.Line => MenuIconKind.FormationLine,
        FormationType.Wedge => MenuIconKind.FormationWedge,
        FormationType.Box => MenuIconKind.FormationBox,
        FormationType.Column => MenuIconKind.FormationColumn,
        _ => MenuIconKind.FormationLine,
    };

    private static (MenuIconKind Icon, string Label, string Tooltip, Vector4 ActiveColor) GetStancePresentation(
        Stance stance) => stance switch
    {
        Stance.Neutral => (
            MenuIconKind.StancePassive,
            "Hold",
            "Hold Position (H)",
            new Vector4(0.32f, 0.42f, 0.52f, 1f)),
        Stance.Defensive => (
            MenuIconKind.StanceDefensive,
            "Defensive",
            "Stance: Defensive (V)",
            new Vector4(0.22f, 0.38f, 0.62f, 1f)),
        Stance.Aggressive => (
            MenuIconKind.StanceAggressive,
            "Aggressive",
            "Stance: Aggressive",
            new Vector4(0.58f, 0.28f, 0.22f, 1f)),
        _ => (
            MenuIconKind.StanceDefensive,
            "Stance",
            "Cycle Stance",
            new Vector4(0.3f, 0.5f, 0.3f, 1f)),
    };

    /// <summary>Compute expanded hit bounds for a command button (visual size unchanged).</summary>
    public static (Vector2 Origin, Vector2 Size) GetExpandedHitRect(
        Vector2 visualPosition, Vector2 visualSize, bool requireMinimumExtent)
    {
        float hitW = visualSize.X + CommandHitPadding * 2f;
        float hitH = visualSize.Y + CommandHitPadding * 2f;
        if (requireMinimumExtent)
        {
            hitW = MathF.Max(hitW, MinimumCommandHitExtent);
            hitH = MathF.Max(hitH, MinimumCommandHitExtent);
        }

        float expandX = (hitW - visualSize.X) * 0.5f;
        float expandY = (hitH - visualSize.Y) * 0.5f;
        return (visualPosition - new Vector2(expandX, expandY), new Vector2(hitW, hitH));
    }

    private static float ComputePanelWidth() =>
        PanelPadding * 2f + GridColumns * ButtonWidth + (GridColumns - 1) * ButtonGap;

    private static float ComputePanelHeight() =>
        PanelPadding * 2f + GridColumns * ButtonHeight + (GridColumns - 1) * ButtonGap;

    private static IconButton CreateCommandButton(
        MenuIconKind icon, string label, string tooltip, int col, int row,
        bool requireMinimumHitExtent = false)
    {
        float x = PanelPadding + col * (ButtonWidth + ButtonGap);
        float y = PanelPadding + row * (ButtonHeight + ButtonGap);
        var button = new IconButton
        {
            Icon = icon,
            Label = label,
            TooltipHint = tooltip,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(x, y),
            Size = new Vector2(ButtonWidth, ButtonHeight),
            FontSize = 14f,
            IconSize = IconButton.DefaultIconSize,
            HitPadding = CommandHitPadding,
            RequireMinimumHitExtent = requireMinimumHitExtent,
        };
        IconButton.ApplyGameplayHudTheme(button);
        if (icon == MenuIconKind.Build)
            button.FontSize = 15f;
        return button;
    }
}