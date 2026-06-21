namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// A runtime cache that maps string asset keys to GPU mesh handles.
/// Provides fallback resolution so a missing mesh gracefully degrades to a default.
/// </summary>
/// <remarks>
/// Register default meshes at startup before any lookup is attempted.
/// Keys use forward-slash separators (e.g. "meshes/hero_vanguard").
/// </remarks>
public sealed class MeshRegistry
{
    private readonly Dictionary<string, MeshEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Encapsulates a registered GPU mesh.</summary>
    public sealed class MeshEntry
    {
        /// <summary>Vertex array object handle.</summary>
        public int Vao { get; init; }

        /// <summary>Vertex buffer object handle.</summary>
        public int Vbo { get; init; }

        /// <summary>Number of vertices to draw.</summary>
        public int VertexCount { get; init; }
    }

    // ── Registration ─────────────────────────────────────────────────────────

    /// <summary>Register (or overwrite) a mesh entry by key.</summary>
    public void Register(string key, int vao, int vbo, int vertexCount)
    {
        _cache[key] = new MeshEntry { Vao = vao, Vbo = vbo, VertexCount = vertexCount };
    }

    /// <summary>
    /// Register a mesh from already-uploaded tuple data
    /// (e.g. from desktop mesh builders or procedural mesh upload).
    /// </summary>
    public void Register(string key, (int vao, int vbo, int vertexCount) mesh) =>
        Register(key, mesh.vao, mesh.vbo, mesh.vertexCount);

    // ── Lookup ────────────────────────────────────────────────────────────────

    /// <summary>Returns true if <paramref name="key"/> is registered.</summary>
    public bool Contains(string key) => _cache.ContainsKey(key);

    /// <summary>
    /// Try to retrieve the entry for <paramref name="key"/>.
    /// Returns false if not found.
    /// </summary>
    public bool TryGet(string key, out MeshEntry? entry) =>
        _cache.TryGetValue(key, out entry);

    /// <summary>
    /// Retrieve the entry for <paramref name="key"/>, or
    /// fall back to <paramref name="fallbackKey"/> if missing.
    /// Returns <c>null</c> if neither is registered.
    /// </summary>
    public MeshEntry? GetOrFallback(string key, string fallbackKey)
    {
        if (_cache.TryGetValue(key, out MeshEntry? entry))
            return entry;

        if (key != fallbackKey)
            Console.WriteLine($"[MeshRegistry] '{key}' not found — using fallback '{fallbackKey}'.");

        return _cache.GetValueOrDefault(fallbackKey);
    }

    /// <summary>Remove a single entry, freeing its slot (does not delete GPU resources).</summary>
    public void Unregister(string key) => _cache.Remove(key);

    /// <summary>Number of currently registered meshes.</summary>
    public int Count => _cache.Count;
}
