using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Config;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private void LoadBalanceConfig()
    {
        const string path = "GameData/Config/balance.json";
        var config = JsonLoader.Load<BalanceConfig>(path);
        if (config == null) return;

        if (config.Movement != null)
        {
            MovementBalance.Apply(config.Movement);
            Console.WriteLine(
                $"[Balance] Movement speed x{MovementBalance.SpeedMultiplier:0.##}, accel x{MovementBalance.AccelerationMultiplier:0.##}");
        }

        if (config.Combat != null)
        {
            CombatBalance.Apply(config.Combat);
            Console.WriteLine(
                $"[Balance] Weapon range x{CombatBalance.WeaponRangeMultiplier:0.##}, projectile scale x{CombatBalance.ProjectileScaleMultiplier:0.##}");
        }

        if (config.Visual != null)
        {
            VisualBalance.Apply(config.Visual);
            Console.WriteLine($"[Balance] Ship scale x{VisualBalance.ShipScaleMultiplier:0.##}");
        }
    }
}