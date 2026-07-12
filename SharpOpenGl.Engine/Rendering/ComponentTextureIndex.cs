namespace SharpOpenGl.Engine.Rendering;

/// <summary>Procedural component surface textures (shader indices 0–2).</summary>
public static class ComponentTextureIndex
{
    public const int Engine = 0;
    public const int Weapon = 1;
    public const int ShieldGenerator = 2;

    public static int Resolve(string? name) => name?.ToLowerInvariant() switch
    {
        "engine" or "engines" or "engine_trail" => Engine,
        "weapon" or "weapons" or "projectile" => Weapon,
        "shield" or "shield_generator" or "shieldgenerator" => ShieldGenerator,
        _ => -1,
    };
}