using System.Globalization;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Parses user-supplied world seeds into deterministic integers for procedural generation.
/// </summary>
public static class ProceduralSeedHelper
{
    /// <summary>
    /// Seed used when <see cref="ParseSeed"/> receives null, empty, or whitespace-only input.
    /// </summary>
    public const int EmptyInputDefaultSeed = 42;

    /// <summary>
    /// Converts seed text to a stable procedural map seed.
    /// Numeric strings (invariant culture) are parsed directly; all other trimmed strings are hashed.
    /// </summary>
    /// <param name="input">User-entered seed text, or null/empty for the documented default.</param>
    /// <returns>A deterministic seed suitable for <c>MapGenerator</c> and economy scatter.</returns>
    public static int ParseSeed(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return EmptyInputDefaultSeed;

        string trimmed = input.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            return value;

        return HashString(trimmed);
    }

    /// <summary>
    /// Polynomial string hash used for non-numeric seed text (stable across runs and platforms).
    /// </summary>
    public static int HashString(string value)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in value)
                hash = hash * 31 + c;
            return hash;
        }
    }
}