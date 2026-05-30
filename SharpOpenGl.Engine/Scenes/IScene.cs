namespace SharpOpenGl.Engine.Scenes;

/// <summary>
/// Represents a self-contained game scene (menu, gameplay level, loading screen, etc.).
/// <see cref="SceneManager"/> calls <see cref="Load"/> when entering and
/// <see cref="Unload"/> when leaving the scene.
/// </summary>
public interface IScene
{
    /// <summary>Called once when transitioning into this scene.</summary>
    void Load();

    /// <summary>Called every frame while this scene is active.</summary>
    /// <param name="deltaTime">Elapsed time in seconds since the last frame.</param>
    void Update(float deltaTime);

    /// <summary>Called once when transitioning away from this scene.</summary>
    void Unload();
}
