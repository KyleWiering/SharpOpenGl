namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Logical game actions that can be mapped to any physical input.
/// Add new actions here; bindings are configured in GameData/Config/controls.json.
/// </summary>
public enum InputAction
{
    // ── Camera movement ────────────────────────────────────────────────────
    CameraMoveLeft,
    CameraMoveRight,
    CameraMoveForward,
    CameraMoveBack,
    CameraZoomIn,
    CameraZoomOut,
    CameraHeightUp,
    CameraHeightDown,
    CameraRotateLeft,
    CameraRotateRight,

    // ── Unit commands ──────────────────────────────────────────────────────
    Select,
    MultiSelect,
    MoveCommand,
    AttackCommand,
    StopCommand,
    PatrolCommand,
    UnitMove,
    UnitAttack,
    UnitAttackMove,
    UnitHoldPosition,
    UnitDefensiveStance,
    UnitHarvest,
    UnitFormationCycle,
    UnitBuildStructures,

    // ── Hero abilities ─────────────────────────────────────────────────────
    Ability1,
    Ability2,
    Ability3,
    Ability4,

    // ── UI / system ────────────────────────────────────────────────────────
    Pause,
    BuildMenu,
    TechMenu,
    Minimap,
    Confirm,
    Cancel,
}
