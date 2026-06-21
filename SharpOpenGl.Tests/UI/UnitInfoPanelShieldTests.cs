using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UnitInfoPanelShieldTests
{
    [Fact]
    public void UnitInfo_hides_shield_bar_color_when_no_shields()
    {
        RaceShieldSchema.ResetForTests();
        var health = new HealthComponent
        {
            MaxHP = 100f, CurrentHP = 100f,
            MaxShields = 0f, CurrentShields = 0f,
        };

        var info = UnitInfo.FromHealth("Fighter", health, EntityDisplayKind.Friendly, "terran");
        Assert.Equal(0f, info.MaxShields);
        Assert.Null(info.ShieldBarColor);
    }

    [Fact]
    public void UnitInfo_uses_race_tint_for_shielded_units()
    {
        RaceShieldSchema.ResetForTests();
        var health = new HealthComponent
        {
            MaxHP = 100f, CurrentHP = 100f,
            MaxShields = 50f, CurrentShields = 50f,
        };

        var solari = UnitInfo.FromHealth("Gunship", health, EntityDisplayKind.Friendly, "solari");
        Assert.NotNull(solari.ShieldBarColor);
        Assert.Equal(RaceShieldSchema.ResolveShieldTint("solari"), solari.ShieldBarColor!.Value);
    }

    [Fact]
    public void UnitInfoPanel_skips_shield_bar_when_max_shields_zero()
    {
        var panel = new UnitInfoPanel
        {
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Korath Raider",
                    MaxHP = 200f,
                    CurrentHP = 200f,
                    MaxShields = 0f,
                    HPFraction = 1f,
                },
            ],
        };

        Assert.Equal(0f, panel.SelectedUnits[0].MaxShields);
    }
}