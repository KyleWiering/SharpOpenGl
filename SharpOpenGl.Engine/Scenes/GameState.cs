namespace SharpOpenGl.Engine.Scenes;

/// <summary>High-level game states that drive top-level scene and system activation.</summary>
public enum GameState
{
    /// <summary>Application is starting up or uninitialized.</summary>
    None,

    /// <summary>Main menu is active.</summary>
    MainMenu,

    /// <summary>Assets and level data are being loaded.</summary>
    Loading,

    /// <summary>Gameplay is running.</summary>
    Playing,

    /// <summary>Gameplay is paused (overlay visible).</summary>
    Paused,
}
