namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Abstract base class for all ECS game systems.
/// Subclasses override <see cref="Update"/> and optionally
/// <see cref="Initialize"/> / <see cref="Dispose"/>.
/// </summary>
public abstract class GameSystem : IDisposable
{
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Called once before the first <see cref="Update"/>.
    /// Override to set up resources that require the <see cref="World"/> to be populated.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// Called every frame while the system is active.
    /// </summary>
    /// <param name="world">The world containing all entities and components.</param>
    /// <param name="deltaTime">Elapsed time in seconds since the last update.</param>
    public abstract void Update(World world, float deltaTime);

    /// <summary>
    /// Called by <see cref="World"/> once per frame after <see cref="Update"/>.
    /// Ensures <see cref="Initialize"/> runs exactly once before the first update.
    /// </summary>
    internal void Tick(World world, float deltaTime)
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }
        Update(world, deltaTime);
    }

    /// <summary>Override to release managed resources.</summary>
    protected virtual void OnDispose() { }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OnDispose();
        GC.SuppressFinalize(this);
    }
}
