using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class PlayerColorPaletteTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    public void GetTint_returns_distinct_colors_for_each_slot(int playerId)
    {
        Vector3 tint = PlayerColorPalette.GetTint(playerId);
        Assert.True(tint.X > 0f && tint.Y > 0f && tint.Z > 0f);
    }

    [Fact]
    public void Eight_player_tints_are_pairwise_different()
    {
        var tints = Enumerable.Range(1, 8).Select(PlayerColorPalette.GetTint).ToArray();
        for (int i = 0; i < tints.Length; i++)
        for (int j = i + 1; j < tints.Length; j++)
            Assert.NotEqual(tints[i], tints[j]);
    }

    [Fact]
    public void GetTint_player_2_is_red_hostile_for_training_spawns()
    {
        Vector3 tint = PlayerColorPalette.GetTint(2);
        Assert.True(tint.X > tint.Y && tint.X > tint.Z);
        Assert.Equal(1.00f, tint.X, precision: 2);
        Assert.Equal(0.32f, tint.Y, precision: 2);
        Assert.Equal(0.28f, tint.Z, precision: 2);
    }

    [Fact]
    public void RaceTextureIndex_resolves_all_races()
    {
        foreach (string raceId in RaceTextureIndex.AllRaceIds)
            Assert.InRange(RaceTextureIndex.Resolve(raceId), 0, 7);
    }
}