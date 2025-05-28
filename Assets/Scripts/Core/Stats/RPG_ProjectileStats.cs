using UnityEngine;

[System.Serializable]
public class RPG_ProjectileStats
{
    [Tooltip("Projectile travel speed in units per second")] public float speed;
    [Tooltip("Lifetime of the projectile in seconds")] public float lifetime;
    [Tooltip("How many targets the projectile can pierce")] public int pierce;
    [Tooltip("How many times the projectile can bounce off surfaces")] public int bounce;
    [Tooltip("How many additional targets the projectile can chain to")] public int chain;

    public override string ToString()
    {
        return $"Speed:{speed} Lifetime:{lifetime}s Pierces:{pierce} " +
               $"Bounces:{bounce} Chains:{chain}";
    }
}
