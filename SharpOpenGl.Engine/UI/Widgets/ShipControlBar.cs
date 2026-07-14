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
    public const float ButtonGap = 10f;
    public const float PanelPadding = 14f;
    public const int GridColumns = 3;

    private readonly Button _moveButton;
    private readonly Button _stopButton;
    private readonly Button _patrolButton;
    private readonly Button _attackButton;
    private readonly Button _attackMoveButton;
    private readonly Button _stanceButton;
    private readonly Button _formationButton;
    private readonly Button _buildButton;
    private readonly Button _harvestButton;

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
        Size = new Vector2(406f, 288f);
        Position = new Vector2(-12f, -12f);

        _moveButton = CreateCommandButton("Move", "Move (M)", 0, 0);
        _stopButton = CreateCommandButton("Stop", "Stop (S)", 1, 0);
        _patrolButton = CreateCommandButton("Ptrl", "Patrol (P)", 2, 0);
        _attackButton = CreateCommandButton("Atk", "Attack (T)", 0, 1);
        _attackMoveButton = CreateCommandButton("A-Mv", "Attack Move (A)", 1, 1);
        _stanceButton = CreateCommandButton("Stnc", "Cycle Stance", 2, 1);
        _formationButton = CreateCommandButton("Line", "Formation: Line", 0, 2);
        _buildButton = CreateCommandButton("Build", "Build (B)", 1, 2);
        _harvestButton = CreateCommandButton("Hvst", "Harvest (H)", 2, 2);

        _moveButton.Clicked += () => SetActiveCommand("move");
        _stopButton.Clicked += () => SetActiveCommand("stop");
        _patrolButton.Clicked += () => SetActiveCommand("patrol");
        _attackButton.Clicked += () => SetActiveCommand("attack");
        _attackMoveButton.Clicked += () => SetActiveCommand("attack_move");
        _stanceButton.Clicked += () => StanceToggled?.Invoke();
        _formationButton.Clicked += () => FormationCycled?.Invoke();
        _buildButton.Clicked += () => SetActiveCommand("build");
        _harvestButton.Clicked += () => SetActiveCommand("harvest");

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
            (_stanceButton.Label, _stanceButton.TooltipHint) = stance.Value switch
            {
                Stance.Neutral => ("[P]", "Stance: Passive"),
                Stance.Defensive => ("[D]", "Stance: Defensive"),
                Stance.Aggressive => ("[A]", "Stance: Aggressive"),
                _ => ("[?]", "Cycle Stance"),
            };
        }
        else
        {
            _stanceButton.Label = "Stnc";
            _stanceButton.TooltipHint = "Cycle Stance";
        }

        if (formation.HasValue)
        {
            string formationLabel = FormationLayout.GetLabel(formation.Value);
            _formationButton.Label = formationLabel;
            _formationButton.TooltipHint = $"Formation: {formationLabel}";
        }
        else if (showFormation)
        {
            _formationButton.Label = "Line";
            _formationButton.TooltipHint = "Formation: Line";
        }
    }

    /// <summary>Handle keyboard shortcuts for commands.</summary>
    public bool HandleKeyShortcut(char key)
    {
        switch (char.ToLowerInvariant(key))
        {
            case 'm': SetActiveCommand("move"); return true;
            case 's': SetActiveCommand("stop"); return true;
            case 'p': SetActiveCommand("patrol"); return true;
            case 't': SetActiveCommand("attack"); return true;
            case 'a': SetActiveCommand("attack_move"); return true;
            case 'h':
                if (!_harvestButton.Visible) return false;
                SetActiveCommand("harvest");
                return true;
            case 'b':
                if (!_buildButton.Visible) return false;
                SetActiveCommand("build");
                return true;
            case 'g': FormationCycled?.Invoke(); return true;
            default: return false;
        }
    }

    private void SetActiveCommand(string command)
    {
        ActiveCommand = command;
        CommandActivated?.Invoke(command);
        UpdateCommandHighlights(command);
    }

    /// <summary>Clear the active command highlight.</summary>
    public void ClearActiveCommand()
    {
        ActiveCommand = null;
        UpdateCommandHighlights(null);
    }

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, new Vector4(0.08f, 0.09f, 0.14f, 0.92f));
        renderer.DrawRectOutline(position, size, new Vector4(0.35f, 0.4f, 0.55f, 1f));
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

    private static void Highlight(Button button, bool active)
    {
        button.NormalColor = active
            ? new Vector4(0.3f, 0.5f, 0.3f, 1f)
            : new Vector4(0.2f, 0.2f, 0.3f, 1f);
    }

    private static Button CreateCommandButton(string label, string tooltip, int col, int row)
    {
        float x = PanelPadding + col * (ButtonWidth + ButtonGap);
        float y = PanelPadding + row * (ButtonHeight + ButtonGap);
        return new Button
        {
            Label = label,
            TooltipHint = tooltip,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(x, y),
            Size = new Vector2(ButtonWidth, ButtonHeight),
            FontSize = 20f,
        };
    }
}