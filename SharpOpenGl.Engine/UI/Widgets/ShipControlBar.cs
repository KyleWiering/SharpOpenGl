using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// A UI panel showing action buttons for the currently selected ship.
/// Displays Move, Stop, Patrol, Attack-Move, and stance buttons.
/// Buttons adapt based on ship capabilities.
/// </summary>
public sealed class ShipControlBar : Widget
{
    private readonly Button _moveButton;
    private readonly Button _stopButton;
    private readonly Button _patrolButton;
    private readonly Button _attackMoveButton;
    private readonly Button _stanceButton;

    /// <summary>Currently active command mode, or null if none.</summary>
    public string? ActiveCommand { get; private set; }

    /// <summary>Raised when a command button is clicked.</summary>
    public event Action<string>? CommandActivated;

    /// <summary>Raised when the stance button is toggled.</summary>
    public event Action? StanceToggled;

    public ShipControlBar()
    {
        Name = "ShipControlBar";
        Anchor = Anchor.BottomCenter;
        Size = new Vector2(400, 60);
        Position = new Vector2(0, -10);

        float buttonWidth = 70f;
        float buttonHeight = 40f;
        float spacing = 8f;
        float startX = spacing;

        _moveButton = CreateButton("Move", startX, buttonWidth, buttonHeight);
        _stopButton = CreateButton("Stop", startX + (buttonWidth + spacing), buttonWidth, buttonHeight);
        _patrolButton = CreateButton("Patrol", startX + 2 * (buttonWidth + spacing), buttonWidth, buttonHeight);
        _attackMoveButton = CreateButton("A-Move", startX + 3 * (buttonWidth + spacing), buttonWidth, buttonHeight);
        _stanceButton = CreateButton("Stance", startX + 4 * (buttonWidth + spacing), buttonWidth, buttonHeight);

        _moveButton.Clicked += () => SetActiveCommand("move");
        _stopButton.Clicked += () => SetActiveCommand("stop");
        _patrolButton.Clicked += () => SetActiveCommand("patrol");
        _attackMoveButton.Clicked += () => SetActiveCommand("attack_move");
        _stanceButton.Clicked += () => StanceToggled?.Invoke();

        AddChild(_moveButton);
        AddChild(_stopButton);
        AddChild(_patrolButton);
        AddChild(_attackMoveButton);
        AddChild(_stanceButton);
    }

    /// <summary>
    /// Update button visibility based on ship capabilities.
    /// </summary>
    /// <param name="hasWeapons">Whether the ship has weapons.</param>
    /// <param name="hasMovement">Whether the ship can move.</param>
    /// <param name="stance">Current stance to display.</param>
    public void UpdateForShip(bool hasWeapons, bool hasMovement, Stance? stance)
    {
        _moveButton.Visible = hasMovement;
        _stopButton.Visible = hasMovement;
        _patrolButton.Visible = hasMovement;
        _attackMoveButton.Visible = hasWeapons && hasMovement;
        _stanceButton.Visible = hasWeapons;

        if (stance.HasValue)
        {
            _stanceButton.Label = stance.Value switch
            {
                Stance.Neutral => "[N]",
                Stance.Defensive => "[D]",
                Stance.Aggressive => "[A]",
                _ => "[?]"
            };
        }
    }

    /// <summary>
    /// Handle keyboard shortcuts for commands.
    /// </summary>
    /// <param name="key">Key character pressed.</param>
    /// <returns>True if the key was consumed.</returns>
    public bool HandleKeyShortcut(char key)
    {
        switch (char.ToLowerInvariant(key))
        {
            case 'm': SetActiveCommand("move"); return true;
            case 's': SetActiveCommand("stop"); return true;
            case 'p': SetActiveCommand("patrol"); return true;
            case 'a': SetActiveCommand("attack_move"); return true;
            default: return false;
        }
    }

    private void SetActiveCommand(string command)
    {
        ActiveCommand = command;
        CommandActivated?.Invoke(command);

        // Highlight active button
        _moveButton.NormalColor = command == "move"
            ? new Vector4(0.3f, 0.5f, 0.3f, 1f)
            : new Vector4(0.2f, 0.2f, 0.3f, 1f);
        _patrolButton.NormalColor = command == "patrol"
            ? new Vector4(0.3f, 0.5f, 0.3f, 1f)
            : new Vector4(0.2f, 0.2f, 0.3f, 1f);
        _attackMoveButton.NormalColor = command == "attack_move"
            ? new Vector4(0.3f, 0.5f, 0.3f, 1f)
            : new Vector4(0.2f, 0.2f, 0.3f, 1f);
    }

    /// <summary>Clear the active command highlight.</summary>
    public void ClearActiveCommand()
    {
        ActiveCommand = null;
        _moveButton.NormalColor = new Vector4(0.2f, 0.2f, 0.3f, 1f);
        _patrolButton.NormalColor = new Vector4(0.2f, 0.2f, 0.3f, 1f);
        _attackMoveButton.NormalColor = new Vector4(0.2f, 0.2f, 0.3f, 1f);
    }

    private static Button CreateButton(string label, float x, float width, float height)
    {
        return new Button
        {
            Label = label,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(x, 10f),
            Size = new Vector2(width, height),
            FontSize = 14f
        };
    }
}
