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

        _moveButton = CreateButton("Move", 0, 0);
        _stopButton = CreateButton("Stop", 1, 0);
        _patrolButton = CreateButton("Patrol", 2, 0);
        _attackButton = CreateButton("Attack", 0, 1);
        _attackMoveButton = CreateButton("A-Move", 1, 1);
        _stanceButton = CreateButton("Stance", 2, 1);
        _formationButton = CreateButton("Line", 0, 2);

        _moveButton.Clicked += () => SetActiveCommand("move");
        _stopButton.Clicked += () => SetActiveCommand("stop");
        _patrolButton.Clicked += () => SetActiveCommand("patrol");
        _attackButton.Clicked += () => SetActiveCommand("attack");
        _attackMoveButton.Clicked += () => SetActiveCommand("attack_move");
        _stanceButton.Clicked += () => StanceToggled?.Invoke();
        _formationButton.Clicked += () => FormationCycled?.Invoke();

        AddChild(_moveButton);
        AddChild(_stopButton);
        AddChild(_patrolButton);
        AddChild(_attackButton);
        AddChild(_attackMoveButton);
        AddChild(_stanceButton);
        AddChild(_formationButton);
    }

    /// <summary>Update button visibility based on ship capabilities.</summary>
    public void UpdateForShip(
        bool hasWeapons,
        bool hasMovement,
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

        if (stance.HasValue)
        {
            _stanceButton.Label = stance.Value switch
            {
                Stance.Neutral => "[P]",
                Stance.Defensive => "[D]",
                Stance.Aggressive => "[A]",
                _ => "[?]"
            };
        }

        if (formation.HasValue)
            _formationButton.Label = FormationLayout.GetLabel(formation.Value);
        else if (showFormation)
            _formationButton.Label = "Line";
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
    }

    private static void Highlight(Button button, bool active)
    {
        button.NormalColor = active
            ? new Vector4(0.3f, 0.5f, 0.3f, 1f)
            : new Vector4(0.2f, 0.2f, 0.3f, 1f);
    }

    private static Button CreateButton(string label, int col, int row)
    {
        float x = PanelPadding + col * (ButtonWidth + ButtonGap);
        float y = PanelPadding + row * (ButtonHeight + ButtonGap);
        return new Button
        {
            Label = label,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(x, y),
            Size = new Vector2(ButtonWidth, ButtonHeight),
            FontSize = 22f,
        };
    }
}